using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for migrating data between different schema versions
/// </summary>
public class DataMigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataMigrationService> _logger;

    public DataMigrationService(ApplicationDbContext context, ILogger<DataMigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Fixes skill RelatedAttribute values to use proper attribute abbreviations instead of skill names
    /// </summary>
    public async Task FixSkillAttributeReferencesAsync()
    {
        var skillsToFix = await _context.SkillDefinitions
            .Where(s => s.RelatedAttribute != null && 
                       (s.RelatedAttribute == "Physicality" || 
                        s.RelatedAttribute == "Dodge" || 
                        s.RelatedAttribute == "Drive" || 
                        s.RelatedAttribute == "Reasoning" || 
                        s.RelatedAttribute == "Awareness" || 
                        s.RelatedAttribute == "Focus" || 
                        s.RelatedAttribute == "Bearing"))
            .ToListAsync();

        if (!skillsToFix.Any())
        {
            _logger.LogInformation("No skill attribute references need fixing");
            return;
        }

        _logger.LogInformation("Fixing {Count} skill attribute references", skillsToFix.Count);

        foreach (var skill in skillsToFix)
        {
            var oldValue = skill.RelatedAttribute;
            skill.RelatedAttribute = ConvertSkillNameToAttribute(skill.RelatedAttribute!);
            
            _logger.LogInformation("Fixed skill '{SkillName}': {OldValue} -> {NewValue}", 
                skill.Name, oldValue, skill.RelatedAttribute);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully fixed {Count} skill attribute references", skillsToFix.Count);
    }

    /// <summary>
    /// Converts legacy skill names to proper attribute abbreviations
    /// </summary>
    private static string ConvertSkillNameToAttribute(string skillName)
    {
        return skillName switch
        {
            "Physicality" => "STR",
            "Dodge" => "DEX",
            "Drive" => "END",
            "Reasoning" => "INT", 
            "Awareness" => "ITT",
            "Focus" => "WIL",
            "Bearing" => "PHY",
            _ => skillName // Leave unchanged if not a recognized skill name
        };
    }

    /// <summary>
    /// Runs all data migration fixes that might be needed
    /// </summary>
    public async Task RunAllDataMigrationsAsync()
    {
        await FixSkillAttributeReferencesAsync();
        // Add other data migrations here as needed
    }
}