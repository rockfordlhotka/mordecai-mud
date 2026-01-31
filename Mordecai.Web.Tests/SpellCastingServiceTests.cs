using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using Mordecai.Web.Services;
using Xunit;

using WebSkillCategory = Mordecai.Web.Data.SkillCategory;
using WebSkillDefinition = Mordecai.Web.Data.SkillDefinition;
using WebCharacterSkill = Mordecai.Web.Data.CharacterSkill;
using WebSkillUsageType = Mordecai.Web.Data.SkillUsageType;

namespace Mordecai.Web.Tests;

public class SpellCastingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly SpellCastingService _service;
    private readonly StubDiceService _diceService;
    private readonly StubCharacterEffectService _characterEffectService;
    private readonly StubRoomEffectService _roomEffectService;
    private readonly StubSkillProgressionService _skillProgressionService;
    private readonly ManaService _manaService;
    private readonly Guid _testCharacterId;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    // Skill IDs
    private const int FireBoltSkillId = 201;
    private const int HealSkillId = 202;
    private const int InvisibilitySkillId = 203;
    private const int LightningStrikeSkillId = 204;

    public SpellCastingServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _contextFactory = new TestDbContextFactory(_options);

        // Setup stub services
        _diceService = new StubDiceService();
        _characterEffectService = new StubCharacterEffectService();
        _roomEffectService = new StubRoomEffectService();
        _skillProgressionService = new StubSkillProgressionService();

        _manaService = new ManaService(_contextFactory, NullLogger<ManaService>.Instance);

        _service = new SpellCastingService(
            _contextFactory,
            _manaService,
            _characterEffectService,
            _roomEffectService,
            _diceService,
            _skillProgressionService,
            NullLogger<SpellCastingService>.Instance);

        _testCharacterId = Guid.NewGuid();

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test character with Focus (WIL) = 12
        var character = new Character
        {
            Id = _testCharacterId,
            Name = "TestMage",
            UserId = "test-user",
            Focus = 12, // WIL
            Physicality = 10,
            Dodge = 10,
            Drive = 10,
            Reasoning = 12,
            Awareness = 10,
            Bearing = 10,
            CurrentFatigue = 15,
            CurrentRoomId = 1
        };
        character.InitializeHealth();
        _context.Characters.Add(character);

        // Create test room
        _context.Rooms.Add(new Room
        {
            Id = 1,
            Name = "Test Room",
            Description = "A test room",
            ZoneId = 1
        });

        // Create zone
        _context.Zones.Add(new Zone
        {
            Id = 1,
            Name = "Test Zone",
            Description = "A test zone"
        });

        // Create skill categories
        var spellCategory = new WebSkillCategory
        {
            Id = 200,
            Name = "Spell Skills",
            Description = "Magic spells",
            DefaultBaseCost = 40,
            DefaultMultiplier = 2.3m,
            IsActive = true,
            CreatedBy = "system"
        };
        _context.SkillCategories.Add(spellCategory);

        var manaRecoveryCategory = new WebSkillCategory
        {
            Id = 201,
            Name = "Mana Recovery",
            Description = "Mana recovery skills",
            DefaultBaseCost = 30,
            DefaultMultiplier = 2.1m,
            IsActive = true,
            CreatedBy = "system"
        };
        _context.SkillCategories.Add(manaRecoveryCategory);

        // Create mana recovery skills
        _context.SkillDefinitions.Add(new WebSkillDefinition
        {
            Id = 101,
            Name = "Fire Mana Recovery",
            Description = "Fire mana regeneration",
            CategoryId = 201,
            SkillType = "ManaRecovery",
            BaseCost = 30,
            Multiplier = 2.1m,
            RelatedAttribute = "WIL",
            IsActive = true,
            CreatedBy = "system"
        });

        _context.SkillDefinitions.Add(new WebSkillDefinition
        {
            Id = 102,
            Name = "Healing Mana Recovery",
            Description = "Healing mana regeneration",
            CategoryId = 201,
            SkillType = "ManaRecovery",
            BaseCost = 30,
            Multiplier = 2.1m,
            RelatedAttribute = "WIL",
            IsActive = true,
            CreatedBy = "system"
        });

        // Create spell skills
        _context.SkillDefinitions.Add(new WebSkillDefinition
        {
            Id = FireBoltSkillId,
            Name = "Fire Bolt",
            Description = "A basic fire projectile",
            CategoryId = 200,
            SkillType = "Spell",
            BaseCost = 20,
            Multiplier = 2.0m,
            RelatedAttribute = "WIL",
            MagicSchool = "Fire",
            ManaCost = 5,
            CooldownSeconds = 0,
            UsesExplodingDice = true,
            IsActive = true,
            CreatedBy = "system"
        });

        _context.SkillDefinitions.Add(new WebSkillDefinition
        {
            Id = HealSkillId,
            Name = "Heal",
            Description = "Restores health to a target",
            CategoryId = 200,
            SkillType = "Spell",
            BaseCost = 20,
            Multiplier = 2.0m,
            RelatedAttribute = "WIL",
            MagicSchool = "Healing",
            ManaCost = 8,
            CooldownSeconds = 0,
            UsesExplodingDice = false,
            IsActive = true,
            CreatedBy = "system"
        });

        _context.SkillDefinitions.Add(new WebSkillDefinition
        {
            Id = InvisibilitySkillId,
            Name = "Invisibility",
            Description = "Makes the caster invisible",
            CategoryId = 200,
            SkillType = "Spell",
            BaseCost = 80,
            Multiplier = 2.8m,
            RelatedAttribute = "WIL",
            MagicSchool = "Illusion",
            ManaCost = 25,
            CooldownSeconds = 60,
            UsesExplodingDice = true,
            IsActive = true,
            CreatedBy = "system"
        });

        _context.SkillDefinitions.Add(new WebSkillDefinition
        {
            Id = LightningStrikeSkillId,
            Name = "Lightning Strike",
            Description = "A bolt of lightning",
            CategoryId = 200,
            SkillType = "Spell",
            BaseCost = 40,
            Multiplier = 2.3m,
            RelatedAttribute = "WIL",
            MagicSchool = "Lightning",
            ManaCost = 12,
            CooldownSeconds = 0,
            UsesExplodingDice = true,
            IsActive = true,
            CreatedBy = "system"
        });

        // Give character Fire Bolt skill at level 3
        _context.CharacterSkills.Add(new WebCharacterSkill
        {
            Id = 1,
            CharacterId = _testCharacterId,
            SkillDefinitionId = FireBoltSkillId,
            Level = 3,
            Experience = 100
        });

        // Give character Heal skill at level 2
        _context.CharacterSkills.Add(new WebCharacterSkill
        {
            Id = 2,
            CharacterId = _testCharacterId,
            SkillDefinitionId = HealSkillId,
            Level = 2,
            Experience = 50
        });

        // Give character Fire Mana Recovery at level 2
        _context.CharacterSkills.Add(new WebCharacterSkill
        {
            Id = 3,
            CharacterId = _testCharacterId,
            SkillDefinitionId = 101,
            Level = 2,
            Experience = 60
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // ==================
    // GetSpellInfoAsync Tests
    // ==================

    [Fact]
    public async Task GetSpellInfoAsync_ValidSpell_ReturnsSpellInfo()
    {
        // Act
        var spellInfo = await _service.GetSpellInfoAsync(FireBoltSkillId);

        // Assert
        Assert.NotNull(spellInfo);
        Assert.Equal("Fire Bolt", spellInfo.Name);
        Assert.Equal(MagicSchool.Fire, spellInfo.School);
        Assert.Equal(5, spellInfo.ManaCost);
        Assert.True(spellInfo.UsesExplodingDice);
    }

    [Fact]
    public async Task GetSpellInfoAsync_InvalidSkillId_ReturnsNull()
    {
        // Act
        var spellInfo = await _service.GetSpellInfoAsync(9999);

        // Assert
        Assert.Null(spellInfo);
    }

    [Fact]
    public async Task GetSpellInfoAsync_NonSpellSkill_ReturnsNull()
    {
        // Act - Try to get mana recovery skill as spell
        var spellInfo = await _service.GetSpellInfoAsync(101);

        // Assert
        Assert.Null(spellInfo);
    }

    // ==================
    // CanCastSpellAsync Tests
    // ==================

    [Fact]
    public async Task CanCastSpellAsync_ValidConditions_ReturnsTrue()
    {
        // Arrange - Ensure character has mana
        await _manaService.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);

        // Act
        var (canCast, reason) = await _service.CanCastSpellAsync(_testCharacterId, FireBoltSkillId);

        // Assert
        Assert.True(canCast);
        Assert.Null(reason);
    }

    [Fact]
    public async Task CanCastSpellAsync_InsufficientMana_ReturnsFalse()
    {
        // Arrange - Create pool and deplete mana
        await _manaService.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        await _manaService.SetManaAsync(_testCharacterId, MagicSchool.Fire, 2); // Less than 5 needed

        // Act
        var (canCast, reason) = await _service.CanCastSpellAsync(_testCharacterId, FireBoltSkillId);

        // Assert
        Assert.False(canCast);
        Assert.Contains("Insufficient", reason);
    }

    [Fact]
    public async Task CanCastSpellAsync_Silenced_ReturnsFalse()
    {
        // Arrange - Setup character as silenced
        _characterEffectService.SetCanCastSpells(false);

        // Act
        var (canCast, reason) = await _service.CanCastSpellAsync(_testCharacterId, FireBoltSkillId);

        // Assert
        Assert.False(canCast);
        Assert.Contains("silenced", reason);
    }

    [Fact]
    public async Task CanCastSpellAsync_Stunned_ReturnsFalse()
    {
        // Arrange - Setup character as stunned
        _characterEffectService.SetCanAct(false);

        // Act
        var (canCast, reason) = await _service.CanCastSpellAsync(_testCharacterId, FireBoltSkillId);

        // Assert
        Assert.False(canCast);
        Assert.Contains("cannot act", reason);
    }

    [Fact]
    public async Task CanCastSpellAsync_InvalidSpell_ReturnsFalse()
    {
        // Act
        var (canCast, reason) = await _service.CanCastSpellAsync(_testCharacterId, 9999);

        // Assert
        Assert.False(canCast);
        Assert.Contains("Invalid", reason);
    }

    // ==================
    // CastSpellAsync Tests
    // ==================

    [Fact]
    public async Task CastSpellAsync_SuccessfulCast_ConsumesMana()
    {
        // Arrange
        await _manaService.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        var initialMana = await _manaService.GetCurrentManaAsync(_testCharacterId, MagicSchool.Fire);

        var request = new SpellCastRequest
        {
            CasterId = _testCharacterId,
            SpellSkillId = FireBoltSkillId,
            TargetCharacterId = _testCharacterId // Target self for test simplicity
        };

        // Act
        var result = await _service.CastSpellAsync(request);

        // Assert
        Assert.True(result.Success, $"Cast failed: {result.Message}");
        Assert.Equal(5, result.ManaConsumed);

        var currentMana = await _manaService.GetCurrentManaAsync(_testCharacterId, MagicSchool.Fire);
        Assert.Equal(initialMana - 5, currentMana);
    }

    [Fact]
    public async Task CastSpellAsync_SuccessfulCast_ConsumesFatigue()
    {
        // Arrange
        await _manaService.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);

        var request = new SpellCastRequest
        {
            CasterId = _testCharacterId,
            SpellSkillId = FireBoltSkillId,
            TargetCharacterId = _testCharacterId // Target self for test simplicity
        };

        // Act
        var result = await _service.CastSpellAsync(request);

        // Assert
        Assert.True(result.Success, $"Cast failed: {result.Message}");
        Assert.Equal(1, result.FatigueConsumed);
    }

    [Fact]
    public async Task CastSpellAsync_CalculatesAbilityScore()
    {
        // Arrange
        await _manaService.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        _diceService.SetRoll(2);

        var request = new SpellCastRequest
        {
            CasterId = _testCharacterId,
            SpellSkillId = FireBoltSkillId,
            TargetCharacterId = _testCharacterId // Target self for test
        };

        // Act
        var result = await _service.CastSpellAsync(request);

        // Assert
        // AS = Focus(12) + Skill(3) - 5 = 10
        Assert.Equal(10, result.AbilityScore);
        Assert.Equal(2, result.DiceRoll);
        // SV = AS + Roll = 10 + 2 = 12
        Assert.Equal(12, result.SuccessValue);
    }

    [Fact]
    public async Task CastSpellAsync_CriticalSuccess_FlagsAsCritical()
    {
        // Arrange
        await _manaService.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        _diceService.SetRoll(6); // Exploded dice

        var request = new SpellCastRequest
        {
            CasterId = _testCharacterId,
            SpellSkillId = FireBoltSkillId,
            TargetCharacterId = _testCharacterId // Target self for test
        };

        // Act
        var result = await _service.CastSpellAsync(request);

        // Assert
        Assert.True(result.IsCritical);
        Assert.False(result.IsFumble);
    }

    [Fact]
    public async Task CastSpellAsync_Fumble_ReturnsFailure()
    {
        // Arrange
        await _manaService.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        _diceService.SetRoll(-6); // Fumble

        var request = new SpellCastRequest
        {
            CasterId = _testCharacterId,
            SpellSkillId = FireBoltSkillId,
            TargetCharacterId = _testCharacterId // Target self for test
        };

        // Act
        var result = await _service.CastSpellAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsFumble);
        Assert.Contains("fizzles", result.Message);
    }

    [Fact]
    public async Task CastSpellAsync_InsufficientMana_Fails()
    {
        // Arrange
        await _manaService.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);
        await _manaService.SetManaAsync(_testCharacterId, MagicSchool.Fire, 2); // Not enough

        var request = new SpellCastRequest
        {
            CasterId = _testCharacterId,
            SpellSkillId = FireBoltSkillId,
            TargetCharacterId = _testCharacterId // Target self for test
        };

        // Act
        var result = await _service.CastSpellAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Insufficient", result.Message);
    }

    [Fact]
    public async Task CastSpellAsync_LogsSkillProgression()
    {
        // Arrange
        await _manaService.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Fire);

        var request = new SpellCastRequest
        {
            CasterId = _testCharacterId,
            SpellSkillId = FireBoltSkillId,
            TargetCharacterId = _testCharacterId // Target self for test
        };

        // Act
        var result = await _service.CastSpellAsync(request);

        // Assert
        Assert.NotNull(result.SkillProgression);
        Assert.True(_skillProgressionService.LogUsageCalled);
    }

    // ==================
    // GetKnownSpellsAsync Tests
    // ==================

    [Fact]
    public async Task GetKnownSpellsAsync_ReturnsCharacterSpells()
    {
        // Act
        var spells = await _service.GetKnownSpellsAsync(_testCharacterId);

        // Assert
        Assert.Equal(2, spells.Count); // Fire Bolt and Heal
        Assert.Contains(spells, s => s.Spell.Name == "Fire Bolt" && s.Level == 3);
        Assert.Contains(spells, s => s.Spell.Name == "Heal" && s.Level == 2);
    }

    [Fact]
    public async Task GetKnownSpellsAsync_UnknownCharacter_ReturnsEmpty()
    {
        // Act
        var spells = await _service.GetKnownSpellsAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(spells);
    }

    // ==================
    // LearnSpellAsync Tests
    // ==================

    [Fact]
    public async Task LearnSpellAsync_NewSpell_ReturnsTrue()
    {
        // Act
        var learned = await _service.LearnSpellAsync(_testCharacterId, LightningStrikeSkillId);

        // Assert
        Assert.True(learned);

        var spells = await _service.GetKnownSpellsAsync(_testCharacterId);
        Assert.Contains(spells, s => s.Spell.Name == "Lightning Strike" && s.Level == 0);
    }

    [Fact]
    public async Task LearnSpellAsync_AlreadyKnown_ReturnsFalse()
    {
        // Act
        var learned = await _service.LearnSpellAsync(_testCharacterId, FireBoltSkillId);

        // Assert
        Assert.False(learned);
    }

    [Fact]
    public async Task LearnSpellAsync_InvalidSpell_ReturnsFalse()
    {
        // Act
        var learned = await _service.LearnSpellAsync(_testCharacterId, 9999);

        // Assert
        Assert.False(learned);
    }

    // ==================
    // GetAllSpellsAsync Tests
    // ==================

    [Fact]
    public async Task GetAllSpellsAsync_ReturnsAllSpells()
    {
        // Act
        var spells = await _service.GetAllSpellsAsync();

        // Assert
        Assert.Equal(4, spells.Count);
        Assert.Contains(spells, s => s.Name == "Fire Bolt");
        Assert.Contains(spells, s => s.Name == "Heal");
        Assert.Contains(spells, s => s.Name == "Invisibility");
        Assert.Contains(spells, s => s.Name == "Lightning Strike");
    }

    // ==================
    // GetSpellsBySchoolAsync Tests
    // ==================

    [Fact]
    public async Task GetSpellsBySchoolAsync_ReturnsSchoolSpells()
    {
        // Act
        var fireSpells = await _service.GetSpellsBySchoolAsync(MagicSchool.Fire);
        var healingSpells = await _service.GetSpellsBySchoolAsync(MagicSchool.Healing);

        // Assert
        Assert.Single(fireSpells);
        Assert.Equal("Fire Bolt", fireSpells[0].Name);

        Assert.Single(healingSpells);
        Assert.Equal("Heal", healingSpells[0].Name);
    }

    // ==================
    // Healing Spell Tests
    // ==================

    [Fact]
    public async Task CastSpellAsync_HealingSpell_RestoresHealth()
    {
        // Arrange
        await _manaService.GetOrCreateManaPoolAsync(_testCharacterId, MagicSchool.Healing);

        // Damage the character first
        await using (var context = new ApplicationDbContext(_options))
        {
            var character = await context.Characters.FindAsync(_testCharacterId);
            character!.CurrentVitality = 5; // Damage them
            await context.SaveChangesAsync();
        }

        var request = new SpellCastRequest
        {
            CasterId = _testCharacterId,
            SpellSkillId = HealSkillId,
            TargetCharacterId = _testCharacterId // Self heal
        };

        // Act
        var result = await _service.CastSpellAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("heals", result.Message.ToLower());
        Assert.True(result.EffectValue > 0);
    }

    // ==================
    // Helper Classes
    // ==================

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

    private class StubDiceService : IDiceService
    {
        private int _roll = 0;

        public void SetRoll(int roll) => _roll = roll;

        public int Roll4dF() => _roll;
        public int RollExploding4dF() => _roll;
        public int Roll4dFWithModifier(int modifier, int minValue = 1, int maxValue = 20) => _roll + modifier;
        public int RollMultiple4dF(int count) => _roll * count;
    }

    private class StubCharacterEffectService : ICharacterEffectService
    {
        private bool _canCastSpells = true;
        private bool _canAct = true;

        public void SetCanCastSpells(bool value) => _canCastSpells = value;
        public void SetCanAct(bool value) => _canAct = value;

        public Task<CharacterEffectSummary> GetEffectSummaryAsync(Guid characterId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CharacterEffectSummary
            {
                CharacterId = characterId,
                CanCastSpells = _canCastSpells,
                CanAct = _canAct,
                CanMove = true
            });
        }

        // Other interface methods - minimal implementation
        public Task<EffectApplicationResult> ApplyEffectAsync(Guid characterId, int effectDefinitionId, Guid? sourceCharacterId = null, Guid? sourceNpcId = null, int? sourceSpellSkillId = null, int? durationSeconds = null, decimal? intensity = null, BodyLocation? bodyLocation = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new EffectApplicationResult { Success = true });

        public Task<EffectApplicationResult> ApplyEffectByNameAsync(Guid characterId, string effectName, Guid? sourceCharacterId = null, Guid? sourceNpcId = null, int? sourceSpellSkillId = null, int? durationSeconds = null, decimal? intensity = null, BodyLocation? bodyLocation = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new EffectApplicationResult { Success = true });

        public Task<EffectApplicationResult> ApplyWoundAsync(Guid characterId, BodyLocation location = BodyLocation.General, Guid? sourceCharacterId = null, Guid? sourceNpcId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new EffectApplicationResult { Success = true });

        public Task<bool> RemoveEffectAsync(int effectId, string reason = "removed", CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<int> RemoveEffectsByTypeAsync(Guid characterId, CharacterEffectType effectType, string reason = "removed", CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<int> HealWoundsAsync(Guid characterId, int count = 1, BodyLocation? location = null, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<IReadOnlyList<CharacterEffect>> GetActiveEffectsAsync(Guid characterId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CharacterEffect>>(new List<CharacterEffect>());

        public Task<IReadOnlyList<CharacterEffect>> GetActiveEffectsByTypeAsync(Guid characterId, CharacterEffectType effectType, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CharacterEffect>>(new List<CharacterEffect>());

        public Task<bool> HasEffectAsync(Guid characterId, int effectDefinitionId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<bool> HasEffectByNameAsync(Guid characterId, string effectName, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<int> GetWoundCountAsync(Guid characterId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<Dictionary<BodyLocation, int>> GetWoundsByLocationAsync(Guid characterId, CancellationToken cancellationToken = default)
            => Task.FromResult(new Dictionary<BodyLocation, int>());

        public Task<IReadOnlyList<string>> ProcessPeriodicEffectsAsync(Guid characterId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(new List<string>());

        public Task<int> CleanupExpiredEffectsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<int> ProcessNaturalWoundHealingAsync(Guid characterId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<CharacterEffectDefinition?> GetEffectDefinitionAsync(int effectDefinitionId, CancellationToken cancellationToken = default)
            => Task.FromResult<CharacterEffectDefinition?>(null);

        public Task<CharacterEffectDefinition?> GetEffectDefinitionByNameAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult<CharacterEffectDefinition?>(null);

        public Task<IReadOnlyList<CharacterEffectDefinition>> GetAllEffectDefinitionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CharacterEffectDefinition>>(new List<CharacterEffectDefinition>());
    }

    private class StubRoomEffectService : IRoomEffectService
    {
        public Task<RoomEffect> ApplyEffectAsync(int roomId, int effectDefinitionId, string sourceType, string? sourceId = null, string? sourceName = null, Guid? casterCharacterId = null, decimal intensity = 1.0m, int? durationOverride = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new RoomEffect { Id = 1, RoomId = roomId, RoomEffectDefinitionId = effectDefinitionId });

        public Task RemoveEffectAsync(int roomEffectId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAllEffectsFromRoomAsync(int roomId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IList<RoomEffect>> GetActiveEffectsInRoomAsync(int roomId, CancellationToken cancellationToken = default)
            => Task.FromResult<IList<RoomEffect>>(new List<RoomEffect>());

        public Task<IList<RoomEffect>> GetVisibleEffectsInRoomAsync(int roomId, CancellationToken cancellationToken = default)
            => Task.FromResult<IList<RoomEffect>>(new List<RoomEffect>());

        public Task ProcessPeriodicEffectsAsync(int roomId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> IsMovementPreventedAsync(int roomId, Guid characterId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task ApplyEntryEffectsAsync(int roomId, Guid characterId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ApplyExitEffectsAsync(int roomId, Guid characterId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CleanupExpiredEffectsAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<RoomEffectDefinition?> GetEffectDefinitionByNameAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult<RoomEffectDefinition?>(null);

        public Task<IList<RoomEffectDefinition>> GetAllEffectDefinitionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IList<RoomEffectDefinition>>(new List<RoomEffectDefinition>());
    }

    private class StubSkillProgressionService : ISkillProgressionService
    {
        public bool LogUsageCalled { get; private set; }

        public Task<SkillProgressionResult> LogUsageAsync(Guid characterId, int skillDefinitionId, WebSkillUsageType usageType, int baseExperience = 1, string? targetId = null, int? targetDifficulty = null, bool actionSucceeded = true, string? context = null, string? details = null, CancellationToken cancellationToken = default)
        {
            LogUsageCalled = true;
            return Task.FromResult(new SkillProgressionResult { ProgressionApplied = true, FinalExperience = 1 });
        }

        public Task<int> GetHourlyUsageCountAsync(Guid characterId, int skillDefinitionId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<int> GetDailyUsageCountAsync(Guid characterId, int skillDefinitionId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<bool> IsTargetOnCooldownAsync(Guid characterId, int skillDefinitionId, string targetId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<decimal> CalculateEffectiveMultiplierAsync(Guid characterId, int skillDefinitionId, WebSkillUsageType usageType, string? targetId = null, int? targetDifficulty = null, bool actionSucceeded = true, CancellationToken cancellationToken = default)
            => Task.FromResult(1.0m);

        public SkillProgressionSettings GetSettings() => new();

        public Task CleanupOldTrackingDataAsync(int hourlyRetentionHours = 24, int dailyRetentionDays = 7, int cooldownRetentionHours = 24, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
