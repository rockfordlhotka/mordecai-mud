using System;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using WebSkillUsageType = Mordecai.Web.Data.SkillUsageType;

namespace Mordecai.Web.Services;

public interface ICharacterService
{
    Task<Character?> GetCharacterByIdAsync(Guid characterId, string userId);
    Task<int?> GetCharacterCurrentRoomAsync(Guid characterId, string userId);
    Task<bool> SetCharacterRoomAsync(Guid characterId, string userId, int roomId);
    Task<bool> CharacterExistsAsync(Guid characterId, string userId);
    Task<bool> DeleteCharacterAsync(Guid characterId, string userId);
    Task<bool> EnsureCharacterHasStartingSkillsAsync(Guid characterId, string userId);
    Task<CharacterHealthSnapshot?> GetCharacterHealthAsync(Guid characterId, string userId, CancellationToken cancellationToken = default);
    Task<CharacterHealthOperationResult> TryConsumeFatigueAsync(Guid characterId, string userId, int fatigueCost, string? exhaustedMessage = null, CancellationToken cancellationToken = default);
    Task<DriveCommandResult> TryPerformDriveConversionAsync(Guid characterId, string userId, CancellationToken cancellationToken = default);
}

public class CharacterService : ICharacterService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IWorldService _worldService;
    private readonly SkillService _skillService;
    private readonly IDiceService _diceService;
    private readonly ILogger<CharacterService> _logger;
    private int? _cachedFocusSkillDefinitionId;
    private int? _cachedDriveSkillDefinitionId;

    public CharacterService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IWorldService worldService,
        SkillService skillService,
        IDiceService diceService,
        ILogger<CharacterService> logger)
    {
        _contextFactory = contextFactory;
        _worldService = worldService;
        _skillService = skillService;
        _diceService = diceService;
        _logger = logger;
    }

    public async Task<Character?> GetCharacterByIdAsync(Guid characterId, string userId)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();
            return await context.Characters
                .Where(c => c.Id == characterId && c.UserId == userId)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character {CharacterId} for user {UserId}", characterId, userId);
            return null;
        }
    }

    public async Task<int?> GetCharacterCurrentRoomAsync(Guid characterId, string userId)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();
            var character = await context.Characters
                .FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId);

            if (character == null)
            {
                return null;
            }

            if (character.CurrentRoomId.HasValue)
            {
                return character.CurrentRoomId;
            }

            var startingRoom = await _worldService.GetStartingRoomAsync();
            if (startingRoom != null)
            {
                character.CurrentRoomId = startingRoom.Id;
                await context.SaveChangesAsync();
                return startingRoom.Id;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current room for character {CharacterId}", characterId);
            return null;
        }
    }

    public async Task<bool> SetCharacterRoomAsync(Guid characterId, string userId, int roomId)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();
            var character = await context.Characters
                .FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId);

            if (character == null)
            {
                return false;
            }

            character.CurrentRoomId = roomId;
            character.LastPlayedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogDebug("Character {CharacterId} room set to {RoomId}", characterId, roomId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting room {RoomId} for character {CharacterId}", roomId, characterId);
            return false;
        }
    }

    public async Task<bool> CharacterExistsAsync(Guid characterId, string userId)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();
            return await context.Characters
                .AnyAsync(c => c.Id == characterId && c.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if character {CharacterId} exists for user {UserId}", 
                characterId, userId);
            return false;
        }
    }

    public async Task<bool> EnsureCharacterHasStartingSkillsAsync(Guid characterId, string userId)
    {
        try
        {
            // Verify the character belongs to the user
            var character = await GetCharacterByIdAsync(characterId, userId);
            if (character == null)
            {
                _logger.LogWarning("Attempted to initialize skills for non-existent character {CharacterId} for user {UserId}", 
                    characterId, userId);
                return false;
            }

            // Check if character already has skills
            await using var context = _contextFactory.CreateDbContext();
            var existingSkillsCount = await context.CharacterSkills
                .Where(cs => cs.CharacterId == characterId)
                .CountAsync();

            if (existingSkillsCount > 0)
            {
                _logger.LogDebug("Character {CharacterId} already has {SkillCount} skills, skipping initialization", 
                    characterId, existingSkillsCount);
                return true; // Already has skills, nothing to do
            }

            // Character has no skills, initialize them
            _logger.LogInformation("Initializing starting skills for existing character {CharacterId} ({CharacterName})", 
                characterId, character.Name);
            
            await _skillService.InitializeCharacterSkillsAsync(characterId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring character {CharacterId} has starting skills", characterId);
            return false;
        }
    }

    public async Task<bool> DeleteCharacterAsync(Guid characterId, string userId)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();
            var character = await context.Characters
                .Where(c => c.Id == characterId && c.UserId == userId)
                .FirstOrDefaultAsync();

            if (character == null)
            {
                _logger.LogWarning("Attempted to delete non-existent character {CharacterId} for user {UserId}", 
                    characterId, userId);
                return false;
            }

            // Start a transaction to ensure all related data is deleted consistently
            await using var ctx = _contextFactory.CreateDbContext();
            using var transaction = await ctx.Database.BeginTransactionAsync();
            
            try
            {
                // Delete related character skills first (foreign key constraint)
                var characterSkills = await ctx.CharacterSkills
                    .Where(cs => cs.CharacterId == characterId)
                    .ToListAsync();
                
                if (characterSkills.Any())
                {
                    ctx.CharacterSkills.RemoveRange(characterSkills);
                    _logger.LogInformation("Deleting {Count} character skills for character {CharacterId}", 
                        characterSkills.Count, characterId);
                }

                // TODO: Add other related data deletion as the game expands:
                // - Character inventory items
                // - Character active effects
                // - Character quest progress
                // - Character social relationships
                // - Character achievements
                // - etc.

                // Delete the character itself
                ctx.Characters.Remove(character);
                
                // Save all changes
                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation("Successfully deleted character {CharacterName} ({CharacterId}) for user {UserId}", 
                    character.Name, characterId, userId);
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during character deletion transaction for {CharacterId}", characterId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting character {CharacterId} for user {UserId}", characterId, userId);
            return false;
        }
    }

    public async Task<CharacterHealthSnapshot?> GetCharacterHealthAsync(Guid characterId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var character = await context.Characters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId, cancellationToken);

            return character != null
                ? CreateHealthSnapshot(character)
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health snapshot for character {CharacterId}", characterId);
            return null;
        }
    }

    public async Task<CharacterHealthOperationResult> TryConsumeFatigueAsync(Guid characterId, string userId, int fatigueCost, string? exhaustedMessage = null, CancellationToken cancellationToken = default)
    {
        if (fatigueCost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fatigueCost), "Fatigue cost must be greater than zero.");
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var character = await context.Characters
                .FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId, cancellationToken);

            if (character == null)
            {
                return new CharacterHealthOperationResult(false, "Character not found.", null);
            }

            var pendingFatigueDamage = Math.Max(0, character.PendingFatigueDamage);
            var availableFatigue = character.CurrentFatigue - pendingFatigueDamage;

            if (availableFatigue <= 0)
            {
                var failureReason = exhaustedMessage ?? "You are completely exhausted and collapse.";
                _logger.LogInformation("Character {CharacterId} attempted to act at zero fatigue.", characterId);
                return new CharacterHealthOperationResult(false, failureReason, CreateHealthSnapshot(character));
            }

            if (FatigueEffectRules.TryGetFocusCheck(availableFatigue, out var focusCheck))
            {
                var focusOutcome = await EnsureLowFatigueFocusCheckAsync(context, character, focusCheck, availableFatigue, cancellationToken);

                if (!focusOutcome.Passed)
                {
                    var failureReason = focusOutcome.FailureReason
                                        ?? exhaustedMessage
                                        ?? "You are too exhausted to continue.";

                    _logger.LogInformation("Character {CharacterId} failed low fatigue Focus check: {Details}", characterId, focusOutcome.Details);
                    return new CharacterHealthOperationResult(false, failureReason, CreateHealthSnapshot(character));
                }

                _logger.LogDebug("Character {CharacterId} passed low fatigue Focus check: {Details}", characterId, focusOutcome.Details);
            }

            try
            {
                character.PendingFatigueDamage = checked(character.PendingFatigueDamage + fatigueCost);
            }
            catch (OverflowException)
            {
                character.PendingFatigueDamage = int.MaxValue;
            }
            character.LastPlayedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return new CharacterHealthOperationResult(true, null, CreateHealthSnapshot(character));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying fatigue cost for character {CharacterId}", characterId);
            return new CharacterHealthOperationResult(false, "An error occurred while applying fatigue.", null);
        }
    }

    public async Task<DriveCommandResult> TryPerformDriveConversionAsync(Guid characterId, string userId, CancellationToken cancellationToken = default)
    {
        const int driveTargetValue = 8;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var character = await context.Characters
                .FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId, cancellationToken);

            if (character == null)
            {
                return new DriveCommandResult(false, "Character not found.", driveTargetValue, 0, null, null, 0, 0, null);
            }

            var driveSkillId = await GetDriveSkillDefinitionIdAsync(context, cancellationToken);
            if (!driveSkillId.HasValue)
            {
                return new DriveCommandResult(false, "Drive skill definition is missing.", driveTargetValue, 0, null, null, 0, 0, CreateHealthSnapshot(character));
            }

            var driveSkill = await _skillService.GetCharacterSkillAsync(characterId, driveSkillId.Value);
            if (driveSkill == null)
            {
                return new DriveCommandResult(false, "You have not cultivated your Drive skill yet.", driveTargetValue, 0, null, null, 0, 0, CreateHealthSnapshot(character));
            }

            var abilityScore = driveSkill.CalculateAbilityScore(character);

            if (abilityScore < driveTargetValue)
            {
                await RecordDriveUsageAsync(characterId, driveSkillId.Value, WebSkillUsageType.TrainingPractice, abilityScore, 0, abilityScore, driveTargetValue, "Drive ability too low to activate.");
                return new DriveCommandResult(false, "Your drive falters; you need an ability score of 8 or higher to channel vitality.", driveTargetValue, abilityScore, null, null, 0, 0, CreateHealthSnapshot(character));
            }

            var diceRoll = _diceService.RollExploding4dF();
            var checkTotal = abilityScore + diceRoll;
            var succeeded = checkTotal >= driveTargetValue;
            var usageType = DetermineDriveUsageType(diceRoll, succeeded);

            if (!succeeded)
            {
                await RecordDriveUsageAsync(characterId, driveSkillId.Value, usageType, abilityScore, diceRoll, checkTotal, driveTargetValue, "Drive conversion failed.");
                return new DriveCommandResult(false, "You grit your teeth, but the strain refuses to convert your vitality.", driveTargetValue, abilityScore, diceRoll, checkTotal, 0, 0, CreateHealthSnapshot(character));
            }

            var healingAmount = abilityScore - driveTargetValue + 2;
            const int damageAmount = 1;

            character.PendingFatigueDamage = SafeAdd(character.PendingFatigueDamage, -healingAmount);
            character.PendingVitalityDamage = SafeAdd(character.PendingVitalityDamage, damageAmount);
            character.LastPlayedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            var snapshot = CreateHealthSnapshot(character);

            await RecordDriveUsageAsync(characterId, driveSkillId.Value, usageType, abilityScore, diceRoll, checkTotal, driveTargetValue, $"Converted {damageAmount} VIT into {healingAmount} FAT.");

            return new DriveCommandResult(true, $"You channel raw endurance, trading {damageAmount} VIT for {healingAmount} FAT recovery.", driveTargetValue, abilityScore, diceRoll, checkTotal, healingAmount, damageAmount, snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing Drive conversion for character {CharacterId}", characterId);
            return new DriveCommandResult(false, "An error interrupted your attempt to channel vitality.", driveTargetValue, 0, null, null, 0, 0, null);
        }
    }

    private async Task<FocusCheckOutcome> EnsureLowFatigueFocusCheckAsync(
        ApplicationDbContext context,
        Character character,
        LowFatigueFocusCheck focusCheck,
        int availableFatigue,
        CancellationToken cancellationToken)
    {
        var focusSkillId = await GetFocusSkillDefinitionIdAsync(context, cancellationToken);
        if (!focusSkillId.HasValue)
        {
            return FocusCheckSuccess("Focus skill definition missing; skipping check.");
        }

        var focusSkill = await context.CharacterSkills
            .Include(cs => cs.SkillDefinition)
            .FirstOrDefaultAsync(
                cs => cs.CharacterId == character.Id && cs.SkillDefinitionId == focusSkillId.Value,
                cancellationToken);

        if (focusSkill == null)
        {
            _logger.LogWarning("Character {CharacterId} is missing a Focus skill record. Allowing action.", character.Id);
            return FocusCheckSuccess("Focus skill record missing; skipping check.");
        }

        var abilityScore = focusSkill.CalculateAbilityScore(character);
        var diceRoll = _diceService.RollExploding4dF();
        var total = abilityScore + diceRoll;
        var succeeded = total >= focusCheck.TargetValue;
        var outcomeDetails =
            $"Focus AS {abilityScore} + {diceRoll:+0;-0;0} (4dF+) = {total} vs TV {focusCheck.TargetValue} (FAT {availableFatigue}).";

        var usageType = availableFatigue switch
        {
            2 or 1 => WebSkillUsageType.ChallengingUse,
            _ => WebSkillUsageType.RoutineUse
        };

        try
        {
            await _skillService.AddSkillUsageAsync(
                character.Id,
                focusSkillId.Value,
                usageType,
                baseUsagePoints: 1,
                context: "Low Fatigue Focus Check",
                details: $"{outcomeDetails} Outcome: {(succeeded ? "success" : "failure")}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record Focus skill usage for character {CharacterId}", character.Id);
        }

        return succeeded
            ? FocusCheckSuccess(outcomeDetails)
            : FocusCheckFailure(focusCheck.FailureMessage, outcomeDetails);
    }

    private async Task<int?> GetFocusSkillDefinitionIdAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (_cachedFocusSkillDefinitionId.HasValue)
        {
            return _cachedFocusSkillDefinitionId;
        }

        var focusSkillId = await context.SkillDefinitions
            .Where(sd => sd.Name == "Focus")
            .Select(sd => (int?)sd.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (focusSkillId.HasValue)
        {
            _cachedFocusSkillDefinitionId = focusSkillId.Value;
        }
        else
        {
            _logger.LogWarning("Focus skill definition not found when evaluating low fatigue.");
        }

        return focusSkillId;
    }

    private async Task<int?> GetDriveSkillDefinitionIdAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (_cachedDriveSkillDefinitionId.HasValue)
        {
            return _cachedDriveSkillDefinitionId;
        }

        var driveSkillId = await context.SkillDefinitions
            .Where(sd => sd.Name == "Drive")
            .Select(sd => (int?)sd.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (driveSkillId.HasValue)
        {
            _cachedDriveSkillDefinitionId = driveSkillId.Value;
        }
        else
        {
            _logger.LogWarning("Drive skill definition not found when executing Drive command.");
        }

        return driveSkillId;
    }

    private readonly record struct FocusCheckOutcome(bool Passed, string? FailureReason, string Details);

    private static FocusCheckOutcome FocusCheckSuccess(string details) => new(true, null, details);

    private static FocusCheckOutcome FocusCheckFailure(string failureReason, string details) => new(false, failureReason, details);

    private static CharacterHealthSnapshot CreateHealthSnapshot(Character character) => new(
        character.CurrentFatigue,
        character.MaxFatigue,
        character.PendingFatigueDamage,
        character.CurrentVitality,
        character.MaxVitality,
        character.PendingVitalityDamage);

    private static int SafeAdd(int current, int delta)
    {
        try
        {
            return checked(current + delta);
        }
        catch (OverflowException)
        {
            return delta > 0 ? int.MaxValue : int.MinValue;
        }
    }

    private static WebSkillUsageType DetermineDriveUsageType(int diceRoll, bool succeeded)
    {
        if (diceRoll >= 4)
        {
            return WebSkillUsageType.CriticalSuccess;
        }

        return succeeded ? WebSkillUsageType.RoutineUse : WebSkillUsageType.ChallengingUse;
    }

    private async Task RecordDriveUsageAsync(
        Guid characterId,
        int driveSkillId,
        WebSkillUsageType usageType,
        int abilityScore,
        int diceRoll,
        int checkTotal,
        int targetValue,
        string details)
    {
    var detailText = $"Drive check: AS {abilityScore} {diceRoll:+0;-0;0} (4dF+) = {checkTotal} vs TV {targetValue}. {details}";
        try
        {
            await _skillService.AddSkillUsageAsync(
                characterId,
                driveSkillId,
                usageType,
                baseUsagePoints: 1,
                context: "Drive Command",
                details: detailText);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record Drive skill usage for character {CharacterId}", characterId);
        }
    }
}