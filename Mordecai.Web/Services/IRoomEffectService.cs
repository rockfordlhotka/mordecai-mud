using Mordecai.Game.Entities;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing room effects - creating, applying, and removing effects from rooms
/// </summary>
public interface IRoomEffectService
{
    /// <summary>
    /// Apply an effect to a room
    /// </summary>
    Task<RoomEffect> ApplyEffectAsync(int roomId, int effectDefinitionId, string sourceType, string? sourceId = null, string? sourceName = null, Guid? casterCharacterId = null, decimal intensity = 1.0m, int? durationOverride = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a specific room effect
    /// </summary>
    Task RemoveEffectAsync(int roomEffectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove all effects from a room
    /// </summary>
    Task RemoveAllEffectsFromRoomAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active effects in a room
    /// </summary>
    Task<IList<RoomEffect>> GetActiveEffectsInRoomAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all visible effects in a room (for display to players)
    /// </summary>
    Task<IList<RoomEffect>> GetVisibleEffectsInRoomAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process periodic effects for a room (damage, healing, etc.)
    /// </summary>
    Task ProcessPeriodicEffectsAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if movement from a room is prevented by effects
    /// </summary>
    Task<bool> IsMovementPreventedAsync(int roomId, Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply entry effects when a character enters a room
    /// </summary>
    Task ApplyEntryEffectsAsync(int roomId, Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply exit effects when a character attempts to leave a room
    /// </summary>
    Task ApplyExitEffectsAsync(int roomId, Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired effects
    /// </summary>
    Task CleanupExpiredEffectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get room effect definition by name
    /// </summary>
    Task<RoomEffectDefinition?> GetEffectDefinitionByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all available room effect definitions
    /// </summary>
    Task<IList<RoomEffectDefinition>> GetAllEffectDefinitionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of applying room effect impacts to a character
/// </summary>
public record RoomEffectApplicationResult(
    bool Success,
    string? Message,
    decimal DamageDealt = 0,
    decimal HealingApplied = 0,
    bool MovementPrevented = false,
    bool SpellcastingPrevented = false,
    Dictionary<string, object>? AdditionalData = null
);