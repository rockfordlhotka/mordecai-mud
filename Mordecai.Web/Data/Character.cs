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

    // Calculated Properties (not stored in database)
    [NotMapped]
    public int AttributeTotal => Physicality + Dodge + Drive + Reasoning + Awareness + Focus + Bearing;

    [NotMapped] 
    public int CalculatedFatigue => (Drive * 2) - 5;

    [NotMapped]
    public int CalculatedVitality => (Physicality + Drive) - 5;
}
