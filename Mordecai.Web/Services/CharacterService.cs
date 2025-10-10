using System;
using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using System.Threading;

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
}

public class CharacterService : ICharacterService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IWorldService _worldService;
    private readonly SkillService _skillService;
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IWorldService worldService,
        SkillService skillService,
        ILogger<CharacterService> logger)
    {
        _contextFactory = contextFactory;
        _worldService = worldService;
        _skillService = skillService;
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
                var failureReason = exhaustedMessage ?? "You are too exhausted to continue.";
                return new CharacterHealthOperationResult(false, failureReason, CreateHealthSnapshot(character));
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

    private static CharacterHealthSnapshot CreateHealthSnapshot(Character character) => new(
        character.CurrentFatigue,
        character.MaxFatigue,
        character.PendingFatigueDamage,
        character.CurrentVitality,
        character.MaxVitality,
        character.PendingVitalityDamage);
}