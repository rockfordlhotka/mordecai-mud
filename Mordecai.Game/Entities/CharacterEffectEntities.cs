using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mordecai.Game.Entities;

/// <summary>
/// Types of character effects
/// </summary>
public enum CharacterEffectType
{
    /// <summary>
    /// Long-term injury: -2 AV per wound, 1 FAT/6 seconds, heals 1 per 4 hours naturally
    /// </summary>
    Wound = 0,

    /// <summary>
    /// Positive temporary modifier (e.g., +2 Physicality, +1 skill)
    /// </summary>
    Buff = 1,

    /// <summary>
    /// Negative temporary modifier (e.g., -2 Dodge, slow)
    /// </summary>
    Debuff = 2,

    /// <summary>
    /// Damage over Time (poison, burning, bleeding)
    /// </summary>
    DamageOverTime = 3,

    /// <summary>
    /// Heal over Time (regeneration, blessing)
    /// </summary>
    HealOverTime = 4,

    /// <summary>
    /// Special status conditions (invisible, silenced, stunned, rooted)
    /// </summary>
    StatusEffect = 5
}

/// <summary>
/// Types of impacts an effect can have on a character
/// </summary>
public enum CharacterEffectImpactType
{
    /// <summary>
    /// Modifies a core attribute (Physicality, Dodge, Drive, etc.)
    /// </summary>
    ModifyAttribute = 0,

    /// <summary>
    /// Modifies a specific skill level
    /// </summary>
    ModifySkill = 1,

    /// <summary>
    /// Flat modifier to Attack Value (AS) for all attacks
    /// </summary>
    ModifyAttackValue = 2,

    /// <summary>
    /// Flat modifier to Success Value (SV) for defense
    /// </summary>
    ModifyDefenseValue = 3,

    /// <summary>
    /// Periodic fatigue damage
    /// </summary>
    PeriodicFatigueDamage = 4,

    /// <summary>
    /// Periodic vitality damage
    /// </summary>
    PeriodicVitalityDamage = 5,

    /// <summary>
    /// Periodic fatigue healing
    /// </summary>
    PeriodicFatigueHealing = 6,

    /// <summary>
    /// Periodic vitality healing
    /// </summary>
    PeriodicVitalityHealing = 7,

    /// <summary>
    /// Modifies maximum fatigue pool
    /// </summary>
    ModifyMaxFatigue = 8,

    /// <summary>
    /// Modifies maximum vitality pool
    /// </summary>
    ModifyMaxVitality = 9,

    /// <summary>
    /// Prevents movement
    /// </summary>
    PreventMovement = 10,

    /// <summary>
    /// Prevents spellcasting
    /// </summary>
    PreventSpellcasting = 11,

    /// <summary>
    /// Prevents all actions (stunned)
    /// </summary>
    PreventActions = 12,

    /// <summary>
    /// Grants invisibility
    /// </summary>
    Invisibility = 13,

    /// <summary>
    /// Modifies damage dealt (percentage)
    /// </summary>
    ModifyDamageDealt = 14,

    /// <summary>
    /// Modifies damage received (percentage)
    /// </summary>
    ModifyDamageReceived = 15
}

/// <summary>
/// Body locations for wound effects
/// </summary>
public enum BodyLocation
{
    General = 0,
    Head = 1,
    Torso = 2,
    LeftArm = 3,
    RightArm = 4,
    LeftLeg = 5,
    RightLeg = 6
}

