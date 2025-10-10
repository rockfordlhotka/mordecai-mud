using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using Mordecai.Web.Services;
using Xunit;
using WebSkillCategory = Mordecai.Web.Data.SkillCategory;
using WebSkillDefinition = Mordecai.Web.Data.SkillDefinition;
using WebSkillUsageLog = Mordecai.Web.Data.SkillUsageLog;
using WebCharacterSkill = Mordecai.Web.Data.CharacterSkill;
using WebSkillUsageType = Mordecai.Web.Data.SkillUsageType;

namespace Mordecai.Web.Tests;

public sealed class DoorInteractionServiceTests
{
    [Fact]
    public async Task OpenAsync_ShouldOpenUnlockedDoor()
    {
        var dbName = $"DoorOpen_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var dice = new StubDiceService(0);
        var skillService = new RecordingSkillService();
        var service = new DoorInteractionService(factory, skillService, dice, NullLogger<DoorInteractionService>.Instance);

        var cancellationToken = TestContext.Current.CancellationToken;

        var (characterId, userId, roomId, destinationRoomId) = await SeedDoorScenarioAsync(factory, configureExit: exit =>
        {
            exit.DoorState = DoorState.Closed;
            exit.LockConfiguration = DoorLockType.None;
            exit.IsLocked = false;
        }, cancellationToken: cancellationToken);

        var result = await service.OpenAsync(characterId, userId, roomId, "north", cancellationToken);

        Assert.True(result.Success);
        Assert.Contains("open", result.Message, StringComparison.OrdinalIgnoreCase);

    await using var verification = await factory.CreateDbContextAsync(cancellationToken);
    var exit = await verification.RoomExits.SingleAsync(e => e.FromRoomId == roomId, cancellationToken);
    Assert.Equal(DoorState.Open, exit.DoorState);
    Assert.False(exit.IsLocked);
    Assert.Equal(DoorLockType.None, exit.LockConfiguration);

    var reciprocal = await verification.RoomExits.SingleAsync(e => e.FromRoomId == destinationRoomId, cancellationToken);
    Assert.Equal(DoorState.Open, reciprocal.DoorState);
    Assert.False(reciprocal.IsLocked);
    Assert.Equal(DoorLockType.None, reciprocal.LockConfiguration);
    }

    [Fact]
    public async Task LockWithDeviceAsync_ShouldFail_WhenDeviceCodeDoesNotMatch()
    {
        var dbName = $"DoorLockFail_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var service = new DoorInteractionService(factory, new RecordingSkillService(), new StubDiceService(0), NullLogger<DoorInteractionService>.Instance);

        var cancellationToken = TestContext.Current.CancellationToken;

        var (characterId, userId, roomId, _) = await SeedDoorScenarioAsync(
            factory,
            configureExit: exit =>
            {
                exit.DoorState = DoorState.Closed;
                exit.LockConfiguration = DoorLockType.None;
                exit.IsLocked = false;
                exit.LockDeviceCode = "golden-key";
            },
            cancellationToken: cancellationToken);

        var result = await service.LockWithDeviceAsync(characterId, userId, roomId, "north", "iron-key", cancellationToken);

        Assert.False(result.Success);
        Assert.Contains("does not fit", result.Message, StringComparison.OrdinalIgnoreCase);

        await using var verification = await factory.CreateDbContextAsync(cancellationToken);
        var exit = await verification.RoomExits.SingleAsync(e => e.FromRoomId == roomId, cancellationToken);
        Assert.Equal(DoorState.Closed, exit.DoorState);
        Assert.False(exit.IsLocked);
        Assert.Equal(DoorLockType.None, exit.LockConfiguration);
    }

    [Fact]
    public async Task UnlockWithDeviceAsync_ShouldFail_WhenDoorNotDeviceLocked()
    {
        var dbName = $"DoorUnlockFail_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var service = new DoorInteractionService(factory, new RecordingSkillService(), new StubDiceService(0), NullLogger<DoorInteractionService>.Instance);

        var cancellationToken = TestContext.Current.CancellationToken;

        var (characterId, userId, roomId, _) = await SeedDoorScenarioAsync(
            factory,
            configureExit: exit =>
            {
                exit.DoorState = DoorState.Closed;
                exit.LockConfiguration = DoorLockType.Spell;
                exit.IsLocked = true;
                exit.LockDeviceCode = "golden-key";
                exit.SpellLockCasterId = Guid.NewGuid();
                exit.SpellLockStrength = 9;
                exit.SpellLockAppliedAt = DateTimeOffset.UtcNow;
            },
            cancellationToken: cancellationToken);

        var result = await service.UnlockWithDeviceAsync(characterId, userId, roomId, "north", "golden-key", cancellationToken);

        Assert.False(result.Success);
        Assert.Contains("not locked with a device", result.Message, StringComparison.OrdinalIgnoreCase);

        await using var verification = await factory.CreateDbContextAsync(cancellationToken);
        var exit = await verification.RoomExits.SingleAsync(e => e.FromRoomId == roomId, cancellationToken);
        Assert.True(exit.IsLocked);
        Assert.Equal(DoorLockType.Spell, exit.LockConfiguration);
    }

