namespace Mordecai.Web.Models;

/// <summary>
/// Represents the seven core attributes of a character
/// </summary>
public class CharacterAttributes
{
    public int Physicality { get; set; } = 10; // STR - Physical strength and power
    public int Dodge { get; set; } = 10;       // DEX - Agility and evasion ability
    public int Drive { get; set; } = 10;       // END - Endurance and stamina
    public int Reasoning { get; set; } = 10;   // INT - Intelligence and logical thinking
    public int Awareness { get; set; } = 10;   // ITT - Intuition and perception
    public int Focus { get; set; } = 10;       // WIL - Willpower and mental concentration
    public int Bearing { get; set; } = 10;     // PHY - Physical beauty and social presence

    /// <summary>
    /// Gets the total sum of all attributes
    /// </summary>
    public int Total => Physicality + Dodge + Drive + Reasoning + Awareness + Focus + Bearing;

    /// <summary>
    /// Gets calculated Fatigue (END × 2) - 5
    /// </summary>
    public int CalculatedFatigue => (Drive * 2) - 5;

    /// <summary>
    /// Gets calculated Vitality (STR + END) - 5
    /// </summary>
    public int CalculatedVitality => (Physicality + Drive) - 5;

    /// <summary>
    /// Creates a copy of the current attributes
    /// </summary>
    public CharacterAttributes Clone()
    {
        return new CharacterAttributes
        {
            Physicality = Physicality,
            Dodge = Dodge,
            Drive = Drive,
            Reasoning = Reasoning,
            Awareness = Awareness,
            Focus = Focus,
            Bearing = Bearing
        };
    }

    /// <summary>
    /// Sets all attributes to the specified values
    /// </summary>
    public void SetAttributes(int physicality, int dodge, int drive, int reasoning, int awareness, int focus, int bearing)
    {
        Physicality = physicality;
        Dodge = dodge;
        Drive = drive;
        Reasoning = reasoning;
        Awareness = awareness;
        Focus = focus;
        Bearing = bearing;
    }
}

/// <summary>
/// Defines species-specific modifiers for character attributes
/// </summary>
public static class SpeciesModifiers
{
    public static readonly Dictionary<string, Dictionary<string, int>> Modifiers = new()
    {
        ["Human"] = new Dictionary<string, int>
        {
            ["Physicality"] = 0,
            ["Dodge"] = 0,
            ["Drive"] = 0,
            ["Reasoning"] = 0,
            ["Awareness"] = 0,
            ["Focus"] = 0,
            ["Bearing"] = 0
        },
        ["Elf"] = new Dictionary<string, int>
        {
            ["Physicality"] = -1,
            ["Dodge"] = 0,
            ["Drive"] = 0,
            ["Reasoning"] = 1,
            ["Awareness"] = 0,
            ["Focus"] = 0,
            ["Bearing"] = 0
        },
        ["Dwarf"] = new Dictionary<string, int>
        {
            ["Physicality"] = 1,
            ["Dodge"] = -1,
            ["Drive"] = 0,
            ["Reasoning"] = 0,
            ["Awareness"] = 0,
            ["Focus"] = 0,
            ["Bearing"] = 0
        },
        ["Halfling"] = new Dictionary<string, int>
        {
            ["Physicality"] = -2,
            ["Dodge"] = 1,
            ["Drive"] = 0,
            ["Reasoning"] = 0,
            ["Awareness"] = 1,
            ["Focus"] = 0,
            ["Bearing"] = 0
        },
        ["Orc"] = new Dictionary<string, int>
        {
            ["Physicality"] = 2,
            ["Dodge"] = 0,
            ["Drive"] = 1,
            ["Reasoning"] = -1,
            ["Awareness"] = 0,
            ["Focus"] = 0,
            ["Bearing"] = -1
        }
    };

    /// <summary>
    /// Gets species modifier for a specific attribute
    /// </summary>
    public static int GetModifier(string species, string attribute)
    {
        return Modifiers.TryGetValue(species, out var speciesModifiers) &&
               speciesModifiers.TryGetValue(attribute, out var modifier)
            ? modifier
            : 0;
    }

    /// <summary>
    /// Gets the base value (4dF + 10) plus species modifier for an attribute
    /// </summary>
    public static int GetBaseValue(string species, string attribute)
    {
        return 10 + GetModifier(species, attribute);
    }

    /// <summary>
    /// Gets all modifiers for a species
    /// </summary>
    public static Dictionary<string, int> GetAllModifiers(string species)
    {
        return Modifiers.TryGetValue(species, out var modifiers) 
            ? new Dictionary<string, int>(modifiers) 
            : new Dictionary<string, int>();
    }
}