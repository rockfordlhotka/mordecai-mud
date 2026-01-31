using Mordecai.Game.Entities;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing character effects (wounds, buffs, debuffs, status effects)
/// </summary>
public interface ICharacterEffectService
{
    /// <summary>
    /// Applies an effect to a character
    /// </summary>
    /// <param name="characterId">Target character</param>
    /// <param name="effectDefinitionId">Effect definition to apply</param>
    /// <param name="sourceCharacterId">Character who applied the effect (optional)</param>
    /// <param name="sourceNpcId">NPC who applied the effect (optional)</param>
    /// <param name="sourceSpellSkillId">Spell skill that caused the effect (optional)</param>
    /// <param name="durationSeconds">Override duration (null = use definition default)</param>
    /// <param name="intensity">Override intensity (null = use definition default)</param>
    /// <param name="bodyLocation">Body location for wound effects</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<EffectApplicationResult> ApplyEffectAsync(
        Guid characterId,
        int effectDefinitionId,
        Guid? sourceCharacterId = null,
        Guid? sourceNpcId = null,
        int? sourceSpellSkillId = null,
        int? durationSeconds = null,
        decimal? intensity = null,
        BodyLocation? bodyLocation = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an effect by name
    /// </summary>
    Task<EffectApplicationResult> ApplyEffectByNameAsync(
        Guid characterId,
        string effectName,
        Guid? sourceCharacterId = null,
        Guid? sourceNpcId = null,
        int? sourceSpellSkillId = null,
        int? durationSeconds = null,
        decimal? intensity = null,
        BodyLocation? bodyLocation = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a wound effect to a character
    /// </summary>
    Task<EffectApplicationResult> ApplyWoundAsync(
        Guid characterId,
        BodyLocation location = BodyLocation.General,
        Guid? sourceCharacterId = null,
        Guid? sourceNpcId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an active effect from a character
    /// </summary>
    /// <param name="effectId">The active effect instance ID</param>
    /// <param name="reason">Reason for removal (expired, dispelled, healed, etc.)</param>
    Task<bool> RemoveEffectAsync(int effectId, string reason = "removed", CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all effects of a specific type from a character
    /// </summary>
    Task<int> RemoveEffectsByTypeAsync(
        Guid characterId,
        CharacterEffectType effectType,
        string reason = "removed",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific number of wounds from a character
    /// </summary>
    /// <param name="characterId">Target character</param>
    /// <param name="count">Number of wounds to heal (0 = all)</param>
    /// <param name="location">Specific body location (null = any)</param>
    Task<int> HealWoundsAsync(
        Guid characterId,
        int count = 1,
        BodyLocation? location = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active effects on a character
    /// </summary>
    Task<IReadOnlyList<CharacterEffect>> GetActiveEffectsAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active effects of a specific type on a character
    /// </summary>
    Task<IReadOnlyList<CharacterEffect>> GetActiveEffectsByTypeAsync(
        Guid characterId,
        CharacterEffectType effectType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a character has a specific effect active
    /// </summary>
    Task<bool> HasEffectAsync(
        Guid characterId,
        int effectDefinitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a character has a specific effect by name
    /// </summary>
    Task<bool> HasEffectByNameAsync(
        Guid characterId,
        string effectName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of all active effects on a character for combat calculations
    /// </summary>
    Task<CharacterEffectSummary> GetEffectSummaryAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total wound count for a character
    /// </summary>
    Task<int> GetWoundCountAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets wound counts by body location
    /// </summary>
    Task<Dictionary<BodyLocation, int>> GetWoundsByLocationAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes periodic effects (damage/healing over time) for a character
    /// </summary>
    /// <returns>List of messages describing what happened</returns>
    Task<IReadOnlyList<string>> ProcessPeriodicEffectsAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all expired effects
    /// </summary>
    /// <returns>Number of effects removed</returns>
    Task<int> CleanupExpiredEffectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes natural wound healing for a character (1 wound per 4 hours)
    /// </summary>
    Task<int> ProcessNaturalWoundHealingAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an effect definition by ID
    /// </summary>
    Task<CharacterEffectDefinition?> GetEffectDefinitionAsync(
        int effectDefinitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an effect definition by name
    /// </summary>
    Task<CharacterEffectDefinition?> GetEffectDefinitionByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all effect definitions
    /// </summary>
    Task<IReadOnlyList<CharacterEffectDefinition>> GetAllEffectDefinitionsAsync(
        CancellationToken cancellationToken = default);
}
