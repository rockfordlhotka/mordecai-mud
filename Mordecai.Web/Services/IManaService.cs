using Mordecai.Game.Entities;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing character mana pools across magic schools
/// </summary>
public interface IManaService
{
    /// <summary>
    /// Gets a character's mana pool for a specific school, creating it if it doesn't exist
    /// </summary>
    Task<CharacterManaPool> GetOrCreateManaPoolAsync(
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mana pools for a character
    /// </summary>
    Task<IReadOnlyList<CharacterManaPool>> GetManaPoolsAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of all mana pools for a character including regen rates
    /// </summary>
    Task<CharacterManaSummary> GetManaSummaryAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to consume mana from a school's pool for casting
    /// </summary>
    /// <param name="characterId">Character consuming mana</param>
    /// <param name="school">Magic school to consume from</param>
    /// <param name="amount">Amount of mana to consume</param>
    /// <returns>Result indicating success/failure and new mana levels</returns>
    Task<ManaOperationResult> ConsumeManaAsync(
        Guid characterId,
        MagicSchool school,
        int amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a character has enough mana to cast (without consuming)
    /// </summary>
    Task<bool> HasEnoughManaAsync(
        Guid characterId,
        MagicSchool school,
        int amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds mana to a school's pool (from gathering or items)
    /// </summary>
    /// <param name="characterId">Character receiving mana</param>
    /// <param name="school">Magic school to add to</param>
    /// <param name="amount">Amount of mana to add (capped at max)</param>
    /// <returns>Result indicating how much was actually added</returns>
    Task<ManaOperationResult> AddManaAsync(
        Guid characterId,
        MagicSchool school,
        int amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes passive mana regeneration for a character
    /// Called periodically by background service
    /// </summary>
    /// <param name="characterId">Character to regenerate mana for</param>
    /// <returns>Total mana regenerated across all pools</returns>
    Task<int> ProcessRegenAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes passive mana regeneration for all characters
    /// Called periodically by background service
    /// </summary>
    /// <returns>Number of characters processed</returns>
    Task<int> ProcessAllRegenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the mana regeneration rate per minute for a school
    /// Formula: School Recovery Skill + (WIL / 2)
    /// </summary>
    Task<decimal> GetRegenRateAsync(
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the maximum mana for a school
    /// Formula: Base (10) + WIL + School Recovery Skill
    /// </summary>
    Task<int> CalculateMaxManaAsync(
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a character's max mana for all pools (call after WIL or skill changes)
    /// </summary>
    Task UpdateMaxManaAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current mana for a specific school
    /// </summary>
    Task<int> GetCurrentManaAsync(
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets mana to a specific value (for admin/testing)
    /// </summary>
    Task SetManaAsync(
        Guid characterId,
        MagicSchool school,
        int amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores all mana pools to full (for resting, items, etc.)
    /// </summary>
    Task RestoreAllManaAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);
}
