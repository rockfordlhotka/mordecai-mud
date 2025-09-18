using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mordecai.Web.Data;

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
    public string Species { get; set; } = "Human"; // Simple placeholder

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastPlayedAt { get; set; }

    // Core Attributes
    public int Physicality { get; set; } = 10; // STR - Physical strength and power
    public int Dodge { get; set; } = 10;       // DEX - Agility and evasion ability
    public int Drive { get; set; } = 10;       // END - Endurance and stamina  
    public int Reasoning { get; set; } = 10;   // INT - Intelligence and logical thinking
    public int Awareness { get; set; } = 10;   // ITT - Intuition and perception
    public int Focus { get; set; } = 10;       // WIL - Willpower and mental concentration
    public int Bearing { get; set; } = 10;     // PHY - Physical beauty and social presence

    // Health System - Current Values
    public int CurrentFatigue { get; set; } = 0;  // Current FAT damage taken
    public int CurrentVitality { get; set; } = 0; // Current VIT damage taken

    // Health System - Pending Pools
    public int PendingFatigueDamage { get; set; } = 0;  // Positive = damage to apply, negative = healing
    public int PendingVitalityDamage { get; set; } = 0; // Positive = damage to apply, negative = healing

    // Calculated Properties (not stored in database)
    [NotMapped]
    public int AttributeTotal => Physicality + Dodge + Drive + Reasoning + Awareness + Focus + Bearing;

    [NotMapped] 
    public int MaxFatigue => (Drive * 2) - 5;

    [NotMapped]
    public int MaxVitality => (Physicality + Drive) - 5;

    [NotMapped]
    public int RemainingFatigue => Math.Max(0, MaxFatigue - CurrentFatigue);

    [NotMapped]
    public int RemainingVitality => Math.Max(0, MaxVitality - CurrentVitality);

    [NotMapped]
    public bool IsUnconscious => CurrentFatigue >= MaxFatigue;

    [NotMapped]
    public bool IsDead => CurrentVitality >= MaxVitality;

    [NotMapped]
    public bool IsHealthy => CurrentFatigue == 0 && CurrentVitality == 0;

    // Navigation properties
    public virtual ICollection<CharacterSkill> Skills { get; set; } = new List<CharacterSkill>();

    /// <summary>
    /// Applies pending damage/healing (called by background service every 10 seconds)
    /// </summary>
    /// <returns>True if any health values changed</returns>
    public bool ApplyPendingHealth()
    {
        var changed = false;

        // Apply half of pending fatigue damage/healing
        if (PendingFatigueDamage != 0)
        {
            var fatigueToApply = PendingFatigueDamage / 2;
            if (fatigueToApply == 0 && PendingFatigueDamage != 0)
            {
                // Ensure we eventually apply all damage/healing
                fatigueToApply = PendingFatigueDamage > 0 ? 1 : -1;
            }

            CurrentFatigue = Math.Max(0, Math.Min(MaxFatigue, CurrentFatigue + fatigueToApply));
            PendingFatigueDamage -= fatigueToApply;
            changed = true;
        }

        // Apply half of pending vitality damage/healing
        if (PendingVitalityDamage != 0)
        {
            var vitalityToApply = PendingVitalityDamage / 2;
            if (vitalityToApply == 0 && PendingVitalityDamage != 0)
            {
                // Ensure we eventually apply all damage/healing
                vitalityToApply = PendingVitalityDamage > 0 ? 1 : -1;
            }

            CurrentVitality = Math.Max(0, Math.Min(MaxVitality, CurrentVitality + vitalityToApply));
            PendingVitalityDamage -= vitalityToApply;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Adds damage to the pending pools (positive values)
    /// </summary>
    public void AddPendingDamage(int fatigueDamage = 0, int vitalityDamage = 0)
    {
        PendingFatigueDamage += Math.Max(0, fatigueDamage);
        PendingVitalityDamage += Math.Max(0, vitalityDamage);
    }

    /// <summary>
    /// Adds healing to the pending pools (negative values in pending pools)
    /// </summary>
    public void AddPendingHealing(int fatigueHealing = 0, int vitalityHealing = 0)
    {
        PendingFatigueDamage -= Math.Max(0, fatigueHealing);
        PendingVitalityDamage -= Math.Max(0, vitalityHealing);
    }

    /// <summary>
    /// Gets a skill by skill definition ID
    /// </summary>
    public CharacterSkill? GetSkill(int skillDefinitionId)
    {
        return Skills.FirstOrDefault(s => s.SkillDefinitionId == skillDefinitionId);
    }

    /// <summary>
    /// Gets a skill by skill name
    /// </summary>
    public CharacterSkill? GetSkill(string skillName)
    {
        return Skills.FirstOrDefault(s => s.SkillDefinition.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the character has a specific skill
    /// </summary>
    public bool HasSkill(int skillDefinitionId)
    {
        return Skills.Any(s => s.SkillDefinitionId == skillDefinitionId);
    }

    /// <summary>
    /// Checks if the character has a specific skill by name
    /// </summary>
    public bool HasSkill(string skillName)
    {
        return Skills.Any(s => s.SkillDefinition.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));
    }
}
