using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using Mordecai.Web.Services;
using Xunit;

using WebCharacterSkill = Mordecai.Web.Data.CharacterSkill;
using WebSkillCategory = Mordecai.Web.Data.SkillCategory;
using WebSkillDefinition = Mordecai.Web.Data.SkillDefinition;
using WebSkillUsageType = Mordecai.Web.Data.SkillUsageType;

namespace Mordecai.Web.Tests;

public sealed class SkillProgressionServiceTests
{
    private static IDbContextFactory<ApplicationDbContext> CreateFactory(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TestDbContextFactory(options);
    }

    private static IOptions<SkillProgressionSettings> CreateSettings(SkillProgressionSettings? settings = null)
    {
        return Options.Create(settings ?? new SkillProgressionSettings());
    }

    private static async Task<(Guid CharacterId, int SkillDefId)> SeedTestDataAsync(ApplicationDbContext context)
    {
        // Create skill category
        var category = new WebSkillCategory
        {
            Name = "Combat",
            Description = "Combat skills",
            DefaultBaseCost = 25,
            DefaultMultiplier = 2.2m,
            IsActive = true,
            CreatedBy = "test"
        };
        context.SkillCategories.Add(category);
        await context.SaveChangesAsync();

        // Create skill definition
        var skillDef = new WebSkillDefinition
        {
            CategoryId = category.Id,
            Name = "Swords",
            Description = "Sword fighting skill",
            SkillType = "WeaponSkill",
            BaseCost = 25,
            Multiplier = 2.2m,
            RelatedAttribute = "STR",
            IsActive = true,
            IsStartingSkill = false,
            CreatedBy = "test"
        };
        context.SkillDefinitions.Add(skillDef);
        await context.SaveChangesAsync();

        // Create character with healthy FAT
        var character = new Character
        {
            Id = Guid.NewGuid(),
            Name = "TestWarrior",
            UserId = "test-user",
            Physicality = 12,
            Dodge = 10,
            Drive = 10,
            Reasoning = 10,
            Awareness = 10,
            Focus = 10,
            Bearing = 10,
            CurrentFatigue = 15, // Max FAT = (Drive + Focus) - 5 = 15
            CurrentVitality = 19
        };
        context.Characters.Add(character);
        await context.SaveChangesAsync();

        return (character.Id, skillDef.Id);
    }

    [Fact]
    public async Task LogUsageAsync_FirstUse_AppliesFullMultiplier()
    {
        // Arrange
        var dbName = $"SkillProg_FirstUse_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings();

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);

        // Act
        var result = await service.LogUsageAsync(
            characterId,
            skillDefId,
            WebSkillUsageType.RoutineUse,
            baseExperience: 1);

