using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

public interface IDoorInteractionService
{
    Task<DoorActionResult> OpenAsync(Guid characterId, string userId, int roomId, string direction, CancellationToken ct = default);
    Task<DoorActionResult> CloseAsync(Guid characterId, string userId, int roomId, string direction, CancellationToken ct = default);
    Task<DoorActionResult> LockWithDeviceAsync(Guid characterId, string userId, int roomId, string direction, string deviceCode, CancellationToken ct = default);
    Task<DoorActionResult> UnlockWithDeviceAsync(Guid characterId, string userId, int roomId, string direction, string deviceCode, CancellationToken ct = default);
    Task<DoorActionResult> AttemptBreakLockAsync(Guid characterId, string userId, int roomId, string direction, CancellationToken ct = default);
}

public sealed record DoorActionResult(
    bool Success,
    string Message,
    RoomExit? Exit = null,
    int? AbilityScore = null,
    int? DiceRoll = null,
    int? Total = null,
    int? TargetValue = null)
{
    public static DoorActionResult Failure(string message, RoomExit? exit = null) => new(false, message, exit);

    public static DoorActionResult SuccessResult(string message, RoomExit? exit = null, int? ability = null, int? roll = null, int? total = null, int? target = null)
        => new(true, message, exit, ability, roll, total, target);

    public bool HasCheckDetails => AbilityScore.HasValue && DiceRoll.HasValue && Total.HasValue && TargetValue.HasValue;
}