    [Fact]
    public async Task AttemptBreakLockAsync_ShouldSucceedAndRecordSkillUsage()
    {
        var dbName = $"DoorBreak_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var dice = new StubDiceService(2); // deterministic roll
        var skillService = new RecordingSkillService();
        var service = new DoorInteractionService(factory, skillService, dice, NullLogger<DoorInteractionService>.Instance);

        var cancellationToken = TestContext.Current.CancellationToken;

        var (characterId, userId, roomId, destinationRoomId) = await SeedDoorScenarioAsync(factory,
            configureExit: exit =>
            {
                exit.DoorState = DoorState.Closed;
                exit.LockConfiguration = DoorLockType.Device;
                exit.IsLocked = true;
                exit.PhysicalityTargetValue = 11;
            },
            seedPhysicalitySkill: true,
            configureCharacter: character => character.Physicality = 10,
            cancellationToken: cancellationToken);

        var result = await service.AttemptBreakLockAsync(characterId, userId, roomId, "north", cancellationToken);

        Assert.True(result.Success);
        Assert.True(result.HasCheckDetails);
        Assert.Equal(10, result.AbilityScore);
        Assert.Equal(2, result.DiceRoll);
        Assert.Equal(12, result.Total);
        Assert.Equal(11, result.TargetValue);

    await using var verification = await factory.CreateDbContextAsync(cancellationToken);
    var exit = await verification.RoomExits.SingleAsync(e => e.FromRoomId == roomId, cancellationToken);
    Assert.Equal(DoorState.Open, exit.DoorState);
    Assert.False(exit.IsLocked);
    Assert.Equal(DoorLockType.None, exit.LockConfiguration);

    var reciprocal = await verification.RoomExits.SingleAsync(e => e.FromRoomId == destinationRoomId, cancellationToken);
    Assert.Equal(DoorState.Open, reciprocal.DoorState);
    Assert.False(reciprocal.IsLocked);
    Assert.Equal(DoorLockType.None, reciprocal.LockConfiguration);

        Assert.True(skillService.AddSkillUsageCalled);
    }

    [Fact]
    public async Task AttemptBreakLockAsync_ShouldClearSpellLockAndMirror()
    {
        var dbName = $"DoorBreakSpell_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var dice = new StubDiceService(3);
        var skillService = new RecordingSkillService();
        var service = new DoorInteractionService(factory, skillService, dice, NullLogger<DoorInteractionService>.Instance);

        var cancellationToken = TestContext.Current.CancellationToken;

        var casterId = Guid.NewGuid();
        var (characterId, userId, roomId, destinationRoomId) = await SeedDoorScenarioAsync(
            factory,
            configureExit: exit =>
            {
                exit.DoorState = DoorState.Closed;
                exit.LockConfiguration = DoorLockType.Spell;
                exit.IsLocked = true;
                exit.SpellLockCasterId = casterId;
                exit.SpellLockStrength = 11;
                exit.SpellLockAppliedAt = DateTimeOffset.UtcNow;
            },
            seedPhysicalitySkill: true,
            configureCharacter: character => character.Physicality = 9,
            cancellationToken: cancellationToken);

        // total = 9 + 3 = 12 meets target 11
        var result = await service.AttemptBreakLockAsync(characterId, userId, roomId, "north", cancellationToken);

        Assert.True(result.Success);
        Assert.True(result.HasCheckDetails);
        Assert.Equal(DoorState.Open, result.Exit?.DoorState);

        await using var verification = await factory.CreateDbContextAsync(cancellationToken);
        var exit = await verification.RoomExits.SingleAsync(e => e.FromRoomId == roomId, cancellationToken);
        Assert.Equal(DoorState.Open, exit.DoorState);
        Assert.False(exit.IsLocked);
        Assert.Equal(DoorLockType.None, exit.LockConfiguration);
        Assert.Null(exit.SpellLockCasterId);
        Assert.Null(exit.SpellLockStrength);
        Assert.Null(exit.SpellLockAppliedAt);

        var reciprocal = await verification.RoomExits.SingleAsync(e => e.FromRoomId == destinationRoomId, cancellationToken);
        Assert.Equal(DoorState.Open, reciprocal.DoorState);
        Assert.False(reciprocal.IsLocked);
        Assert.Equal(DoorLockType.None, reciprocal.LockConfiguration);
        Assert.Null(reciprocal.SpellLockCasterId);
        Assert.Null(reciprocal.SpellLockStrength);
        Assert.Null(reciprocal.SpellLockAppliedAt);

        Assert.True(skillService.AddSkillUsageCalled);
    }

