using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing character skills and skill progression
/// </summary>
public class SkillService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SkillService> _logger;

    public SkillService(ApplicationDbContext context, ILogger<SkillService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all skill categories with their skills
    /// </summary>
    public async Task<List<SkillCategory>> GetSkillCategoriesAsync()
    {
        return await _context.SkillCategories
            .Include(sc => sc.Skills.Where(sd => sd.IsActive))
            .Where(sc => sc.IsActive)
            .OrderBy(sc => sc.DisplayOrder)
            .ThenBy(sc => sc.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a character's skills with their definitions
    /// </summary>
    public async Task<List<CharacterSkill>> GetCharacterSkillsAsync(Guid characterId)
    {
        return await _context.CharacterSkills
            .Include(cs => cs.SkillDefinition)
            .ThenInclude(sd => sd.Category)
            .Where(cs => cs.CharacterId == characterId)
            .OrderBy(cs => cs.SkillDefinition.Category.DisplayOrder)
            .ThenBy(cs => cs.SkillDefinition.DisplayOrder)
            .ThenBy(cs => cs.SkillDefinition.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific character skill
    /// </summary>
    public async Task<CharacterSkill?> GetCharacterSkillAsync(Guid characterId, int skillDefinitionId)
    {
        return await _context.CharacterSkills
            .Include(cs => cs.SkillDefinition)
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == skillDefinitionId);
    }

    /// <summary>
    /// Adds starting skills to a new character
    /// </summary>
    public async Task InitializeCharacterSkillsAsync(Guid characterId)
    {
        var character = await _context.Characters.FindAsync(characterId);
        if (character == null)
        {
            throw new ArgumentException("Character not found", nameof(characterId));
        }

        var startingSkills = await _context.SkillDefinitions
            .Where(sd => sd.IsStartingSkill && sd.IsActive)
            .ToListAsync();

        foreach (var skillDef in startingSkills)
        {
            // Calculate initial skill level based on related attribute
            int initialUsagePoints = 0;
            if (!string.IsNullOrEmpty(skillDef.RelatedAttribute))
            {
                int attributeValue = character.GetAttributeValue(skillDef.RelatedAttribute);
                // Convert attribute value to equivalent usage points (attribute becomes starting level)
                initialUsagePoints = skillDef.CalculateTotalUsageForLevel(attributeValue);
            }

            var characterSkill = new CharacterSkill
            {
                CharacterId = characterId,
                SkillDefinitionId = skillDef.Id,
                TotalUsagePoints = initialUsagePoints,
                CurrentLevel = skillDef.CalculateCurrentLevel(initialUsagePoints),
                LearnedAt = DateTimeOffset.UtcNow
            };

            _context.CharacterSkills.Add(characterSkill);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Initialized starting skills for character {CharacterId}", characterId);
    }

    /// <summary>
    /// Adds usage points to a skill and handles progression
    /// </summary>
    public async Task<bool> AddSkillUsageAsync(
        Guid characterId, 
        int skillDefinitionId, 
        SkillUsageType usageType, 
        int baseUsagePoints = 1,
        string? context = null,
        string? details = null)
    {
        var characterSkill = await GetCharacterSkillAsync(characterId, skillDefinitionId);
        if (characterSkill == null)
        {
            // Character doesn't have this skill yet - learn it
            characterSkill = await LearnSkillAsync(characterId, skillDefinitionId);
            if (characterSkill == null)
            {
                return false;
            }
        }

        // Calculate final usage points with multiplier
        decimal multiplier = SkillUsageLog.GetUsageMultiplier(usageType);
        int finalUsagePoints = (int)Math.Ceiling(baseUsagePoints * multiplier);

        // Record the usage in the log
        var usageLog = new SkillUsageLog
        {
            CharacterId = characterId,
            SkillDefinitionId = skillDefinitionId,
            UsageType = usageType,
            BaseUsagePoints = baseUsagePoints,
            UsageMultiplier = multiplier,
            FinalUsagePoints = finalUsagePoints,
            SkillLevelBefore = characterSkill.CurrentLevel,
            Context = context,
            Details = details,
            UsedAt = DateTimeOffset.UtcNow
        };

        // Add usage points to the skill
        bool didAdvance = characterSkill.AddUsagePoints(finalUsagePoints);
        usageLog.SkillLevelAfter = characterSkill.CurrentLevel;
        usageLog.DidAdvance = didAdvance;

        _context.SkillUsageLogs.Add(usageLog);
        _context.CharacterSkills.Update(characterSkill);
        await _context.SaveChangesAsync();

        if (didAdvance)
        {
            _logger.LogInformation(
                "Skill advanced: Character {CharacterId} skill {SkillId} from level {OldLevel} to {NewLevel}",
                characterId, skillDefinitionId, usageLog.SkillLevelBefore, usageLog.SkillLevelAfter);
        }

        return didAdvance;
    }

    /// <summary>
    /// Learns a new skill for a character
    /// </summary>
    public async Task<CharacterSkill?> LearnSkillAsync(Guid characterId, int skillDefinitionId)
    {
        var skillDef = await _context.SkillDefinitions.FindAsync(skillDefinitionId);
        if (skillDef == null || !skillDef.IsActive)
        {
            return null;
        }

        var character = await _context.Characters.FindAsync(characterId);
        if (character == null)
        {
            return null;
        }

        // Check if character already has this skill
        var existingSkill = await _context.CharacterSkills
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == skillDefinitionId);
        
        if (existingSkill != null)
        {
            return existingSkill;
        }

        // Calculate initial usage points based on related attribute
        int initialUsagePoints = 0;
        if (!string.IsNullOrEmpty(skillDef.RelatedAttribute))
        {
            int attributeValue = character.GetAttributeValue(skillDef.RelatedAttribute);
            // Give some initial progress based on attribute (but don't start at full attribute level)
            initialUsagePoints = Math.Max(0, skillDef.CalculateTotalUsageForLevel(attributeValue - 5));
        }

        var characterSkill = new CharacterSkill
        {
            CharacterId = characterId,
            SkillDefinitionId = skillDefinitionId,
            TotalUsagePoints = initialUsagePoints,
            CurrentLevel = skillDef.CalculateCurrentLevel(initialUsagePoints),
            LearnedAt = DateTimeOffset.UtcNow
        };

        _context.CharacterSkills.Add(characterSkill);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Character {CharacterId} learned skill {SkillId} at level {Level}", 
            characterId, skillDefinitionId, characterSkill.CurrentLevel);

        return characterSkill;
    }

    /// <summary>
    /// Gets skill usage statistics for a character
    /// </summary>
    public async Task<List<SkillUsageLog>> GetSkillUsageHistoryAsync(
        Guid characterId, 
        int? skillDefinitionId = null, 
        int take = 50)
    {
        var query = _context.SkillUsageLogs
            .Include(sul => sul.SkillDefinition)
            .Where(sul => sul.CharacterId == characterId);

        if (skillDefinitionId.HasValue)
        {
            query = query.Where(sul => sul.SkillDefinitionId == skillDefinitionId.Value);
        }

        return await query
            .OrderByDescending(sul => sul.UsedAt)
            .Take(take)
            .ToListAsync();
    }
}