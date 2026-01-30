using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mordecai.Game.Entities;
using Mordecai.Game.Services;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Background worker that periodically applies pending damage and healing to character health pools.
/// </summary>
public class HealthTickBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HealthTickBackgroundService> _logger;
    private const int ProcessingIntervalSeconds = 3;
    private const int FatigueRegenPerTick = 1;
    private const int VitalityRegenPerTick = 1;
    private static readonly TimeSpan PassiveVitalityRegenInterval = TimeSpan.FromHours(1);

    public HealthTickBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<HealthTickBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health tick background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingHealthAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying pending health changes");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(ProcessingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Health tick background service stopped.");
    }

    private async Task ProcessPendingHealthAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var characters = await context.Characters
            .Where(c => c.PendingFatigueDamage != 0
                        || c.PendingVitalityDamage != 0
                        || c.CurrentFatigue < ((c.Drive + c.Focus) - 5 > 1 ? (c.Drive + c.Focus) - 5 : 1)
                        || c.CurrentVitality < ((c.Physicality * 2) - 5 > 1 ? (c.Physicality * 2) - 5 : 1))
            .ToListAsync(cancellationToken);

        if (characters.Count == 0)
        {
            return;
        }

    var updatedCount = 0;
    var now = DateTimeOffset.UtcNow;
    var baseFatigueRegenInterval = TimeSpan.FromSeconds(ProcessingIntervalSeconds);

        foreach (var character in characters)
        {
            var characterUpdated = false;
            var maxFatigue = Math.Max(1, (character.Drive + character.Focus) - 5);
            var maxVitality = Math.Max(1, (character.Physicality * 2) - 5);
            var availableVitality = VitalityEffectRules.CalculateAvailableVitality(character.CurrentVitality, character.PendingVitalityDamage);
            var regenInterval = VitalityEffectRules.GetFatigueRegenInterval(availableVitality, baseFatigueRegenInterval);

            if (regenInterval is null)
            {
                if (character.LastFatigueRegenAt.HasValue)
                {
                    character.LastFatigueRegenAt = null;
                    characterUpdated = true;
                }
            }
            else
            {
                if (character.LastFatigueRegenAt is null)
                {
                    character.LastFatigueRegenAt = now;
                    characterUpdated = true;
                }

                if (character.CurrentFatigue < maxFatigue)
                {
                    var lastRegen = character.LastFatigueRegenAt ?? now;
                    if (now - lastRegen >= regenInterval.Value)
                    {
                        character.PendingFatigueDamage = SafeAdd(character.PendingFatigueDamage, -FatigueRegenPerTick);
                        character.LastFatigueRegenAt = now;
                        characterUpdated = true;
                    }
                }
            }

            if (ApplyPassiveVitalityRegen(character, now, maxVitality))
            {
                characterUpdated = true;
            }

            if (ApplyPendingPools(character))
            {
                characterUpdated = true;
            }

            if (characterUpdated)
            {
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Applied health tick updates for {CharacterCount} characters", updatedCount);
        }

        // Process NPC (ActiveSpawn) pending damage
        await ProcessNpcPendingHealthAsync(context, cancellationToken);

        // Process expired timed penalties for combat participants
        await ProcessTimedPenaltiesAsync(context, cancellationToken);

        // Process NPC AI decisions for active combat sessions
        await ProcessNpcAiDecisionsAsync(scope, cancellationToken);
    }

    private async Task ProcessNpcAiDecisionsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var npcAiService = scope.ServiceProvider.GetService<INpcAiService>();
        if (npcAiService == null)
        {
            return; // Service not registered
        }

        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Find all active combat sessions with NPC participants
        var activeSessions = await context.CombatSessions
            .Include(s => s.Participants)
                .ThenInclude(p => p.ActiveSpawn)
                    .ThenInclude(asp => asp!.NpcTemplate)
            .Where(s => s.IsActive && s.Participants.Any(p => p.ActiveSpawnId != null && p.IsActive))
            .ToListAsync(cancellationToken);

        if (activeSessions.Count == 0)
        {
            return;
        }

        foreach (var session in activeSessions)
        {
            var npcParticipants = session.Participants
                .Where(p => p.ActiveSpawnId != null && p.IsActive && p.ActiveSpawn != null)
                .ToList();

            foreach (var participant in npcParticipants)
            {
                try
                {
                    await npcAiService.DecideAndActAsync(participant.ActiveSpawn!, session, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing NPC AI for spawn {SpawnId}", participant.ActiveSpawnId);
                }
            }
        }
    }

    private async Task ProcessNpcPendingHealthAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var npcs = await context.ActiveSpawns
            .Include(asp => asp.NpcTemplate)
            .Where(asp => asp.IsActive &&
                   (asp.PendingFatigueDamage != 0 || asp.PendingVitalityDamage != 0))
            .ToListAsync(cancellationToken);

        if (npcs.Count == 0)
        {
            return;
        }

        var updatedCount = 0;

        foreach (var npc in npcs)
        {
            var npcUpdated = false;

            if (ProcessNpcFatiguePool(npc))
            {
                npcUpdated = true;
            }

            if (ProcessNpcVitalityPool(npc))
            {
                npcUpdated = true;
            }

            if (npcUpdated)
            {
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Applied health tick updates for {NpcCount} NPCs", updatedCount);
        }
    }

    private static bool ProcessNpcFatiguePool(ActiveSpawn npc)
    {
        if (npc.PendingFatigueDamage == 0)
        {
            return false;
        }

        var pending = npc.PendingFatigueDamage;
        var amount = Math.Max(1, (int)Math.Ceiling(Math.Abs(pending) / 2.0));

        if (pending > 0)
        {
            // Damage
            var beforeFatigue = npc.CurrentFatigue;
            var applied = Math.Min(amount, npc.CurrentFatigue);
            npc.CurrentFatigue = Math.Max(0, npc.CurrentFatigue - applied);
            npc.PendingFatigueDamage = Math.Max(0, npc.PendingFatigueDamage - amount);

            // Overflow to vitality
            var overflow = amount - applied;
            if (overflow > 0)
            {
                npc.PendingVitalityDamage += overflow;
            }

            // Fatigue crash penalty
            var fatigueCrash = beforeFatigue > 0 && npc.CurrentFatigue == 0;
            if (fatigueCrash)
            {
                npc.PendingVitalityDamage = SafeAdd(npc.PendingVitalityDamage, 2);
            }

            return applied > 0 || overflow > 0 || fatigueCrash;
        }
        else
        {
            // Healing - NPCs calculate max fatigue from attributes: (END + WIL) - 5
            var template = npc.NpcTemplate;
            var maxFatigue = template != null 
                ? Math.Max(1, (template.Endurance + template.Willpower) - 5)
                : 5;
            var capacity = maxFatigue - npc.CurrentFatigue;
            if (capacity <= 0)
            {
                npc.PendingFatigueDamage = 0;
                return true;
            }

            var applied = Math.Min(amount, capacity);
            npc.CurrentFatigue = Math.Min(maxFatigue, npc.CurrentFatigue + applied);
            npc.PendingFatigueDamage = Math.Min(0, npc.PendingFatigueDamage + amount);

            return applied > 0;
        }
    }

    private static bool ProcessNpcVitalityPool(ActiveSpawn npc)
    {
        if (npc.PendingVitalityDamage == 0)
        {
            return false;
        }

        var pending = npc.PendingVitalityDamage;
        var amount = Math.Max(1, (int)Math.Ceiling(Math.Abs(pending) / 2.0));

        if (pending > 0)
        {
            // Damage
            var applied = Math.Min(amount, npc.CurrentVitality);
            npc.CurrentVitality = Math.Max(0, npc.CurrentVitality - applied);
            npc.PendingVitalityDamage = Math.Max(0, npc.PendingVitalityDamage - amount);
            return applied > 0;
        }
        else
        {
            // Healing - NPCs calculate max vitality from attributes: (STR * 2) - 5
            var template = npc.NpcTemplate;
            var maxVitality = template != null 
                ? Math.Max(1, (template.Strength * 2) - 5)
                : 10;
            var capacity = maxVitality - npc.CurrentVitality;
            if (capacity <= 0)
            {
                npc.PendingVitalityDamage = 0;
                return false;
            }

            var applied = Math.Min(amount, capacity);
            npc.CurrentVitality = Math.Min(maxVitality, npc.CurrentVitality + applied);
            npc.PendingVitalityDamage = Math.Min(0, npc.PendingVitalityDamage + amount);
            return applied > 0;
        }
    }

    private async Task ProcessTimedPenaltiesAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var participants = await context.CombatParticipants
            .Where(p => p.IsActive && p.TimedPenaltiesJson != null)
            .ToListAsync(cancellationToken);

        if (participants.Count == 0)
        {
            return;
        }

        var updatedCount = 0;

        foreach (var participant in participants)
        {
            var penalties = JsonSerializer.Deserialize<List<TimedPenalty>>(participant.TimedPenaltiesJson!);
            var activePenalties = penalties?.Where(p => p.ExpiresAt > now).ToList();

            var newJson = activePenalties?.Count > 0
                ? JsonSerializer.Serialize(activePenalties)
                : null;

            if (newJson != participant.TimedPenaltiesJson)
            {
                participant.TimedPenaltiesJson = newJson;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Cleaned up timed penalties for {Count} combat participants", updatedCount);
        }
    }

    private static bool ApplyPendingPools(Character character)
    {
        var updated = false;
        updated |= ProcessFatiguePool(character);
        updated |= ProcessVitalityPool(character);
        return updated;
    }

    private static bool ProcessFatiguePool(Character character)
    {
        if (character.PendingFatigueDamage == 0)
        {
            return false;
        }

        var pending = character.PendingFatigueDamage;
        var amount = Math.Max(1, (int)Math.Ceiling(Math.Abs(pending) / 2.0));

        if (pending > 0)
        {
            var beforeFatigue = character.CurrentFatigue;
            var applied = Math.Min(amount, character.CurrentFatigue);
            character.CurrentFatigue = Math.Max(0, character.CurrentFatigue - applied);
            character.PendingFatigueDamage = Math.Max(0, character.PendingFatigueDamage - amount);

            var overflow = amount - applied;
            if (overflow > 0)
            {
                character.PendingVitalityDamage += overflow;
            }

            var fatigueCrash = beforeFatigue > 0 && character.CurrentFatigue == 0;
            if (fatigueCrash)
            {
                character.PendingVitalityDamage = SafeAdd(character.PendingVitalityDamage, 2);
            }

            return applied > 0 || overflow > 0 || fatigueCrash;
        }
        else
        {
            var capacity = character.MaxFatigue - character.CurrentFatigue;
            if (capacity <= 0)
            {
                character.PendingFatigueDamage = 0;
                return true;
            }

            var applied = Math.Min(amount, capacity);
            character.CurrentFatigue = Math.Min(character.MaxFatigue, character.CurrentFatigue + applied);
            character.PendingFatigueDamage = Math.Min(0, character.PendingFatigueDamage + amount);

            var overflow = amount - applied;
            if (overflow > 0)
            {
                character.PendingFatigueDamage = 0;
            }

            return applied > 0 || overflow > 0;
        }
    }

    private static bool ProcessVitalityPool(Character character)
    {
        if (character.PendingVitalityDamage == 0)
        {
            return false;
        }

        var pending = character.PendingVitalityDamage;
        var amount = Math.Max(1, (int)Math.Ceiling(Math.Abs(pending) / 2.0));

        if (pending > 0)
        {
            var applied = Math.Min(amount, character.CurrentVitality);
            character.CurrentVitality = Math.Max(0, character.CurrentVitality - applied);
            character.PendingVitalityDamage = Math.Max(0, character.PendingVitalityDamage - amount);
            return applied > 0;
        }
        else
        {
            var capacity = character.MaxVitality - character.CurrentVitality;
            if (capacity <= 0)
            {
                character.PendingVitalityDamage = 0;
                return false;
            }

            var applied = Math.Min(amount, capacity);
            character.CurrentVitality = Math.Min(character.MaxVitality, character.CurrentVitality + applied);
            character.PendingVitalityDamage = Math.Min(0, character.PendingVitalityDamage + amount);
            return applied > 0;
        }
    }

    internal static bool ApplyPassiveVitalityRegen(Character character, DateTimeOffset now, int maxVitality)
    {
        if (character.CurrentVitality <= 0)
        {
            if (character.LastVitalityRegenAt is not null)
            {
                character.LastVitalityRegenAt = null;
                return true;
            }

            return false;
        }

        if (character.LastVitalityRegenAt is null)
        {
            character.LastVitalityRegenAt = now;
            return true;
        }

        if (character.CurrentVitality >= maxVitality)
        {
            if (character.LastVitalityRegenAt != now)
            {
                character.LastVitalityRegenAt = now;
                return true;
            }

            return false;
        }

        var lastRegen = character.LastVitalityRegenAt.Value;
        var elapsed = now - lastRegen;
        if (elapsed < PassiveVitalityRegenInterval)
        {
            return false;
        }

        var regenTicks = (int)(elapsed.Ticks / PassiveVitalityRegenInterval.Ticks);
        if (regenTicks <= 0)
        {
            return false;
        }

        var potentialHealing = regenTicks * VitalityRegenPerTick;
        var missingVitality = Math.Max(0, maxVitality - character.CurrentVitality);
        var healAmount = Math.Min(potentialHealing, missingVitality);

        if (healAmount > 0)
        {
            character.CurrentVitality = Math.Min(maxVitality, character.CurrentVitality + healAmount);
        }

        var nextTimestamp = lastRegen.AddTicks(PassiveVitalityRegenInterval.Ticks * regenTicks);
        character.LastVitalityRegenAt = character.CurrentVitality >= maxVitality ? now : nextTimestamp;

        return healAmount > 0 || character.LastVitalityRegenAt != lastRegen;
    }

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
}