public sealed class DoorInteractionService : IDoorInteractionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ISkillService _skillService;
    private readonly IDiceService _diceService;
    private readonly ILogger<DoorInteractionService> _logger;
    private readonly ConcurrentDictionary<string, int> _skillDefinitionIdCache = new(StringComparer.OrdinalIgnoreCase);

    private const int DefaultBreakTargetValue = 10;

    public DoorInteractionService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISkillService skillService,
        IDiceService diceService,
        ILogger<DoorInteractionService> logger)
    {
        _contextFactory = contextFactory;
        _skillService = skillService;
        _diceService = diceService;
        _logger = logger;
    }

    public async Task<DoorActionResult> OpenAsync(Guid characterId, string userId, int roomId, string direction, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

    var doorLookup = await LoadDoorAsync(context, characterId, userId, roomId, direction, ct, includeCharacter: true);
        if (!doorLookup.Success)
        {
            return doorLookup.Result;
        }

        var exit = doorLookup.Exit!;
        if (!exit.HasDoor)
        {
            return DoorActionResult.Failure("There is no door in that direction.", exit);
        }

        if (!exit.IsDoorClosed)
        {
            return DoorActionResult.SuccessResult($"The {exit.GetDoorDisplayName()} is already open.", exit);
        }

        if (exit.IsDoorLocked)
        {
            return DoorActionResult.Failure($"The {exit.GetDoorDisplayName()} is locked.", exit);
        }

        exit.DoorState = DoorState.Open;
        await MirrorDoorStateAsync(context, exit, ct);
        await context.SaveChangesAsync(ct);

        return DoorActionResult.SuccessResult($"You open the {exit.GetDoorDisplayName()} to the {direction}.", exit);
    }

    public async Task<DoorActionResult> CloseAsync(Guid characterId, string userId, int roomId, string direction, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

    var doorLookup = await LoadDoorAsync(context, characterId, userId, roomId, direction, ct, includeCharacter: true);
        if (!doorLookup.Success)
        {
            return doorLookup.Result;
        }

        var exit = doorLookup.Exit!;
        if (!exit.HasDoor)
        {
            return DoorActionResult.Failure("There is no door in that direction.", exit);
        }

        if (exit.IsDoorClosed)
        {
            return DoorActionResult.SuccessResult($"The {exit.GetDoorDisplayName()} is already closed.", exit);
        }

        exit.DoorState = DoorState.Closed;
        await MirrorDoorStateAsync(context, exit, ct);
        await context.SaveChangesAsync(ct);

        return DoorActionResult.SuccessResult($"You close the {exit.GetDoorDisplayName()} to the {direction}.", exit);
    }

    public async Task<DoorActionResult> LockWithDeviceAsync(Guid characterId, string userId, int roomId, string direction, string deviceCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(deviceCode))
        {
            return DoorActionResult.Failure("You must specify which device you are using to lock the door.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

    var doorLookup = await LoadDoorAsync(context, characterId, userId, roomId, direction, ct, includeCharacter: true);
        if (!doorLookup.Success)
        {
            return doorLookup.Result;
        }

        var exit = doorLookup.Exit!;
        if (!exit.HasDoor)
        {
            return DoorActionResult.Failure("There is no door in that direction.", exit);
        }

        if (!exit.IsDoorClosed)
        {
            return DoorActionResult.Failure($"You must close the {exit.GetDoorDisplayName()} before locking it.", exit);
        }

        if (exit.IsDoorLocked)
        {
            return DoorActionResult.Failure($"The {exit.GetDoorDisplayName()} is already locked.", exit);
        }

        if (exit.LockConfiguration == DoorLockType.Spell)
        {
            return DoorActionResult.Failure($"The {exit.GetDoorDisplayName()} is currently bound by magic.", exit);
        }

        if (string.IsNullOrWhiteSpace(exit.LockDeviceCode))
        {
            return DoorActionResult.Failure("This door is not configured with a locking device.", exit);
        }

        if (!string.Equals(exit.LockDeviceCode, deviceCode, StringComparison.OrdinalIgnoreCase))
        {
            return DoorActionResult.Failure("That device does not fit this lock.", exit);
        }

        exit.LockConfiguration = DoorLockType.Device;
        exit.IsLocked = true;
        await MirrorDoorStateAsync(context, exit, ct);
        await context.SaveChangesAsync(ct);

        return DoorActionResult.SuccessResult($"You lock the {exit.GetDoorDisplayName()} with the {deviceCode}.", exit);
    }

    public async Task<DoorActionResult> UnlockWithDeviceAsync(Guid characterId, string userId, int roomId, string direction, string deviceCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(deviceCode))
        {
            return DoorActionResult.Failure("You must specify which device you are using to unlock the door.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

    var doorLookup = await LoadDoorAsync(context, characterId, userId, roomId, direction, ct, includeCharacter: true);
        if (!doorLookup.Success)
        {
            return doorLookup.Result;
        }

        var exit = doorLookup.Exit!;
        if (!exit.HasDoor)
        {
            return DoorActionResult.Failure("There is no door in that direction.", exit);
        }

        if (!exit.IsDoorLocked || exit.LockConfiguration != DoorLockType.Device)
        {
            return DoorActionResult.Failure($"The {exit.GetDoorDisplayName()} is not locked with a device.", exit);
        }

        if (string.IsNullOrWhiteSpace(exit.LockDeviceCode))
        {
            return DoorActionResult.Failure("This door does not respond to that device.", exit);
        }

        if (!string.Equals(exit.LockDeviceCode, deviceCode, StringComparison.OrdinalIgnoreCase))
        {
            return DoorActionResult.Failure("That device does not fit this lock.", exit);
        }

        exit.IsLocked = false;
        await MirrorDoorStateAsync(context, exit, ct);
        await context.SaveChangesAsync(ct);

        return DoorActionResult.SuccessResult($"You unlock the {exit.GetDoorDisplayName()} with the {deviceCode}.", exit);
    }

    public async Task<DoorActionResult> AttemptBreakLockAsync(Guid characterId, string userId, int roomId, string direction, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var doorLookup = await LoadDoorAsync(context, characterId, userId, roomId, direction, ct, includeCharacter: true);
        if (!doorLookup.Success)
        {
            return doorLookup.Result;
        }

        var exit = doorLookup.Exit!;
        if (!exit.HasDoor)
        {
            return DoorActionResult.Failure("There is no door in that direction.", exit);
        }

        if (!exit.IsDoorLocked)
        {
            return DoorActionResult.Failure($"The {exit.GetDoorDisplayName()} is not locked.", exit);
        }

        var character = doorLookup.Character!;
        var abilityScore = character.Physicality;
        var roll = _diceService.Roll4dF();
        var total = abilityScore + roll;

        var targetValue = exit.GetPhysicalityTargetValue();
        if (targetValue <= 0)
        {
            targetValue = DefaultBreakTargetValue;
        }

        var didSucceed = total >= targetValue;
        await RecordPhysicalityUsageAsync(characterId, abilityScore, targetValue, total, didSucceed, ct);

        if (!didSucceed)
        {
            return DoorActionResult.Failure($"You slam into the {exit.GetDoorDisplayName()} but it holds fast.", exit)
                with { AbilityScore = abilityScore, DiceRoll = roll, Total = total, TargetValue = targetValue };
        }

        exit.DoorState = DoorState.Open;
        exit.IsLocked = false;
        if (exit.LockConfiguration == DoorLockType.Spell)
        {
            exit.ClearSpellLock();
        }
        else
        {
            exit.LockConfiguration = DoorLockType.None;
        }

        await MirrorDoorStateAsync(context, exit, ct);
        await context.SaveChangesAsync(ct);

        var successMessage = $"You smash the {exit.GetDoorDisplayName()} open in a shower of splinters.";
        return DoorActionResult.SuccessResult(successMessage, exit, abilityScore, roll, total, targetValue);
    }

    private async Task MirrorDoorStateAsync(ApplicationDbContext context, RoomExit sourceExit, CancellationToken ct)
    {
        var reciprocal = await context.RoomExits
            .AsTracking()
            .FirstOrDefaultAsync(
                e => e.FromRoomId == sourceExit.ToRoomId &&
                     e.ToRoomId == sourceExit.FromRoomId &&
                     e.IsActive,
                ct);

        if (reciprocal == null)
        {
            return;
        }

        reciprocal.DoorState = sourceExit.DoorState;
        reciprocal.LockConfiguration = sourceExit.LockConfiguration;
        reciprocal.IsLocked = sourceExit.IsLocked;
        reciprocal.LockDeviceCode = sourceExit.LockDeviceCode;
        reciprocal.PhysicalityTargetValue = sourceExit.PhysicalityTargetValue;
        reciprocal.SpellLockAppliedAt = sourceExit.SpellLockAppliedAt;
        reciprocal.SpellLockCasterId = sourceExit.SpellLockCasterId;
        reciprocal.SpellLockStrength = sourceExit.SpellLockStrength;
    }

    private async Task RecordPhysicalityUsageAsync(Guid characterId, int abilityScore, int targetValue, int total, bool success, CancellationToken ct)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var skillId = await GetSkillDefinitionIdAsync(context, "Physicality", ct);
            if (!skillId.HasValue)
            {
                return;
            }

        var usageType = success ? Mordecai.Web.Data.SkillUsageType.ChallengingUse : Mordecai.Web.Data.SkillUsageType.RoutineUse;
            var details = $"Door break attempt: AS {abilityScore}, roll {total - abilityScore:+0;-0;0}, total {total}, TV {targetValue}, success {success}";
            await _skillService.AddSkillUsageAsync(characterId, skillId.Value, usageType, 1, "Door Interaction", details);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record Physicality skill usage for character {CharacterId}", characterId);
        }
    }

    private async Task<int?> GetSkillDefinitionIdAsync(ApplicationDbContext context, string skillName, CancellationToken ct)
    {
        if (_skillDefinitionIdCache.TryGetValue(skillName, out var cachedId))
        {
            return cachedId;
        }

        var definition = await context.SkillDefinitions
            .AsNoTracking()
            .Where(sd => sd.Name == skillName)
            .Select(sd => new { sd.Id })
            .FirstOrDefaultAsync(ct);

        if (definition == null)
        {
            return null;
        }

        _skillDefinitionIdCache.TryAdd(skillName, definition.Id);
        return definition.Id;
    }

    private async Task<(bool Success, DoorActionResult Result, RoomExit? Exit, Character? Character)> LoadDoorAsync(
        ApplicationDbContext context,
        Guid characterId,
        string userId,
        int roomId,
        string direction,
        CancellationToken ct,
        bool includeCharacter = false)
    {
        if (string.IsNullOrWhiteSpace(direction))
        {
            return (false, DoorActionResult.Failure("Which direction?"), null, null);
        }

        var normalizedDirection = direction.Trim().ToLowerInvariant();

        var characterQuery = context.Characters
            .AsNoTracking()
            .Where(c => c.Id == characterId && c.UserId == userId);

        Character? character = null;
        if (includeCharacter)
        {
            character = await characterQuery.FirstOrDefaultAsync(ct);
            if (character == null)
            {
                return (false, DoorActionResult.Failure("Character not found."), null, null);
            }

            if (character.CurrentRoomId != roomId)
            {
                return (false, DoorActionResult.Failure("You are not in that room."), null, null);
            }
        }
        else
        {
            var exists = await characterQuery.AnyAsync(ct);
            if (!exists)
            {
                return (false, DoorActionResult.Failure("Character not found."), null, null);
            }
        }

        var exit = await context.RoomExits
            .Include(e => e.ToRoom)
            .AsTracking()
            .FirstOrDefaultAsync(
                e => e.FromRoomId == roomId &&
                     e.Direction.ToLower() == normalizedDirection &&
                     e.IsActive,
                ct);

        if (exit == null)
        {
            return (false, DoorActionResult.Failure("No exit exists in that direction."), null, character);
        }

        if (!exit.HasDoor)
        {
            return (true, DoorActionResult.SuccessResult("There is no door in that direction.", exit), exit, character);
        }

        return (true, DoorActionResult.SuccessResult(string.Empty, exit), exit, character);
    }
}
