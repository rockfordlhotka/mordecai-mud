using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Game.Services;
using Mordecai.Messaging.Messages;
using Mordecai.Messaging.Services;
using Mordecai.Web.Data;
using Mordecai.Web.Services;
using Xunit;

namespace Mordecai.Web.Tests;

public sealed class NpcAiTests
{
    [Fact]
    public async Task DecideAndAct_NpcAtLowVit_AttemptsFlee()
    {
        // Arrange
        var dbName = $"NpcAi_Flee_{Guid.NewGuid()}";
        var (factory, combatService, messagePublisher, diceService) = CreateServices(dbName);

        await using var context = await factory.CreateDbContextAsync();
        var (spawn, session, _) = await SeedCombatWithNpcAsync(context, npcCurrentVitality: 2, npcMaxVit: 10);

        var aiService = new NpcAiService(context, combatService, messagePublisher, new TestLogger<NpcAiService>());

        // Act - NPC at 20% VIT (2/10), should try to flee
        var acted = await aiService.DecideAndActAsync(spawn, session);

        // Assert - NPC should have acted (attempted flee)
        Assert.True(acted);
    }

    [Fact]
    public async Task DecideAndAct_NpcWithLowFat_SwitchesToParryMode()
    {
        // Arrange
        var dbName = $"NpcAi_Parry_{Guid.NewGuid()}";
        var (factory, combatService, messagePublisher, diceService) = CreateServices(dbName);

        await using var context = await factory.CreateDbContextAsync();
        var (spawn, session, _) = await SeedCombatWithNpcAsync(context, npcCurrentFatigue: 1);

        var aiService = new NpcAiService(context, combatService, messagePublisher, new TestLogger<NpcAiService>());

        // Should use parry when FAT < 3
        var shouldParry = aiService.ShouldUseParryMode(spawn, spawn.NpcTemplate!);

        // Assert
        Assert.True(shouldParry);
    }

    [Fact]
    public async Task DecideAndAct_NpcWithHighFat_UsesDodgeMode()
    {
        // Arrange
        var dbName = $"NpcAi_Dodge_{Guid.NewGuid()}";
        var (factory, combatService, messagePublisher, diceService) = CreateServices(dbName);

        await using var context = await factory.CreateDbContextAsync();
        var (spawn, session, _) = await SeedCombatWithNpcAsync(context, npcCurrentFatigue: 10);

        var aiService = new NpcAiService(context, combatService, messagePublisher, new TestLogger<NpcAiService>());

        // Should use dodge when FAT >= 3
        var shouldParry = aiService.ShouldUseParryMode(spawn, spawn.NpcTemplate!);

        // Assert
        Assert.False(shouldParry);
    }

    [Fact]
    public void GetFleeThreshold_DefaultThreshold_Returns25Percent()
    {
        // Arrange
        var template = new NpcTemplate { Name = "Goblin", Description = "Test", CreatedBy = "test" };
        var aiService = new NpcAiService(null!, null!, null!, new TestLogger<NpcAiService>());

        // Act
        var threshold = aiService.GetFleeThreshold(template);

        // Assert
        Assert.Equal(0.25m, threshold);
    }

    [Fact]
    public void GetFleeThreshold_CustomThreshold_ReturnsConfiguredValue()
    {
        // Arrange
        var template = new NpcTemplate
        {
            Name = "Brave Goblin",
            Description = "Test",
            CreatedBy = "test",
            BehaviorConfig = """{"FleeThreshold": 0.10}"""
        };
        var aiService = new NpcAiService(null!, null!, null!, new TestLogger<NpcAiService>());

        // Act
        var threshold = aiService.GetFleeThreshold(template);

        // Assert
        Assert.Equal(0.10m, threshold);
    }

    [Fact]
    public async Task DecideAndAct_NpcHealthy_AttacksPlayer()
    {
        // Arrange
        var dbName = $"NpcAi_Attack_{Guid.NewGuid()}";
        var (factory, combatService, messagePublisher, diceService) = CreateServices(dbName);

        await using var context = await factory.CreateDbContextAsync();
        // NPC at full health (10/10 VIT), should attack
        var (spawn, session, playerId) = await SeedCombatWithNpcAsync(context, npcCurrentVitality: 10, npcMaxVit: 10, npcCurrentFatigue: 10);

        var aiService = new NpcAiService(context, combatService, messagePublisher, new TestLogger<NpcAiService>());

        // Act
        var acted = await aiService.DecideAndActAsync(spawn, session);

        // Assert - NPC should have acted (attacked)
        Assert.True(acted);
    }

    #region Test Helpers

    private static (IDbContextFactory<ApplicationDbContext>, ICombatService, IGameMessagePublisher, IDiceService) CreateServices(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var factory = new TestDbContextFactory(options);
        var diceService = new StubDiceService();
        var messagePublisher = new StubMessagePublisher();
        var combatService = new StubCombatService();

        return (factory, combatService, messagePublisher, diceService);
    }

