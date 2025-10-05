using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using System.Text.Json;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing room effects - creating, applying, and removing effects from rooms
/// </summary>
public class RoomEffectService : IRoomEffectService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<RoomEffectService> _logger;

    public RoomEffectService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ILogger<RoomEffectService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<RoomEffect> ApplyEffectAsync(int roomId, int effectDefinitionId, string sourceType, string? sourceId = null, string? sourceName = null, Guid? casterCharacterId = null, decimal intensity = 1.0m, int? durationOverride = null, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var effectDefinition = await context.RoomEffectDefinitions
            .FirstOrDefaultAsync(red => red.Id == effectDefinitionId && red.IsActive, cancellationToken);

        if (effectDefinition == null)
        {
            throw new ArgumentException($"Room effect definition with ID {effectDefinitionId} not found or inactive.");
        }

        var room = await context.Rooms
            .FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive, cancellationToken);

        if (room == null)
        {
            throw new ArgumentException($"Room with ID {roomId} not found or inactive.");
        }

        // Check if this effect is stackable or if we need to replace/modify existing effects
        var existingEffects = await context.RoomEffects
            .Where(re => re.RoomId == roomId && re.RoomEffectDefinitionId == effectDefinitionId && re.IsActive)
            .ToListAsync(cancellationToken);

        RoomEffect roomEffect;

        if (effectDefinition.IsStackable && existingEffects.Count < effectDefinition.MaxStacks)
        {
            // Create a new stacked effect
            roomEffect = new RoomEffect
            {
                RoomId = roomId,
                RoomEffectDefinitionId = effectDefinitionId,
                SourceType = sourceType,
                SourceId = sourceId,
                SourceName = sourceName,
                CasterCharacterId = casterCharacterId,
                StackCount = 1,
                Intensity = intensity,
                StartTime = DateTimeOffset.UtcNow,
                EndTime = durationOverride.HasValue ? DateTimeOffset.UtcNow.AddSeconds(durationOverride.Value) :
                         effectDefinition.DefaultDuration > 0 ? DateTimeOffset.UtcNow.AddSeconds(effectDefinition.DefaultDuration) : null,
                IsActive = true
            };

            context.RoomEffects.Add(roomEffect);
        }
        else if (effectDefinition.IsStackable && existingEffects.Count >= effectDefinition.MaxStacks)
        {
            // Replace the oldest stack
            var oldestEffect = existingEffects.OrderBy(re => re.StartTime).First();
            oldestEffect.StartTime = DateTimeOffset.UtcNow;
            oldestEffect.EndTime = durationOverride.HasValue ? DateTimeOffset.UtcNow.AddSeconds(durationOverride.Value) :
                                  effectDefinition.DefaultDuration > 0 ? DateTimeOffset.UtcNow.AddSeconds(effectDefinition.DefaultDuration) : null;
            oldestEffect.Intensity = intensity;
            oldestEffect.SourceType = sourceType;
            oldestEffect.SourceId = sourceId;
            oldestEffect.SourceName = sourceName;
            oldestEffect.CasterCharacterId = casterCharacterId;

            roomEffect = oldestEffect;
        }
        else if (existingEffects.Any())
        {
            // Not stackable - refresh the existing effect
            var existingEffect = existingEffects.First();
            existingEffect.StartTime = DateTimeOffset.UtcNow;
            existingEffect.EndTime = durationOverride.HasValue ? DateTimeOffset.UtcNow.AddSeconds(durationOverride.Value) :
                                    effectDefinition.DefaultDuration > 0 ? DateTimeOffset.UtcNow.AddSeconds(effectDefinition.DefaultDuration) : null;
            existingEffect.Intensity = Math.Max(existingEffect.Intensity, intensity); // Use higher intensity
            existingEffect.SourceType = sourceType;
            existingEffect.SourceId = sourceId;
            existingEffect.SourceName = sourceName;
            existingEffect.CasterCharacterId = casterCharacterId;

            roomEffect = existingEffect;
        }
        else
        {
            // Create new effect
            roomEffect = new RoomEffect
            {
                RoomId = roomId,
                RoomEffectDefinitionId = effectDefinitionId,
                SourceType = sourceType,
                SourceId = sourceId,
                SourceName = sourceName,
                CasterCharacterId = casterCharacterId,
                StackCount = 1,
                Intensity = intensity,
                StartTime = DateTimeOffset.UtcNow,
                EndTime = durationOverride.HasValue ? DateTimeOffset.UtcNow.AddSeconds(durationOverride.Value) :
                         effectDefinition.DefaultDuration > 0 ? DateTimeOffset.UtcNow.AddSeconds(effectDefinition.DefaultDuration) : null,
                IsActive = true
            };

            context.RoomEffects.Add(roomEffect);
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Applied room effect {EffectName} to room {RoomId} with intensity {Intensity}", 
            effectDefinition.Name, roomId, intensity);

        return roomEffect;
    }

    public async Task RemoveEffectAsync(int roomEffectId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var roomEffect = await context.RoomEffects
            .FirstOrDefaultAsync(re => re.Id == roomEffectId, cancellationToken);

        if (roomEffect != null)
        {
            roomEffect.IsActive = false;
            roomEffect.EndTime = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed room effect {RoomEffectId}", roomEffectId);
        }
    }

    public async Task RemoveAllEffectsFromRoomAsync(int roomId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var activeEffects = await context.RoomEffects
            .Where(re => re.RoomId == roomId && re.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var effect in activeEffects)
        {
            effect.IsActive = false;
            effect.EndTime = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed all effects from room {RoomId} (count: {Count})", roomId, activeEffects.Count);
    }

    public async Task<IList<RoomEffect>> GetActiveEffectsInRoomAsync(int roomId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.RoomEffects
            .Include(re => re.RoomEffectDefinition)
            .ThenInclude(red => red.Impacts)
            .Where(re => re.RoomId == roomId && re.IsActive && (re.EndTime == null || re.EndTime > DateTimeOffset.UtcNow))
            .OrderBy(re => re.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<RoomEffect>> GetVisibleEffectsInRoomAsync(int roomId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.RoomEffects
            .Include(re => re.RoomEffectDefinition)
            .Where(re => re.RoomId == roomId && re.IsActive && re.RoomEffectDefinition.IsVisible && 
                        (re.EndTime == null || re.EndTime > DateTimeOffset.UtcNow))
            .OrderBy(re => re.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task ProcessPeriodicEffectsAsync(int roomId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var activeEffects = await context.RoomEffects
            .Include(re => re.RoomEffectDefinition)
            .ThenInclude(red => red.Impacts)
            .Where(re => re.RoomId == roomId && re.IsActive && 
                        re.RoomEffectDefinition.TickInterval > 0 &&
                        (re.EndTime == null || re.EndTime > DateTimeOffset.UtcNow))
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        foreach (var effect in activeEffects)
        {
            var lastTick = effect.LastTickTime ?? effect.StartTime;
            var secondsSinceLastTick = (now - lastTick).TotalSeconds;

            if (secondsSinceLastTick >= effect.RoomEffectDefinition.TickInterval)
            {
                // Get characters in the room
                var charactersInRoom = await context.Characters
                    .Where(c => c.CurrentRoomId == roomId)
                    .ToListAsync(cancellationToken);

                foreach (var character in charactersInRoom)
                {
                    await ApplyEffectImpactsToCharacterAsync(context, effect, character, "Periodic", cancellationToken);
                }

                effect.LastTickTime = now;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsMovementPreventedAsync(int roomId, Guid characterId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var preventionEffects = await context.RoomEffects
            .Include(re => re.RoomEffectDefinition)
            .ThenInclude(red => red.Impacts)
            .Where(re => re.RoomId == roomId && re.IsActive && 
                        (re.EndTime == null || re.EndTime > DateTimeOffset.UtcNow))
            .SelectMany(re => re.RoomEffectDefinition.Impacts)
            .Where(rei => rei.ImpactType == "MovementPrevention" && 
                         (rei.TargetType == "AllOccupants" || rei.TargetType == "ExitTrigger"))
            .AnyAsync(cancellationToken);

        return preventionEffects;
    }

    public async Task ApplyEntryEffectsAsync(int roomId, Guid characterId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var character = await context.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character == null) return;

        var entryEffects = await context.RoomEffects
            .Include(re => re.RoomEffectDefinition)
            .ThenInclude(red => red.Impacts)
            .Where(re => re.RoomId == roomId && re.IsActive && 
                        (re.EndTime == null || re.EndTime > DateTimeOffset.UtcNow))
            .Where(re => re.RoomEffectDefinition.Impacts.Any(rei => rei.TargetType == "EntryTrigger"))
            .ToListAsync(cancellationToken);

        foreach (var effect in entryEffects)
        {
            await ApplyEffectImpactsToCharacterAsync(context, effect, character, "Entry", cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyExitEffectsAsync(int roomId, Guid characterId, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var character = await context.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character == null) return;

        var exitEffects = await context.RoomEffects
            .Include(re => re.RoomEffectDefinition)
            .ThenInclude(red => red.Impacts)
            .Where(re => re.RoomId == roomId && re.IsActive && 
                        (re.EndTime == null || re.EndTime > DateTimeOffset.UtcNow))
            .Where(re => re.RoomEffectDefinition.Impacts.Any(rei => rei.TargetType == "ExitTrigger"))
            .ToListAsync(cancellationToken);

        foreach (var effect in exitEffects)
        {
            await ApplyEffectImpactsToCharacterAsync(context, effect, character, "Exit", cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupExpiredEffectsAsync(CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var expiredEffects = await context.RoomEffects
            .Where(re => re.IsActive && re.EndTime != null && re.EndTime <= DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var effect in expiredEffects)
        {
            effect.IsActive = false;
        }

        if (expiredEffects.Any())
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} expired room effects", expiredEffects.Count);
        }
    }

    public async Task<RoomEffectDefinition?> GetEffectDefinitionByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.RoomEffectDefinitions
            .Include(red => red.Impacts)
            .FirstOrDefaultAsync(red => red.Name == name && red.IsActive, cancellationToken);
    }

    public async Task<IList<RoomEffectDefinition>> GetAllEffectDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.RoomEffectDefinitions
            .Include(red => red.Impacts)
            .Where(red => red.IsActive)
            .OrderBy(red => red.EffectType)
            .ThenBy(red => red.Name)
            .ToListAsync(cancellationToken);
    }

    private Task ApplyEffectImpactsToCharacterAsync(ApplicationDbContext context, RoomEffect effect, Character character, string applicationType, CancellationToken cancellationToken)
    {
        var impacts = effect.RoomEffectDefinition.Impacts
            .Where(rei => rei.TargetType == "AllOccupants" || rei.TargetType == $"{applicationType}Trigger")
            .ToList();

        foreach (var impact in impacts)
        {
            var impactValue = CalculateImpactValue(impact, effect.Intensity);
            
            // Apply the impact based on type
            switch (impact.ImpactType)
            {
                case "PeriodicDamage":
                    if (impact.TargetAttribute == "Health")
                    {
                        // Apply damage to character
                        character.CurrentFatigue = Math.Max(0, character.CurrentFatigue - (int)impactValue);
                        character.CurrentVitality = Math.Max(0, character.CurrentVitality - (int)Math.Max(0, impactValue - character.CurrentFatigue));
                    }
                    break;

                case "PeriodicHealing":
                    if (impact.TargetAttribute == "Health")
                    {
                        // Apply healing to character
                        character.CurrentFatigue = Math.Min(character.CalculatedFatigue, character.CurrentFatigue + (int)impactValue);
                        if (character.CurrentFatigue >= character.CalculatedFatigue)
                        {
                            character.CurrentVitality = Math.Min(character.CalculatedVitality, character.CurrentVitality + (int)impactValue);
                        }
                    }
                    break;

                // Other impact types would be handled here (MovementPrevention, SkillBonus, etc.)
                // For now, we'll log them for future implementation
                default:
                    _logger.LogDebug("Room effect impact type {ImpactType} not yet implemented", impact.ImpactType);
                    break;
            }

            // Log the application
            var applicationLog = new RoomEffectApplicationLog
            {
                RoomEffectId = effect.Id,
                CharacterId = character.Id,
                ApplicationType = applicationType,
                ImpactType = impact.ImpactType,
                ImpactValue = impactValue,
                Timestamp = DateTimeOffset.UtcNow,
                Details = JsonSerializer.Serialize(new { 
                    EffectName = effect.RoomEffectDefinition.Name,
                    Intensity = effect.Intensity,
                    DamageType = impact.DamageType 
                })
            };

            context.RoomEffectApplicationLogs.Add(applicationLog);
        }

        return Task.CompletedTask;
    }

    private static decimal CalculateImpactValue(RoomEffectImpact impact, decimal intensity)
    {
        var value = impact.ImpactValue;

        // Apply intensity multiplier
        value *= intensity;

        // Apply percentage if specified
        if (impact.IsPercentage)
        {
            // For percentage-based impacts, the value represents a percentage
            // This would need context about what it's a percentage of
            // For now, we'll treat it as a flat percentage value
        }

        // Apply formula if specified
        if (!string.IsNullOrEmpty(impact.ImpactFormula))
        {
            // For now, we'll support simple "intensity * X" formulas
            if (impact.ImpactFormula.StartsWith("intensity * "))
            {
                if (decimal.TryParse(impact.ImpactFormula.Substring(12), out var multiplier))
                {
                    value = intensity * multiplier;
                }
            }
        }

        return value;
    }
}