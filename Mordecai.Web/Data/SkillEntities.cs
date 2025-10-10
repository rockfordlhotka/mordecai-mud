using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mordecai.Web.Data;

/// <summary>
/// Represents a category of skills for organization and shared properties
/// </summary>
public class SkillCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Default base cost for skills in this category (can be overridden per skill)
    /// </summary>
    public int DefaultBaseCost { get; set; } = 25;

    /// <summary>
    /// Default multiplier for skills in this category (can be overridden per skill)
    /// </summary>
    public decimal DefaultMultiplier { get; set; } = 2.2m;

    /// <summary>
    /// Whether skills in this category advance passively over time
    /// </summary>
    public bool AllowsPassiveAdvancement { get; set; } = false;

    /// <summary>
    /// Whether skills in this category can be taught by other players
    /// </summary>
    public bool AllowsTeaching { get; set; } = true;

    /// <summary>
    /// Display order for UI purposes
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<SkillDefinition> Skills { get; set; } = new List<SkillDefinition>();
}

/// <summary>
/// Types of skill usage for different advancement rates
/// </summary>
public enum SkillUsageType
{
    /// <summary>
    /// Standard skill application under normal conditions (1.0x multiplier)
    /// </summary>
    RoutineUse = 0,

    /// <summary>
    /// Skill use under difficult circumstances (1.5x multiplier)
    /// </summary>
    ChallengingUse = 1,

    /// <summary>
    /// Exceptional skill performance - critical success (2.0x multiplier)
    /// </summary>
    CriticalSuccess = 2,

    /// <summary>
    /// Instructing other players (0.8x multiplier)
    /// </summary>
    TeachingOthers = 3,

    /// <summary>
    /// Deliberate practice in safe conditions (0.5x multiplier)
    /// </summary>
    TrainingPractice = 4
}

/// <summary>
/// Detailed log of skill usage events for analytics and validation
/// </summary>
public class SkillUsageLog
{
    [Key]
    public long Id { get; set; }

    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public int SkillDefinitionId { get; set; }

    [Required]
    public SkillUsageType UsageType { get; set; }

    /// <summary>
    /// Base usage points before multipliers
    /// </summary>
    public int BaseUsagePoints { get; set; }

    /// <summary>
    /// Multiplier applied based on usage type
    /// </summary>
    public decimal UsageMultiplier { get; set; }

    /// <summary>
    /// Final usage points added (BaseUsagePoints * UsageMultiplier)
    /// </summary>
    public int FinalUsagePoints { get; set; }

    /// <summary>
    /// Character's level in this skill before this usage
    /// </summary>
    public int SkillLevelBefore { get; set; }

    /// <summary>
    /// Character's level in this skill after this usage
    /// </summary>
    public int SkillLevelAfter { get; set; }

    /// <summary>
    /// Whether this usage resulted in a skill level advancement
    /// </summary>
    public bool DidAdvance { get; set; } = false;

    /// <summary>
    /// Context or reason for the skill usage (combat, crafting, etc.)
    /// </summary>
    [StringLength(100)]
    public string? Context { get; set; }

    /// <summary>
    /// Additional details about the usage event
    /// </summary>
    [StringLength(500)]
    public string? Details { get; set; }

    public DateTimeOffset UsedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SkillDefinitionId))]
    public virtual SkillDefinition SkillDefinition { get; set; } = null!;

    /// <summary>
    /// Gets the usage multiplier for a given usage type
    /// </summary>
    public static decimal GetUsageMultiplier(SkillUsageType usageType)
    {
        return usageType switch
        {
            SkillUsageType.RoutineUse => 1.0m,
            SkillUsageType.ChallengingUse => 1.5m,
            SkillUsageType.CriticalSuccess => 2.0m,
            SkillUsageType.TeachingOthers => 0.8m,
            SkillUsageType.TrainingPractice => 0.5m,
            _ => 1.0m
        };
    }
}

