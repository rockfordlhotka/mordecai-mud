using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mordecai.Game.Entities;
using Mordecai.Messaging.Messages;
using Mordecai.Messaging.Services;
using Mordecai.Web.Data;
using Mordecai.Web.Services;
using Xunit;

namespace Mordecai.Web.Tests;

public sealed class CombatOrchestrationTests
{
    [Fact]
    public async Task InitiateCombat_WhenNeitherInCombat_CreatesNewSession()
    {
        // Arrange
        var dbName = $"CombatOrch_NewSession_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var publisher = new StubMessagePublisher();
        var diceService = new StubDiceService();

        await using var context = await factory.CreateDbContextAsync();
        var (playerCharacterId, npcId, roomId) = await SeedCombatScenarioAsync(context);

        var service = new CombatService(
            await factory.CreateDbContextAsync(),
            publisher,
            diceService,
            new StubSkillProgressionService(),
            NullLogger<CombatService>.Instance);

        // Act
        var sessionId = await service.InitiateCombatAsync(
            playerCharacterId,
            attackerIsPlayer: true,
            npcId,
            targetIsPlayer: false);

        // Assert
        Assert.NotNull(sessionId);

        await using var verifyContext = await factory.CreateDbContextAsync();
        var session = await verifyContext.CombatSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        Assert.NotNull(session);
        Assert.True(session.IsActive);
        Assert.Equal(roomId, session.RoomId);
        Assert.Equal(2, session.Participants.Count);
        Assert.Contains(session.Participants, p => p.CharacterId == playerCharacterId);
        Assert.Contains(session.Participants, p => p.CharacterId == null); // NPC has no CharacterId
    }

    [Fact]
    public async Task InitiateCombat_WhenAttackerAlreadyInCombat_ReturnsExistingSession()
    {
        // Arrange
        var dbName = $"CombatOrch_AttackerInCombat_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var publisher = new StubMessagePublisher();
        var diceService = new StubDiceService();

        await using var context = await factory.CreateDbContextAsync();
        var (playerCharacterId, npcId1, roomId) = await SeedCombatScenarioAsync(context);

        // Create a second NPC for the player to attack
        var npcTemplate2 = await context.NpcTemplates.FirstAsync();
        var spawnerInstance = await context.SpawnerInstances.FirstAsync();
        var npcId2 = Guid.NewGuid();
        var activeSpawn2 = new ActiveSpawn
        {
            NpcId = npcId2,
            SpawnerInstanceId = spawnerInstance.Id,
            NpcTemplateId = npcTemplate2.Id,
            CurrentRoomId = roomId,
            IsActive = true,
            CurrentVitality = 10,
            CurrentFatigue = 5
        };
        context.ActiveSpawns.Add(activeSpawn2);
        await context.SaveChangesAsync();

        var service = new CombatService(
            await factory.CreateDbContextAsync(),
            publisher,
            diceService,
            new StubSkillProgressionService(),
            NullLogger<CombatService>.Instance);

        // First combat - player attacks NPC1
        var firstSessionId = await service.InitiateCombatAsync(
            playerCharacterId,
            attackerIsPlayer: true,
            npcId1,
            targetIsPlayer: false);

        // Act - player attacks NPC2 while already in combat
        var secondSessionId = await service.InitiateCombatAsync(
            playerCharacterId,
            attackerIsPlayer: true,
            npcId2,
            targetIsPlayer: false);

        // Assert - returns same session
        Assert.NotNull(firstSessionId);
        Assert.NotNull(secondSessionId);
        Assert.Equal(firstSessionId, secondSessionId);
    }

    [Fact]
    public async Task InitiateCombat_WhenTargetAlreadyInCombat_JoinsExistingSession()
    {
        // Arrange
        var dbName = $"CombatOrch_TargetInCombat_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var publisher = new StubMessagePublisher();
        var diceService = new StubDiceService();

        await using var context = await factory.CreateDbContextAsync();
        var (player1Id, npcId, roomId) = await SeedCombatScenarioAsync(context);

        // Create a second player character
        var player2Id = Guid.NewGuid();
        var player2 = new Character
        {
            Id = player2Id,
            UserId = Guid.NewGuid().ToString(),
            Name = "Test Fighter 2",
            CurrentRoomId = roomId,
            CurrentVitality = 10,
            CurrentFatigue = 5
        };
        context.Characters.Add(player2);
        await context.SaveChangesAsync();

        var service = new CombatService(
            await factory.CreateDbContextAsync(),
            publisher,
            diceService,
            new StubSkillProgressionService(),
            NullLogger<CombatService>.Instance);

        // Player 1 attacks NPC first
        var firstSessionId = await service.InitiateCombatAsync(
            player1Id,
            attackerIsPlayer: true,
            npcId,
            targetIsPlayer: false);

        // Act - Player 2 attacks same NPC (NPC already in combat)
        var secondSessionId = await service.InitiateCombatAsync(
            player2Id,
            attackerIsPlayer: true,
            npcId,
            targetIsPlayer: false);

        // Assert - same session, but player 2 added as participant
        Assert.NotNull(firstSessionId);
        Assert.NotNull(secondSessionId);
        Assert.Equal(firstSessionId, secondSessionId);

        await using var verifyContext = await factory.CreateDbContextAsync();
        var session = await verifyContext.CombatSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == firstSessionId);

