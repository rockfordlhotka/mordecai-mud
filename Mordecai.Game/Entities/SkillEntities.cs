using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mordecai.Game.Entities;

/// <summary>
/// Represents a player character in the game
/// </summary>
public class Character
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty; // FK to AspNetUsers

    [StringLength(30)]
    public string Species { get; set; } = "Human";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastPlayedAt { get; set; }

    // Core Attributes (the raw stats rolled with 4dF + species modifiers)
    public int Physicality { get; set; } = 10; // STR - Physical strength and power
    public int Dodge { get; set; } = 10;       // DEX - Agility and evasion ability
    public int Drive { get; set; } = 10;       // END - Endurance and stamina  
    public int Reasoning { get; set; } = 10;   // INT - Intelligence and logical thinking
    public int Awareness { get; set; } = 10;   // ITT - Intuition and perception
    public int Focus { get; set; } = 10;       // WIL - Willpower and mental concentration
    public int Bearing { get; set; } = 10;     // PHY - Physical beauty and social presence

    // Health pools (current values)
    public int CurrentFatigue { get; set; }
    public int CurrentVitality { get; set; }

    /// <summary>
    /// The most recent time passive fatigue recovery was applied.
    /// </summary>
    public DateTimeOffset? LastFatigueRegenAt { get; set; }

    /// <summary>
    /// The most recent time passive vitality recovery was applied.
    /// </summary>
    public DateTimeOffset? LastVitalityRegenAt { get; set; }

    // Pending damage/healing pools
    public int PendingFatigueDamage { get; set; } = 0;
    public int PendingVitalityDamage { get; set; } = 0;

    // Currency (stored as individual coin counts)
    // Total value in copper = Copper + (Silver * 20) + (Gold * 400) + (Platinum * 8000)
    public int CopperCoins { get; set; } = 0;
    public int SilverCoins { get; set; } = 0;
    public int GoldCoins { get; set; } = 0;
    public int PlatinumCoins { get; set; } = 0;

    // Current location
    public int? CurrentRoomId { get; set; }

    // Navigation properties
    public virtual ICollection<CharacterSkill> Skills { get; set; } = new List<CharacterSkill>();
    public virtual Room? CurrentRoom { get; set; }

    // Calculated Properties (not stored in database)
    [NotMapped]
    public int MaxFatigue => Math.Max(1, (Drive * 2) - 5);

    [NotMapped]
    public int MaxVitality => Math.Max(1, (Physicality + Drive) - 5);

    [NotMapped]
    public int CalculatedFatigue => MaxFatigue;

    [NotMapped]
    public int CalculatedVitality => MaxVitality;

    [NotMapped] 
    public int AttributeTotal => Physicality + Dodge + Drive + Reasoning + Awareness + Focus + Bearing;

    /// <summary>
    /// Calculates total wealth in copper pieces
    /// </summary>
    [NotMapped]
    public int TotalCopperValue => CopperCoins + (SilverCoins * 20) + (GoldCoins * 400) + (PlatinumCoins * 8000);

    /// <summary>
    /// Calculates weight of carried coins in pounds (100 coins = 1 pound)
    /// </summary>
    [NotMapped]
    public decimal CoinWeight => (CopperCoins + SilverCoins + GoldCoins + PlatinumCoins) / 100.0m;

    /// <summary>
    /// Initializes health pools based on attributes (for new characters)
    /// </summary>
    public void InitializeHealth()
    {
        CurrentFatigue = MaxFatigue;
        CurrentVitality = MaxVitality;
        PendingFatigueDamage = 0;
        PendingVitalityDamage = 0;
        LastFatigueRegenAt = DateTimeOffset.UtcNow;
        LastVitalityRegenAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the value of a character's attribute by name
    /// </summary>
    public int GetAttributeValue(string attributeName)
    {
        return attributeName switch
        {
            "STR" => Physicality,
            "DEX" => Dodge,
            "END" => Drive,
            "INT" => Reasoning,
            "ITT" => Awareness,
            "WIL" => Focus,
            "PHY" => Bearing,
            // Legacy support for full names
            "Physicality" => Physicality,
            "Dodge" => Dodge,
            "Drive" => Drive,
            "Reasoning" => Reasoning,
            "Awareness" => Awareness,
            "Focus" => Focus,
            "Bearing" => Bearing,
            _ => 10 // Default value
        };
    }
}

