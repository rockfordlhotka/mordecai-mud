using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mordecai.Web.Data;

/// <summary>
/// Tracks skill usage within rolling hourly windows for diminishing returns calculation.
/// Each record represents usage count for a specific skill during a 1-hour window.
/// </summary>
public class SkillUsageHourlyTracking
{
    [Key]
    public long Id { get; set; }

    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public int SkillDefinitionId { get; set; }

    /// <summary>
    /// Start time of the 1-hour tracking window (truncated to hour boundary)
    /// </summary>
    [Required]
    public DateTimeOffset WindowStartTime { get; set; }

    /// <summary>
    /// Number of skill uses counted in this window
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Last time this record was updated
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SkillDefinitionId))]
    public virtual SkillDefinition SkillDefinition { get; set; } = null!;
}

/// <summary>
/// Tracks cooldowns for skill usage against specific targets to prevent spam grinding.
/// A skill use only counts toward progression if sufficient time has passed since last counted use.
/// </summary>
public class SkillUsageTargetCooldown
{
    [Key]
    public long Id { get; set; }

    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public int SkillDefinitionId { get; set; }

    /// <summary>
    /// Identifier for the target (NPC ID, recipe ID, player ID, room ID, etc.)
    /// Format: "type:id" e.g., "npc:abc123", "recipe:iron_sword", "player:def456"
    /// </summary>
    [Required]
    [StringLength(150)]
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// When this skill use was last counted toward progression for this target
    /// </summary>
    [Required]
    public DateTimeOffset LastCountedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SkillDefinitionId))]
    public virtual SkillDefinition SkillDefinition { get; set; } = null!;
}

/// <summary>
/// Tracks daily skill usage for fresh learning bonus calculation
/// </summary>
public class SkillUsageDailyTracking
{
    [Key]
    public long Id { get; set; }

    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public int SkillDefinitionId { get; set; }

    /// <summary>
    /// The date (UTC) for this tracking record
    /// </summary>
    [Required]
    public DateOnly TrackingDate { get; set; }

    /// <summary>
    /// Number of skill uses counted on this day
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Last time this record was updated
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SkillDefinitionId))]
    public virtual SkillDefinition SkillDefinition { get; set; } = null!;
}

/// <summary>
/// Configuration settings for skill progression anti-abuse mechanics.
/// These can be stored in GameConfiguration or loaded from appsettings.
/// </summary>
public class SkillProgressionSettings
{
    /// <summary>
    /// First hourly threshold - uses below this get full multiplier
    /// </summary>
    public int HourlyUsageThreshold1 { get; set; } = 50;

    /// <summary>
    /// Multiplier for uses below first threshold (full value)
    /// </summary>
    public decimal HourlyMultiplier1 { get; set; } = 1.0m;

    /// <summary>
    /// Second hourly threshold - uses between threshold1 and this get reduced multiplier
    /// </summary>
    public int HourlyUsageThreshold2 { get; set; } = 100;

    /// <summary>
    /// Multiplier for uses between threshold1 and threshold2
    /// </summary>
    public decimal HourlyMultiplier2 { get; set; } = 0.5m;

    /// <summary>
    /// Third hourly threshold - uses between threshold2 and this get minimal multiplier
    /// </summary>
    public int HourlyUsageThreshold3 { get; set; } = 150;

    /// <summary>
    /// Multiplier for uses between threshold2 and threshold3
    /// </summary>
    public decimal HourlyMultiplier3 { get; set; } = 0.1m;

    /// <summary>
    /// Multiplier for uses beyond threshold3 (no progression)
    /// </summary>
    public decimal HourlyMultiplierBeyond { get; set; } = 0.0m;

    // Daily fresh learning settings
    public int DailyFreshUsageThreshold { get; set; } = 100;
    public decimal DailyFreshMultiplier { get; set; } = 1.5m;
    public int DailyFatigueThreshold { get; set; } = 200;
    public decimal DailyFatigueMultiplier { get; set; } = 0.5m;

    // Challenge rating multipliers
    public decimal ChallengeTrivialMultiplier { get; set; } = 0.1m;   // Differential -10 or lower
    public decimal ChallengeEasyMultiplier { get; set; } = 0.5m;      // Differential -9 to -5
    public decimal ChallengeAppropriateMultiplier { get; set; } = 1.0m; // Differential -4 to +4
    public decimal ChallengeDifficultMultiplier { get; set; } = 1.5m;  // Differential +5 to +9
    public decimal ChallengeOverwhelmingMultiplier { get; set; } = 0.5m; // Differential +10 or higher

    // Target cooldowns (seconds)
    public int CombatTargetCooldownSeconds { get; set; } = 30;
    public int SpellTargetCooldownSeconds { get; set; } = 20;
    public int CraftingRecipeCooldownSeconds { get; set; } = 60;
    public int SocialTargetCooldownSeconds { get; set; } = 120;

    // FAT-based penalties
    public decimal LowFatThresholdPercent { get; set; } = 0.25m; // 25%
    public decimal LowFatMultiplier { get; set; } = 0.5m;
    public decimal ZeroFatMultiplier { get; set; } = 0.0m;

    // Failed action penalty
    public decimal FailedActionMultiplier { get; set; } = 0.2m;
}

/// <summary>
/// Result of a skill progression calculation, including applied multipliers and feedback
/// </summary>
public class SkillProgressionResult
{
    /// <summary>
    /// Whether progression was applied (false if blocked by cooldown, FAT, etc.)
    /// </summary>
    public bool ProgressionApplied { get; set; }

    /// <summary>
    /// Base experience points before any multipliers
    /// </summary>
    public int BaseExperience { get; set; }

    /// <summary>
    /// Final experience points after all multipliers
    /// </summary>
    public int FinalExperience { get; set; }

    /// <summary>
    /// Combined multiplier from all sources
    /// </summary>
    public decimal TotalMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Multiplier from usage type (routine, challenging, critical, etc.)
    /// </summary>
    public decimal UsageTypeMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Multiplier from hourly diminishing returns
    /// </summary>
    public decimal HourlyMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Multiplier from daily fresh learning / fatigue
    /// </summary>
    public decimal DailyMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Multiplier from challenge rating
    /// </summary>
    public decimal ChallengeMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Multiplier from FAT status
    /// </summary>
    public decimal FatigueMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Whether the skill leveled up as a result
    /// </summary>
    public bool DidLevelUp { get; set; }

    /// <summary>
    /// New skill level (if leveled up)
    /// </summary>
    public int? NewLevel { get; set; }

    /// <summary>
    /// Reason progression was blocked (if not applied)
    /// </summary>
    public string? BlockedReason { get; set; }

    /// <summary>
    /// Feedback message for the player
    /// </summary>
    public string? FeedbackMessage { get; set; }
}