        Assert.NotNull(session);
        Assert.Equal(3, session.Participants.Count); // player1, NPC, player2
        Assert.Contains(session.Participants, p => p.CharacterId == player1Id);
        Assert.Contains(session.Participants, p => p.CharacterId == player2Id);
    }

    [Fact]
    public async Task InitiateCombat_ParticipantStateInitialized_Correctly()
    {
        // Arrange
        var dbName = $"CombatOrch_StateInit_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var publisher = new StubMessagePublisher();
        var diceService = new StubDiceService();

        await using var context = await factory.CreateDbContextAsync();
        var (playerCharacterId, npcId, _) = await SeedCombatScenarioAsync(context);

        var service = new CombatService(
            await factory.CreateDbContextAsync(),
            publisher,
            diceService,
            new StubSkillProgressionService(),
            NullLogger<CombatService>.Instance);

        // Act
        var sessionId = await service.InitiateCombatAsync(
            playerCharacterId,
            attackerIsPlayer: true,
            npcId,
            targetIsPlayer: false);

        // Assert participant state
        await using var verifyContext = await factory.CreateDbContextAsync();
        var participants = await verifyContext.CombatParticipants
            .Where(p => p.CombatSessionId == sessionId)
            .ToListAsync();

        foreach (var participant in participants)
        {
            Assert.False(participant.IsInParryMode);
            Assert.Null(participant.LastRangedAttack);
            Assert.True(participant.IsActive);
            Assert.True(participant.JoinedAt <= DateTimeOffset.UtcNow);
            Assert.Null(participant.LeftAt);
        }
    }

    [Fact]
    public async Task FleeFromCombat_WhenOnlyTwoParticipants_EndsSession()
    {
        // Arrange
        var dbName = $"CombatOrch_FleeEnds_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var publisher = new StubMessagePublisher();
        var diceService = new StubDiceService();

        await using var context = await factory.CreateDbContextAsync();
        var (playerCharacterId, npcId, _) = await SeedCombatScenarioAsync(context);

        var service = new CombatService(
            await factory.CreateDbContextAsync(),
            publisher,
            diceService,
            new StubSkillProgressionService(),
            NullLogger<CombatService>.Instance);

        // Start combat
        var sessionId = await service.InitiateCombatAsync(
            playerCharacterId,
            attackerIsPlayer: true,
            npcId,
            targetIsPlayer: false);

        Assert.NotNull(sessionId);

        // Act - player flees
        var fleeResult = await service.FleeFromCombatAsync(playerCharacterId, isPlayer: true);

        // Assert
        Assert.True(fleeResult);

        await using var verifyContext = await factory.CreateDbContextAsync();
        var session = await verifyContext.CombatSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        Assert.NotNull(session);
        Assert.False(session.IsActive);
        Assert.NotNull(session.EndedAt);
    }

    [Fact]
    public async Task IsInCombat_ReturnsTrue_WhenInActiveCombat()
    {
        // Arrange
        var dbName = $"CombatOrch_IsInCombat_{Guid.NewGuid()}";
        var factory = CreateFactory(dbName);
        var publisher = new StubMessagePublisher();
        var diceService = new StubDiceService();

        await using var context = await factory.CreateDbContextAsync();
        var (playerCharacterId, npcId, _) = await SeedCombatScenarioAsync(context);

        var service = new CombatService(
            await factory.CreateDbContextAsync(),
            publisher,
            diceService,
            new StubSkillProgressionService(),
            NullLogger<CombatService>.Instance);

        // Before combat
        var beforeCombat = await service.IsInCombatAsync(playerCharacterId, isPlayer: true);
        Assert.False(beforeCombat);

        // Start combat
        await service.InitiateCombatAsync(
            playerCharacterId,
            attackerIsPlayer: true,
            npcId,
            targetIsPlayer: false);

        // Act
        var duringCombat = await service.IsInCombatAsync(playerCharacterId, isPlayer: true);

        // Assert
        Assert.True(duringCombat);
    }

    #region Test Helpers

    private static IDbContextFactory<ApplicationDbContext> CreateFactory(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new TestDbContextFactory(options);
    }

    private static async Task<(Guid PlayerId, Guid NpcId, int RoomId)> SeedCombatScenarioAsync(
        ApplicationDbContext context)
    {
        // Zone
        var zone = new Zone
        {
            Name = "Test Zone",
            Description = "Zone",
            CreatedBy = "tests"
        };
        context.Zones.Add(zone);
        await context.SaveChangesAsync();

        // Room type
        var roomType = new RoomType
        {
            Name = "Test Type",
            Description = "Type"
        };
        context.RoomTypes.Add(roomType);
        await context.SaveChangesAsync();

        // Room
        var room = new Room
        {
            ZoneId = zone.Id,
            RoomTypeId = roomType.Id,
            Name = "Test Room",
            Description = "Room",
            CreatedBy = "tests"
        };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        // Player character
        var playerId = Guid.NewGuid();
        var player = new Character
        {
            Id = playerId,
            UserId = Guid.NewGuid().ToString(),
            Name = "Test Fighter",
            CurrentRoomId = room.Id,
            CurrentVitality = 10,
            CurrentFatigue = 5
        };
        context.Characters.Add(player);
        await context.SaveChangesAsync();

        // NPC template
        var npcTemplate = new NpcTemplate
        {
            Name = "goblin",
            Description = "A green creature",
            ShortDescription = "a goblin",
            CreatedBy = "tests"
        };
        context.NpcTemplates.Add(npcTemplate);
        await context.SaveChangesAsync();

        // Spawner template
        var spawnerTemplate = new SpawnerTemplate
        {
            Name = "Test Spawner",
            CreatedBy = "tests"
        };
        context.SpawnerTemplates.Add(spawnerTemplate);
        await context.SaveChangesAsync();

        // Spawner instance
        var spawnerInstance = new SpawnerInstance
        {
            SpawnerTemplateId = spawnerTemplate.Id,
            RoomId = room.Id
        };
        context.SpawnerInstances.Add(spawnerInstance);
        await context.SaveChangesAsync();

        // Active spawn (NPC)
        var npcId = Guid.NewGuid();
        var activeSpawn = new ActiveSpawn
        {
            NpcId = npcId,
            SpawnerInstanceId = spawnerInstance.Id,
            NpcTemplateId = npcTemplate.Id,
            CurrentRoomId = room.Id,
            IsActive = true,
            CurrentVitality = 10,
            CurrentFatigue = 5
        };
        context.ActiveSpawns.Add(activeSpawn);
        await context.SaveChangesAsync();

        return (playerId, npcId, room.Id);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public ApplicationDbContext CreateDbContext() => new(_options);

        public async Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return CreateDbContext();
        }
    }

    private sealed class StubMessagePublisher : IGameMessagePublisher
    {
        public List<GameMessage> PublishedMessages { get; } = [];

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : GameMessage
        {
            PublishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishBatchAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : GameMessage
        {
            PublishedMessages.AddRange(messages);
            return Task.CompletedTask;
        }
    }

    private sealed class StubDiceService : IDiceService
    {
        public int Roll4dF() => 0;
        public int RollExploding4dF() => 0;
        public int Roll4dFWithModifier(int modifier, int minValue = 1, int maxValue = 20) => modifier;
        public int RollMultiple4dF(int count) => 0;
    }

    private sealed class StubSkillProgressionService : ISkillProgressionService
    {
        public Task<SkillProgressionResult> LogUsageAsync(Guid characterId, int skillDefinitionId, Data.SkillUsageType usageType, int baseExperience = 1, string? targetId = null, int? targetDifficulty = null, bool actionSucceeded = true, string? context = null, string? details = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SkillProgressionResult { ProgressionApplied = true, FinalExperience = baseExperience });
        }

        public Task<int> GetHourlyUsageCountAsync(Guid characterId, int skillDefinitionId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<int> GetDailyUsageCountAsync(Guid characterId, int skillDefinitionId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<bool> IsTargetOnCooldownAsync(Guid characterId, int skillDefinitionId, string targetId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<decimal> CalculateEffectiveMultiplierAsync(Guid characterId, int skillDefinitionId, Data.SkillUsageType usageType, string? targetId = null, int? targetDifficulty = null, bool actionSucceeded = true, CancellationToken cancellationToken = default)
            => Task.FromResult(1.0m);

        public SkillProgressionSettings GetSettings() => new();

        public Task CleanupOldTrackingDataAsync(int hourlyRetentionHours = 24, int dailyRetentionDays = 7, int cooldownRetentionHours = 24, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    #endregion
}
