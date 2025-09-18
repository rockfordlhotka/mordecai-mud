using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mordecai.Web.Data;

/// <summary>
/// Defines available skills in the game
/// </summary>
public class SkillDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of skill: AttributeSkill, WeaponSkill, SpellSkill, CraftingSkill, etc.
    /// </summary>
    [Required]
    [StringLength(30)]
    public string SkillType { get; set; } = string.Empty;

    /// <summary>
    /// For attribute skills, this is the related attribute name (Physicality, Dodge, etc.)
    /// For other skills, this can be null or used for grouping
    /// </summary>
    [StringLength(30)]
    public string? RelatedAttribute { get; set; }

    /// <summary>
    /// For spell skills, this groups spells by magic school
    /// </summary>
    [StringLength(30)]
    public string? MagicSchool { get; set; }

    /// <summary>
    /// Whether this skill is automatically given to new characters
    /// </summary>
    public bool IsStartingSkill { get; set; } = false;

    /// <summary>
    /// Base experience required to advance from level 0 to 1
    /// </summary>
    public int BaseExperienceRequired { get; set; } = 100;

    /// <summary>
    /// Multiplier for experience requirements at higher levels
    /// </summary>
    public decimal LevelMultiplier { get; set; } = 1.5m;

    /// <summary>
    /// Maximum level this skill can reach (0 = no limit)
    /// </summary>
    public int MaxLevel { get; set; } = 0;

    /// <summary>
    /// Whether this skill is currently active/available
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User who created this skill definition
    /// </summary>
    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public virtual ICollection<CharacterSkill> CharacterSkills { get; set; } = new List<CharacterSkill>();

    /// <summary>
    /// Calculates experience required to reach a specific level
    /// </summary>
    public int CalculateExperienceRequired(int targetLevel)
    {
        if (targetLevel <= 0) return 0;
        
        var totalExperience = 0;
        for (int level = 1; level <= targetLevel; level++)
        {
            var experienceForLevel = (int)(BaseExperienceRequired * Math.Pow((double)LevelMultiplier, level - 1));
            totalExperience += experienceForLevel;
        }
        return totalExperience;
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

    // Navigation properties
    [ForeignKey(nameof(CharacterId))]
    public virtual Character Character { get; set; } = null!;

    [ForeignKey(nameof(SkillDefinitionId))]
    public virtual SkillDefinition SkillDefinition { get; set; } = null!;

    /// <summary>
    /// Calculates the Ability Score: Attribute - 5 + Level
    /// For attribute skills, uses the character's attribute value
    /// For other skills, returns just the level (since no direct attribute relationship)
    /// </summary>
    public int CalculateAbilityScore()
    {
        if (SkillDefinition.SkillType == "AttributeSkill" && !string.IsNullOrEmpty(SkillDefinition.RelatedAttribute))
        {
            var attributeValue = GetAttributeValue(SkillDefinition.RelatedAttribute);
            return attributeValue - 5 + Level;
        }
        
        // For non-attribute skills, just return the level as the ability score
        return Level;
    }

    /// <summary>
    /// Gets the character's attribute value based on the attribute name
    /// </summary>
    private int GetAttributeValue(string attributeName) => attributeName switch
    {
        "Physicality" => Character.Physicality,
        "Dodge" => Character.Dodge,
        "Drive" => Character.Drive,
        "Reasoning" => Character.Reasoning,
        "Awareness" => Character.Awareness,
        "Focus" => Character.Focus,
        "Bearing" => Character.Bearing,
        _ => 10 // Default fallback
    };

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
        if (SkillDefinition.MaxLevel > 0 && Level >= SkillDefinition.MaxLevel)
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