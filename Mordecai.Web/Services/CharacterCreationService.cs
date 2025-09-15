using Mordecai.Web.Models;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for generating character attributes during character creation
/// </summary>
public interface ICharacterCreationService
{
    /// <summary>
    /// Generates random attributes for a character based on species
    /// </summary>
    CharacterAttributes GenerateRandomAttributes(string species);

    /// <summary>
    /// Validates that attribute adjustments are within acceptable bounds
    /// </summary>
    bool ValidateAttributeAdjustments(CharacterAttributes attributes, int originalTotal, string species);

    /// <summary>
    /// Gets the minimum and maximum allowed values for an attribute based on species
    /// </summary>
    (int min, int max) GetAttributeBounds(string species, string attributeName);
}

public class CharacterCreationService : ICharacterCreationService
{
    private readonly IDiceService _diceService;

    public CharacterCreationService(IDiceService diceService)
    {
        _diceService = diceService;
    }

    public CharacterAttributes GenerateRandomAttributes(string species)
    {
        var attributes = new CharacterAttributes();
        
        // Roll 4dF + base value for each attribute
        attributes.Physicality = Roll4dFWithSpeciesModifier(species, "Physicality");
        attributes.Dodge = Roll4dFWithSpeciesModifier(species, "Dodge");
        attributes.Drive = Roll4dFWithSpeciesModifier(species, "Drive");
        attributes.Reasoning = Roll4dFWithSpeciesModifier(species, "Reasoning");
        attributes.Awareness = Roll4dFWithSpeciesModifier(species, "Awareness");
        attributes.Focus = Roll4dFWithSpeciesModifier(species, "Focus");
        attributes.Bearing = Roll4dFWithSpeciesModifier(species, "Bearing");

        return attributes;
    }

    public bool ValidateAttributeAdjustments(CharacterAttributes attributes, int originalTotal, string species)
    {
        // Check total hasn't changed
        if (attributes.Total != originalTotal)
            return false;

        // Check each attribute is within bounds
        var attributeNames = new[] { "Physicality", "Dodge", "Drive", "Reasoning", "Awareness", "Focus", "Bearing" };
        var attributeValues = new[] { attributes.Physicality, attributes.Dodge, attributes.Drive, 
                                    attributes.Reasoning, attributes.Awareness, attributes.Focus, attributes.Bearing };

        for (int i = 0; i < attributeNames.Length; i++)
        {
            var (min, max) = GetAttributeBounds(species, attributeNames[i]);
            if (attributeValues[i] < min || attributeValues[i] > max)
                return false;
        }

        return true;
    }

    public (int min, int max) GetAttributeBounds(string species, string attributeName)
    {
        // For humans, attributes can't be lower than 6 or higher than 14
        if (species == "Human")
        {
            return (6, 14);
        }

        // For other species, calculate bounds based on base + modifier + 4dF range
        int baseValue = SpeciesModifiers.GetBaseValue(species, attributeName);
        
        // 4dF can roll -4 to +4, so:
        int minPossible = Math.Max(1, baseValue - 4); // Can't go below 1
        int maxPossible = Math.Min(20, baseValue + 4); // Cap at 20
        
        return (minPossible, maxPossible);
    }

    private int Roll4dFWithSpeciesModifier(string species, string attributeName)
    {
        int baseValue = SpeciesModifiers.GetBaseValue(species, attributeName);
        var (min, max) = GetAttributeBounds(species, attributeName);
        
        return _diceService.Roll4dFWithModifier(baseValue, min, max);
    }
}