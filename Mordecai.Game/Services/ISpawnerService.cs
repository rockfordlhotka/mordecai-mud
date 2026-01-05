using Mordecai.Game.Entities;

namespace Mordecai.Game.Services;

/// <summary>
/// Service for managing NPC spawning and spawner instances
/// </summary>
public interface ISpawnerService
{
    /// <summary>
    /// Processes all enabled spawners and spawns NPCs if conditions are met
    /// Called by the SpawnerTickService background service
    /// </summary>
    Task ProcessSpawnersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to spawn an NPC from a specific spawner instance
    /// </summary>
    /// <returns>NpcId if spawned successfully, null otherwise</returns>
    Task<Guid?> TrySpawnNpcAsync(int spawnerInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Despawns an NPC and records the reason
    /// </summary>
    Task DespawnNpcAsync(Guid npcId, DespawnReason reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current count of active NPCs for a spawner instance
    /// </summary>
    Task<int> GetActiveNpcCountAsync(int spawnerInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if spawn conditions are met for a room
    /// </summary>
    Task<bool> CanSpawnInRoomAsync(int roomId, SpawnConditions conditions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables a spawner instance
    /// </summary>
    Task SetSpawnerEnabledAsync(int spawnerInstanceId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active spawns for a spawner instance
    /// </summary>
    Task<IReadOnlyList<ActiveSpawn>> GetActiveSpawnsAsync(int spawnerInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up inactive spawns (deactivated NPCs)
    /// </summary>
    Task CleanupInactiveSpawnsAsync(CancellationToken cancellationToken = default);
}
