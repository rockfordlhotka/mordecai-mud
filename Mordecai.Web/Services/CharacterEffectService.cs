using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing character effects (wounds, buffs, debuffs, status effects)
/// </summary>
public sealed class CharacterEffectService : ICharacterEffectService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<CharacterEffectService> _logger;

    // Well-known effect names
    public const string WoundEffectName = "Wound";

    public CharacterEffectService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ILogger<CharacterEffectService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<EffectApplicationResult> ApplyEffectAsync(
        Guid characterId,
        int effectDefinitionId,
        Guid? sourceCharacterId = null,
        Guid? sourceNpcId = null,
        int? sourceSpellSkillId = null,
        int? durationSeconds = null,
        decimal? intensity = null,
        BodyLocation? bodyLocation = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var definition = await context.CharacterEffectDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == effectDefinitionId && d.IsActive, cancellationToken);

        if (definition == null)
        {
            _logger.LogWarning("Effect definition {EffectDefinitionId} not found or inactive", effectDefinitionId);
            return new EffectApplicationResult
            {
                Success = false,
                Message = "Effect definition not found"
            };
        }

        return await ApplyEffectInternalAsync(
            context, characterId, definition,
            sourceCharacterId, sourceNpcId, sourceSpellSkillId,
            durationSeconds, intensity, bodyLocation, cancellationToken);
    }

    public async Task<EffectApplicationResult> ApplyEffectByNameAsync(
        Guid characterId,
        string effectName,
        Guid? sourceCharacterId = null,
        Guid? sourceNpcId = null,
        int? sourceSpellSkillId = null,
        int? durationSeconds = null,
        decimal? intensity = null,
        BodyLocation? bodyLocation = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var definition = await context.CharacterEffectDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Name == effectName && d.IsActive, cancellationToken);

        if (definition == null)
        {
            _logger.LogWarning("Effect definition '{EffectName}' not found or inactive", effectName);
            return new EffectApplicationResult
            {
                Success = false,
                Message = $"Effect '{effectName}' not found"
            };
        }

        return await ApplyEffectInternalAsync(
            context, characterId, definition,
            sourceCharacterId, sourceNpcId, sourceSpellSkillId,
            durationSeconds, intensity, bodyLocation, cancellationToken);
    }

    public async Task<EffectApplicationResult> ApplyWoundAsync(
        Guid characterId,
        BodyLocation location = BodyLocation.General,
        Guid? sourceCharacterId = null,
        Guid? sourceNpcId = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplyEffectByNameAsync(
            characterId,
            WoundEffectName,
            sourceCharacterId,
            sourceNpcId,
            sourceSpellSkillId: null,
            durationSeconds: null, // Wounds are permanent until healed
            intensity: 1.0m,
            location,
            cancellationToken);
    }

    private async Task<EffectApplicationResult> ApplyEffectInternalAsync(
        ApplicationDbContext context,
        Guid characterId,
        CharacterEffectDefinition definition,
        Guid? sourceCharacterId,
        Guid? sourceNpcId,
        int? sourceSpellSkillId,
        int? durationSeconds,
        decimal? intensity,
        BodyLocation? bodyLocation,
        CancellationToken cancellationToken)
    {
        var effectIntensity = intensity ?? definition.DefaultIntensity;
        var effectDuration = durationSeconds ?? definition.DefaultDurationSeconds;

        // Check for existing effect if stackable
        if (definition.IsStackable)
        {
            var existingEffect = await context.CharacterEffects
                .FirstOrDefaultAsync(e =>
                    e.CharacterId == characterId &&
                    e.EffectDefinitionId == definition.Id &&
                    e.IsActive &&
                    (bodyLocation == null || e.BodyLocation == bodyLocation),
                    cancellationToken);

            if (existingEffect != null)
            {
                // Stack the effect
                if (existingEffect.CurrentStacks < definition.MaxStacks)
                {
                    existingEffect.CurrentStacks++;
                    existingEffect.AppliedAt = DateTimeOffset.UtcNow;

                    // Refresh duration if applicable
                    if (effectDuration > 0)
                    {
                        existingEffect.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(effectDuration);
                    }

                    await context.SaveChangesAsync(cancellationToken);

                    _logger.LogDebug(
                        "Stacked effect '{EffectName}' on character {CharacterId}, now at {Stacks} stacks",
                        definition.Name, characterId, existingEffect.CurrentStacks);

                    return new EffectApplicationResult
                    {
                        Success = true,
                        Effect = existingEffect,
                        WasStacked = true,
                        NewStackCount = existingEffect.CurrentStacks,
                        Message = $"{definition.Name} stacked ({existingEffect.CurrentStacks}/{definition.MaxStacks})"
                    };
                }
                else
                {
                    // At max stacks, just refresh duration
                    if (effectDuration > 0)
                    {
                        existingEffect.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(effectDuration);
                        existingEffect.AppliedAt = DateTimeOffset.UtcNow;
                        await context.SaveChangesAsync(cancellationToken);
                    }

                    return new EffectApplicationResult
                    {
                        Success = true,
                        Effect = existingEffect,
                        WasRefreshed = true,
                        NewStackCount = existingEffect.CurrentStacks,
                        Message = $"{definition.Name} refreshed (max stacks)"
                    };
                }
            }
        }
        else
        {
            // Non-stackable: check if already has effect
            var existingEffect = await context.CharacterEffects
                .FirstOrDefaultAsync(e =>
                    e.CharacterId == characterId &&
                    e.EffectDefinitionId == definition.Id &&
                    e.IsActive,
                    cancellationToken);

            if (existingEffect != null)
            {
                // Refresh the effect
                existingEffect.AppliedAt = DateTimeOffset.UtcNow;
                existingEffect.Intensity = effectIntensity;

                if (effectDuration > 0)
                {
                    existingEffect.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(effectDuration);
                }

                await context.SaveChangesAsync(cancellationToken);

                return new EffectApplicationResult
                {
                    Success = true,
                    Effect = existingEffect,
                    WasRefreshed = true,
                    Message = $"{definition.Name} refreshed"
                };
            }
        }

        // Create new effect
        var newEffect = new CharacterEffect
        {
            CharacterId = characterId,
            EffectDefinitionId = definition.Id,
            SourceCharacterId = sourceCharacterId,
            SourceNpcId = sourceNpcId,
            SourceSpellSkillId = sourceSpellSkillId,
            AppliedAt = DateTimeOffset.UtcNow,
            // duration = 0 means instant expiry, duration = null (default) means permanent
            // duration > 0 means expires in that many seconds
            ExpiresAt = durationSeconds switch
            {
                0 => DateTimeOffset.UtcNow, // Instant expiry
                > 0 => DateTimeOffset.UtcNow.AddSeconds(durationSeconds.Value),
                _ when definition.DefaultDurationSeconds > 0 => DateTimeOffset.UtcNow.AddSeconds(definition.DefaultDurationSeconds),
                _ => null // Permanent
            },
            CurrentStacks = 1,
            Intensity = effectIntensity,
            BodyLocation = bodyLocation,
            IsActive = true
        };

        context.CharacterEffects.Add(newEffect);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Applied effect '{EffectName}' to character {CharacterId}",
            definition.Name, characterId);

        return new EffectApplicationResult
        {
            Success = true,
            Effect = newEffect,
            NewStackCount = 1,
            Message = $"{definition.Name} applied"
        };
    }

    public async Task<bool> RemoveEffectAsync(int effectId, string reason = "removed", CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var effect = await context.CharacterEffects
            .FirstOrDefaultAsync(e => e.Id == effectId && e.IsActive, cancellationToken);

        if (effect == null)
            return false;

        effect.IsActive = false;
        effect.RemovedAt = DateTimeOffset.UtcNow;
        effect.RemovalReason = reason;

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Removed effect {EffectId} from character {CharacterId}: {Reason}",
            effectId, effect.CharacterId, reason);

        return true;
    }

    public async Task<int> RemoveEffectsByTypeAsync(
        Guid characterId,
        CharacterEffectType effectType,
        string reason = "removed",
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var effects = await context.CharacterEffects
            .Include(e => e.EffectDefinition)
            .Where(e => e.CharacterId == characterId &&
                       e.IsActive &&
                       e.EffectDefinition.EffectType == effectType)
            .ToListAsync(cancellationToken);

        foreach (var effect in effects)
        {
            effect.IsActive = false;
            effect.RemovedAt = DateTimeOffset.UtcNow;
            effect.RemovalReason = reason;
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Removed {Count} {EffectType} effects from character {CharacterId}",
            effects.Count, effectType, characterId);

        return effects.Count;
    }

    public async Task<int> HealWoundsAsync(
        Guid characterId,
        int count = 1,
        BodyLocation? location = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var woundQuery = context.CharacterEffects
            .Include(e => e.EffectDefinition)
            .Where(e => e.CharacterId == characterId &&
                       e.IsActive &&
                       e.EffectDefinition.EffectType == CharacterEffectType.Wound);

        if (location.HasValue)
        {
            woundQuery = woundQuery.Where(e => e.BodyLocation == location.Value);
        }

        var wounds = await woundQuery
            .OrderBy(e => e.AppliedAt) // Heal oldest wounds first
            .ToListAsync(cancellationToken);

        int healed = 0;
        int remaining = count == 0 ? int.MaxValue : count; // 0 = heal unlimited

        foreach (var wound in wounds)
        {
            if (remaining <= 0)
                break;

            int toHeal = Math.Min(wound.CurrentStacks, remaining);
            wound.CurrentStacks -= toHeal;
            healed += toHeal;
            remaining -= toHeal;

            if (wound.CurrentStacks <= 0)
            {
                wound.IsActive = false;
                wound.RemovedAt = DateTimeOffset.UtcNow;
                wound.RemovalReason = "healed";
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Healed {Count} wounds from character {CharacterId}", healed, characterId);

        return healed;
    }

    public async Task<IReadOnlyList<CharacterEffect>> GetActiveEffectsAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.CharacterEffects
            .AsNoTracking()
            .Include(e => e.EffectDefinition)
                .ThenInclude(d => d.Impacts)
            .Where(e => e.CharacterId == characterId && e.IsActive)
            .OrderBy(e => e.EffectDefinition.EffectType)
            .ThenBy(e => e.AppliedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CharacterEffect>> GetActiveEffectsByTypeAsync(
        Guid characterId,
        CharacterEffectType effectType,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.CharacterEffects
            .AsNoTracking()
            .Include(e => e.EffectDefinition)
                .ThenInclude(d => d.Impacts)
            .Where(e => e.CharacterId == characterId &&
                       e.IsActive &&
                       e.EffectDefinition.EffectType == effectType)
            .OrderBy(e => e.AppliedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasEffectAsync(
        Guid characterId,
        int effectDefinitionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.CharacterEffects
            .AsNoTracking()
            .AnyAsync(e => e.CharacterId == characterId &&
                          e.EffectDefinitionId == effectDefinitionId &&
                          e.IsActive,
                cancellationToken);
    }

    public async Task<bool> HasEffectByNameAsync(
        Guid characterId,
        string effectName,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.CharacterEffects
            .AsNoTracking()
            .Include(e => e.EffectDefinition)
            .AnyAsync(e => e.CharacterId == characterId &&
                          e.EffectDefinition.Name == effectName &&
                          e.IsActive,
                cancellationToken);
    }

    public async Task<CharacterEffectSummary> GetEffectSummaryAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var effects = await context.CharacterEffects
            .AsNoTracking()
            .Include(e => e.EffectDefinition)
                .ThenInclude(d => d.Impacts)
            .Where(e => e.CharacterId == characterId && e.IsActive)
            .ToListAsync(cancellationToken);

        var summary = new CharacterEffectSummary { CharacterId = characterId };

        foreach (var effect in effects)
        {
            var definition = effect.EffectDefinition;
            var stacks = effect.CurrentStacks;
            var intensity = effect.Intensity;

            summary.ActiveEffectNames.Add(stacks > 1 ? $"{definition.Name} x{stacks}" : definition.Name);

            // Track wounds
            if (definition.EffectType == CharacterEffectType.Wound)
            {
                summary.WoundCount += stacks;
                var loc = effect.BodyLocation ?? BodyLocation.General;
                summary.WoundsByLocation.TryAdd(loc, 0);
                summary.WoundsByLocation[loc] += stacks;

                // Wounds apply -2 AV per stack
                summary.TotalAttackValueModifier -= 2 * stacks;
            }

            // Process impacts
            foreach (var impact in definition.Impacts)
            {
                var modValue = impact.ModifierValue;
                if (impact.ScalesWithIntensity)
                {
                    modValue *= intensity;
                }
                modValue *= stacks;

                var intModValue = (int)Math.Round(modValue);

                switch (impact.ImpactType)
                {
                    case CharacterEffectImpactType.ModifyAttribute:
                        if (!string.IsNullOrEmpty(impact.TargetAttribute))
                        {
                            summary.AttributeModifiers.TryAdd(impact.TargetAttribute, 0);
                            summary.AttributeModifiers[impact.TargetAttribute] += intModValue;
                        }
                        break;

                    case CharacterEffectImpactType.ModifySkill:
                        if (impact.TargetSkillDefinitionId.HasValue)
                        {
                            summary.SkillModifiers.TryAdd(impact.TargetSkillDefinitionId.Value, 0);
                            summary.SkillModifiers[impact.TargetSkillDefinitionId.Value] += intModValue;
                        }
                        break;

                    case CharacterEffectImpactType.ModifyAttackValue:
                        summary.TotalAttackValueModifier += intModValue;
                        break;

                    case CharacterEffectImpactType.ModifyDefenseValue:
                        summary.TotalDefenseValueModifier += intModValue;
                        break;

                    case CharacterEffectImpactType.ModifyMaxFatigue:
                        summary.MaxFatigueModifier += intModValue;
                        break;

                    case CharacterEffectImpactType.ModifyMaxVitality:
                        summary.MaxVitalityModifier += intModValue;
                        break;

                    case CharacterEffectImpactType.ModifyDamageDealt:
                        summary.DamageDealtModifier += modValue;
                        break;

                    case CharacterEffectImpactType.ModifyDamageReceived:
                        summary.DamageReceivedModifier += modValue;
                        break;

                    case CharacterEffectImpactType.PreventMovement:
                        summary.CanMove = false;
                        break;

                    case CharacterEffectImpactType.PreventSpellcasting:
                        summary.CanCastSpells = false;
                        break;

                    case CharacterEffectImpactType.PreventActions:
                        summary.CanAct = false;
                        break;

                    case CharacterEffectImpactType.Invisibility:
                        summary.IsInvisible = true;
                        break;
                }
            }
        }

        return summary;
    }

    public async Task<int> GetWoundCountAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.CharacterEffects
            .AsNoTracking()
            .Include(e => e.EffectDefinition)
            .Where(e => e.CharacterId == characterId &&
                       e.IsActive &&
                       e.EffectDefinition.EffectType == CharacterEffectType.Wound)
            .SumAsync(e => e.CurrentStacks, cancellationToken);
    }

    public async Task<Dictionary<BodyLocation, int>> GetWoundsByLocationAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var wounds = await context.CharacterEffects
            .AsNoTracking()
            .Include(e => e.EffectDefinition)
            .Where(e => e.CharacterId == characterId &&
                       e.IsActive &&
                       e.EffectDefinition.EffectType == CharacterEffectType.Wound)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<BodyLocation, int>();
        foreach (var wound in wounds)
        {
            var loc = wound.BodyLocation ?? BodyLocation.General;
            result.TryAdd(loc, 0);
            result[loc] += wound.CurrentStacks;
        }

        return result;
    }

    public async Task<IReadOnlyList<string>> ProcessPeriodicEffectsAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var effects = await context.CharacterEffects
            .Include(e => e.EffectDefinition)
                .ThenInclude(d => d.Impacts)
            .Where(e => e.CharacterId == characterId &&
                       e.IsActive &&
                       e.EffectDefinition.TickIntervalSeconds > 0)
            .ToListAsync(cancellationToken);

        var character = await context.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character == null)
            return messages;

        foreach (var effect in effects)
        {
            var lastTick = effect.LastTickAt ?? effect.AppliedAt;
            var intervalSeconds = effect.EffectDefinition.TickIntervalSeconds;

            // Check if enough time has passed for a tick
            if ((now - lastTick).TotalSeconds < intervalSeconds)
                continue;

            // Calculate number of ticks that should have occurred
            var ticksPending = (int)((now - lastTick).TotalSeconds / intervalSeconds);

            foreach (var impact in effect.EffectDefinition.Impacts)
            {
                var damagePerTick = (int)Math.Round(impact.ModifierValue * effect.Intensity * effect.CurrentStacks);

                switch (impact.ImpactType)
                {
                    case CharacterEffectImpactType.PeriodicFatigueDamage:
                        var fatDamage = damagePerTick * ticksPending;
                        character.CurrentFatigue = Math.Max(0, character.CurrentFatigue - fatDamage);
                        messages.Add($"{effect.EffectDefinition.Name} deals {fatDamage} fatigue damage");
                        break;

                    case CharacterEffectImpactType.PeriodicVitalityDamage:
                        var vitDamage = damagePerTick * ticksPending;
                        character.CurrentVitality = Math.Max(0, character.CurrentVitality - vitDamage);
                        messages.Add($"{effect.EffectDefinition.Name} deals {vitDamage} vitality damage");
                        break;

                    case CharacterEffectImpactType.PeriodicFatigueHealing:
                        var fatHeal = damagePerTick * ticksPending;
                        character.CurrentFatigue = Math.Min(character.MaxFatigue, character.CurrentFatigue + fatHeal);
                        messages.Add($"{effect.EffectDefinition.Name} restores {fatHeal} fatigue");
                        break;

                    case CharacterEffectImpactType.PeriodicVitalityHealing:
                        var vitHeal = damagePerTick * ticksPending;
                        character.CurrentVitality = Math.Min(character.MaxVitality, character.CurrentVitality + vitHeal);
                        messages.Add($"{effect.EffectDefinition.Name} restores {vitHeal} vitality");
                        break;
                }
            }

            effect.LastTickAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);

        return messages;
    }

    public async Task<int> CleanupExpiredEffectsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var expiredEffects = await context.CharacterEffects
            .Where(e => e.IsActive && e.ExpiresAt != null && e.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var effect in expiredEffects)
        {
            effect.IsActive = false;
            effect.RemovedAt = now;
            effect.RemovalReason = "expired";
        }

        await context.SaveChangesAsync(cancellationToken);

        if (expiredEffects.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired effects", expiredEffects.Count);
        }

        return expiredEffects.Count;
    }

    public async Task<int> ProcessNaturalWoundHealingAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        // Natural healing: 1 wound per 4 hours
        const int HealingIntervalSeconds = 4 * 60 * 60; // 4 hours

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var wounds = await context.CharacterEffects
            .Include(e => e.EffectDefinition)
            .Where(e => e.CharacterId == characterId &&
                       e.IsActive &&
                       e.EffectDefinition.EffectType == CharacterEffectType.Wound)
            .OrderBy(e => e.AppliedAt)
            .ToListAsync(cancellationToken);

        if (wounds.Count == 0)
            return 0;

        int healed = 0;
        foreach (var wound in wounds)
        {
            var timeSinceApplied = (now - wound.AppliedAt).TotalSeconds;
            var woundsToHeal = (int)(timeSinceApplied / HealingIntervalSeconds);

            if (woundsToHeal > 0)
            {
                var healAmount = Math.Min(woundsToHeal, wound.CurrentStacks);
                wound.CurrentStacks -= healAmount;
                healed += healAmount;

                // Reset the timer based on remaining stacks
                wound.AppliedAt = now;

                if (wound.CurrentStacks <= 0)
                {
                    wound.IsActive = false;
                    wound.RemovedAt = now;
                    wound.RemovalReason = "natural_healing";
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        if (healed > 0)
        {
            _logger.LogDebug("Naturally healed {Count} wounds for character {CharacterId}", healed, characterId);
        }

        return healed;
    }

    public async Task<CharacterEffectDefinition?> GetEffectDefinitionAsync(
        int effectDefinitionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.CharacterEffectDefinitions
            .AsNoTracking()
            .Include(d => d.Impacts)
            .FirstOrDefaultAsync(d => d.Id == effectDefinitionId, cancellationToken);
    }

    public async Task<CharacterEffectDefinition?> GetEffectDefinitionByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.CharacterEffectDefinitions
            .AsNoTracking()
            .Include(d => d.Impacts)
            .FirstOrDefaultAsync(d => d.Name == name && d.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<CharacterEffectDefinition>> GetAllEffectDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.CharacterEffectDefinitions
            .AsNoTracking()
            .Include(d => d.Impacts)
            .Where(d => d.IsActive)
            .OrderBy(d => d.EffectType)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }
}
