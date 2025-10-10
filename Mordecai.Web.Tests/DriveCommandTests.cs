using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mordecai.Game.Entities;
using Mordecai.Web.Services;
using Xunit;
using WebApplicationDbContext = Mordecai.Web.Data.ApplicationDbContext;
using WebSkillCategory = Mordecai.Web.Data.SkillCategory;
using WebSkillDefinition = Mordecai.Web.Data.SkillDefinition;
using WebCharacterSkill = Mordecai.Web.Data.CharacterSkill;

namespace Mordecai.Web.Tests;

public sealed class DriveCommandTests
{
    [Fact]
    public async Task DriveConversion_ShouldFail_WhenFatigueIsFull()
    {
        // Arrange
        var dbName = $"DriveFull_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var cancellationToken = TestContext.Current.CancellationToken;
        var (characterId, userId, maxFatigue, maxVitality) = await SeedCharacterAsync(factory, configureCharacter: character =>
        {
            character.InitializeHealth();
            character.PendingFatigueDamage = 0;
            character.PendingVitalityDamage = 0;
        }, cancellationToken);

        var characterService = CreateCharacterService(factory, new StubDiceService(0));

        // Act
        var result = await characterService.TryPerformDriveConversionAsync(characterId, userId, cancellationToken);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(0, result.VitalityDamageAmount);
        Assert.Equal(0, result.FatigueHealingAmount);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(maxFatigue, result.Snapshot!.CurrentFatigue);
        Assert.Equal(0, result.Snapshot.PendingFatigueDamage);
        Assert.Equal(maxVitality, result.Snapshot.CurrentVitality);
        Assert.Equal(0, result.Snapshot.PendingVitalityDamage);

    await using var verificationContext = await factory.CreateDbContextAsync(cancellationToken);
    var persisted = await verificationContext.Characters.SingleAsync(c => c.Id == characterId, cancellationToken);
        Assert.Equal(maxFatigue, persisted.CurrentFatigue);
        Assert.Equal(0, persisted.PendingFatigueDamage);
        Assert.Equal(maxVitality, persisted.CurrentVitality);
        Assert.Equal(0, persisted.PendingVitalityDamage);
    }

    [Fact]
    public async Task DriveConversion_ShouldClampHealingToAvailableFatigue()
    {
        // Arrange
        var dbName = $"DriveClamp_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var cancellationToken = TestContext.Current.CancellationToken;
        var (characterId, userId, maxFatigue, maxVitality) = await SeedCharacterAsync(factory, configureCharacter: character =>
        {
            character.InitializeHealth();
            character.CurrentFatigue = character.MaxFatigue - 1;
            character.PendingFatigueDamage = 0;
            character.PendingVitalityDamage = 0;
        }, cancellationToken);

        var diceService = new StubDiceService(0); // deterministic success when AS >= TV
        var characterService = CreateCharacterService(factory, diceService);

        // Act
        var result = await characterService.TryPerformDriveConversionAsync(characterId, userId, cancellationToken);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.VitalityDamageAmount);
        Assert.Equal(1, result.FatigueHealingAmount);
        Assert.Equal(0, result.DiceRoll);
        var snapshot = result.Snapshot!;
        Assert.Equal(maxFatigue - 1, snapshot.CurrentFatigue); // pending heal not yet applied
        Assert.Equal(-1, snapshot.PendingFatigueDamage);
        Assert.Equal(maxVitality, snapshot.CurrentVitality);
        Assert.Equal(1, snapshot.PendingVitalityDamage);