        // Assert
        Assert.True(result.ProgressionApplied);
        Assert.Equal(1.0m, result.UsageTypeMultiplier);
        Assert.Equal(1.0m, result.HourlyMultiplier);
        Assert.Equal(1.5m, result.DailyMultiplier); // Fresh learning bonus
        Assert.Equal(1.0m, result.FatigueMultiplier);
        Assert.Equal(1.5m, result.TotalMultiplier); // 1.0 * 1.0 * 1.5 * 1.0
        Assert.Equal(2, result.FinalExperience); // Ceiling(1 * 1.5) = 2
    }

    [Fact]
    public async Task LogUsageAsync_HourlyDiminishingReturns_AppliesReducedMultiplier()
    {
        // Arrange
        var dbName = $"SkillProg_Hourly_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings(new SkillProgressionSettings
        {
            HourlyUsageThreshold1 = 5,  // Lower thresholds for testing
            HourlyUsageThreshold2 = 10,
            HourlyUsageThreshold3 = 15,
            DailyFreshUsageThreshold = 1000 // Disable daily bonus for this test
        });

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);

        // Act - Use skill 6 times to cross first threshold
        for (int i = 0; i < 6; i++)
        {
            await service.LogUsageAsync(characterId, skillDefId, WebSkillUsageType.RoutineUse, baseExperience: 1);
        }

        // Get the 7th usage result
        var result = await service.LogUsageAsync(characterId, skillDefId, WebSkillUsageType.RoutineUse, baseExperience: 1);

        // Assert - Should now be at 0.5x hourly multiplier
        Assert.True(result.ProgressionApplied);
        Assert.Equal(0.5m, result.HourlyMultiplier);
    }

    [Fact]
    public async Task LogUsageAsync_TargetCooldown_BlocksProgression()
    {
        // Arrange
        var dbName = $"SkillProg_Cooldown_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings(new SkillProgressionSettings
        {
            CombatTargetCooldownSeconds = 30
        });

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);
        var targetId = "npc:" + Guid.NewGuid();

        // Act - First use should succeed
        var result1 = await service.LogUsageAsync(
            characterId, skillDefId, WebSkillUsageType.RoutineUse,
            baseExperience: 1, targetId: targetId);

        // Second use with same target should be blocked by cooldown
        var result2 = await service.LogUsageAsync(
            characterId, skillDefId, WebSkillUsageType.RoutineUse,
            baseExperience: 1, targetId: targetId);

        // Assert
        Assert.True(result1.ProgressionApplied);
        Assert.False(result2.ProgressionApplied);
        Assert.Equal("Target cooldown active", result2.BlockedReason);
    }

    [Fact]
    public async Task LogUsageAsync_DifferentTargets_BothProgress()
    {
        // Arrange
        var dbName = $"SkillProg_DiffTargets_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings();

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);
        var target1 = "npc:" + Guid.NewGuid();
        var target2 = "npc:" + Guid.NewGuid();

        // Act - Different targets should both work
        var result1 = await service.LogUsageAsync(
            characterId, skillDefId, WebSkillUsageType.RoutineUse,
            baseExperience: 1, targetId: target1);

        var result2 = await service.LogUsageAsync(
            characterId, skillDefId, WebSkillUsageType.RoutineUse,
            baseExperience: 1, targetId: target2);

        // Assert
        Assert.True(result1.ProgressionApplied);
        Assert.True(result2.ProgressionApplied);
    }

    [Fact]
    public async Task LogUsageAsync_ZeroFatigue_BlocksProgression()
    {
        // Arrange
        var dbName = $"SkillProg_ZeroFat_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings();

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        // Set character's fatigue to 0
        var character = await seedContext.Characters.FindAsync(characterId);
        character!.CurrentFatigue = 0;
        await seedContext.SaveChangesAsync();

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);

        // Act
        var result = await service.LogUsageAsync(characterId, skillDefId, WebSkillUsageType.RoutineUse, baseExperience: 1);

        // Assert
        Assert.False(result.ProgressionApplied);
        Assert.Equal("Character exhausted (FAT = 0)", result.BlockedReason);
    }

    [Fact]
    public async Task LogUsageAsync_LowFatigue_ReducesMultiplier()
    {
        // Arrange
        var dbName = $"SkillProg_LowFat_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings(new SkillProgressionSettings
        {
            LowFatThresholdPercent = 0.25m,
            LowFatMultiplier = 0.5m,
            DailyFreshUsageThreshold = 1000 // Disable for cleaner test
        });

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        // Set character's fatigue to ~20% (below 25% threshold)
        var character = await seedContext.Characters.FindAsync(characterId);
        character!.CurrentFatigue = 3; // 3/15 = 20%
        await seedContext.SaveChangesAsync();

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);

        // Act
        var result = await service.LogUsageAsync(characterId, skillDefId, WebSkillUsageType.RoutineUse, baseExperience: 1);

        // Assert
        Assert.True(result.ProgressionApplied);
        Assert.Equal(0.5m, result.FatigueMultiplier);
    }

    [Fact]
    public async Task LogUsageAsync_ChallengeMultiplier_DifficultOpponent()
    {
        // Arrange
        var dbName = $"SkillProg_Challenge_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings(new SkillProgressionSettings
        {
            DailyFreshUsageThreshold = 1000 // Disable for cleaner test
        });

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        // Create a character skill at level 2
        var charSkill = new WebCharacterSkill
        {
            CharacterId = characterId,
            SkillDefinitionId = skillDefId,
            Level = 2,
            Experience = 80,
            LearnedAt = DateTimeOffset.UtcNow
        };
        seedContext.CharacterSkills.Add(charSkill);
        await seedContext.SaveChangesAsync();

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);

        // Act - Fight a level 8 opponent (diff = +6, "Difficult" range)
        var result = await service.LogUsageAsync(
            characterId, skillDefId, WebSkillUsageType.RoutineUse,
            baseExperience: 1, targetDifficulty: 8);

        // Assert - Should get 1.5x challenge multiplier
        Assert.True(result.ProgressionApplied);
        Assert.Equal(1.5m, result.ChallengeMultiplier);
    }

    [Fact]
    public async Task LogUsageAsync_ChallengeMultiplier_TrivialOpponent()
    {
        // Arrange
        var dbName = $"SkillProg_Trivial_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings(new SkillProgressionSettings
        {
            DailyFreshUsageThreshold = 1000
        });

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        // Create a character skill at level 12
        var charSkill = new WebCharacterSkill
        {
            CharacterId = characterId,
            SkillDefinitionId = skillDefId,
            Level = 12,
            Experience = 10000,
            LearnedAt = DateTimeOffset.UtcNow
        };
        seedContext.CharacterSkills.Add(charSkill);
        await seedContext.SaveChangesAsync();

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);

        // Act - Fight a level 1 opponent (diff = -11, "Trivial" range)
        var result = await service.LogUsageAsync(
            characterId, skillDefId, WebSkillUsageType.RoutineUse,
            baseExperience: 1, targetDifficulty: 1);

        // Assert - Should get 0.1x trivial multiplier
        Assert.True(result.ProgressionApplied);
        Assert.Equal(0.1m, result.ChallengeMultiplier);
    }

    [Fact]
    public async Task LogUsageAsync_FailedAction_ReducedMultiplier()
    {
        // Arrange
        var dbName = $"SkillProg_Failed_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings(new SkillProgressionSettings
        {
            FailedActionMultiplier = 0.2m,
            DailyFreshUsageThreshold = 1000
        });

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);

        // Act
        var result = await service.LogUsageAsync(
            characterId, skillDefId, WebSkillUsageType.RoutineUse,
            baseExperience: 1, actionSucceeded: false);

        // Assert - Usage type multiplier should be reduced
        Assert.True(result.ProgressionApplied);
        Assert.Equal(0.2m, result.UsageTypeMultiplier); // 1.0 * 0.2
    }

    [Fact]
    public async Task LogUsageAsync_CriticalSuccess_BonusMultiplier()
    {
        // Arrange
        var dbName = $"SkillProg_Crit_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings(new SkillProgressionSettings
        {
            DailyFreshUsageThreshold = 1000
        });

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);

        // Act
        var result = await service.LogUsageAsync(
            characterId, skillDefId, WebSkillUsageType.CriticalSuccess,
            baseExperience: 1);

        // Assert
        Assert.True(result.ProgressionApplied);
        Assert.Equal(2.0m, result.UsageTypeMultiplier);
    }

    [Fact]
    public async Task LogUsageAsync_SkillLevelUp_ReturnsDidLevelUp()
    {
        // Arrange
        var dbName = $"SkillProg_LevelUp_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings(new SkillProgressionSettings
        {
            DailyFreshUsageThreshold = 1000
        });

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        // Create character skill at level 0 with 24 XP (needs 25 for level 1)
        var charSkill = new WebCharacterSkill
        {
            CharacterId = characterId,
            SkillDefinitionId = skillDefId,
            Level = 0,
            Experience = 24,
            LearnedAt = DateTimeOffset.UtcNow
        };
        seedContext.CharacterSkills.Add(charSkill);
        await seedContext.SaveChangesAsync();

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);

        // Act - Add 1 XP to push over level threshold
        var result = await service.LogUsageAsync(
            characterId, skillDefId, WebSkillUsageType.RoutineUse,
            baseExperience: 1);

        // Assert
        Assert.True(result.ProgressionApplied);
        Assert.True(result.DidLevelUp);
        Assert.Equal(1, result.NewLevel);
    }

    [Fact]
    public async Task GetHourlyUsageCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var dbName = $"SkillProg_GetHourly_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings();

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);

        // Act - Use skill 5 times
        for (int i = 0; i < 5; i++)
        {
            await service.LogUsageAsync(characterId, skillDefId, WebSkillUsageType.RoutineUse);
        }

        var count = await service.GetHourlyUsageCountAsync(characterId, skillDefId);

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task IsTargetOnCooldownAsync_ReturnsTrueWhenOnCooldown()
    {
        // Arrange
        var dbName = $"SkillProg_IsCooldown_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var settings = CreateSettings(new SkillProgressionSettings
        {
            CombatTargetCooldownSeconds = 60
        });

        await using var seedContext = await factory.CreateDbContextAsync();
        var (characterId, skillDefId) = await SeedTestDataAsync(seedContext);

        var service = new SkillProgressionService(factory, settings, NullLogger<SkillProgressionService>.Instance);
        var targetId = "npc:" + Guid.NewGuid();

        // Use skill once
        await service.LogUsageAsync(characterId, skillDefId, WebSkillUsageType.RoutineUse, targetId: targetId);

        // Act
        var isOnCooldown = await service.IsTargetOnCooldownAsync(characterId, skillDefId, targetId);

        // Assert
        Assert.True(isOnCooldown);
    }

    // Helper class for in-memory database testing
    private class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public ApplicationDbContext CreateDbContext()
        {
            return new ApplicationDbContext(_options);
        }
    }
}