    [Fact]
    public async Task AttemptBreakLockAsync_ShouldFailWhenTargetNotMetAndLeaveDoorClosed()
    {
        var dbName = $"DoorBreakFail_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var dice = new StubDiceService(-2);
        var skillService = new RecordingSkillService();
        var service = new DoorInteractionService(factory, skillService, dice, NullLogger<DoorInteractionService>.Instance);

        var cancellationToken = TestContext.Current.CancellationToken;

        var (characterId, userId, roomId, destinationRoomId) = await SeedDoorScenarioAsync(
            factory,
            configureExit: exit =>
            {
                exit.DoorState = DoorState.Closed;
                exit.LockConfiguration = DoorLockType.Device;
                exit.IsLocked = true;
                exit.PhysicalityTargetValue = 15;
            },
            seedPhysicalitySkill: true,
            configureCharacter: character => character.Physicality = 10,
            cancellationToken: cancellationToken);

        // total = 10 - 2 = 8 < 15 so failure
        var result = await service.AttemptBreakLockAsync(characterId, userId, roomId, "north", cancellationToken);

        Assert.False(result.Success);
        Assert.True(result.HasCheckDetails);
        Assert.Contains("holds fast", result.Message, StringComparison.OrdinalIgnoreCase);

        await using var verification = await factory.CreateDbContextAsync(cancellationToken);
        var exit = await verification.RoomExits.SingleAsync(e => e.FromRoomId == roomId, cancellationToken);
        Assert.Equal(DoorState.Closed, exit.DoorState);
        Assert.True(exit.IsLocked);
        Assert.Equal(DoorLockType.Device, exit.LockConfiguration);

        var reciprocal = await verification.RoomExits.SingleAsync(e => e.FromRoomId == destinationRoomId, cancellationToken);
        Assert.Equal(DoorState.Closed, reciprocal.DoorState);
        Assert.True(reciprocal.IsLocked);
        Assert.Equal(DoorLockType.Device, reciprocal.LockConfiguration);

        Assert.True(skillService.AddSkillUsageCalled);
    }

