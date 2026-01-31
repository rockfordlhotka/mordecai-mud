using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing character mana pools across magic schools
/// </summary>
public class ManaService : IManaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<ManaService> _logger;

    // Base mana before WIL and skill bonuses
    private const int BaseMana = 10;

    // Mana recovery skill names by school
    private static readonly Dictionary<MagicSchool, string> RecoverySkillNames = new()
    {
        { MagicSchool.Fire, "Fire Mana Recovery" },
        { MagicSchool.Healing, "Healing Mana Recovery" },
        { MagicSchool.Lightning, "Lightning Mana Recovery" },
        { MagicSchool.Illusion, "Illusion Mana Recovery" }
    };

    public ManaService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ILogger<ManaService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<CharacterManaPool> GetOrCreateManaPoolAsync(
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pool = await context.CharacterManaPools
            .FirstOrDefaultAsync(mp => mp.CharacterId == characterId && mp.School == school, cancellationToken);

        if (pool != null)
            return pool;

        // Create new pool
        var maxMana = await CalculateMaxManaInternalAsync(context, characterId, school, cancellationToken);

        pool = new CharacterManaPool
        {
            CharacterId = characterId,
            School = school,
            CurrentMana = maxMana, // Start full
            MaxMana = maxMana,
            LastRegenAt = DateTimeOffset.UtcNow
        };

        context.CharacterManaPools.Add(pool);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created {School} mana pool for character {CharacterId} with max {MaxMana}",
            school, characterId, maxMana);

        return pool;
    }

    public async Task<IReadOnlyList<CharacterManaPool>> GetManaPoolsAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.CharacterManaPools
            .AsNoTracking()
            .Where(mp => mp.CharacterId == characterId)
            .OrderBy(mp => mp.School)
            .ToListAsync(cancellationToken);
    }

    public async Task<CharacterManaSummary> GetManaSummaryAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pools = await context.CharacterManaPools
            .AsNoTracking()
            .Where(mp => mp.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        var summary = new CharacterManaSummary
        {
            CharacterId = characterId
        };

        foreach (var pool in pools)
        {
            var regenRate = await GetRegenRateInternalAsync(context, characterId, pool.School, cancellationToken);
            summary.Pools[pool.School] = new ManaPoolInfo
            {
                School = pool.School,
                CurrentMana = pool.CurrentMana,
                MaxMana = pool.MaxMana,
                RegenPerMinute = regenRate,
                IsGathering = pool.GatheringStartedAt.HasValue
            };
        }

        return summary;
    }

    public async Task<ManaOperationResult> ConsumeManaAsync(
        Guid characterId,
        MagicSchool school,
        int amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return new ManaOperationResult
            {
                Success = false,
                Message = "Amount must be positive",
                School = school
            };
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pool = await context.CharacterManaPools
            .FirstOrDefaultAsync(mp => mp.CharacterId == characterId && mp.School == school, cancellationToken);

        if (pool == null)
        {
            // Create pool if it doesn't exist
            pool = await CreatePoolInternalAsync(context, characterId, school, cancellationToken);
        }

        if (pool.CurrentMana < amount)
        {
            return new ManaOperationResult
            {
                Success = false,
                Message = $"Insufficient {school} mana (have {pool.CurrentMana}, need {amount})",
                School = school,
                PreviousMana = pool.CurrentMana,
                CurrentMana = pool.CurrentMana,
                MaxMana = pool.MaxMana,
                AmountChanged = 0
            };
        }

        var previousMana = pool.CurrentMana;
        pool.CurrentMana -= amount;

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Character {CharacterId} consumed {Amount} {School} mana ({Previous} -> {Current})",
            characterId, amount, school, previousMana, pool.CurrentMana);

        return new ManaOperationResult
        {
            Success = true,
            Message = $"Consumed {amount} {school} mana",
            School = school,
            PreviousMana = previousMana,
            CurrentMana = pool.CurrentMana,
            MaxMana = pool.MaxMana,
            AmountChanged = -amount
        };
    }

    public async Task<bool> HasEnoughManaAsync(
        Guid characterId,
        MagicSchool school,
        int amount,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pool = await context.CharacterManaPools
            .AsNoTracking()
            .FirstOrDefaultAsync(mp => mp.CharacterId == characterId && mp.School == school, cancellationToken);

        if (pool == null)
        {
            // No pool = 0 mana (pool will be created on first use)
            return amount <= 0;
        }

        return pool.CurrentMana >= amount;
    }

    public async Task<ManaOperationResult> AddManaAsync(
        Guid characterId,
        MagicSchool school,
        int amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return new ManaOperationResult
            {
                Success = false,
                Message = "Amount must be positive",
                School = school
            };
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pool = await context.CharacterManaPools
            .FirstOrDefaultAsync(mp => mp.CharacterId == characterId && mp.School == school, cancellationToken);

        if (pool == null)
        {
            pool = await CreatePoolInternalAsync(context, characterId, school, cancellationToken);
        }

        var previousMana = pool.CurrentMana;
        var actualAdded = Math.Min(amount, pool.MaxMana - pool.CurrentMana);
        pool.CurrentMana += actualAdded;

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Character {CharacterId} gained {Amount} {School} mana ({Previous} -> {Current})",
            characterId, actualAdded, school, previousMana, pool.CurrentMana);

        return new ManaOperationResult
        {
            Success = true,
            Message = actualAdded < amount
                ? $"Gained {actualAdded} {school} mana (capped at max)"
                : $"Gained {actualAdded} {school} mana",
            School = school,
            PreviousMana = previousMana,
            CurrentMana = pool.CurrentMana,
            MaxMana = pool.MaxMana,
            AmountChanged = actualAdded
        };
    }

    public async Task<int> ProcessRegenAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pools = await context.CharacterManaPools
            .Where(mp => mp.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        int totalRegen = 0;

        foreach (var pool in pools)
        {
            if (pool.CurrentMana >= pool.MaxMana)
            {
                pool.LastRegenAt = now;
                continue;
            }

            var minutesSinceLastRegen = (decimal)(now - pool.LastRegenAt).TotalMinutes;
            if (minutesSinceLastRegen < 0.1m) // Skip if less than 6 seconds
                continue;

            var regenRate = await GetRegenRateInternalAsync(context, characterId, pool.School, cancellationToken);
            var regenAmount = (int)(regenRate * minutesSinceLastRegen);

            if (regenAmount > 0)
            {
                var previousMana = pool.CurrentMana;
                pool.CurrentMana = Math.Min(pool.MaxMana, pool.CurrentMana + regenAmount);
                pool.LastRegenAt = now;
                totalRegen += pool.CurrentMana - previousMana;
            }
            else
            {
                // Update timestamp even if no regen to avoid accumulating too much
                pool.LastRegenAt = now;
            }
        }

        if (totalRegen > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Character {CharacterId} regenerated {Total} total mana", characterId, totalRegen);
        }

        return totalRegen;
    }

    public async Task<int> ProcessAllRegenAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Get all characters with mana pools that aren't full
        var characterIds = await context.CharacterManaPools
            .Where(mp => mp.CurrentMana < mp.MaxMana)
            .Select(mp => mp.CharacterId)
            .Distinct()
            .ToListAsync(cancellationToken);

        int processedCount = 0;
        foreach (var characterId in characterIds)
        {
            await ProcessRegenAsync(characterId, cancellationToken);
            processedCount++;
        }

        if (processedCount > 0)
        {
            _logger.LogDebug("Processed mana regen for {Count} characters", processedCount);
        }

        return processedCount;
    }

    public async Task<decimal> GetRegenRateAsync(
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await GetRegenRateInternalAsync(context, characterId, school, cancellationToken);
    }

    public async Task<int> CalculateMaxManaAsync(
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await CalculateMaxManaInternalAsync(context, characterId, school, cancellationToken);
    }

    public async Task UpdateMaxManaAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pools = await context.CharacterManaPools
            .Where(mp => mp.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        foreach (var pool in pools)
        {
            var newMax = await CalculateMaxManaInternalAsync(context, characterId, pool.School, cancellationToken);
            pool.MaxMana = newMax;

            // Cap current mana at new max
            if (pool.CurrentMana > newMax)
            {
                pool.CurrentMana = newMax;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated max mana for character {CharacterId}", characterId);
    }

    public async Task<int> GetCurrentManaAsync(
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pool = await context.CharacterManaPools
            .AsNoTracking()
            .FirstOrDefaultAsync(mp => mp.CharacterId == characterId && mp.School == school, cancellationToken);

        return pool?.CurrentMana ?? 0;
    }

    public async Task SetManaAsync(
        Guid characterId,
        MagicSchool school,
        int amount,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pool = await context.CharacterManaPools
            .FirstOrDefaultAsync(mp => mp.CharacterId == characterId && mp.School == school, cancellationToken);

        if (pool == null)
        {
            pool = await CreatePoolInternalAsync(context, characterId, school, cancellationToken);
        }

        pool.CurrentMana = Math.Max(0, Math.Min(amount, pool.MaxMana));
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Set {School} mana for character {CharacterId} to {Amount}",
            school, characterId, pool.CurrentMana);
    }

    public async Task RestoreAllManaAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pools = await context.CharacterManaPools
            .Where(mp => mp.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        foreach (var pool in pools)
        {
            pool.CurrentMana = pool.MaxMana;
            pool.LastRegenAt = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Restored all mana for character {CharacterId}", characterId);
    }

    // ==================
    // Internal Helpers
    // ==================

    private async Task<CharacterManaPool> CreatePoolInternalAsync(
        ApplicationDbContext context,
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken)
    {
        var maxMana = await CalculateMaxManaInternalAsync(context, characterId, school, cancellationToken);

        var pool = new CharacterManaPool
        {
            CharacterId = characterId,
            School = school,
            CurrentMana = maxMana, // Start full
            MaxMana = maxMana,
            LastRegenAt = DateTimeOffset.UtcNow
        };

        context.CharacterManaPools.Add(pool);
        await context.SaveChangesAsync(cancellationToken);

        return pool;
    }

    /// <summary>
    /// Calculates regen rate: School Recovery Skill + (Focus / 2) per minute
    /// Focus is the WIL equivalent attribute
    /// </summary>
    private async Task<decimal> GetRegenRateInternalAsync(
        ApplicationDbContext context,
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken)
    {
        // Get character's Focus (WIL equivalent)
        var character = await context.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character == null)
            return 0;

        var focus = character.Focus;

        // Get recovery skill level
        var recoverySkillName = RecoverySkillNames[school];
        var skillLevel = await GetSkillLevelAsync(context, characterId, recoverySkillName, cancellationToken);

        // Formula: Skill + (Focus / 2) per minute
        return skillLevel + (focus / 2m);
    }

    /// <summary>
    /// Calculates max mana: Base (10) + Focus + School Recovery Skill
    /// Focus is the WIL equivalent attribute
    /// </summary>
    private async Task<int> CalculateMaxManaInternalAsync(
        ApplicationDbContext context,
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken)
    {
        // Get character's Focus (WIL equivalent)
        var character = await context.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character == null)
            return BaseMana;

        var focus = character.Focus;

        // Get recovery skill level
        var recoverySkillName = RecoverySkillNames[school];
        var skillLevel = await GetSkillLevelAsync(context, characterId, recoverySkillName, cancellationToken);

        // Formula: Base + Focus + Skill
        return BaseMana + focus + skillLevel;
    }

    private async Task<int> GetSkillLevelAsync(
        ApplicationDbContext context,
        Guid characterId,
        string skillName,
        CancellationToken cancellationToken)
    {
        var skill = await context.CharacterSkills
            .AsNoTracking()
            .Include(cs => cs.SkillDefinition)
            .FirstOrDefaultAsync(cs =>
                cs.CharacterId == characterId &&
                cs.SkillDefinition.Name == skillName,
                cancellationToken);

        return skill?.Level ?? 0;
    }
}