/// <summary>
/// Template definition for character effects (wounds, buffs, debuffs, etc.)
/// </summary>
public class CharacterEffectDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of effect (Wound, Buff, Debuff, DoT, HoT, StatusEffect)
    /// </summary>
    public CharacterEffectType EffectType { get; set; }

    /// <summary>
    /// Default duration in seconds. 0 = permanent until removed/healed.
    /// </summary>
    public int DefaultDurationSeconds { get; set; } = 0;

    /// <summary>
    /// Default intensity/strength (1.0 = normal, 2.0 = double effect)
    /// </summary>
    public decimal DefaultIntensity { get; set; } = 1.0m;

    /// <summary>
    /// Whether multiple instances of this effect can stack on the same character
    /// </summary>
    public bool IsStackable { get; set; } = false;

    /// <summary>
    /// Maximum number of stacks if stackable
    /// </summary>
    public int MaxStacks { get; set; } = 1;

    /// <summary>
    /// Interval in seconds between periodic effect applications. 0 = no periodic effects.
    /// </summary>
    public int TickIntervalSeconds { get; set; } = 0;

    /// <summary>
    /// Whether this effect is visible to the affected character
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Whether this effect is visible to other players
    /// </summary>
    public bool IsVisibleToOthers { get; set; } = true;

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
    /// Whether this effect can be dispelled by magic
    /// </summary>
    public bool IsDispellable { get; set; } = true;

    /// <summary>
    /// Skill ID required to dispel this effect (null = any dispel works)
    /// </summary>
    public int? DispelSkillId { get; set; }

    /// <summary>
    /// Difficulty to dispel this effect
    /// </summary>
    public int DispelDifficulty { get; set; } = 0;

    /// <summary>
    /// System-defined effect that cannot be modified by admins
    /// </summary>
    public bool IsSystemEffect { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<CharacterEffectImpact> Impacts { get; set; } = new List<CharacterEffectImpact>();
    public virtual ICollection<CharacterEffect> ActiveEffects { get; set; } = new List<CharacterEffect>();
}

/// <summary>
/// Defines how an effect impacts a character's stats or abilities
/// </summary>
public class CharacterEffectImpact
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterEffectDefinitionId { get; set; }

    /// <summary>
    /// Type of impact (ModifyAttribute, ModifySkill, PeriodicDamage, etc.)
    /// </summary>
    public CharacterEffectImpactType ImpactType { get; set; }

    /// <summary>
    /// Target attribute name for ModifyAttribute impacts (Physicality, Dodge, Drive, Reasoning, Awareness, Focus, Bearing)
    /// </summary>
    [StringLength(50)]
    public string? TargetAttribute { get; set; }

    /// <summary>
    /// Target skill definition ID for ModifySkill impacts
    /// </summary>
    public int? TargetSkillDefinitionId { get; set; }

    /// <summary>
    /// Modifier value (+2, -3, etc.) or damage/healing amount per tick
    /// </summary>
    public decimal ModifierValue { get; set; }

    /// <summary>
    /// If true, ModifierValue is a percentage (e.g., 0.10 = +10%)
    /// If false, ModifierValue is a flat amount
    /// </summary>
    public bool IsPercentage { get; set; } = false;

    /// <summary>
    /// Damage type for periodic damage effects (Bashing, Cutting, Piercing, Fire, Cold, etc.)
    /// </summary>
    [StringLength(30)]
    public string? DamageType { get; set; }

    /// <summary>
    /// Whether this impact scales with the effect's intensity
    /// </summary>
    public bool ScalesWithIntensity { get; set; } = true;

    /// <summary>
    /// Order in which impacts are applied (lower = first)
    /// </summary>
    public int ApplyOrder { get; set; } = 0;

    // Navigation properties
    public virtual CharacterEffectDefinition Definition { get; set; } = null!;
}