        await using var verificationContext = await factory.CreateDbContextAsync(cancellationToken);
        var persisted = await verificationContext.Characters.SingleAsync(c => c.Id == characterId, cancellationToken);
        Assert.Equal(maxFatigue - 1, persisted.CurrentFatigue);
        Assert.Equal(-1, persisted.PendingFatigueDamage);
        Assert.Equal(maxVitality, persisted.CurrentVitality);
        Assert.Equal(1, persisted.PendingVitalityDamage);
    }

    [Fact]
    public async Task DriveConversion_ShouldNotRunWhenHealingAlreadyPending()
    {
        // Arrange
        var dbName = $"DrivePending_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var cancellationToken = TestContext.Current.CancellationToken;
        var (characterId, userId, maxFatigue, _) = await SeedCharacterAsync(factory, configureCharacter: character =>
        {
            character.InitializeHealth();
            character.CurrentFatigue = character.MaxFatigue - 1;
            character.PendingFatigueDamage = -1;
            character.PendingVitalityDamage = 0;
        }, cancellationToken);

        var characterService = CreateCharacterService(factory, new StubDiceService(0));

        // Act
        var result = await characterService.TryPerformDriveConversionAsync(characterId, userId, cancellationToken);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(0, result.FatigueHealingAmount);
        Assert.Equal(0, result.VitalityDamageAmount);
        var snapshot = result.Snapshot!;
        Assert.Equal(maxFatigue - 1, snapshot.CurrentFatigue);
        Assert.Equal(-1, snapshot.PendingFatigueDamage);
        Assert.Equal(0, snapshot.PendingVitalityDamage);

        await using var verificationContext = await factory.CreateDbContextAsync(cancellationToken);
        var persisted = await verificationContext.Characters.SingleAsync(c => c.Id == characterId, cancellationToken);
        Assert.Equal(-1, persisted.PendingFatigueDamage);
        Assert.Equal(0, persisted.PendingVitalityDamage);
    }

    private static CharacterService CreateCharacterService(IDbContextFactory<WebApplicationDbContext> factory, IDiceService diceService)
    {
        var worldService = new StubWorldService();
        var skillService = new SkillService(factory, NullLogger<SkillService>.Instance);
        return new CharacterService(factory, worldService, skillService, diceService, NullLogger<CharacterService>.Instance);
    }

    private static IDbContextFactory<WebApplicationDbContext> CreateFactory(string databaseName)
    {
        var options = new DbContextOptionsBuilder<WebApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new TestDbContextFactory(options);
    }

    private static async Task<(Guid CharacterId, string UserId, int MaxFatigue, int MaxVitality)> SeedCharacterAsync(
        IDbContextFactory<WebApplicationDbContext> factory,
        Action<Character>? configureCharacter,
        CancellationToken cancellationToken)
    {
        var userId = Guid.NewGuid().ToString();
        var characterId = Guid.NewGuid();

        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var category = new WebSkillCategory
        {
            Name = "Core",
            Description = "Core attributes",
            CreatedBy = "tests",
            DisplayOrder = 1
        };

        context.SkillCategories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        var driveDefinition = new WebSkillDefinition
        {
            CategoryId = category.Id,
            Name = "Drive",
            Description = "Drive skill",
            SkillType = "CoreAttribute",
            RelatedAttribute = "Drive",
            CreatedBy = "tests"
        };

        context.SkillDefinitions.Add(driveDefinition);
        await context.SaveChangesAsync(cancellationToken);

        var character = new Character
        {
            Id = characterId,
            Name = "Test Character",
            UserId = userId,
            Drive = 10,
            Physicality = 10,
            Focus = 10,
            Dodge = 10,
            Reasoning = 10,
            Awareness = 10,
            Bearing = 10,
            CurrentFatigue = 0,
            CurrentVitality = 0
        };

        configureCharacter?.Invoke(character);

        context.Characters.Add(character);
        await context.SaveChangesAsync(cancellationToken);

        var characterSkill = new WebCharacterSkill
        {
            CharacterId = characterId,
            SkillDefinitionId = driveDefinition.Id,
            SkillDefinition = driveDefinition,
            Level = 3,
            Experience = 0,
            LearnedAt = DateTimeOffset.UtcNow
        };

        context.CharacterSkills.Add(characterSkill);
        await context.SaveChangesAsync(cancellationToken);

        return (characterId, userId, character.MaxFatigue, character.MaxVitality);
    }

    private sealed class StubDiceService : IDiceService
    {
        private readonly int _roll;

        public StubDiceService(int roll)
        {
            _roll = roll;
        }

        public int Roll4dF() => _roll;
        public int Roll4dFWithModifier(int modifier, int minValue = 1, int maxValue = 20) => _roll + modifier;
        public int RollExploding4dF() => _roll;
        public int RollMultiple4dF(int count) => _roll * count;
    }

    private sealed class StubWorldService : IWorldService
    {
        public Task<bool> CanMoveFromRoomAsync(int fromRoomId, string direction) => Task.FromResult(false);
        public Task<IReadOnlyList<int>> GetOccupiedRoomsAsync(CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<int>)Array.Empty<int>());
        public Task<Room?> GetRoomByCoordinatesAsync(int x, int y, int z, string? zoneName = null) => Task.FromResult<Room?>(null);
        public Task<Room?> GetRoomByExitAsync(int fromRoomId, string direction) => Task.FromResult<Room?>(null);
        public Task<Room?> GetRoomByIdAsync(int roomId) => Task.FromResult<Room?>(null);
        public Task<string> GetRoomDescriptionAsync(int roomId, bool isNight = false) => Task.FromResult(string.Empty);
        public Task<IReadOnlyList<RoomExit>> GetExitsFromRoomAsync(int roomId) => Task.FromResult((IReadOnlyList<RoomExit>)Array.Empty<RoomExit>());
        public Task<IReadOnlyList<RoomExit>> GetHiddenExitsFromRoomAsync(int roomId) => Task.FromResult((IReadOnlyList<RoomExit>)Array.Empty<RoomExit>());
        public Task<RoomExit?> GetExitFromRoomAsync(int fromRoomId, string direction) => Task.FromResult<RoomExit?>(null);
        public Task<Room?> GetStartingRoomAsync() => Task.FromResult<Room?>(null);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<WebApplicationDbContext>
    {
        private readonly DbContextOptions<WebApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<WebApplicationDbContext> options)
        {
            _options = options;
        }

        public WebApplicationDbContext CreateDbContext() => new(_options);
        public ValueTask<WebApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(new WebApplicationDbContext(_options));
    }
}