    private static async Task<(ActiveSpawn spawn, CombatSession session, Guid playerId)> SeedCombatWithNpcAsync(
        ApplicationDbContext context,
        int npcCurrentVitality = 10,
        int npcMaxVit = 10,
        int npcCurrentFatigue = 5)
    {
        var zone = new Zone { Name = "Test Zone", Description = "Zone", CreatedBy = "tests" };
        context.Zones.Add(zone);
        await context.SaveChangesAsync();

        var roomType = new RoomType { Name = "Test Type", Description = "Type" };
        context.RoomTypes.Add(roomType);
        await context.SaveChangesAsync();

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

        // Calculate strength from desired max VIT: VIT = (STR * 2) - 5 => STR = (VIT + 5) / 2
        var strength = (npcMaxVit + 5) / 2;
        var npcTemplate = new NpcTemplate
        {
            Name = "goblin",
            Description = "A green creature",
            ShortDescription = "a goblin",
            CreatedBy = "tests",
            Strength = strength,
            Endurance = 6,
            Willpower = 4
        };
        context.NpcTemplates.Add(npcTemplate);
        await context.SaveChangesAsync();

        var spawnerTemplate = new SpawnerTemplate { Name = "Test Spawner", CreatedBy = "tests" };
        context.SpawnerTemplates.Add(spawnerTemplate);
        await context.SaveChangesAsync();

        var spawnerInstance = new SpawnerInstance
        {
            SpawnerTemplateId = spawnerTemplate.Id,
            RoomId = room.Id
        };
        context.SpawnerInstances.Add(spawnerInstance);
        await context.SaveChangesAsync();

        var activeSpawn = new ActiveSpawn
        {
            NpcId = Guid.NewGuid(),
            SpawnerInstanceId = spawnerInstance.Id,
            NpcTemplateId = npcTemplate.Id,
            CurrentRoomId = room.Id,
            IsActive = true,
            CurrentFatigue = npcCurrentFatigue,
            CurrentVitality = npcCurrentVitality
        };
        context.ActiveSpawns.Add(activeSpawn);
        await context.SaveChangesAsync();

        // Load template navigation property
        await context.Entry(activeSpawn).Reference(a => a.NpcTemplate).LoadAsync();

        var playerId = Guid.NewGuid();
        var character = new Character
        {
            Id = playerId,
            UserId = "test-user",
            Name = "Test Hero",
            CurrentRoomId = room.Id,
            Physicality = 10,
            Dodge = 10,
            Drive = 10,
            Reasoning = 10,
            Awareness = 10,
            Focus = 10,
            Bearing = 10,
            CurrentFatigue = 10,
            CurrentVitality = 10
        };
        context.Characters.Add(character);
        await context.SaveChangesAsync();

        var session = new CombatSession
        {
            RoomId = room.Id,
            IsActive = true
        };
        context.CombatSessions.Add(session);
        await context.SaveChangesAsync();

        // Add NPC participant
        var npcParticipant = new CombatParticipant
        {
            CombatSessionId = session.Id,
            ActiveSpawnId = activeSpawn.Id,
            ParticipantName = npcTemplate.Name,
            IsActive = true
        };
        context.CombatParticipants.Add(npcParticipant);

        // Add player participant
        var playerParticipant = new CombatParticipant
        {
            CombatSessionId = session.Id,
            CharacterId = playerId,
            ParticipantName = character.Name,
            IsActive = true
        };
        context.CombatParticipants.Add(playerParticipant);
        await context.SaveChangesAsync();

        // Load session participants
        await context.Entry(session).Collection(s => s.Participants).LoadAsync();

        return (activeSpawn, session, playerId);
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

    private sealed class StubDiceService : IDiceService
    {
        public int Roll4dF() => 0;
        public int RollExploding4dF() => 0;
        public int Roll4dFWithModifier(int modifier, int minValue = 1, int maxValue = 20) => modifier;
        public int RollMultiple4dF(int count) => 0;
        public int Roll(int min, int max) => min;
    }

    private sealed class StubMessagePublisher : IGameMessagePublisher
    {
        public List<object> PublishedMessages { get; } = new();

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : GameMessage
        {
            PublishedMessages.Add(message!);
            return Task.CompletedTask;
        }

        public Task PublishBatchAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : GameMessage
        {
            foreach (var msg in messages)
            {
                PublishedMessages.Add(msg!);
            }
            return Task.CompletedTask;
        }
    }

    private sealed class StubCombatService : ICombatService
    {
        public bool FleeResult { get; set; } = true;
        public int? AttackResult { get; set; } = 1;

        public Task<bool> FleeFromCombatAsync(Guid participantId, bool isPlayer, CancellationToken cancellationToken = default)
            => Task.FromResult(FleeResult);

        public Task SetParryModeAsync(Guid participantId, bool isPlayer, bool enabled, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<int?> PerformMeleeAttackAsync(Guid attackerId, bool attackerIsPlayer, Guid targetId, bool targetIsPlayer,
            bool isDualWield = false, bool isOffHand = false, CancellationToken cancellationToken = default)
            => Task.FromResult(AttackResult);

        public Task<Guid?> InitiateCombatAsync(Guid attackerId, bool attackerIsPlayer, Guid targetId, bool targetIsPlayer, CancellationToken cancellationToken = default)
            => Task.FromResult<Guid?>(Guid.NewGuid());

        public Task<int?> PerformRangedAttackAsync(Guid attackerId, bool attackerIsPlayer, Guid targetId, bool targetIsPlayer, int range, CancellationToken cancellationToken = default)
            => Task.FromResult<int?>(null);

        public Task<int> PerformKnockbackAsync(Guid attackerId, bool attackerIsPlayer, Guid targetId, bool targetIsPlayer, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task EndCombatAsync(Guid combatSessionId, string reason, Guid? winnerId = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<CombatSession?> GetActiveCombatSessionAsync(Guid participantId, bool isPlayer, CancellationToken cancellationToken = default)
            => Task.FromResult<CombatSession?>(null);

        public Task<bool> IsInCombatAsync(Guid participantId, bool isPlayer, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<List<CombatParticipant>> GetCombatParticipantsAsync(Guid combatSessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<CombatParticipant>());

        public Task<CharacterCombatState> GetCharacterCombatStateAsync(Guid characterId, CancellationToken cancellationToken = default)
            => Task.FromResult(new CharacterCombatState(false, null, false));
    }

    private sealed class TestLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    #endregion
}
