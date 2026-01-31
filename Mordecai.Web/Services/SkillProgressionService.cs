using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mordecai.Web.Data;
using GameEntities = Mordecai.Game.Entities;

namespace Mordecai.Web.Services;

/// <summary>
/// Implementation of skill progression service with anti-abuse mechanics.
/// Uses IDbContextFactory for Blazor Server safety.
/// </summary>
public sealed class SkillProgressionService : ISkillProgressionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly SkillProgressionSettings _settings;
    private readonly ILogger<SkillProgressionService> _logger;

    public SkillProgressionService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IOptions<SkillProgressionSettings> settings,
        ILogger<SkillProgressionService> logger)
    {
        _contextFactory = contextFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public SkillProgressionSettings GetSettings() => _settings;

    public async Task<SkillProgressionResult> LogUsageAsync(
        Guid characterId,
        int skillDefinitionId,
        SkillUsageType usageType,
        int baseExperience = 1,
        string? targetId = null,
        int? targetDifficulty = null,
        bool actionSucceeded = true,
        string? context = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var result = new SkillProgressionResult
        {
            BaseExperience = baseExperience,
            ProgressionApplied = true
        };

        // Get character for FAT check
        var character = await dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character == null)
        {
            result.ProgressionApplied = false;
            result.BlockedReason = "Character not found";
            return result;
        }

        // Get skill definition
        var skillDef = await dbContext.SkillDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(sd => sd.Id == skillDefinitionId, cancellationToken);

        if (skillDef == null)
        {
            result.ProgressionApplied = false;
            result.BlockedReason = "Skill not found";
            return result;
        }

        // Get character skill
        var characterSkill = await dbContext.CharacterSkills
            .Include(cs => cs.SkillDefinition)
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == skillDefinitionId, cancellationToken);

        if (characterSkill == null)
        {
            // Auto-learn the skill if not known
            characterSkill = new CharacterSkill
            {
                CharacterId = characterId,
                SkillDefinitionId = skillDefinitionId,
                Experience = 0,
                Level = 0,
                LearnedAt = DateTimeOffset.UtcNow
            };
            dbContext.CharacterSkills.Add(characterSkill);
            // Need to load SkillDefinition for later calculations
            characterSkill.SkillDefinition = skillDef;
        }

        var now = DateTimeOffset.UtcNow;

        // 1. Check target cooldown
        if (!string.IsNullOrEmpty(targetId))
        {
            var cooldownSeconds = GetCooldownForSkillType(skillDef.SkillType);
            var isOnCooldown = await CheckAndUpdateTargetCooldownAsync(
                dbContext, characterId, skillDefinitionId, targetId, cooldownSeconds, now, cancellationToken);

            if (isOnCooldown)
            {
                result.ProgressionApplied = false;
                result.BlockedReason = "Target cooldown active";
                result.FeedbackMessage = "You've already learned from recent practice against this target.";
                return result;
            }
        }

        // 2. Calculate FAT multiplier
        result.FatigueMultiplier = CalculateFatigueMultiplier(character);
        if (result.FatigueMultiplier == 0)
        {
            result.ProgressionApplied = false;
            result.BlockedReason = "Character exhausted (FAT = 0)";
            result.FeedbackMessage = "You are too exhausted to learn effectively. Rest before training.";
            return result;
        }

        // 3. Calculate usage type multiplier
        result.UsageTypeMultiplier = SkillUsageLog.GetUsageMultiplier(usageType);

        // 4. Apply failed action penalty
        if (!actionSucceeded)
        {
            result.UsageTypeMultiplier *= _settings.FailedActionMultiplier;
        }

        // 5. Calculate hourly diminishing returns
        var hourlyUsage = await GetOrCreateHourlyTrackingAsync(
            dbContext, characterId, skillDefinitionId, now, cancellationToken);
        result.HourlyMultiplier = CalculateHourlyMultiplier(hourlyUsage.UsageCount);

        // 6. Calculate daily multiplier (fresh learning bonus / fatigue)
        var dailyUsage = await GetOrCreateDailyTrackingAsync(
            dbContext, characterId, skillDefinitionId, now, cancellationToken);
        result.DailyMultiplier = CalculateDailyMultiplier(dailyUsage.UsageCount);

        // 7. Calculate challenge multiplier
        if (targetDifficulty.HasValue)
        {
            int challengeDifferential = targetDifficulty.Value - characterSkill.Level;
            result.ChallengeMultiplier = CalculateChallengeMultiplier(challengeDifferential);
        }

        // Calculate total multiplier
        result.TotalMultiplier = result.UsageTypeMultiplier
            * result.HourlyMultiplier
            * result.DailyMultiplier
            * result.ChallengeMultiplier
            * result.FatigueMultiplier;

        // Calculate final experience
        result.FinalExperience = (int)Math.Ceiling(baseExperience * result.TotalMultiplier);

        // Update tracking counters
        hourlyUsage.UsageCount++;
        hourlyUsage.LastUpdatedAt = now;
        dailyUsage.UsageCount++;
        dailyUsage.LastUpdatedAt = now;

        // Apply experience if any
        if (result.FinalExperience > 0)
        {
            int levelBefore = characterSkill.Level;
            result.DidLevelUp = characterSkill.AddExperience(result.FinalExperience);
            
            if (result.DidLevelUp)
            {
                result.NewLevel = characterSkill.Level;
                _logger.LogInformation(
                    "Character {CharacterId} skill {SkillName} advanced from {OldLevel} to {NewLevel}",
                    characterId, skillDef.Name, levelBefore, characterSkill.Level);
            }

            // Log the usage
            var usageLog = new SkillUsageLog
            {
                CharacterId = characterId,
                SkillDefinitionId = skillDefinitionId,
                UsageType = usageType,
                BaseUsagePoints = baseExperience,
                UsageMultiplier = result.TotalMultiplier,
                FinalUsagePoints = result.FinalExperience,
                SkillLevelBefore = levelBefore,
                SkillLevelAfter = characterSkill.Level,
                DidAdvance = result.DidLevelUp,
                Context = context,
                Details = details,
                UsedAt = now
            };
            dbContext.SkillUsageLogs.Add(usageLog);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Generate feedback message
        result.FeedbackMessage = GenerateFeedbackMessage(result, hourlyUsage.UsageCount, dailyUsage.UsageCount);

        return result;
    }

    public async Task<int> GetHourlyUsageCountAsync(
        Guid characterId,
        int skillDefinitionId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var windowStart = GetHourWindowStart(DateTimeOffset.UtcNow);

        var tracking = await dbContext.Set<SkillUsageHourlyTracking>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.CharacterId == characterId &&
                t.SkillDefinitionId == skillDefinitionId &&
                t.WindowStartTime == windowStart,
                cancellationToken);

        return tracking?.UsageCount ?? 0;
    }

    public async Task<int> GetDailyUsageCountAsync(
        Guid characterId,
        int skillDefinitionId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var tracking = await dbContext.Set<SkillUsageDailyTracking>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.CharacterId == characterId &&
                t.SkillDefinitionId == skillDefinitionId &&
                t.TrackingDate == today,
                cancellationToken);

        return tracking?.UsageCount ?? 0;
    }

    public async Task<bool> IsTargetOnCooldownAsync(
        Guid characterId,
        int skillDefinitionId,
        string targetId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var skillDef = await dbContext.SkillDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(sd => sd.Id == skillDefinitionId, cancellationToken);

        if (skillDef == null) return false;

        var cooldownSeconds = GetCooldownForSkillType(skillDef.SkillType);
        var cooldownThreshold = DateTimeOffset.UtcNow.AddSeconds(-cooldownSeconds);

        var cooldown = await dbContext.Set<SkillUsageTargetCooldown>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.CharacterId == characterId &&
                c.SkillDefinitionId == skillDefinitionId &&
                c.TargetId == targetId,
                cancellationToken);

        return cooldown != null && cooldown.LastCountedAt > cooldownThreshold;
    }

    public async Task<decimal> CalculateEffectiveMultiplierAsync(
        Guid characterId,
        int skillDefinitionId,
        SkillUsageType usageType,
        string? targetId = null,
        int? targetDifficulty = null,
        bool actionSucceeded = true,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var character = await dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character == null) return 0;

        var characterSkill = await dbContext.CharacterSkills
            .AsNoTracking()
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == skillDefinitionId, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        // FAT multiplier
        decimal fatMult = CalculateFatigueMultiplier(character);
        if (fatMult == 0) return 0;

        // Usage type multiplier
        decimal usageMult = SkillUsageLog.GetUsageMultiplier(usageType);
        if (!actionSucceeded) usageMult *= _settings.FailedActionMultiplier;

        // Hourly multiplier
        var hourlyCount = await GetHourlyUsageCountAsync(characterId, skillDefinitionId, cancellationToken);
        decimal hourlyMult = CalculateHourlyMultiplier(hourlyCount);

        // Daily multiplier
        var dailyCount = await GetDailyUsageCountAsync(characterId, skillDefinitionId, cancellationToken);
        decimal dailyMult = CalculateDailyMultiplier(dailyCount);

        // Challenge multiplier
        decimal challengeMult = 1.0m;
        if (targetDifficulty.HasValue && characterSkill != null)
        {
            int diff = targetDifficulty.Value - characterSkill.Level;
            challengeMult = CalculateChallengeMultiplier(diff);
        }

        return usageMult * hourlyMult * dailyMult * challengeMult * fatMult;
    }

    public async Task CleanupOldTrackingDataAsync(
        int hourlyRetentionHours = 24,
        int dailyRetentionDays = 7,
        int cooldownRetentionHours = 24,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var hourlyThreshold = DateTimeOffset.UtcNow.AddHours(-hourlyRetentionHours);
        var dailyThreshold = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-dailyRetentionDays));
        var cooldownThreshold = DateTimeOffset.UtcNow.AddHours(-cooldownRetentionHours);

        // Clean hourly tracking
        var oldHourly = await dbContext.Set<SkillUsageHourlyTracking>()
            .Where(t => t.WindowStartTime < hourlyThreshold)
            .ToListAsync(cancellationToken);
        dbContext.RemoveRange(oldHourly);

        // Clean daily tracking
        var oldDaily = await dbContext.Set<SkillUsageDailyTracking>()
            .Where(t => t.TrackingDate < dailyThreshold)
            .ToListAsync(cancellationToken);
        dbContext.RemoveRange(oldDaily);

        // Clean cooldowns
        var oldCooldowns = await dbContext.Set<SkillUsageTargetCooldown>()
            .Where(c => c.LastCountedAt < cooldownThreshold)
            .ToListAsync(cancellationToken);
        dbContext.RemoveRange(oldCooldowns);

        var totalRemoved = oldHourly.Count + oldDaily.Count + oldCooldowns.Count;
        if (totalRemoved > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Cleaned up {Count} old skill tracking records (hourly: {Hourly}, daily: {Daily}, cooldowns: {Cooldowns})",
                totalRemoved, oldHourly.Count, oldDaily.Count, oldCooldowns.Count);
        }
    }

    #region Private Helper Methods

    private decimal CalculateFatigueMultiplier(GameEntities.Character character)
    {
        if (character.CurrentFatigue <= 0)
            return _settings.ZeroFatMultiplier;

        decimal fatPercent = (decimal)character.CurrentFatigue / character.MaxFatigue;
        if (fatPercent < _settings.LowFatThresholdPercent)
            return _settings.LowFatMultiplier;

        return 1.0m;
    }

    private decimal CalculateHourlyMultiplier(int usageCount)
    {
        if (usageCount < _settings.HourlyUsageThreshold1)
            return _settings.HourlyMultiplier1;
        if (usageCount < _settings.HourlyUsageThreshold2)
            return _settings.HourlyMultiplier2;
        if (usageCount < _settings.HourlyUsageThreshold3)
            return _settings.HourlyMultiplier3;
        return _settings.HourlyMultiplierBeyond;
    }

    private decimal CalculateDailyMultiplier(int usageCount)
    {
        if (usageCount < _settings.DailyFreshUsageThreshold)
            return _settings.DailyFreshMultiplier;
        if (usageCount < _settings.DailyFatigueThreshold)
            return 1.0m;
        return _settings.DailyFatigueMultiplier;
    }

    private decimal CalculateChallengeMultiplier(int challengeDifferential)
    {
        return challengeDifferential switch
        {
            <= -10 => _settings.ChallengeTrivialMultiplier,
            <= -5 => _settings.ChallengeEasyMultiplier,
            <= 4 => _settings.ChallengeAppropriateMultiplier,
            <= 9 => _settings.ChallengeDifficultMultiplier,
            _ => _settings.ChallengeOverwhelmingMultiplier
        };
    }

    private int GetCooldownForSkillType(string skillType)
    {
        return skillType.ToLowerInvariant() switch
        {
            "weaponskill" or "weapon" or "combat" => _settings.CombatTargetCooldownSeconds,
            "spellskill" or "spell" or "magic" => _settings.SpellTargetCooldownSeconds,
            "craftingskill" or "crafting" => _settings.CraftingRecipeCooldownSeconds,
            "socialskill" or "social" => _settings.SocialTargetCooldownSeconds,
            _ => _settings.CombatTargetCooldownSeconds // Default to combat cooldown
        };
    }

    private static DateTimeOffset GetHourWindowStart(DateTimeOffset time)
    {
        return new DateTimeOffset(
            time.Year, time.Month, time.Day, time.Hour, 0, 0, time.Offset);
    }

    private async Task<SkillUsageHourlyTracking> GetOrCreateHourlyTrackingAsync(
        ApplicationDbContext dbContext,
        Guid characterId,
        int skillDefinitionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var windowStart = GetHourWindowStart(now);

        var tracking = await dbContext.Set<SkillUsageHourlyTracking>()
            .FirstOrDefaultAsync(t =>
                t.CharacterId == characterId &&
                t.SkillDefinitionId == skillDefinitionId &&
                t.WindowStartTime == windowStart,
                cancellationToken);

        if (tracking == null)
        {
            tracking = new SkillUsageHourlyTracking
            {
                CharacterId = characterId,
                SkillDefinitionId = skillDefinitionId,
                WindowStartTime = windowStart,
                UsageCount = 0,
                LastUpdatedAt = now
            };
            dbContext.Set<SkillUsageHourlyTracking>().Add(tracking);
        }

        return tracking;
    }

    private async Task<SkillUsageDailyTracking> GetOrCreateDailyTrackingAsync(
        ApplicationDbContext dbContext,
        Guid characterId,
        int skillDefinitionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var tracking = await dbContext.Set<SkillUsageDailyTracking>()
            .FirstOrDefaultAsync(t =>
                t.CharacterId == characterId &&
                t.SkillDefinitionId == skillDefinitionId &&
                t.TrackingDate == today,
                cancellationToken);

        if (tracking == null)
        {
            tracking = new SkillUsageDailyTracking
            {
                CharacterId = characterId,
                SkillDefinitionId = skillDefinitionId,
                TrackingDate = today,
                UsageCount = 0,
                LastUpdatedAt = now
            };
            dbContext.Set<SkillUsageDailyTracking>().Add(tracking);
        }

        return tracking;
    }

    private async Task<bool> CheckAndUpdateTargetCooldownAsync(
        ApplicationDbContext dbContext,
        Guid characterId,
        int skillDefinitionId,
        string targetId,
        int cooldownSeconds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var cooldownThreshold = now.AddSeconds(-cooldownSeconds);

        var cooldown = await dbContext.Set<SkillUsageTargetCooldown>()
            .FirstOrDefaultAsync(c =>
                c.CharacterId == characterId &&
                c.SkillDefinitionId == skillDefinitionId &&
                c.TargetId == targetId,
                cancellationToken);

        if (cooldown != null)
        {
            if (cooldown.LastCountedAt > cooldownThreshold)
            {
                // Still on cooldown
                return true;
            }

            // Cooldown expired, update timestamp
            cooldown.LastCountedAt = now;
            return false;
        }

        // No cooldown record exists, create one
        cooldown = new SkillUsageTargetCooldown
        {
            CharacterId = characterId,
            SkillDefinitionId = skillDefinitionId,
            TargetId = targetId,
            LastCountedAt = now
        };
        dbContext.Set<SkillUsageTargetCooldown>().Add(cooldown);

        return false;
    }

    private string? GenerateFeedbackMessage(SkillProgressionResult result, int hourlyCount, int dailyCount)
    {
        // Priority order: most impactful messages first
        
        if (result.FatigueMultiplier < 1.0m && result.FatigueMultiplier > 0)
        {
            return "You're getting tired. Skill training is becoming less effective.";
        }

        if (result.HourlyMultiplier == 0)
        {
            return "Your body and mind need rest. Skill progression temporarily suspended.";
        }

        if (result.HourlyMultiplier == _settings.HourlyMultiplier3)
        {
            return "You've been training intensively. Skill gains are significantly reduced.";
        }

        if (result.HourlyMultiplier == _settings.HourlyMultiplier2)
        {
            return "Your practice is becoming less effective. Consider resting or varying your training.";
        }

        if (result.ChallengeMultiplier == _settings.ChallengeTrivialMultiplier)
        {
            return "This opponent is too weak to teach you much.";
        }

        if (result.ChallengeMultiplier == _settings.ChallengeDifficultMultiplier)
        {
            return "Fighting this challenging opponent is excellent training!";
        }

        if (result.ChallengeMultiplier == _settings.ChallengeOverwhelmingMultiplier)
        {
            return "This foe is beyond your current skill level. You're learning slowly through adversity.";
        }

        if (result.DailyMultiplier == _settings.DailyFreshMultiplier && dailyCount < 10)
        {
            return "Your mind is fresh! Skill training is highly effective today.";
        }

        if (result.DailyMultiplier == _settings.DailyFatigueMultiplier)
        {
            return "You've trained extensively today. Mental fatigue is reducing learning effectiveness.";
        }

        if (result.DidLevelUp)
        {
            return $"Your skill has improved to level {result.NewLevel}!";
        }

        return null;
    }

    #endregion
}
