using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Game.Services;
using Mordecai.Messaging.Messages;
using Mordecai.Messaging.Services;
using Mordecai.Web.Data;
using System.Text.Json;

namespace Mordecai.Web.Services;

/// <summary>
/// NPC artificial intelligence service for combat decisions
/// </summary>
public sealed class NpcAiService : INpcAiService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICombatService _combatService;
    private readonly IGameMessagePublisher _messagePublisher;
    private readonly ILogger<NpcAiService> _logger;

    // Default flee threshold: 25% of max VIT
    private const decimal DefaultFleeThreshold = 0.25m;

    // Minimum FAT before switching to parry mode
    private const int MinFatigueForDodge = 3;

    public NpcAiService(
        ApplicationDbContext dbContext,
        ICombatService combatService,
        IGameMessagePublisher messagePublisher,
        ILogger<NpcAiService> logger)
    {
        _dbContext = dbContext;
        _combatService = combatService;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<bool> DecideAndActAsync(
        ActiveSpawn activeSpawn,
        CombatSession session,
        CancellationToken cancellationToken = default)
    {
        if (activeSpawn.NpcTemplate == null)
        {
            _logger.LogWarning("NPC AI: ActiveSpawn {Id} has no template", activeSpawn.Id);
            return false;
        }

        var template = activeSpawn.NpcTemplate;
        var maxVit = CalculateMaxVitality(template);

        // Priority 1: Check if should flee
        var fleeThreshold = GetFleeThreshold(template);
        var vitPercent = maxVit > 0 ? (decimal)activeSpawn.CurrentVitality / maxVit : 1m;

        if (vitPercent <= fleeThreshold)
        {
            _logger.LogDebug("NPC {Name} attempting to flee (VIT: {Vit}/{Max}, threshold: {Threshold}%)",
                template.Name, activeSpawn.CurrentVitality, maxVit, fleeThreshold * 100);

            var fled = await _combatService.FleeFromCombatAsync(
                activeSpawn.NpcId,
                isPlayer: false,
                cancellationToken);

            if (fled)
            {
                await _messagePublisher.PublishAsync(new CombatAction(
                    activeSpawn.NpcId,
                    template.Name,
                    Guid.Empty,
                    string.Empty,
                    activeSpawn.CurrentRoomId ?? 0,
                    $"{template.Name} flees from combat!",
                    0,
                    false,
                    "Flee",
                    SoundLevel.Normal
                ), cancellationToken);

                _logger.LogInformation("NPC {Name} successfully fled from combat", template.Name);
                return true;
            }

            // Failed to flee - continue to other actions
            _logger.LogDebug("NPC {Name} failed to flee", template.Name);
        }

        // Priority 2: Update defense mode based on FAT
        var shouldParry = ShouldUseParryMode(activeSpawn, template);
        var participant = await GetNpcParticipantAsync(activeSpawn.Id, session.Id, cancellationToken);

        if (participant != null && participant.IsInParryMode != shouldParry)
        {
            await _combatService.SetParryModeAsync(
                activeSpawn.NpcId,
                isPlayer: false,
                shouldParry,
                cancellationToken);

            _logger.LogDebug("NPC {Name} switched to {Mode} mode",
                template.Name, shouldParry ? "parry" : "dodge");
        }

        // Priority 3: Counterattack a player in the session
        var target = await SelectTargetAsync(session, activeSpawn.Id, cancellationToken);
        if (target == null)
        {
            _logger.LogDebug("NPC {Name} has no valid targets", template.Name);
            return false;
        }

        // Perform melee attack
        var result = await _combatService.PerformMeleeAttackAsync(
            activeSpawn.NpcId,
            attackerIsPlayer: false,
            target.CharacterId!.Value,
            targetIsPlayer: true,
            cancellationToken: cancellationToken);

        if (result.HasValue)
        {
            _logger.LogDebug("NPC {Name} attacked {Target} with result {Result}",
                template.Name, target.ParticipantName, result.Value);
            return true;
        }

        return false;
    }

    public decimal GetFleeThreshold(NpcTemplate template)
    {
        // Check BehaviorConfig for custom threshold
        if (!string.IsNullOrEmpty(template.BehaviorConfig))
        {
            try
            {
                var config = JsonSerializer.Deserialize<NpcBehaviorConfig>(template.BehaviorConfig);
                if (config?.FleeThreshold.HasValue == true)
                {
                    return config.FleeThreshold.Value;
                }
            }
            catch (JsonException)
            {
                // Ignore malformed config, use default
            }
        }

        return DefaultFleeThreshold;
    }

    public bool ShouldUseParryMode(ActiveSpawn spawn, NpcTemplate template)
    {
        // Switch to parry mode when FAT is low to conserve stamina
        // Parry uses weapon skill but costs no FAT
        // Dodge costs 1 FAT per defense
        return spawn.CurrentFatigue < MinFatigueForDodge;
    }

    private static int CalculateMaxVitality(NpcTemplate template)
    {
        // VIT = (STR × 2) - 5
        var vit = (template.Strength * 2) - 5;
        return vit > 1 ? vit : 1;
    }

    private async Task<CombatParticipant?> GetNpcParticipantAsync(
        int activeSpawnId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CombatParticipants
            .FirstOrDefaultAsync(p =>
                p.CombatSessionId == sessionId &&
                p.ActiveSpawnId == activeSpawnId &&
                p.IsActive,
                cancellationToken);
    }

    private async Task<CombatParticipant?> SelectTargetAsync(
        CombatSession session,
        int npcActiveSpawnId,
        CancellationToken cancellationToken)
    {
        // Select a random player target from the combat session
        var players = await _dbContext.CombatParticipants
            .Where(p =>
                p.CombatSessionId == session.Id &&
                p.CharacterId != null &&
                p.IsActive)
            .ToListAsync(cancellationToken);

        if (players.Count == 0)
        {
            return null;
        }

        // Return first player for simplicity (could randomize later)
        return players[0];
    }
}

/// <summary>
/// Configuration for NPC AI behavior (stored in NpcTemplate.BehaviorConfig)
/// </summary>
public class NpcBehaviorConfig
{
    /// <summary>
    /// VIT percentage threshold for flee attempts (0.0-1.0)
    /// </summary>
    public decimal? FleeThreshold { get; set; }

    /// <summary>
    /// Whether this NPC will fight to the death
    /// </summary>
    public bool NeverFlee { get; set; } = false;

    /// <summary>
    /// Aggression level affects attack frequency (future use)
    /// </summary>
    public int AggressionLevel { get; set; } = 5;
}
