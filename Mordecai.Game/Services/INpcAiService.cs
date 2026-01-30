using Mordecai.Game.Entities;

namespace Mordecai.Game.Services;

/// <summary>
/// Service interface for NPC artificial intelligence decisions in combat
/// </summary>
public interface INpcAiService
{
    /// <summary>
    /// Execute AI decision loop for an NPC in combat.
    /// Priority: flee → defense mode → counterattack
    /// </summary>
    /// <param name="activeSpawn">The NPC spawn to process</param>
    /// <param name="session">The combat session the NPC is in</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if NPC took an action, false otherwise</returns>
    Task<bool> DecideAndActAsync(
        ActiveSpawn activeSpawn,
        CombatSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get flee threshold for an NPC (percentage of max VIT)
    /// </summary>
    decimal GetFleeThreshold(NpcTemplate template);

    /// <summary>
    /// Determine if NPC should switch to parry mode based on current state
    /// </summary>
    bool ShouldUseParryMode(ActiveSpawn spawn, NpcTemplate template);
}
