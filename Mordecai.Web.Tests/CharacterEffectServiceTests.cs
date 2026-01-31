using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using Mordecai.Web.Services;
using Xunit;

namespace Mordecai.Web.Tests;

public class CharacterEffectServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly CharacterEffectService _service;
    private readonly Guid _testCharacterId;
    private readonly Guid _sourceCharacterId;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    // Effect definition IDs from seed data
    private const int WoundEffectId = 1;
    private const int StrengthBuffId = 2;
    private const int PoisonEffectId = 3;
    private const int BattleFocusId = 4;
    private const int IronSkinId = 5;
    private const int StunnedId = 6;

    public CharacterEffectServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _contextFactory = new TestDbContextFactory(_options);
        _service = new CharacterEffectService(_contextFactory, NullLogger<CharacterEffectService>.Instance);

        _testCharacterId = Guid.NewGuid();
        _sourceCharacterId = Guid.NewGuid();

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create wound effect definition
        var wound = new CharacterEffectDefinition
        {
            Id = WoundEffectId,
            Name = "Wound",
            Description = "A serious injury",
            EffectType = CharacterEffectType.Wound,
            DefaultDurationSeconds = 0, // Permanent
            DefaultIntensity = 1.0m,
            IsStackable = true,
            MaxStacks = 10,
            TickIntervalSeconds = 6,
            IsVisible = true,
            IsSystemEffect = true,
            CreatedBy = "system"
        };
        wound.Impacts.Add(new CharacterEffectImpact
        {
            Id = 1,
            CharacterEffectDefinitionId = WoundEffectId,
            ImpactType = CharacterEffectImpactType.PeriodicFatigueDamage,
            ModifierValue = 1,
            ScalesWithIntensity = false
        });
        _context.CharacterEffectDefinitions.Add(wound);

        // Create strength buff
        var strengthBuff = new CharacterEffectDefinition
        {
            Id = StrengthBuffId,
            Name = "Strength",
            Description = "Enhanced physical strength",
            EffectType = CharacterEffectType.Buff,
            DefaultDurationSeconds = 300, // 5 minutes
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IsDispellable = true,
            CreatedBy = "system"
        };
        strengthBuff.Impacts.Add(new CharacterEffectImpact
        {
            Id = 2,
            CharacterEffectDefinitionId = StrengthBuffId,
            ImpactType = CharacterEffectImpactType.ModifyAttribute,
            TargetAttribute = "Physicality",
            ModifierValue = 2,
            ScalesWithIntensity = true
        });
        _context.CharacterEffectDefinitions.Add(strengthBuff);

        // Create poison DoT
        var poison = new CharacterEffectDefinition
        {
            Id = PoisonEffectId,
            Name = "Poison",
            Description = "Toxic damage over time",
            EffectType = CharacterEffectType.DamageOverTime,
            DefaultDurationSeconds = 60,
            DefaultIntensity = 1.0m,
            IsStackable = true,
            MaxStacks = 5,
            TickIntervalSeconds = 6,
            IsVisible = true,
            IsDispellable = true,
            CreatedBy = "system"
        };
        poison.Impacts.Add(new CharacterEffectImpact
        {
            Id = 3,
            CharacterEffectDefinitionId = PoisonEffectId,
            ImpactType = CharacterEffectImpactType.PeriodicVitalityDamage,
            ModifierValue = 2,
            DamageType = "Poison",
            ScalesWithIntensity = true
        });
        _context.CharacterEffectDefinitions.Add(poison);

        // Create battle focus buff (+AV)
        var battleFocus = new CharacterEffectDefinition
        {
            Id = BattleFocusId,
            Name = "Battle Focus",
            Description = "Enhanced combat accuracy",
            EffectType = CharacterEffectType.Buff,
            DefaultDurationSeconds = 180,
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IsDispellable = true,
            CreatedBy = "system"
        };
        battleFocus.Impacts.Add(new CharacterEffectImpact
        {
            Id = 4,
            CharacterEffectDefinitionId = BattleFocusId,
            ImpactType = CharacterEffectImpactType.ModifyAttackValue,
            ModifierValue = 2,
            ScalesWithIntensity = true
        });
        _context.CharacterEffectDefinitions.Add(battleFocus);

        // Create iron skin buff (+SV)
        var ironSkin = new CharacterEffectDefinition
        {
            Id = IronSkinId,
            Name = "Iron Skin",
            Description = "Enhanced defense",
            EffectType = CharacterEffectType.Buff,
            DefaultDurationSeconds = 180,
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IsDispellable = true,
            CreatedBy = "system"
        };
        ironSkin.Impacts.Add(new CharacterEffectImpact
        {
            Id = 5,
            CharacterEffectDefinitionId = IronSkinId,
            ImpactType = CharacterEffectImpactType.ModifyDefenseValue,
            ModifierValue = 2,
            ScalesWithIntensity = true
        });
        _context.CharacterEffectDefinitions.Add(ironSkin);

        // Create stun effect
        var stunned = new CharacterEffectDefinition
        {
            Id = StunnedId,
            Name = "Stunned",
            Description = "Unable to act",
            EffectType = CharacterEffectType.StatusEffect,
            DefaultDurationSeconds = 6,
            DefaultIntensity = 1.0m,
            IsStackable = false,
            MaxStacks = 1,
            TickIntervalSeconds = 0,
            IsVisible = true,
            IsDispellable = false,
            CreatedBy = "system"
        };
        stunned.Impacts.Add(new CharacterEffectImpact
        {
            Id = 6,
            CharacterEffectDefinitionId = StunnedId,
            ImpactType = CharacterEffectImpactType.PreventActions,
            ModifierValue = 1,
            ScalesWithIntensity = false
        });
        _context.CharacterEffectDefinitions.Add(stunned);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // ==================
    // ApplyEffectAsync Tests
    // ==================

    [Fact]
    public async Task ApplyEffectAsync_AppliesBuffSuccessfully()
    {
        // Act
        var result = await _service.ApplyEffectAsync(
            _testCharacterId,
            StrengthBuffId,
            _sourceCharacterId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Effect);
        Assert.Equal(1, result.Effect.CurrentStacks);
        Assert.NotNull(result.Effect.ExpiresAt);
    }

    [Fact]
    public async Task ApplyEffectByNameAsync_AppliesBuffSuccessfully()
    {
        // Act
        var result = await _service.ApplyEffectByNameAsync(
            _testCharacterId,
            "Strength",
            _sourceCharacterId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Effect);
    }

    [Fact]
    public async Task ApplyEffectAsync_NonStackableEffect_RefreshesOnReapply()
    {
        // Arrange - Apply effect first time
        await _service.ApplyEffectAsync(_testCharacterId, StrengthBuffId, _sourceCharacterId);
        
        // Wait a bit to ensure time passes
        await Task.Delay(10);
        
        // Act - Apply same effect again
        var result = await _service.ApplyEffectAsync(_testCharacterId, StrengthBuffId, _sourceCharacterId);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("refreshed", result.Message.ToLower());
        Assert.Equal(1, result.Effect!.CurrentStacks); // Still 1 stack
    }

    [Fact]
    public async Task ApplyEffectAsync_StackableEffect_IncreasesStacks()
    {
        // Arrange - Apply poison first time
        await _service.ApplyEffectAsync(_testCharacterId, PoisonEffectId, _sourceCharacterId);

        // Act - Apply poison second time
        var result = await _service.ApplyEffectAsync(_testCharacterId, PoisonEffectId, _sourceCharacterId);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("stacked", result.Message.ToLower());
        Assert.Equal(2, result.Effect!.CurrentStacks);
    }

    [Fact]
    public async Task ApplyEffectAsync_StackableEffect_RespectsMaxStacks()
    {
        // Arrange - Apply poison to max stacks (5)
        for (int i = 0; i < 5; i++)
        {
            await _service.ApplyEffectAsync(_testCharacterId, PoisonEffectId, _sourceCharacterId);
        }

        // Act - Try to apply again
        var result = await _service.ApplyEffectAsync(_testCharacterId, PoisonEffectId, _sourceCharacterId);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("max stacks", result.Message.ToLower());
        Assert.Equal(5, result.Effect!.CurrentStacks); // Still at max
    }

    [Fact]
    public async Task ApplyEffectByNameAsync_UnknownEffect_ReturnsFalse()
    {
        // Act
        var result = await _service.ApplyEffectByNameAsync(_testCharacterId, "NonExistent", _sourceCharacterId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message.ToLower());
    }

    // ==================
    // ApplyWoundAsync Tests
    // ==================

    [Fact]
    public async Task ApplyWoundAsync_CreatesWoundEffect()
    {
        // Act
        var result = await _service.ApplyWoundAsync(
            _testCharacterId,
            BodyLocation.Torso,
            _sourceCharacterId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Effect);
        Assert.Equal(BodyLocation.Torso, result.Effect.BodyLocation);
        Assert.Equal(1, result.Effect.CurrentStacks);
        Assert.Null(result.Effect.ExpiresAt); // Wounds don't expire automatically
    }

    [Fact]
    public async Task ApplyWoundAsync_SameLocation_StacksWounds()
    {
        // Arrange - Apply first wound
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);

        // Act - Apply second wound to same location
        var result = await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Effect!.CurrentStacks);
    }

    [Fact]
    public async Task ApplyWoundAsync_DifferentLocations_CreatesSeparateEffects()
    {
        // Act
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.LeftArm, _sourceCharacterId);

        // Assert
        var wounds = await _service.GetActiveEffectsAsync(_testCharacterId);
        Assert.Equal(2, wounds.Count);
        Assert.Contains(wounds, w => w.BodyLocation == BodyLocation.Torso);
        Assert.Contains(wounds, w => w.BodyLocation == BodyLocation.LeftArm);
    }

    // ==================
    // GetEffectSummaryAsync Tests
    // ==================

    [Fact]
    public async Task GetEffectSummaryAsync_WoundsApplyAVPenalty()
    {
        // Arrange - Apply 3 wounds
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.LeftArm, _sourceCharacterId);

        // Act
        var summary = await _service.GetEffectSummaryAsync(_testCharacterId);

        // Assert - 3 wounds = -6 AV
        Assert.Equal(-6, summary.TotalAttackValueModifier);
        Assert.Equal(3, summary.WoundCount);
    }

    [Fact]
    public async Task GetEffectSummaryAsync_BuffsModifyAttributes()
    {
        // Arrange
        await _service.ApplyEffectAsync(_testCharacterId, StrengthBuffId, _sourceCharacterId);

        // Act
        var summary = await _service.GetEffectSummaryAsync(_testCharacterId);

        // Assert
        Assert.Contains(summary.AttributeModifiers, kvp => 
            kvp.Key == "Physicality" && kvp.Value == 2);
    }

    [Fact]
    public async Task GetEffectSummaryAsync_BattleFocusModifiesAV()
    {
        // Arrange
        await _service.ApplyEffectAsync(_testCharacterId, BattleFocusId, _sourceCharacterId);

        // Act
        var summary = await _service.GetEffectSummaryAsync(_testCharacterId);

        // Assert
        Assert.Equal(2, summary.TotalAttackValueModifier);
    }

    [Fact]
    public async Task GetEffectSummaryAsync_IronSkinModifiesSV()
    {
        // Arrange
        await _service.ApplyEffectAsync(_testCharacterId, IronSkinId, _sourceCharacterId);

        // Act
        var summary = await _service.GetEffectSummaryAsync(_testCharacterId);

        // Assert
        Assert.Equal(2, summary.TotalDefenseValueModifier);
    }

    [Fact]
    public async Task GetEffectSummaryAsync_CombinesMultipleEffects()
    {
        // Arrange
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId); // -2 AV
        await _service.ApplyEffectAsync(_testCharacterId, BattleFocusId, _sourceCharacterId); // +2 AV
        await _service.ApplyEffectAsync(_testCharacterId, IronSkinId, _sourceCharacterId); // +2 SV

        // Act
        var summary = await _service.GetEffectSummaryAsync(_testCharacterId);

        // Assert
        Assert.Equal(0, summary.TotalAttackValueModifier); // -2 + 2 = 0
        Assert.Equal(2, summary.TotalDefenseValueModifier);
        Assert.Equal(1, summary.WoundCount);
    }

    [Fact]
    public async Task GetEffectSummaryAsync_StunPreventsActions()
    {
        // Arrange
        await _service.ApplyEffectAsync(_testCharacterId, StunnedId, _sourceCharacterId);

        // Act
        var summary = await _service.GetEffectSummaryAsync(_testCharacterId);

        // Assert
        Assert.False(summary.CanAct);
    }

    // ==================
    // RemoveEffectAsync Tests
    // ==================

    [Fact]
    public async Task RemoveEffectAsync_RemovesExistingEffect()
    {
        // Arrange
        var applyResult = await _service.ApplyEffectAsync(_testCharacterId, StrengthBuffId, _sourceCharacterId);

        // Act
        var removed = await _service.RemoveEffectAsync(applyResult.Effect!.Id);

        // Assert
        Assert.True(removed);
        var effects = await _service.GetActiveEffectsAsync(_testCharacterId);
        Assert.Empty(effects);
    }

    [Fact]
    public async Task RemoveEffectAsync_NonExistentEffect_ReturnsFalse()
    {
        // Act
        var removed = await _service.RemoveEffectAsync(999999);

        // Assert
        Assert.False(removed);
    }

    // ==================
    // HealWoundsAsync Tests
    // ==================

    [Fact]
    public async Task HealWoundsAsync_RemovesAllWoundsFromLocation()
    {
        // Arrange
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.LeftArm, _sourceCharacterId);

        // Act - Heal all wounds on torso (count=0 means all)
        var healed = await _service.HealWoundsAsync(_testCharacterId, 0, BodyLocation.Torso);

        // Assert
        Assert.Equal(2, healed); // 2 stacks on torso
        var summary = await _service.GetEffectSummaryAsync(_testCharacterId);
        Assert.Equal(1, summary.WoundCount); // Only left arm wound remains
    }

    [Fact]
    public async Task HealWoundsAsync_PartialHeal_ReducesStacks()
    {
        // Arrange
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);

        // Act - Heal only 1 wound
        var healed = await _service.HealWoundsAsync(_testCharacterId, 1, BodyLocation.Torso);

        // Assert
        Assert.Equal(1, healed);
        var effects = await _service.GetActiveEffectsAsync(_testCharacterId);
        var torsoWound = effects.FirstOrDefault(e => e.BodyLocation == BodyLocation.Torso);
        Assert.NotNull(torsoWound);
        Assert.Equal(2, torsoWound.CurrentStacks); // 3 - 1 = 2
    }

    [Fact]
    public async Task HealWoundsAsync_NoLocation_HealsAllWounds()
    {
        // Arrange
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.LeftArm, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.RightLeg, _sourceCharacterId);

        // Act - Heal all wounds (no location specified, count=0 means all)
        var healed = await _service.HealWoundsAsync(_testCharacterId, 0, null);

        // Assert
        Assert.Equal(3, healed);
        var summary = await _service.GetEffectSummaryAsync(_testCharacterId);
        Assert.Equal(0, summary.WoundCount);
    }

    // ==================
    // HasEffectAsync Tests
    // ==================

    [Fact]
    public async Task HasEffectAsync_WithActiveEffect_ReturnsTrue()
    {
        // Arrange
        await _service.ApplyEffectAsync(_testCharacterId, StrengthBuffId, _sourceCharacterId);

        // Act
        var hasEffect = await _service.HasEffectAsync(_testCharacterId, StrengthBuffId);

        // Assert
        Assert.True(hasEffect);
    }

    [Fact]
    public async Task HasEffectAsync_WithoutEffect_ReturnsFalse()
    {
        // Act
        var hasEffect = await _service.HasEffectAsync(_testCharacterId, StrengthBuffId);

        // Assert
        Assert.False(hasEffect);
    }

    [Fact]
    public async Task HasEffectByNameAsync_WithActiveEffect_ReturnsTrue()
    {
        // Arrange
        await _service.ApplyEffectAsync(_testCharacterId, StrengthBuffId, _sourceCharacterId);

        // Act
        var hasEffect = await _service.HasEffectByNameAsync(_testCharacterId, "Strength");

        // Assert
        Assert.True(hasEffect);
    }

    // ==================
    // RemoveEffectsByTypeAsync Tests
    // ==================

    [Fact]
    public async Task RemoveEffectsByTypeAsync_RemovesAllBuffs()
    {
        // Arrange
        await _service.ApplyEffectAsync(_testCharacterId, StrengthBuffId, _sourceCharacterId); // Buff
        await _service.ApplyEffectAsync(_testCharacterId, BattleFocusId, _sourceCharacterId); // Buff
        await _service.ApplyEffectAsync(_testCharacterId, StunnedId, _sourceCharacterId); // Status effect

        // Act
        var removed = await _service.RemoveEffectsByTypeAsync(_testCharacterId, CharacterEffectType.Buff);

        // Assert
        Assert.Equal(2, removed);
        var effects = await _service.GetActiveEffectsAsync(_testCharacterId);
        Assert.Single(effects);
    }

    // ==================
    // GetWoundCountAsync Tests
    // ==================

    [Fact]
    public async Task GetWoundCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Head, _sourceCharacterId);

        // Act
        var totalWounds = await _service.GetWoundCountAsync(_testCharacterId);

        // Assert
        Assert.Equal(3, totalWounds);
    }

    [Fact]
    public async Task GetWoundsByLocationAsync_ReturnsCorrectCounts()
    {
        // Arrange
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Head, _sourceCharacterId);

        // Act
        var woundsByLocation = await _service.GetWoundsByLocationAsync(_testCharacterId);

        // Assert
        Assert.Equal(2, woundsByLocation.Count);
        Assert.Equal(2, woundsByLocation[BodyLocation.Torso]);
        Assert.Equal(1, woundsByLocation[BodyLocation.Head]);
    }

    // ==================
    // CleanupExpiredEffectsAsync Tests
    // ==================

    [Fact]
    public async Task CleanupExpiredEffectsAsync_RemovesExpiredEffects()
    {
        // Arrange - Apply buff with instant expiry (duration = 0)
        await _service.ApplyEffectAsync(
            _testCharacterId,
            StrengthBuffId,
            _sourceCharacterId,
            durationSeconds: 0);

        // Act
        var expired = await _service.CleanupExpiredEffectsAsync();

        // Assert
        Assert.Equal(1, expired);
        var effects = await _service.GetActiveEffectsAsync(_testCharacterId);
        Assert.Empty(effects);
    }

    // ==================
    // Integration Tests
    // ==================

    [Fact]
    public async Task ComplexCombatScenario_CalculatesCorrectModifiers()
    {
        // Arrange - Simulate a character in combat with various effects
        
        // 2 wounds on torso (-4 AV from wounds)
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        await _service.ApplyWoundAsync(_testCharacterId, BodyLocation.Torso, _sourceCharacterId);
        
        // Strength buff (+2 Physicality)
        await _service.ApplyEffectAsync(_testCharacterId, StrengthBuffId, _sourceCharacterId);
        
        // Battle Focus (+2 AV)
        await _service.ApplyEffectAsync(_testCharacterId, BattleFocusId, _sourceCharacterId);
        
        // Iron Skin (+2 SV)
        await _service.ApplyEffectAsync(_testCharacterId, IronSkinId, _sourceCharacterId);
        
        // Poison (3 stacks)
        await _service.ApplyEffectAsync(_testCharacterId, PoisonEffectId, _sourceCharacterId);
        await _service.ApplyEffectAsync(_testCharacterId, PoisonEffectId, _sourceCharacterId);
        await _service.ApplyEffectAsync(_testCharacterId, PoisonEffectId, _sourceCharacterId);

        // Act
        var summary = await _service.GetEffectSummaryAsync(_testCharacterId);

        // Assert
        Assert.Equal(2, summary.WoundCount);
        Assert.Equal(-2, summary.TotalAttackValueModifier); // -4 (wounds) + 2 (battle focus) = -2
        Assert.Equal(2, summary.TotalDefenseValueModifier); // +2 (iron skin)
        Assert.Contains(summary.AttributeModifiers, kvp => kvp.Key == "Physicality" && kvp.Value == 2);
        
        // Verify active effects count
        var effects = await _service.GetActiveEffectsAsync(_testCharacterId);
        Assert.Equal(5, effects.Count); // 1 wound (with 2 stacks), 3 buffs, 1 poison (with 3 stacks)
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

