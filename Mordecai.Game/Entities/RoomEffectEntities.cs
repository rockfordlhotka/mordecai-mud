using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mordecai.Game.Entities;

/// <summary>
/// Represents room effect definitions (templates for effects that can be applied to rooms)
/// </summary>
public class RoomEffectDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Broad category of effect (Environmental, Elemental, Magical, Movement, Combat, Sensory)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EffectType { get; set; } = string.Empty;

    /// <summary>
    /// Specific category within effect type (Fire, Ice, Poison, Blessing, Curse, Illusion, Physical)
    /// </summary>
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Icon name for UI display
    /// </summary>
    [StringLength(50)]
    public string? IconName { get; set; }

    /// <summary>
    /// Effect color for UI theming (hex color code)
    /// </summary>
    [StringLength(20)]
    public string? EffectColor { get; set; }

    /// <summary>
    /// Whether this effect is visible to players or hidden
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Skill required to detect hidden effects
    /// </summary>
    public int? DetectionSkillId { get; set; }

    /// <summary>
    /// Difficulty level for skill check to detect hidden effects
    /// </summary>
    public decimal DetectionDifficulty { get; set; } = 0;

    /// <summary>
    /// Default duration in seconds (0 = permanent until removed)
    /// </summary>
    public int DefaultDuration { get; set; } = 0;

    /// <summary>
    /// Default intensity/strength level (1.0 = normal)
    /// </summary>
    public decimal DefaultIntensity { get; set; } = 1.0m;

    /// <summary>
    /// Whether multiple instances can exist in the same room
    /// </summary>
    public bool IsStackable { get; set; } = false;

    /// <summary>
    /// Maximum number of stacks if stackable
    /// </summary>
    public int MaxStacks { get; set; } = 1;

    /// <summary>
    /// Interval in seconds between periodic effect applications (0 = no periodic effects)
    /// </summary>
    public int TickInterval { get; set; } = 0;

    /// <summary>
    /// JSON array of removal methods: ["time", "dispel", "manual", "zone_reset"]
    /// </summary>
    [StringLength(500)]
    public string RemovalMethods { get; set; } = "[\"time\"]";

    /// <summary>
    /// User who created this effect definition
    /// </summary>
    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<RoomEffectImpact> Impacts { get; set; } = new List<RoomEffectImpact>();
    public virtual ICollection<RoomEffect> ActiveEffects { get; set; } = new List<RoomEffect>();
}

/// <summary>
/// Represents room effect impacts on gameplay mechanics
/// </summary>
public class RoomEffectImpact
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RoomEffectDefinitionId { get; set; }

    /// <summary>
    /// Type of impact on gameplay (MovementPrevention, PeriodicDamage, SkillBonus, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ImpactType { get; set; } = string.Empty;

    /// <summary>
    /// When this impact applies (AllOccupants, EntryTrigger, ExitTrigger, PeriodicTrigger, ActionTrigger)
    /// </summary>
    [Required]
    [StringLength(30)]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Specific skill affected by this impact (nullable)
    /// </summary>
    public int? TargetSkillId { get; set; }

    /// <summary>
    /// Specific attribute affected (Health, Mana, MovementSpeed, Vision, Communication)
    /// </summary>
    [StringLength(30)]
    public string? TargetAttribute { get; set; }

    /// <summary>
    /// Numeric value of the effect
    /// </summary>
    public decimal ImpactValue { get; set; } = 0;

    /// <summary>
    /// Formula for complex calculations (e.g., "intensity * 5")
    /// </summary>
    [StringLength(200)]
    public string? ImpactFormula { get; set; }

    /// <summary>
    /// Whether the impact value is a percentage or flat value
    /// </summary>
    public bool IsPercentage { get; set; } = false;

    /// <summary>
    /// Type of damage for damage effects (Fire, Ice, Poison, Physical, etc.)
    /// </summary>
    [StringLength(30)]
    public string? DamageType { get; set; }

    /// <summary>
    /// Skill that provides resistance to this impact
    /// </summary>
    public int? ResistanceSkillId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(RoomEffectDefinitionId))]
    public virtual RoomEffectDefinition RoomEffectDefinition { get; set; } = null!;
}

/// <summary>
/// Represents active room effects (instances of effects currently affecting rooms)
/// </summary>
public class RoomEffect
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RoomId { get; set; }

    [Required]
    public int RoomEffectDefinitionId { get; set; }

    /// <summary>
    /// Source type that created this effect (Spell, Item, NPC, Environmental, System, Admin)
    /// </summary>
    [Required]
    [StringLength(30)]
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the source (character who cast spell, item ID, NPC ID, etc.)
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Display name of the source for UI
    /// </summary>
    [StringLength(100)]
    public string? SourceName { get; set; }

    /// <summary>
    /// Character who created this effect (if applicable)
    /// </summary>
    public Guid? CasterCharacterId { get; set; }

    /// <summary>
    /// Current number of stacks if stackable
    /// </summary>
    public int StackCount { get; set; } = 1;

    /// <summary>
    /// Multiplier for effect strength
    /// </summary>
    public decimal Intensity { get; set; } = 1.0m;

    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this effect expires (null for permanent effects)
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Last time this effect applied its periodic impact
    /// </summary>
    public DateTimeOffset? LastTickTime { get; set; }

    /// <summary>
    /// JSON field for effect-specific data and parameters
    /// </summary>
    [StringLength(2000)]
    public string? CustomData { get; set; }

    public bool IsActive { get; set; } = true;

    // Calculated property for UI
    [NotMapped]
    public int? RemainingDurationSeconds
    {
        get
        {
            if (EndTime == null) return null;
            var remaining = EndTime.Value - DateTimeOffset.UtcNow;
            return remaining.TotalSeconds > 0 ? (int)remaining.TotalSeconds : 0;
        }
    }

    // Navigation properties
    [ForeignKey(nameof(RoomId))]
    public virtual Room Room { get; set; } = null!;

    [ForeignKey(nameof(RoomEffectDefinitionId))]
    public virtual RoomEffectDefinition RoomEffectDefinition { get; set; } = null!;

    public virtual ICollection<RoomEffectApplicationLog> ApplicationLogs { get; set; } = new List<RoomEffectApplicationLog>();
}

/// <summary>
/// Represents log of room effect applications (for debugging and statistics)
/// </summary>
public class RoomEffectApplicationLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RoomEffectId { get; set; }

    /// <summary>
    /// Character affected by this application (nullable for room-wide effects)
    /// </summary>
    public Guid? CharacterId { get; set; }

    /// <summary>
    /// Type of application (Entry, Exit, Periodic, Action, Resistance)
    /// </summary>
    [Required]
    [StringLength(30)]
    public string ApplicationType { get; set; } = string.Empty;

    /// <summary>
    /// Type of impact applied (matches RoomEffectImpacts.ImpactType)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ImpactType { get; set; } = string.Empty;

    /// <summary>
    /// Actual value applied after calculations
    /// </summary>
    public decimal ImpactValue { get; set; } = 0;

    /// <summary>
    /// Dice roll result if resistance was attempted
    /// </summary>
    public decimal? ResistanceRoll { get; set; }

    /// <summary>
    /// Whether resistance check succeeded
    /// </summary>
    public bool? ResistanceSuccess { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// JSON field for additional context and details
    /// </summary>
    [StringLength(1000)]
    public string? Details { get; set; }

    // Navigation properties
    [ForeignKey(nameof(RoomEffectId))]
    public virtual RoomEffect RoomEffect { get; set; } = null!;
}