/// <summary>
/// Defines available skills in the game
/// </summary>
public class SkillDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of skill: CoreAttribute, WeaponSkill, SpellSkill, CraftingSkill, etc.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string SkillType { get; set; } = string.Empty;

    /// <summary>
    /// Usage events required to advance from level 0 to level 1
    /// </summary>
    public int BaseCost { get; set; } = 25;

    /// <summary>
    /// Multiplier applied to calculate cost for each subsequent level
    /// Cost(N ? N+1) = BaseCost � (Multiplier^N)
    /// </summary>
    public decimal Multiplier { get; set; } = 2.2m;

    /// <summary>
    /// Related attribute that influences this skill (required)
    /// Maps to Character attribute properties: STR, DEX, END, INT, ITT, WIL, PHY
    /// </summary>
    [Required]
    [StringLength(50)]
    public string RelatedAttribute { get; set; } = string.Empty;

    /// <summary>
    /// For spell skills - the magic school they belong to
    /// </summary>
    [StringLength(50)]
    public string? MagicSchool { get; set; }

    /// <summary>
    /// Mana cost for spell skills (per cast)
    /// </summary>
    public int? ManaCost { get; set; }

    /// <summary>
    /// Cooldown in seconds between skill uses
    /// </summary>
    public decimal CooldownSeconds { get; set; } = 0;

    /// <summary>
    /// Whether this skill can advance passively over time
    /// </summary>
    public bool AllowsPassiveAdvancement { get; set; } = false;

    /// <summary>
    /// Whether this skill can be taught by other players
    /// </summary>
    public bool AllowsTeaching { get; set; } = true;

    /// <summary>
    /// Whether this skill uses exploding dice (4dF+) for resolution
    /// </summary>
    public bool UsesExplodingDice { get; set; } = false;

    /// <summary>
    /// Maximum practical level for this skill (for balance purposes)
    /// 0 = no limit, typically 10-15 for most skills
    /// </summary>
    public int MaxPracticalLevel { get; set; } = 10;

    /// <summary>
    /// Whether all characters start with this skill
    /// </summary>
    public bool IsStartingSkill { get; set; } = false;

    /// <summary>
    /// Display order within category for UI purposes
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// JSON field for skill-specific properties and configuration
    /// </summary>
    [StringLength(2000)]
    public string? CustomProperties { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CategoryId))]
    public virtual SkillCategory Category { get; set; } = null!;

    public virtual ICollection<CharacterSkill> CharacterSkills { get; set; } = new List<CharacterSkill>();

    /// <summary>
    /// Calculates the usage cost required to advance from the specified level to the next
    /// </summary>
    public int CalculateUsageCostForLevel(int currentLevel)
    {
        if (currentLevel < 0) return BaseCost;
        return (int)Math.Ceiling(BaseCost * Math.Pow((double)Multiplier, currentLevel));
    }

    /// <summary>
    /// Calculates the total usage points required to reach the specified level from 0
    /// </summary>
    public int CalculateTotalUsageForLevel(int targetLevel)
    {
        if (targetLevel <= 0) return 0;
        
        int total = 0;
        for (int level = 0; level < targetLevel; level++)
        {
            total += CalculateUsageCostForLevel(level);
        }
        return total;
    }

    /// <summary>
    /// Calculates the current skill level based on total usage points
    /// </summary>
    public int CalculateCurrentLevel(int totalUsagePoints)
    {
        if (totalUsagePoints <= 0) return 0;
        
        int level = 0;
        int usedPoints = 0;
        
        while (usedPoints < totalUsagePoints)
        {
            int costForNextLevel = CalculateUsageCostForLevel(level);
            if (usedPoints + costForNextLevel > totalUsagePoints)
                break;
            
            usedPoints += costForNextLevel;
            level++;
        }
        
        return level;
    }

    /// <summary>
    /// Calculates experience required to reach a specific level (legacy method for compatibility)
    /// </summary>
    public int CalculateExperienceRequired(int targetLevel)
    {
        return CalculateTotalUsageForLevel(targetLevel);
    }
}

/// <summary>
/// Tracks a character's progress in a specific skill
/// </summary>
public class CharacterSkill
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public int SkillDefinitionId { get; set; }

    /// <summary>
    /// Current skill level (0-10+ depending on skill definition)
    /// </summary>
    public int Level { get; set; } = 0;

    /// <summary>
    /// Current experience points in this skill
    /// </summary>
    public int Experience { get; set; } = 0;

    /// <summary>
    /// Last time this skill was used (for tracking usage patterns)
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Total number of times this skill has been used
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// When this character first learned this skill
    /// </summary>
    public DateTimeOffset LearnedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties - NOTE: Character reference removed to avoid circular dependency
    // Character should be accessed through the DbContext if needed

    [ForeignKey(nameof(SkillDefinitionId))]
    public virtual SkillDefinition SkillDefinition { get; set; } = null!;


    /// <summary>
    /// Calculates the ability score using the owning character's primary attribute for this skill.
    /// AS = attribute value + skill level - 5 (minimum of 0).
    /// </summary>
    /// <param name="character">The character owning this skill.</param>
    /// <returns>Ability score for the skill.</returns>
    /// <exception cref="ArgumentNullException">Thrown when character is null.</exception>
    public int CalculateAbilityScore(Mordecai.Game.Entities.Character character)
    {
        if (character is null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        var relatedAttribute = SkillDefinition?.RelatedAttribute;
        var attributeValue = !string.IsNullOrWhiteSpace(relatedAttribute)
            ? character.GetAttributeValue(relatedAttribute!)
            : 10;

        var abilityScore = attributeValue + Level - 5;
        return Math.Max(0, abilityScore);
    }

    /// <summary>
    /// Calculates the effective skill level including any bonuses
    /// </summary>
    public int CalculateEffectiveLevel()
    {
        // Base level from skill progression
        int effectiveLevel = Level;
        
        // TODO: Add attribute bonuses, equipment bonuses, temporary effects, etc.
        // For now, just return the base level
        
        return Math.Max(0, effectiveLevel);
    }

    /// <summary>
    /// Calculates experience required to reach the next level
    /// </summary>
    public int ExperienceToNextLevel()
    {
        var nextLevelRequired = SkillDefinition.CalculateExperienceRequired(Level + 1);
        return Math.Max(0, nextLevelRequired - Experience);
    }

    /// <summary>
    /// Checks if the character can level up this skill
    /// </summary>
    public bool CanLevelUp()
    {
        if (SkillDefinition.MaxPracticalLevel > 0 && Level >= SkillDefinition.MaxPracticalLevel)
            return false;
            
        return ExperienceToNextLevel() == 0;
    }

    /// <summary>
    /// Adds experience and handles level ups
    /// </summary>
    /// <returns>True if the character leveled up</returns>
    public bool AddExperience(int experienceGained)
    {
        Experience += experienceGained;
        UsageCount++;
        LastUsedAt = DateTimeOffset.UtcNow;

        var leveledUp = false;
        
        // Check for level ups (can potentially level multiple times)
        while (CanLevelUp() && ExperienceToNextLevel() == 0)
        {
            Level++;
            leveledUp = true;
        }

        return leveledUp;
    }
}