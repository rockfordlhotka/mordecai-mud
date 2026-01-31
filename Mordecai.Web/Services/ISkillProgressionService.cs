using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing skill progression with anti-abuse mechanics.
/// Handles usage-based advancement with diminishing returns, cooldowns, and challenge multipliers.
/// </summary>
public interface ISkillProgressionService
{
    /// <summary>
    /// Logs a skill usage event and applies progression with all anti-abuse multipliers.
    /// </summary>
    /// <param name="characterId">The character using the skill</param>
    /// <param name="skillDefinitionId">The skill being used</param>
    /// <param name="usageType">Type of usage (routine, challenging, critical, etc.)</param>
    /// <param name="baseExperience">Base experience points before multipliers (default 1)</param>
    /// <param name="targetId">Optional target identifier for cooldown tracking (format: "type:id")</param>
    /// <param name="targetDifficulty">Optional difficulty rating for challenge multiplier calculation</param>
    /// <param name="actionSucceeded">Whether the skill action succeeded (failed actions get reduced XP)</param>
    /// <param name="context">Optional context string for logging</param>
    /// <param name="details">Optional details string for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing applied multipliers, final XP, and feedback message</returns>
    Task<SkillProgressionResult> LogUsageAsync(
        Guid characterId,
        int skillDefinitionId,
        SkillUsageType usageType,
        int baseExperience = 1,
        string? targetId = null,
        int? targetDifficulty = null,
        bool actionSucceeded = true,
        string? context = null,
        string? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current hourly usage count for a character's skill.
    /// </summary>
    Task<int> GetHourlyUsageCountAsync(
        Guid characterId,
        int skillDefinitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current daily usage count for a character's skill.
    /// </summary>
    Task<int> GetDailyUsageCountAsync(
        Guid characterId,
        int skillDefinitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a target cooldown is active for a character's skill use.
    /// </summary>
    /// <returns>True if cooldown is active and usage should not count</returns>
    Task<bool> IsTargetOnCooldownAsync(
        Guid characterId,
        int skillDefinitionId,
        string targetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the effective multiplier for a skill use without applying it.
    /// Useful for UI preview of expected progression.
    /// </summary>
    Task<decimal> CalculateEffectiveMultiplierAsync(
        Guid characterId,
        int skillDefinitionId,
        SkillUsageType usageType,
        string? targetId = null,
        int? targetDifficulty = null,
        bool actionSucceeded = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current progression settings.
    /// </summary>
    SkillProgressionSettings GetSettings();

    /// <summary>
    /// Cleans up old tracking data beyond the retention period.
    /// Should be called periodically by a background service.
    /// </summary>
    Task CleanupOldTrackingDataAsync(
        int hourlyRetentionHours = 24,
        int dailyRetentionDays = 7,
        int cooldownRetentionHours = 24,
        CancellationToken cancellationToken = default);
}