/// <summary>
/// Represents a character's progress in a specific skill
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
    /// Total usage points accumulated for this skill
    /// </summary>
    public int TotalUsagePoints { get; set; } = 0;

    /// <summary>
    /// Cached current level (calculated from TotalUsagePoints)
    /// Updated when TotalUsagePoints changes
    /// </summary>
    public int CurrentLevel { get; set; } = 0;

    /// <summary>
    /// When this skill was first learned by the character
    /// </summary>
    public DateTimeOffset LearnedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this skill was last used
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// When this skill last advanced to a new level
    /// </summary>
    public DateTimeOffset? LastAdvancedAt { get; set; }

    /// <summary>
    /// Number of times this skill has been used (for statistics)
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Whether this character can teach this skill to others
    /// Typically requires a minimum skill level
    /// </summary>
    public bool CanTeach { get; set; } = false;

    /// <summary>
    /// JSON field for character-specific skill properties and modifiers
    /// </summary>
    [StringLength(1000)]
    public string? CustomProperties { get; set; }

    // Navigation properties
    public virtual Character Character { get; set; } = null!;

    [ForeignKey(nameof(SkillDefinitionId))]
    public virtual SkillDefinition SkillDefinition { get; set; } = null!;

    /// <summary>
    /// Calculates the ability score (AS) for this skill using the owning character.
    /// AS = primary attribute + current level - 5.
    /// </summary>
    public int CalculateAbilityScore()
    {
        var relatedAttribute = SkillDefinition?.RelatedAttribute;
        var attributeValue = !string.IsNullOrWhiteSpace(relatedAttribute)
            ? Character.GetAttributeValue(relatedAttribute!)
            : 10;

        var abilityScore = attributeValue + CurrentLevel - 5;
        return Math.Max(0, abilityScore);
    }

    /// <summary>
    /// Calculates the effective skill level including any bonuses
    /// </summary>
    public int CalculateEffectiveLevel()
    {
        // Base effective level is the ability score, additional modifiers can be layered on later.
        return CalculateAbilityScore();
    }

    /// <summary>
    /// Gets progress toward the next level (0.0 to 1.0)
    /// </summary>
    public decimal GetProgressToNextLevel()
    {
        return SkillDefinition.CalculateProgressToNextLevel(TotalUsagePoints);
    }

    /// <summary>
    /// Adds usage points and updates the current level if necessary
    /// </summary>
    public bool AddUsagePoints(int points)
    {
        if (points <= 0) return false;
        
        int oldLevel = CurrentLevel;
        TotalUsagePoints += points;
        UsageCount++;
        LastUsedAt = DateTimeOffset.UtcNow;
        
        // Recalculate current level
        CurrentLevel = SkillDefinition.CalculateCurrentLevel(TotalUsagePoints);
        
        // Check if we advanced
        if (CurrentLevel > oldLevel)
        {
            LastAdvancedAt = DateTimeOffset.UtcNow;
            
            // Update teaching ability (example: can teach at level 5+)
            CanTeach = CurrentLevel >= 5;
            
            return true; // Skill advanced
        }
        
        return false; // No advancement
    }
}

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

    // Navigation properties
    public virtual ICollection<SkillDefinition> Skills { get; set; } = new List<SkillDefinition>();
}

/// <summary>
/// Represents the definition and properties of a skill
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
    /// Type of skill (CoreAttribute, Weapon, Spell, Crafting, Social, etc.)
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
    /// Related attribute that influences this skill (optional)
    /// Maps to Character attribute properties: Physicality, Dodge, Drive, etc.
    /// </summary>
    [StringLength(50)]
    public string? RelatedAttribute { get; set; }

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
    /// Calculates progress toward the next level (0.0 to 1.0)
    /// </summary>
    public decimal CalculateProgressToNextLevel(int totalUsagePoints)
    {
        int currentLevel = CalculateCurrentLevel(totalUsagePoints);
        int usageForCurrentLevel = CalculateTotalUsageForLevel(currentLevel);
        int usageForNextLevel = CalculateTotalUsageForLevel(currentLevel + 1);
        
        if (usageForNextLevel == usageForCurrentLevel) return 1.0m;
        
        int progressPoints = totalUsagePoints - usageForCurrentLevel;
        int requiredPoints = usageForNextLevel - usageForCurrentLevel;
        
        return Math.Max(0, Math.Min(1.0m, (decimal)progressPoints / requiredPoints));
    }
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
    public virtual Character Character { get; set; } = null!;

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