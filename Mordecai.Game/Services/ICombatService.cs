using Mordecai.Game.Entities;

namespace Mordecai.Game.Services;

/// <summary>
/// Combat state snapshot for a character
/// </summary>
public record CharacterCombatState(
    bool IsInCombat,
    Guid? SessionId,
    bool IsParryMode,
    bool IsFleeing = false,
    string? TargetName = null
);

/// <summary>
/// Service interface for managing combat sessions and resolving combat actions
/// </summary>
public interface ICombatService
{
    /// <summary>
    /// Initiates combat between an attacker and a target
    /// </summary>
    /// <returns>Combat session ID, or null if combat could not be initiated</returns>
    Task<Guid?> InitiateCombatAsync(
        Guid attackerId,
        bool attackerIsPlayer,
        Guid targetId,
        bool targetIsPlayer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a melee attack from one participant to another
    /// </summary>
    /// <returns>Success value of the attack, or null if attack failed</returns>
    Task<int?> PerformMeleeAttackAsync(
        Guid attackerId,
        bool attackerIsPlayer,
        Guid targetId,
        bool targetIsPlayer,
        bool isDualWield = false,
        bool isOffHand = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a ranged attack from one participant to another
    /// </summary>
    /// <returns>Success value of the attack, or null if attack failed</returns>
    Task<int?> PerformRangedAttackAsync(
        Guid attackerId,
        bool attackerIsPlayer,
        Guid targetId,
        bool targetIsPlayer,
        int range,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a knockback attack to stun the target
    /// </summary>
    /// <returns>Duration in seconds the target is stunned, or 0 if failed</returns>
    Task<int> PerformKnockbackAsync(
        Guid attackerId,
        bool attackerIsPlayer,
        Guid targetId,
        bool targetIsPlayer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles parry mode for a participant
    /// </summary>
    Task SetParryModeAsync(
        Guid participantId,
        bool isPlayer,
        bool enabled,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to flee from combat
    /// </summary>
    /// <returns>True if flee was successful, false otherwise</returns>
    Task<bool> FleeFromCombatAsync(
        Guid participantId,
        bool isPlayer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends a combat session
    /// </summary>
    Task EndCombatAsync(
        Guid combatSessionId,
        string reason,
        Guid? winnerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active combat session for a character or NPC
    /// </summary>
    Task<CombatSession?> GetActiveCombatSessionAsync(
        Guid participantId,
        bool isPlayer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a character or NPC is currently in combat
    /// </summary>
    Task<bool> IsInCombatAsync(
        Guid participantId,
        bool isPlayer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all participants in a combat session
    /// </summary>
    Task<List<CombatParticipant>> GetCombatParticipantsAsync(
        Guid combatSessionId,
        CancellationToken cancellationToken = default);
        
    /// <summary>
    /// Gets the combat state snapshot for a player character
    /// </summary>
    Task<CharacterCombatState> GetCharacterCombatStateAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);
}
