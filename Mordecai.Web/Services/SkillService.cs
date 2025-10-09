using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;
using GameCharacterSkill = Mordecai.Game.Entities.CharacterSkill;
using WebCharacterSkill = Mordecai.Web.Data.CharacterSkill;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing character skills and skill progression
/// </summary>
public interface ISkillService
{
    Task<List<SkillCategory>> GetSkillCategoriesAsync();
    Task<List<WebCharacterSkill>> GetCharacterSkillsAsync(Guid characterId);
    Task<WebCharacterSkill?> GetCharacterSkillAsync(Guid characterId, int skillDefinitionId);
    Task InitializeCharacterSkillsAsync(Guid characterId);
    Task<bool> AddSkillUsageAsync(Guid characterId, int skillDefinitionId, SkillUsageType usageType, int baseUsagePoints = 1, string? context = null, string? details = null);
    Task<WebCharacterSkill?> LearnSkillAsync(Guid characterId, int skillDefinitionId);
    Task<List<SkillUsageLog>> GetSkillUsageHistoryAsync(Guid characterId, int? skillDefinitionId = null, int take = 50);
}

/// <summary>
/// Implementation that uses IDbContextFactory to create short-lived contexts per operation
/// which is safe for Blazor Server concurrent callbacks.
/// </summary>
public class SkillService : ISkillService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<SkillService> _logger;

    public SkillService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<SkillService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<SkillCategory>> GetSkillCategoriesAsync()
    {
        await using var ctx = _contextFactory.CreateDbContext();
        return await ctx.SkillCategories
            .Include(sc => sc.Skills.Where(sd => sd.IsActive))
            .Where(sc => sc.IsActive)
            .OrderBy(sc => sc.DisplayOrder)
            .ThenBy(sc => sc.Name)
            .ToListAsync();
    }

    public async Task<List<WebCharacterSkill>> GetCharacterSkillsAsync(Guid characterId)
    {
        await using var ctx = _contextFactory.CreateDbContext();
        return await ctx.CharacterSkills
            .Include(cs => cs.SkillDefinition)
            .ThenInclude(sd => sd.Category)
            .Where(cs => cs.CharacterId == characterId)
            .OrderBy(cs => cs.SkillDefinition.Category.DisplayOrder)
            .ThenBy(cs => cs.SkillDefinition.DisplayOrder)
            .ThenBy(cs => cs.SkillDefinition.Name)
            .ToListAsync();
    }

    public async Task<WebCharacterSkill?> GetCharacterSkillAsync(Guid characterId, int skillDefinitionId)
    {
        await using var ctx = _contextFactory.CreateDbContext();
        return await ctx.CharacterSkills
            .Include(cs => cs.SkillDefinition)
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == skillDefinitionId);
    }

    public async Task InitializeCharacterSkillsAsync(Guid characterId)
    {
        await using var ctx = _contextFactory.CreateDbContext();
        var character = await ctx.Characters.FindAsync(characterId);
        if (character == null) return;

        var startingSkills = await ctx.SkillDefinitions
            .Where(sd => sd.IsStartingSkill)
            .ToListAsync();

        foreach (var sd in startingSkills)
        {
            var characterSkill = new WebCharacterSkill
            {
                CharacterId = characterId,
                SkillDefinitionId = sd.Id,
                Experience = 0,
                Level = sd.CalculateCurrentLevel(0),
                LearnedAt = DateTimeOffset.UtcNow
            };

            ctx.CharacterSkills.Add(characterSkill);
        }

        await ctx.SaveChangesAsync();
        _logger.LogInformation("Initialized starting skills for character {CharacterId}", characterId);
    }

    public async Task<bool> AddSkillUsageAsync(Guid characterId, int skillDefinitionId, SkillUsageType usageType, int baseUsagePoints = 1, string? context = null, string? details = null)
    {
        await using var ctx = _contextFactory.CreateDbContext();

        var characterSkill = await ctx.CharacterSkills
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == skillDefinitionId);

        if (characterSkill == null)
        {
            var learned = await LearnSkillAsync(characterId, skillDefinitionId);
            if (learned == null) return false;
            // re-query into this context to get a tracked instance
            characterSkill = await ctx.CharacterSkills
                .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == skillDefinitionId);
            if (characterSkill == null) return false;
        }

        decimal multiplier = SkillUsageLog.GetUsageMultiplier(usageType);
        int finalExperience = (int)Math.Ceiling(baseUsagePoints * multiplier);

        bool didAdvance = characterSkill.AddExperience(finalExperience);

        ctx.CharacterSkills.Update(characterSkill);
        await ctx.SaveChangesAsync();

        if (didAdvance)
        {
            _logger.LogInformation("Skill advanced: Character {CharacterId} skill {SkillId} to level {NewLevel}", characterId, skillDefinitionId, characterSkill.Level);
        }

        try
        {
            ctx.SkillUsageLogs.Add(new SkillUsageLog
            {
                CharacterId = characterId,
                SkillDefinitionId = skillDefinitionId,
                UsedAt = DateTimeOffset.UtcNow,
                UsageType = usageType,
                BaseUsagePoints = baseUsagePoints,
                UsageMultiplier = multiplier,
                FinalUsagePoints = finalExperience,
                SkillLevelBefore = characterSkill.Level - (didAdvance ? 1 : 0),
                SkillLevelAfter = characterSkill.Level,
                DidAdvance = didAdvance,
                Context = context,
                Details = details
            });
            await ctx.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write skill usage log for character {CharacterId}", characterId);
        }

        return didAdvance;
    }

    public async Task<WebCharacterSkill?> LearnSkillAsync(Guid characterId, int skillDefinitionId)
    {
        await using var ctx = _contextFactory.CreateDbContext();

        var skillDef = await ctx.SkillDefinitions.FindAsync(skillDefinitionId);
        if (skillDef == null || !skillDef.IsActive) return null;

        var character = await ctx.Characters.FindAsync(characterId);
        if (character == null) return null;

        var existingSkill = await ctx.CharacterSkills
            .FirstOrDefaultAsync(cs => cs.CharacterId == characterId && cs.SkillDefinitionId == skillDefinitionId);

        if (existingSkill != null) return existingSkill;

        int initialExperience = 0;
        if (!string.IsNullOrEmpty(skillDef.RelatedAttribute))
        {
            int attributeValue = character.GetAttributeValue(skillDef.RelatedAttribute);
            initialExperience = Math.Max(0, skillDef.CalculateExperienceRequired(Math.Max(0, attributeValue - 5)));
        }

        var characterSkill = new WebCharacterSkill
        {
            CharacterId = characterId,
            SkillDefinitionId = skillDefinitionId,
            Experience = initialExperience,
            Level = skillDef.CalculateCurrentLevel(initialExperience),
            LearnedAt = DateTimeOffset.UtcNow
        };

        ctx.CharacterSkills.Add(characterSkill);
        await ctx.SaveChangesAsync();

        _logger.LogInformation("Character {CharacterId} learned skill {SkillId} at level {Level}", characterId, skillDefinitionId, characterSkill.Level);

        return characterSkill;
    }

    public async Task<List<SkillUsageLog>> GetSkillUsageHistoryAsync(Guid characterId, int? skillDefinitionId = null, int take = 50)
    {
        await using var ctx = _contextFactory.CreateDbContext();
        var query = ctx.SkillUsageLogs
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

    private int CalculateCurrentLevel(SkillDefinition skillDef, int experience)
    {
        if (experience <= 0) return 0;

        int level = 0;
        int totalNeeded = 0;

        while (totalNeeded < experience)
        {
            level++;
            totalNeeded = skillDef.CalculateExperienceRequired(level);
        }

        return level - 1;
    }
}
