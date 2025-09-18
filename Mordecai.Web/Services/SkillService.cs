using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;
using Mordecai.Web.Models;

namespace Mordecai.Web.Services;

public interface ISkillService
{
    Task InitializeBaseAttributeSkillsAsync();
    Task CreateAttributeSkillsForCharacterAsync(Character character);
    Task<CharacterSkill?> GetCharacterSkillAsync(Guid characterId, int skillDefinitionId);
    Task<CharacterSkill?> GetCharacterSkillAsync(Guid characterId, string skillName);
    Task<List<CharacterSkill>> GetCharacterSkillsAsync(Guid characterId);
    Task<List<SkillDefinition>> GetAllSkillDefinitionsAsync();
    Task<List<SkillDefinition>> GetStartingSkillDefinitionsAsync();
    Task<bool> AddExperienceAsync(Guid characterId, int skillDefinitionId, int experience);
    Task<bool> LearnSkillAsync(Guid characterId, int skillDefinitionId);
}

public class SkillService : ISkillService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SkillService> _logger;

    // Define the 7 base attribute skills
    private static readonly Dictionary<string, string> BaseAttributeSkills = new()
    {
        ["Physicality"] = "Physical strength and power, used for melee combat and carrying capacity",
        ["Dodge"] = "Agility and evasion ability, used for avoiding attacks and quick movements",
        ["Drive"] = "Endurance and stamina, determines health pools and resistance to fatigue",
        ["Reasoning"] = "Intelligence and logical thinking, used for crafting and problem-solving",
        ["Awareness"] = "Intuition and perception, used for detecting hidden things and understanding situations",
        ["Focus"] = "Willpower and mental concentration, used for spellcasting and resisting mental effects",
        ["Bearing"] = "Physical beauty and social presence, used for social interactions and leadership"
    };

    public SkillService(ApplicationDbContext context, ILogger<SkillService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ensures all base attribute skills exist in the database
    /// Should be called during application startup
    /// </summary>
    public async Task InitializeBaseAttributeSkillsAsync()
    {
        foreach (var (attributeName, description) in BaseAttributeSkills)
        {
            var existingSkill = await _context.SkillDefinitions
                .FirstOrDefaultAsync(sd => sd.Name == attributeName && sd.SkillType == "AttributeSkill");

            if (existingSkill == null)
            {
                var skillDefinition = new SkillDefinition
                {
                    Name = attributeName,
                    Description = description,
                    SkillType = "AttributeSkill",
                    RelatedAttribute = attributeName,
                    IsStartingSkill = true,
                    BaseExperienceRequired = 50, // Lower for attribute skills since they're fundamental
                    LevelMultiplier = 1.2m, // Slower progression for attribute skills
                    MaxLevel = 10, // Cap attribute skills at level 10
                    IsActive = true,
                    CreatedBy = "System"
                };

                _context.SkillDefinitions.Add(skillDefinition);
                _logger.LogInformation("Created base attribute skill: {SkillName}", attributeName);
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates all 7 base attribute skills for a new character
    /// </summary>
    public async Task CreateAttributeSkillsForCharacterAsync(Character character)
    {
        // Get all starting skill definitions
        var startingSkills = await GetStartingSkillDefinitionsAsync();
        var attributeSkills = startingSkills.Where(sd => sd.SkillType == "AttributeSkill").ToList();

        foreach (var skillDef in attributeSkills)
        {
            // Check if character already has this skill
            var existingSkill = await _context.CharacterSkills
                .FirstOrDefaultAsync(cs => cs.CharacterId == character.Id && cs.SkillDefinitionId == skillDef.Id);

            if (existingSkill == null)
            {
                var characterSkill = new CharacterSkill
                {
                    CharacterId = character.Id,
                    SkillDefinitionId = skillDef.Id,
                    Level = 0, // Start at level 0
                    Experience = 0,
                    UsageCount = 0,
                    LearnedAt = DateTimeOffset.UtcNow
                };

                _context.CharacterSkills.Add(characterSkill);
                _logger.LogInformation("Added base skill {SkillName} to character {CharacterName}", 
                    skillDef.Name, character.Name);
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets a specific character skill by skill definition ID
    /// </summary>
    public async Task<CharacterSkill?> GetCharacterSkillAsync(Guid characterId, int skillDefinitionId)
    {
        return await _context.CharacterSkills
            .Include(cs => cs.SkillDefinition)
            .Include(cs => cs.Character)
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == skillDefinitionId);
    }

    /// <summary>
    /// Gets a specific character skill by skill name
    /// </summary>
    public async Task<CharacterSkill?> GetCharacterSkillAsync(Guid characterId, string skillName)
    {
        return await _context.CharacterSkills
            .Include(cs => cs.SkillDefinition)
            .Include(cs => cs.Character)
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && 
                                      cs.SkillDefinition.Name == skillName);
    }

    /// <summary>
    /// Gets all skills for a character
    /// </summary>
    public async Task<List<CharacterSkill>> GetCharacterSkillsAsync(Guid characterId)
    {
        return await _context.CharacterSkills
            .Include(cs => cs.SkillDefinition)
            .Where(cs => cs.CharacterId == characterId)
            .OrderBy(cs => cs.SkillDefinition.SkillType)
            .ThenBy(cs => cs.SkillDefinition.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all skill definitions
    /// </summary>
    public async Task<List<SkillDefinition>> GetAllSkillDefinitionsAsync()
    {
        return await _context.SkillDefinitions
            .Where(sd => sd.IsActive)
            .OrderBy(sd => sd.SkillType)
            .ThenBy(sd => sd.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all starting skill definitions
    /// </summary>
    public async Task<List<SkillDefinition>> GetStartingSkillDefinitionsAsync()
    {
        return await _context.SkillDefinitions
            .Where(sd => sd.IsActive && sd.IsStartingSkill)
            .ToListAsync();
    }

    /// <summary>
    /// Adds experience to a character's skill and handles level ups
    /// </summary>
    public async Task<bool> AddExperienceAsync(Guid characterId, int skillDefinitionId, int experience)
    {
        var characterSkill = await GetCharacterSkillAsync(characterId, skillDefinitionId);
        if (characterSkill == null)
        {
            _logger.LogWarning("Character {CharacterId} does not have skill {SkillDefinitionId}", 
                characterId, skillDefinitionId);
            return false;
        }

        var leveledUp = characterSkill.AddExperience(experience);
        
        await _context.SaveChangesAsync();

        if (leveledUp)
        {
            _logger.LogInformation("Character {CharacterName} leveled up {SkillName} to level {Level}", 
                characterSkill.Character.Name, characterSkill.SkillDefinition.Name, characterSkill.Level);
        }

        return leveledUp;
    }

    /// <summary>
    /// Teaches a new skill to a character
    /// </summary>
    public async Task<bool> LearnSkillAsync(Guid characterId, int skillDefinitionId)
    {
        // Check if character already has this skill
        var existingSkill = await _context.CharacterSkills
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == skillDefinitionId);

        if (existingSkill != null)
        {
            _logger.LogWarning("Character {CharacterId} already has skill {SkillDefinitionId}", 
                characterId, skillDefinitionId);
            return false;
        }

        // Verify skill definition exists
        var skillDefinition = await _context.SkillDefinitions
            .FirstOrDefaultAsync(sd => sd.Id == skillDefinitionId && sd.IsActive);

        if (skillDefinition == null)
        {
            _logger.LogWarning("Skill definition {SkillDefinitionId} not found or inactive", skillDefinitionId);
            return false;
        }

        // Create the character skill
        var characterSkill = new CharacterSkill
        {
            CharacterId = characterId,
            SkillDefinitionId = skillDefinitionId,
            Level = 0,
            Experience = 0,
            UsageCount = 0,
            LearnedAt = DateTimeOffset.UtcNow
        };

        _context.CharacterSkills.Add(characterSkill);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Character {CharacterId} learned skill {SkillName}", 
            characterId, skillDefinition.Name);

        return true;
    }
}