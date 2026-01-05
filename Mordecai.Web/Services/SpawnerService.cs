using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Mordecai.Game.Entities;
using Mordecai.Game.Services;
using Mordecai.Messaging.Services;
using Mordecai.Messaging.Messages;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing NPC spawning and spawner instances
/// </summary>
public class SpawnerService : ISpawnerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SpawnerService> _logger;
    private readonly IGameMessagePublisher _messagePublisher;
    private readonly IGameTimeService _gameTimeService;
    private readonly Random _random = new();

    public SpawnerService(
        ApplicationDbContext context,
        ILogger<SpawnerService> logger,
        IGameMessagePublisher messagePublisher,
        IGameTimeService gameTimeService)
    {
        _context = context;
        _logger = logger;
        _messagePublisher = messagePublisher;
        _gameTimeService = gameTimeService;
    }

    /// <summary>
    /// Processes all enabled spawners and spawns NPCs if conditions are met
    /// </summary>
    public async Task ProcessSpawnersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Get all enabled spawner instances that are ready to spawn
        var readySpawners = await _context.SpawnerInstances
            .Include(si => si.SpawnerTemplate)
                .ThenInclude(st => st.SpawnTable)
                    .ThenInclude(sne => sne.NpcTemplate)
            .Include(si => si.Room)
            .Include(si => si.Zone)
            .Where(si => si.IsEnabled
                      && si.SpawnerTemplate.IsActive
                      && (si.NextSpawnTime == null || si.NextSpawnTime <= now))
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Processing {Count} ready spawner instances", readySpawners.Count);

        foreach (var spawner in readySpawners)
        {
            try
            {
                await TrySpawnNpcAsync(spawner.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing spawner {SpawnerId}", spawner.Id);
            }
        }
    }

    /// <summary>
    /// Attempts to spawn an NPC from a specific spawner instance
    /// </summary>
    public async Task<Guid?> TrySpawnNpcAsync(int spawnerInstanceId, CancellationToken cancellationToken = default)
    {
        var spawner = await _context.SpawnerInstances
            .Include(si => si.SpawnerTemplate)
                .ThenInclude(st => st.SpawnTable)
                    .ThenInclude(sne => sne.NpcTemplate)
            .Include(si => si.Room)
            .Include(si => si.ActiveSpawns.Where(asp => asp.IsActive))
            .FirstOrDefaultAsync(si => si.Id == spawnerInstanceId, cancellationToken);

        if (spawner == null)
        {
            _logger.LogWarning("Spawner instance {SpawnerId} not found", spawnerInstanceId);
            return null;
        }

        if (!spawner.IsEnabled || !spawner.SpawnerTemplate.IsActive)
        {
            _logger.LogDebug("Spawner {SpawnerId} is disabled or template is inactive", spawnerInstanceId);
            return null;
        }

        // Check if at max capacity
        var activeCount = spawner.ActiveSpawns.Count(asp => asp.IsActive);
        if (activeCount >= spawner.SpawnerTemplate.MaxActiveCreatures)
        {
            _logger.LogDebug("Spawner {SpawnerId} at max capacity ({Current}/{Max})",
                spawnerInstanceId, activeCount, spawner.SpawnerTemplate.MaxActiveCreatures);

            // Schedule next spawn check
            await ScheduleNextSpawnAsync(spawner, cancellationToken);
            return null;
        }

        // Determine spawn room
        int spawnRoomId;
        if (spawner.Type == SpawnerType.RoomBound && spawner.RoomId.HasValue)
        {
            spawnRoomId = spawner.RoomId.Value;
        }
        else if (spawner.Type == SpawnerType.AreaRoaming && spawner.CurrentRoomId.HasValue)
        {
            spawnRoomId = spawner.CurrentRoomId.Value;
        }
        else
        {
            _logger.LogWarning("Spawner {SpawnerId} has no valid spawn room", spawnerInstanceId);
            return null;
        }

        // Parse and check spawn conditions
        var conditions = ParseSpawnConditions(spawner.SpawnerTemplate.ConditionsJson);
        if (!await CanSpawnInRoomAsync(spawnRoomId, conditions, cancellationToken))
        {
            _logger.LogDebug("Spawn conditions not met for spawner {SpawnerId} in room {RoomId}",
                spawnerInstanceId, spawnRoomId);

            // Schedule next spawn check
            await ScheduleNextSpawnAsync(spawner, cancellationToken);
            return null;
        }

        // Select NPC to spawn
        var npcTemplate = SelectNpcTemplate(spawner.SpawnerTemplate);
        if (npcTemplate == null)
        {
            _logger.LogWarning("No valid NPC template found for spawner {SpawnerId}", spawnerInstanceId);
            return null;
        }

        // Create the NPC (placeholder - actual NPC entity will be implemented later)
        var npcId = Guid.NewGuid();

        // Record the active spawn
        var activeSpawn = new ActiveSpawn
        {
            NpcId = npcId,
            SpawnerInstanceId = spawnerInstanceId,
            NpcTemplateId = npcTemplate.Id,
            CurrentRoomId = spawnRoomId,
            SpawnedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        _context.ActiveSpawns.Add(activeSpawn);

        // Update spawner times
        spawner.LastSpawnTime = DateTimeOffset.UtcNow;
        await ScheduleNextSpawnAsync(spawner, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // Publish spawn event
        var spawnEvent = new NpcSpawnedEvent(
            npcId,
            npcTemplate.Id,
            npcTemplate.Name,
            spawnRoomId,
            spawnerInstanceId
        );
        await _messagePublisher.PublishAsync(spawnEvent, cancellationToken);

        _logger.LogInformation("Spawned NPC {NpcName} ({NpcId}) in room {RoomId} from spawner {SpawnerId}",
            npcTemplate.Name, npcId, spawnRoomId, spawnerInstanceId);

        return npcId;
    }

    /// <summary>
    /// Despawns an NPC and records the reason
    /// </summary>
    public async Task DespawnNpcAsync(Guid npcId, DespawnReason reason, CancellationToken cancellationToken = default)
    {
        var activeSpawn = await _context.ActiveSpawns
            .Include(asp => asp.NpcTemplate)
            .FirstOrDefaultAsync(asp => asp.NpcId == npcId && asp.IsActive, cancellationToken);

        if (activeSpawn == null)
        {
            _logger.LogWarning("Active spawn not found for NPC {NpcId}", npcId);
            return;
        }

        activeSpawn.IsActive = false;
        activeSpawn.DeactivatedAt = DateTimeOffset.UtcNow;
        activeSpawn.DespawnReason = reason;

        await _context.SaveChangesAsync(cancellationToken);

        // Publish despawn event
        var despawnEvent = new NpcDespawnedEvent(
            npcId,
            activeSpawn.NpcTemplateId,
            activeSpawn.NpcTemplate.Name,
            activeSpawn.CurrentRoomId,
            reason,
            activeSpawn.SpawnerInstanceId
        );
        await _messagePublisher.PublishAsync(despawnEvent, cancellationToken);

        _logger.LogInformation("Despawned NPC {NpcName} ({NpcId}) - Reason: {Reason}",
            activeSpawn.NpcTemplate.Name, npcId, reason);
    }

    /// <summary>
    /// Gets the current count of active NPCs for a spawner instance
    /// </summary>
    public async Task<int> GetActiveNpcCountAsync(int spawnerInstanceId, CancellationToken cancellationToken = default)
    {
        return await _context.ActiveSpawns
            .Where(asp => asp.SpawnerInstanceId == spawnerInstanceId && asp.IsActive)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if spawn conditions are met for a room
    /// </summary>
    public async Task<bool> CanSpawnInRoomAsync(int roomId, SpawnConditions conditions, CancellationToken cancellationToken = default)
    {
        // Check spawn chance
        if (conditions.SpawnChance < 1.0f)
        {
            var roll = (float)_random.NextDouble();
            if (roll > conditions.SpawnChance)
            {
                _logger.LogDebug("Spawn chance failed: {Roll:F2} > {Chance:F2}", roll, conditions.SpawnChance);
                return false;
            }
        }

        // Check time of day
        if (conditions.RequiredTimeOfDay.HasValue)
        {
            var currentTime = _gameTimeService.CurrentTimeOfDay;
            if (currentTime != conditions.RequiredTimeOfDay.Value)
            {
                _logger.LogDebug("Time of day condition not met: {Current} != {Required}",
                    currentTime, conditions.RequiredTimeOfDay.Value);
                return false;
            }
        }

        // Check for players in room
        if (conditions.BlockIfPlayersPresent)
        {
            var hasPlayers = await _context.Characters
                .AnyAsync(c => c.CurrentRoomId == roomId, cancellationToken);

            if (hasPlayers)
            {
                _logger.LogDebug("Players present in room {RoomId}, blocking spawn", roomId);
                return false;
            }
        }

        // Check for creatures in room
        if (conditions.BlockIfCreaturesPresent)
        {
            var hasCreatures = await _context.ActiveSpawns
                .AnyAsync(asp => asp.CurrentRoomId == roomId && asp.IsActive, cancellationToken);

            if (hasCreatures)
            {
                _logger.LogDebug("Creatures present in room {RoomId}, blocking spawn", roomId);
                return false;
            }
        }

        // Check max creatures in room
        if (conditions.MaxCreaturesInRoom.HasValue)
        {
            var creatureCount = await _context.ActiveSpawns
                .Where(asp => asp.CurrentRoomId == roomId && asp.IsActive)
                .CountAsync(cancellationToken);

            if (creatureCount >= conditions.MaxCreaturesInRoom.Value)
            {
                _logger.LogDebug("Room {RoomId} at max creature capacity ({Current}/{Max})",
                    roomId, creatureCount, conditions.MaxCreaturesInRoom.Value);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Enables or disables a spawner instance
    /// </summary>
    public async Task SetSpawnerEnabledAsync(int spawnerInstanceId, bool enabled, CancellationToken cancellationToken = default)
    {
        var spawner = await _context.SpawnerInstances
            .Include(si => si.SpawnerTemplate)
            .FirstOrDefaultAsync(si => si.Id == spawnerInstanceId, cancellationToken);

        if (spawner == null)
        {
            throw new InvalidOperationException($"Spawner instance {spawnerInstanceId} not found");
        }

        spawner.IsEnabled = enabled;

        if (enabled)
        {
            // Schedule next spawn
            await ScheduleNextSpawnAsync(spawner, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Publish state change event
        var stateEvent = new SpawnerStateChangedEvent(
            spawnerInstanceId,
            spawner.SpawnerTemplateId,
            spawner.SpawnerTemplate.Name,
            enabled,
            spawner.RoomId,
            spawner.ZoneId
        );
        await _messagePublisher.PublishAsync(stateEvent, cancellationToken);

        _logger.LogInformation("Spawner {SpawnerId} {State}",
            spawnerInstanceId, enabled ? "enabled" : "disabled");
    }

    /// <summary>
    /// Gets all active spawns for a spawner instance
    /// </summary>
    public async Task<IReadOnlyList<ActiveSpawn>> GetActiveSpawnsAsync(int spawnerInstanceId, CancellationToken cancellationToken = default)
    {
        return await _context.ActiveSpawns
            .Include(asp => asp.NpcTemplate)
            .Include(asp => asp.CurrentRoom)
            .Where(asp => asp.SpawnerInstanceId == spawnerInstanceId && asp.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Cleans up inactive spawns older than 24 hours
    /// </summary>
    public async Task CleanupInactiveSpawnsAsync(CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddHours(-24);

        var oldSpawns = await _context.ActiveSpawns
            .Where(asp => !asp.IsActive && asp.DeactivatedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (oldSpawns.Count > 0)
        {
            _context.ActiveSpawns.RemoveRange(oldSpawns);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cleaned up {Count} inactive spawn records", oldSpawns.Count);
        }
    }

    #region Private Helper Methods

    private SpawnConditions ParseSpawnConditions(string? conditionsJson)
    {
        if (string.IsNullOrWhiteSpace(conditionsJson))
        {
            return new SpawnConditions();
        }

        try
        {
            return JsonSerializer.Deserialize<SpawnConditions>(conditionsJson) ?? new SpawnConditions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing spawn conditions JSON: {Json}", conditionsJson);
            return new SpawnConditions();
        }
    }

    private NpcTemplate? SelectNpcTemplate(SpawnerTemplate spawnerTemplate)
    {
        if (spawnerTemplate.SpawnTable == null || spawnerTemplate.SpawnTable.Count == 0)
        {
            return null;
        }

        var activeEntries = spawnerTemplate.SpawnTable
            .Where(sne => sne.NpcTemplate.IsActive)
            .ToList();

        if (activeEntries.Count == 0)
        {
            return null;
        }

        switch (spawnerTemplate.SpawnBehavior)
        {
            case SpawnBehavior.Fixed:
                return activeEntries[0].NpcTemplate;

            case SpawnBehavior.Random:
                var randomIndex = _random.Next(activeEntries.Count);
                return activeEntries[randomIndex].NpcTemplate;

            case SpawnBehavior.Weighted:
                var totalWeight = activeEntries.Sum(e => e.Weight);
                var roll = _random.Next(totalWeight);
                var cumulative = 0;

                foreach (var entry in activeEntries)
                {
                    cumulative += entry.Weight;
                    if (roll < cumulative)
                    {
                        return entry.NpcTemplate;
                    }
                }

                // Fallback to last entry
                return activeEntries[^1].NpcTemplate;

            default:
                return activeEntries[0].NpcTemplate;
        }
    }

    private async Task ScheduleNextSpawnAsync(SpawnerInstance spawner, CancellationToken cancellationToken)
    {
        var template = spawner.SpawnerTemplate;
        var intervalRange = template.SpawnIntervalMax - template.SpawnIntervalMin;
        var randomOffset = intervalRange > 0 ? _random.Next(intervalRange) : 0;
        var nextSpawnSeconds = template.SpawnIntervalMin + randomOffset;

        spawner.NextSpawnTime = DateTimeOffset.UtcNow.AddSeconds(nextSpawnSeconds);

        _logger.LogDebug("Scheduled next spawn for spawner {SpawnerId} at {NextSpawnTime}",
            spawner.Id, spawner.NextSpawnTime);
    }

    #endregion
}