    private static async Task<(Guid CharacterId, string UserId, int RoomId, int DestinationRoomId)> SeedDoorScenarioAsync(
        IDbContextFactory<ApplicationDbContext> factory,
        Action<RoomExit>? configureExit = null,
        bool seedPhysicalitySkill = false,
        Action<Character>? configureCharacter = null,
        bool createReciprocalExit = true,
        CancellationToken cancellationToken = default)
    {
        var userId = Guid.NewGuid().ToString();
        var characterId = Guid.NewGuid();

        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var zone = new Zone
        {
            Name = "Test Zone",
            Description = "Zone",
            CreatedBy = "tests"
        };
        context.Zones.Add(zone);
        await context.SaveChangesAsync(cancellationToken);

        var roomType = new RoomType
        {
            Name = "Test Type",
            Description = "Type"
        };
        context.RoomTypes.Add(roomType);
        await context.SaveChangesAsync(cancellationToken);

        var room = new Room
        {
            ZoneId = zone.Id,
            RoomTypeId = roomType.Id,
            Name = "Entry",
            Description = "A room",
            CreatedBy = "tests"
        };
        context.Rooms.Add(room);
        await context.SaveChangesAsync(cancellationToken);

        var destination = new Room
        {
            ZoneId = zone.Id,
            RoomTypeId = roomType.Id,
            Name = "Hall",
            Description = "Another room",
            CreatedBy = "tests"
        };
        context.Rooms.Add(destination);
        await context.SaveChangesAsync(cancellationToken);

        var exit = new RoomExit
        {
            FromRoomId = room.Id,
            ToRoomId = destination.Id,
            Direction = "north",
            DoorName = "oak door",
            DoorState = DoorState.Closed,
            LockConfiguration = DoorLockType.None,
            IsLocked = false,
            IsActive = true
        };
        configureExit?.Invoke(exit);
        context.RoomExits.Add(exit);
        await context.SaveChangesAsync(cancellationToken);

        if (createReciprocalExit)
        {
            var reciprocalExit = new RoomExit
            {
                FromRoomId = destination.Id,
                ToRoomId = room.Id,
                Direction = "south",
                DoorName = exit.DoorName,
                DoorState = exit.DoorState,
                LockConfiguration = exit.LockConfiguration,
                IsLocked = exit.IsLocked,
                LockDeviceCode = exit.LockDeviceCode,
                PhysicalityTargetValue = exit.PhysicalityTargetValue,
                SpellLockAppliedAt = exit.SpellLockAppliedAt,
                SpellLockCasterId = exit.SpellLockCasterId,
                SpellLockStrength = exit.SpellLockStrength,
                IsActive = true
            };

            context.RoomExits.Add(reciprocalExit);
            await context.SaveChangesAsync(cancellationToken);
        }

        var character = new Character
        {
            Id = characterId,
            Name = "Tester",
            UserId = userId,
            CurrentRoomId = room.Id,
            Physicality = 10,
            Dodge = 10,
            Drive = 10,
            Focus = 10,
            Reasoning = 10,
            Awareness = 10,
            Bearing = 10
        };
        configureCharacter?.Invoke(character);
        context.Characters.Add(character);
        await context.SaveChangesAsync(cancellationToken);

        if (seedPhysicalitySkill)
        {
            var category = new WebSkillCategory
            {
                Name = "Core",
                Description = "Core",
                CreatedBy = "tests"
            };
            context.SkillCategories.Add(category);
            await context.SaveChangesAsync(cancellationToken);

            context.SkillDefinitions.Add(new WebSkillDefinition
            {
                CategoryId = category.Id,
                Name = "Physicality",
                Description = "Physicality",
                CreatedBy = "tests"
            });
            await context.SaveChangesAsync(cancellationToken);
        }

    return (characterId, userId, room.Id, destination.Id);
    }

    private static IDbContextFactory<ApplicationDbContext> CreateFactory(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new TestDbContextFactory(options);
    }

    private sealed class StubDiceService : IDiceService
    {
        private readonly int _roll;

        public StubDiceService(int roll)
        {
            _roll = roll;
        }

        public int Roll4dF() => _roll;
        public int RollExploding4dF() => _roll;
        public int Roll4dFWithModifier(int modifier, int minValue = 1, int maxValue = 20) => _roll + modifier;
        public int RollMultiple4dF(int count) => _roll * count;
    }

    private sealed class RecordingSkillService : ISkillService
    {
        public bool AddSkillUsageCalled { get; private set; }

        public Task<List<WebSkillCategory>> GetSkillCategoriesAsync() => Task.FromResult(new List<WebSkillCategory>());
        public Task<List<WebCharacterSkill>> GetCharacterSkillsAsync(Guid characterId) => Task.FromResult(new List<WebCharacterSkill>());
        public Task<WebCharacterSkill?> GetCharacterSkillAsync(Guid characterId, int skillDefinitionId) => Task.FromResult<WebCharacterSkill?>(null);
        public Task InitializeCharacterSkillsAsync(Guid characterId) => Task.CompletedTask;

        public Task<bool> AddSkillUsageAsync(Guid characterId, int skillDefinitionId, WebSkillUsageType usageType, int baseUsagePoints = 1, string? context = null, string? details = null)
        {
            AddSkillUsageCalled = true;
            return Task.FromResult(true);
        }

        public Task<WebCharacterSkill?> LearnSkillAsync(Guid characterId, int skillDefinitionId) => Task.FromResult<WebCharacterSkill?>(null);
        public Task<List<WebSkillUsageLog>> GetSkillUsageHistoryAsync(Guid characterId, int? skillDefinitionId = null, int take = 50) => Task.FromResult(new List<WebSkillUsageLog>());
    }

    private sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public ApplicationDbContext CreateDbContext() => new(_options);
        public ValueTask<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(new ApplicationDbContext(_options));
    }
}