/// <summary>
/// Active effect instance on a character
/// </summary>
public class CharacterEffect
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Character affected by this effect
    /// </summary>
    [Required]
    public Guid CharacterId { get; set; }

    /// <summary>
    /// Effect definition being applied
    /// </summary>
    [Required]
    public int EffectDefinitionId { get; set; }

    /// <summary>
    /// Character who applied this effect (null for environmental/system effects)
    /// </summary>
    public Guid? SourceCharacterId { get; set; }

    /// <summary>
    /// NPC who applied this effect (null if from player or system)
    /// </summary>
    public Guid? SourceNpcId { get; set; }

    /// <summary>
    /// Spell skill that caused this effect (null for non-spell effects)
    /// </summary>
    public int? SourceSpellSkillId { get; set; }

    /// <summary>
    /// When the effect was applied
    /// </summary>
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the effect expires. Null = permanent until removed.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Last time periodic effects were applied
    /// </summary>
    public DateTimeOffset? LastTickAt { get; set; }

    /// <summary>
    /// Current number of stacks for stackable effects
    /// </summary>
    public int CurrentStacks { get; set; } = 1;

    /// <summary>
    /// Intensity of this effect instance (may differ from definition default)
    /// </summary>
    public decimal Intensity { get; set; } = 1.0m;

    /// <summary>
    /// Body location for wound effects
    /// </summary>
    public BodyLocation? BodyLocation { get; set; }

    /// <summary>
    /// Whether this effect is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the effect was removed (for history tracking)
    /// </summary>
    public DateTimeOffset? RemovedAt { get; set; }

    /// <summary>
    /// How the effect was removed (expired, dispelled, healed, etc.)
    /// </summary>
    [StringLength(50)]
    public string? RemovalReason { get; set; }

    // Navigation properties
    public virtual Character Character { get; set; } = null!;
    public virtual Character? SourceCharacter { get; set; }
    public virtual CharacterEffectDefinition EffectDefinition { get; set; } = null!;
}

/// <summary>
/// Result of applying an effect to a character
/// </summary>
public class EffectApplicationResult
{
    public bool Success { get; set; }
    public CharacterEffect? Effect { get; set; }
    public string? Message { get; set; }
    public bool WasStacked { get; set; }
    public int NewStackCount { get; set; }
    public bool WasRefreshed { get; set; }
}

/// <summary>
/// Summary of active effects on a character for combat/stat calculations
/// </summary>
public class CharacterEffectSummary
{
    public Guid CharacterId { get; set; }

    /// <summary>
    /// Total AV modifier from all effects (including wounds)
    /// </summary>
    public int TotalAttackValueModifier { get; set; }

    /// <summary>
    /// Total SV modifier from all effects
    /// </summary>
    public int TotalDefenseValueModifier { get; set; }

    /// <summary>
    /// Attribute modifiers (key = attribute name, value = total modifier)
    /// </summary>
    public Dictionary<string, int> AttributeModifiers { get; set; } = new();

    /// <summary>
    /// Skill modifiers (key = skill definition ID, value = total modifier)
    /// </summary>
    public Dictionary<int, int> SkillModifiers { get; set; } = new();

    /// <summary>
    /// Max FAT modifier from effects
    /// </summary>
    public int MaxFatigueModifier { get; set; }

    /// <summary>
    /// Max VIT modifier from effects
    /// </summary>
    public int MaxVitalityModifier { get; set; }

    /// <summary>
    /// Damage dealt modifier (percentage, e.g., 0.10 = +10%)
    /// </summary>
    public decimal DamageDealtModifier { get; set; }

    /// <summary>
    /// Damage received modifier (percentage, e.g., -0.10 = -10% damage taken)
    /// </summary>
    public decimal DamageReceivedModifier { get; set; }

    /// <summary>
    /// Total wound count
    /// </summary>
    public int WoundCount { get; set; }

    /// <summary>
    /// Wounds by body location
    /// </summary>
    public Dictionary<BodyLocation, int> WoundsByLocation { get; set; } = new();

    /// <summary>
    /// Whether character can move
    /// </summary>
    public bool CanMove { get; set; } = true;

    /// <summary>
    /// Whether character can cast spells
    /// </summary>
    public bool CanCastSpells { get; set; } = true;

    /// <summary>
    /// Whether character can take any actions
    /// </summary>
    public bool CanAct { get; set; } = true;

    /// <summary>
    /// Whether character is invisible
    /// </summary>
    public bool IsInvisible { get; set; }

    /// <summary>
    /// List of active effect names for display
    /// </summary>
    public List<string> ActiveEffectNames { get; set; } = new();
}
