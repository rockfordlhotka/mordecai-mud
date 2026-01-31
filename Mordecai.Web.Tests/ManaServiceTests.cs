using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using Mordecai.Web.Services;
using Xunit;

using WebSkillCategory = Mordecai.Web.Data.SkillCategory;
using WebSkillDefinition = Mordecai.Web.Data.SkillDefinition;
using WebCharacterSkill = Mordecai.Web.Data.CharacterSkill;

namespace Mordecai.Web.Tests;

public class ManaServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ManaService _service;
    private readonly Guid _testCharacterId;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public ManaServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _contextFactory = new TestDbContextFactory(_options);
        _service = new ManaService(_contextFactory, NullLogger<ManaService>.Instance);

        _testCharacterId = Guid.NewGuid();

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test character with Focus = 12
        var character = new Character
        {
            Id = _testCharacterId,
            Name = "TestMage",
            UserId = "test-user",
            Focus = 12, // WIL equivalent
            Physicality = 10,
            Dodge = 10,
            Drive = 10,
            Reasoning = 10,
            Awareness = 10,
            Bearing = 10
        };
        _context.Characters.Add(character);

        // Create skill category for mana recovery
        var manaCategory = new WebSkillCategory
        {
            Id = 100,
            Name = "Mana Recovery",
            Description = "Skills for mana regeneration",
            DefaultBaseCost = 30,
            DefaultMultiplier = 2.1m,
            IsActive = true
        };
        _context.SkillCategories.Add(manaCategory);

        // Create Fire Mana Recovery skill definition
        var fireRecovery = new WebSkillDefinition
        {
            Id = 101,
            Name = "Fire Mana Recovery",
            Description = "Governs how quickly Fire school mana regenerates",
            CategoryId = 100,
            BaseCost = 30,
            Multiplier = 2.1m,
            RelatedAttribute = "WIL",
            IsActive = true
        };
        _context.SkillDefinitions.Add(fireRecovery);

        // Create Healing Mana Recovery skill definition
        var healingRecovery = new WebSkillDefinition
        {
            Id = 102,
            Name = "Healing Mana Recovery",
            Description = "Governs how quickly Healing school mana regenerates",
            CategoryId = 100,
            BaseCost = 30,
            Multiplier = 2.1m,
            RelatedAttribute = "WIL",
            IsActive = true
        };
        _context.SkillDefinitions.Add(healingRecovery);

        // Give character Fire Mana Recovery at level 3
        var characterSkill = new WebCharacterSkill
        {
            Id = 1,
            CharacterId = _testCharacterId,
            SkillDefinitionId = 101,
            Level = 3,
            Experience = 100
        };
        _context.CharacterSkills.Add(characterSkill);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // ==================
    // GetOrCreateManaPoolAsync Tests
    // ==================

    [Fact]
    public async Task GetOrCreateManaPoolAsync_CreatesNewPool()
    {
        // Act
        var pool = await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);

        // Assert
        Assert.NotNull(pool);
        Assert.Equal(_testCharacterId, pool.CharacterId);
        Assert.Equal(MagicSchool.Fire, pool.School);
        // MaxMana = Base(10) + Focus(12) + Skill(3) = 25
        Assert.Equal(25, pool.MaxMana);
        Assert.Equal(25, pool.CurrentMana); // Starts full
    }

    [Fact]
    public async Task GetOrCreateManaPoolAsync_ReturnsExistingPool()
    {
        // Arrange - Create pool first
        var firstPool = await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);

        // Modify current mana
        await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Fire, 10);

        // Act - Get pool again
        var secondPool = await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);

        // Assert - Should be same pool with modified mana
        Assert.Equal(firstPool.Id, secondPool.Id);
        Assert.Equal(15, secondPool.CurrentMana); // 25 - 10 = 15
    }

    [Fact]
    public async Task GetOrCreateManaPoolAsync_NoSkill_UsesBaseAndFocusOnly()
    {
        // Act - Healing has no skill for this character
        var pool = await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Healing);

        // Assert
        // MaxMana = Base(10) + Focus(12) + Skill(0) = 22
        Assert.Equal(22, pool.MaxMana);
    }

    // ==================
    // ConsumeManaAsync Tests
    // ==================

    [Fact]
    public async Task ConsumeManaAsync_SuccessfulConsumption()
    {
        // Arrange
        await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);

        // Act
        var result = await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Fire, 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(25, result.PreviousMana);
        Assert.Equal(15, result.CurrentMana);
        Assert.Equal(-10, result.AmountChanged);
    }

    [Fact]
    public async Task ConsumeManaAsync_InsufficientMana_ReturnsFalse()
    {
        // Arrange
        await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);

        // Act - Try to consume more than available
        var result = await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Fire, 50);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Insufficient", result.Message);
        Assert.Equal(25, result.CurrentMana); // Unchanged
    }

    [Fact]
    public async Task ConsumeManaAsync_ZeroAmount_ReturnsFalse()
    {
        // Act
        var result = await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Fire, 0);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("positive", result.Message);
    }

    [Fact]
    public async Task ConsumeManaAsync_CreatesPoolIfNotExists()
    {
        // Act - Consume from non-existent pool (will be created first)
        var result = await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Lightning, 5);

        // Assert - Pool should be created and consumption should succeed
        Assert.True(result.Success);
        // Lightning pool: Base(10) + Focus(12) + Skill(0) = 22
        Assert.Equal(22, result.PreviousMana);
        Assert.Equal(17, result.CurrentMana);
    }

    // ==================
    // HasEnoughManaAsync Tests
    // ==================

    [Fact]
    public async Task HasEnoughManaAsync_SufficientMana_ReturnsTrue()
    {
        // Arrange
        await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);

        // Act
        var hasEnough = await _service.HasEnoughManaAsync(_testCharacterId, MagicSchool.Fire, 20);

        // Assert
        Assert.True(hasEnough);
    }

    [Fact]
    public async Task HasEnoughManaAsync_InsufficientMana_ReturnsFalse()
    {
        // Arrange
        await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);

        // Act
        var hasEnough = await _service.HasEnoughManaAsync(_testCharacterId, MagicSchool.Fire, 50);

        // Assert
        Assert.False(hasEnough);
    }

    [Fact]
    public async Task HasEnoughManaAsync_NoPool_ReturnsFalseForPositiveAmount()
    {
        // Act - No pool exists
        var hasEnough = await _service.HasEnoughManaAsync(_testCharacterId, MagicSchool.Illusion, 1);

        // Assert
        Assert.False(hasEnough);
    }

    // ==================
    // AddManaAsync Tests
    // ==================

    [Fact]
    public async Task AddManaAsync_AddsToPool()
    {
        // Arrange
        await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Fire, 20);

        // Act
        var result = await _service.AddManaAsync(_testCharacterId, MagicSchool.Fire, 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5, result.PreviousMana); // 25 - 20 = 5
        Assert.Equal(15, result.CurrentMana); // 5 + 10 = 15
        Assert.Equal(10, result.AmountChanged);
    }

    [Fact]
    public async Task AddManaAsync_CapsAtMax()
    {
        // Arrange
        await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Fire, 5);

        // Act - Add more than needed to fill
        var result = await _service.AddManaAsync(_testCharacterId, MagicSchool.Fire, 20);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(20, result.PreviousMana); // 25 - 5 = 20
        Assert.Equal(25, result.CurrentMana); // Capped at max
        Assert.Equal(5, result.AmountChanged); // Only added 5
        Assert.Contains("capped", result.Message);
    }

    // ==================
    // GetRegenRateAsync Tests
    // ==================

    [Fact]
    public async Task GetRegenRateAsync_CalculatesCorrectly()
    {
        // Act
        // Formula: Skill(3) + Focus(12)/2 = 3 + 6 = 9
        var rate = await _service.GetRegenRateAsync(_testCharacterId, MagicSchool.Fire);

        // Assert
        Assert.Equal(9m, rate);
    }

    [Fact]
    public async Task GetRegenRateAsync_NoSkill_UsesFocusOnly()
    {
        // Act - Healing has no skill
        // Formula: Skill(0) + Focus(12)/2 = 0 + 6 = 6
        var rate = await _service.GetRegenRateAsync(_testCharacterId, MagicSchool.Healing);

        // Assert
        Assert.Equal(6m, rate);
    }

    // ==================
    // CalculateMaxManaAsync Tests
    // ==================

    [Fact]
    public async Task CalculateMaxManaAsync_CalculatesCorrectly()
    {
        // Act
        // Formula: Base(10) + Focus(12) + Skill(3) = 25
        var max = await _service.CalculateMaxManaAsync(_testCharacterId, MagicSchool.Fire);

        // Assert
        Assert.Equal(25, max);
    }

    // ==================
    // ProcessRegenAsync Tests
    // ==================

    [Fact]
    public async Task ProcessRegenAsync_RegeneratesMana()
    {
        // Arrange - Create pool and consume some mana
        var pool = await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Fire, 15);

        // Manually set LastRegenAt to 1 minute ago to simulate time passing
        await using (var ctx = new ApplicationDbContext(_options))
        {
            var p = await ctx.CharacterManaPools.FirstAsync(mp => mp.Id == pool.Id);
            p.LastRegenAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await ctx.SaveChangesAsync();
        }

        // Act
        var regen = await _service.ProcessRegenAsync(_testCharacterId);

        // Assert
        // Regen rate is 9 per minute, 1 minute passed = 9 mana
        Assert.Equal(9, regen);

        // Verify new mana level
        var current = await _service.GetCurrentManaAsync(_testCharacterId, MagicSchool.Fire);
        Assert.Equal(19, current); // 10 + 9 = 19
    }

    [Fact]
    public async Task ProcessRegenAsync_DoesNotExceedMax()
    {
        // Arrange - Create pool and consume small amount
        var pool = await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Fire, 3);

        // Manually set LastRegenAt to 1 minute ago
        await using (var ctx = new ApplicationDbContext(_options))
        {
            var p = await ctx.CharacterManaPools.FirstAsync(mp => mp.Id == pool.Id);
            p.LastRegenAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await ctx.SaveChangesAsync();
        }

        // Act
        var regen = await _service.ProcessRegenAsync(_testCharacterId);

        // Assert - Should only regen 3 (to cap at 25)
        Assert.Equal(3, regen);
        var current = await _service.GetCurrentManaAsync(_testCharacterId, MagicSchool.Fire);
        Assert.Equal(25, current);
    }

    // ==================
    // RestoreAllManaAsync Tests
    // ==================

    [Fact]
    public async Task RestoreAllManaAsync_RestoresAllPools()
    {
        // Arrange - Create multiple pools and consume mana
        await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Healing);
        await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Fire, 20);
        await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Healing, 10);

        // Act
        await _service.RestoreAllManaAsync(_testCharacterId);

        // Assert
        var fireMana = await _service.GetCurrentManaAsync(_testCharacterId, MagicSchool.Fire);
        var healingMana = await _service.GetCurrentManaAsync(_testCharacterId, MagicSchool.Healing);
        Assert.Equal(25, fireMana);
        Assert.Equal(22, healingMana);
    }

    // ==================
    // GetManaSummaryAsync Tests
    // ==================

    [Fact]
    public async Task GetManaSummaryAsync_ReturnsAllPools()
    {
        // Arrange
        await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Healing);
        await _service.ConsumeManaAsync(_testCharacterId, MagicSchool.Fire, 10);

        // Act
        var summary = await _service.GetManaSummaryAsync(_testCharacterId);

        // Assert
        Assert.Equal(2, summary.Pools.Count);
        Assert.True(summary.Pools.ContainsKey(MagicSchool.Fire));
        Assert.True(summary.Pools.ContainsKey(MagicSchool.Healing));
        Assert.Equal(15, summary.Pools[MagicSchool.Fire].CurrentMana);
        Assert.Equal(22, summary.Pools[MagicSchool.Healing].CurrentMana);
        Assert.Equal(37, summary.TotalCurrentMana); // 15 + 22
        Assert.Equal(47, summary.TotalMaxMana); // 25 + 22
    }

    // ==================
    // UpdateMaxManaAsync Tests
    // ==================

    [Fact]
    public async Task UpdateMaxManaAsync_RecalculatesMaxMana()
    {
        // Arrange - Create pool
        await _service.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);

        // Increase skill level
        await using (var ctx = new ApplicationDbContext(_options))
        {
            var skill = await ctx.CharacterSkills.FirstAsync(cs => 
                cs.CharacterId == _testCharacterId && cs.SkillDefinitionId == 101);
            skill.Level = 5; // Was 3
            await ctx.SaveChangesAsync();
        }

        // Act
        await _service.UpdateMaxManaAsync(_testCharacterId);

        // Assert - New max = Base(10) + Focus(12) + Skill(5) = 27
        var pools = await _service.GetManaPoolsAsync(_testCharacterId);
        Assert.Equal(27, pools[0].MaxMana);
    }

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
