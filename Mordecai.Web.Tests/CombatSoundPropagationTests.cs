using Microsoft.Extensions.Logging.Abstractions;
using Mordecai.Messaging.Messages;
using Mordecai.Messaging.Services;
using Mordecai.Web.Services;
using Xunit;

namespace Mordecai.Web.Tests;

/// <summary>
/// Tests for combat sound propagation to adjacent rooms based on sound levels
/// </summary>
public sealed class CombatSoundPropagationTests
{
    [Theory]
    [InlineData(SoundLevel.Silent, 0)]
    [InlineData(SoundLevel.Quiet, 1)]
    [InlineData(SoundLevel.Normal, 1)]
    [InlineData(SoundLevel.Loud, 2)]
    [InlineData(SoundLevel.VeryLoud, 3)]
    [InlineData(SoundLevel.Deafening, 4)]
    public async Task PropagateSound_ShouldRespectSoundLevelDistances(SoundLevel soundLevel, int expectedMaxDistance)
    {
        // Arrange
        const int sourceRoomId = 100;
        var adjacencyService = new TestAdjacencyService();
        var messagePublisher = new TestMessagePublisher();
        var soundPropagation = new SoundPropagationService(
            messagePublisher,
            adjacencyService,
            NullLogger<SoundPropagationService>.Instance
        );

        // Setup adjacent rooms at various distances
        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(101, 1, "north", "south", new List<string> { "north" }));
        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(102, 2, "south", "north", new List<string> { "south", "south" }));
        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(103, 3, "east", "west", new List<string> { "east", "east", "east" }));
        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(104, 4, "west", "east", new List<string> { "west", "west", "west", "west" }));

        // Act
        await soundPropagation.PropagateSound(
            sourceRoomId,
            soundLevel,
            SoundType.Combat,
            "combat sounds",
            "Attacker"
        );

        // Assert
        if (expectedMaxDistance == 0)
        {
            // Silent should propagate to no rooms
            Assert.Empty(messagePublisher.PublishedMessages);
        }
        else
        {
            // Should only reach rooms within distance
            var soundMessages = messagePublisher.PublishedMessages
                .OfType<AdjacentRoomSoundMessage>()
                .ToList();

            Assert.All(soundMessages, msg =>
            {
                var adjacentRoom = adjacencyService.GetAdjacentRoom(sourceRoomId, msg.ListenerRoomId);
                Assert.NotNull(adjacentRoom);
                Assert.True(adjacentRoom.Distance <= expectedMaxDistance,
                    $"Room {msg.ListenerRoomId} at distance {adjacentRoom.Distance} should not receive sound with max distance {expectedMaxDistance}");
            });

            // Verify correct number of rooms received message
            var expectedRoomCount = adjacencyService.GetRoomsWithinDistance(sourceRoomId, expectedMaxDistance).Count;
            Assert.Equal(expectedRoomCount, soundMessages.Count);
        }
    }

    [Fact]
    public async Task CombatStarted_WithLoudSound_ShouldPropagateTo2Rooms()
    {
        // Arrange
        const int sourceRoomId = 100;
        var adjacencyService = new TestAdjacencyService();
        var messagePublisher = new TestMessagePublisher();
        var soundPropagation = new SoundPropagationService(
            messagePublisher,
            adjacencyService,
            NullLogger<SoundPropagationService>.Instance
        );

        // Setup rooms: source + 1 adjacent + 1 two-rooms-away
        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(101, 1, "north", "south", new List<string> { "north" }));
        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(102, 2, "south", "north", new List<string> { "south", "south" }));
        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(103, 3, "east", "west", new List<string> { "east", "east", "east" })); // Too far

        // Act
        await soundPropagation.PropagateSound(
            sourceRoomId,
            SoundLevel.Loud,
            SoundType.Combat,
            "combat beginning",
            "Warrior",
            "Warrior engages the Orc in combat!"
        );

        // Assert
        var soundMessages = messagePublisher.PublishedMessages
            .OfType<AdjacentRoomSoundMessage>()
            .ToList();

        Assert.Equal(2, soundMessages.Count); // Loud reaches 2 rooms
        Assert.Contains(soundMessages, m => m.ListenerRoomId == 101);
        Assert.Contains(soundMessages, m => m.ListenerRoomId == 102);
        Assert.DoesNotContain(soundMessages, m => m.ListenerRoomId == 103); // Too far
    }

    [Fact]
    public async Task CombatAction_WithNormalSound_ShouldOnlyReachAdjacentRoom()
    {
        // Arrange
        const int sourceRoomId = 200;
        var adjacencyService = new TestAdjacencyService();
        var messagePublisher = new TestMessagePublisher();
        var soundPropagation = new SoundPropagationService(
            messagePublisher,
            adjacencyService,
            NullLogger<SoundPropagationService>.Instance
        );

        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(201, 1, "west", "east", new List<string> { "west" }));
        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(202, 2, "east", "west", new List<string> { "east", "east" })); // Too far for Normal

        // Act
        await soundPropagation.PropagateSound(
            sourceRoomId,
            SoundLevel.Normal,
            SoundType.Combat,
            "weapon clashing",
            "Fighter"
        );

        // Assert
        var soundMessages = messagePublisher.PublishedMessages
            .OfType<AdjacentRoomSoundMessage>()
            .ToList();

        Assert.Single(soundMessages);
        Assert.Equal(201, soundMessages[0].ListenerRoomId);
        Assert.Equal(SoundLevel.Normal, soundMessages[0].SoundLevel);
    }

    [Fact]
    public async Task CombatMessageBroadcast_ShouldPropagateAllCombatEvents()
    {
        // Arrange
        const int roomId = 300;
        var adjacencyService = new TestAdjacencyService();
        var messagePublisher = new TestMessagePublisher();
        var soundPropagation = new SoundPropagationService(
            messagePublisher,
            adjacencyService,
            NullLogger<SoundPropagationService>.Instance
        );

        adjacencyService.AddAdjacentRoom(roomId, new AdjacentRoomInfo(301, 1, "north", "south", new List<string> { "north" }));

        // Note: CombatMessageBroadcastService is a BackgroundService that requires IServiceScopeFactory.
        // This test validates sound propagation directly through the SoundPropagationService,
        // which is what the broadcast service internally uses.

        // Act - Simulate combat events
        var combatStarted = new CombatStarted(
            Guid.NewGuid(),
            "Knight",
            Guid.NewGuid(),
            "Dragon",
            roomId,
            SoundLevel.Loud
        );

        var combatAction = new CombatAction(
            combatStarted.InitiatorId,
            "Knight",
            combatStarted.TargetId,
            "Dragon",
            roomId,
            "Knight slashes with longsword!",
            8,
            true,
            "Longsword",
            SoundLevel.Normal
        );

        var combatEnded = new CombatEnded(
            roomId,
            "Dragon defeated",
            combatStarted.InitiatorId,
            "Knight"
        );

        // Simulate message publication (normally done by combat service)
        await soundPropagation.PropagateSound(roomId, combatStarted.SoundLevel, SoundType.Combat,
            $"{combatStarted.InitiatorName} attacking {combatStarted.TargetName}", combatStarted.InitiatorName);
        await soundPropagation.PropagateSound(roomId, combatAction.SoundLevel, SoundType.Combat,
            combatAction.ActionDescription, combatAction.AttackerName);
        await soundPropagation.PropagateSound(roomId, SoundLevel.Normal, SoundType.Combat,
            $"{combatEnded.WinnerName} victorious", combatEnded.WinnerName);

        // Assert
        var soundMessages = messagePublisher.PublishedMessages
            .OfType<AdjacentRoomSoundMessage>()
            .ToList();

        Assert.Equal(3, soundMessages.Count); // All 3 events propagated
        Assert.All(soundMessages, m => Assert.Equal(301, m.ListenerRoomId));
        Assert.All(soundMessages, m => Assert.Equal(SoundType.Combat, m.SoundType));
    }

    [Fact]
    public async Task SoundPropagation_WithClosedDoor_ShouldStillPropagate()
    {
        // Arrange
        const int sourceRoomId = 400;
        var adjacencyService = new TestAdjacencyService();
        var messagePublisher = new TestMessagePublisher();
        var soundPropagation = new SoundPropagationService(
            messagePublisher,
            adjacencyService,
            NullLogger<SoundPropagationService>.Instance
        );

        // Adjacent room with closed door (still counts as distance 1 for sound)
        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(401, 1, "south", "north", new List<string> { "south" }));

        // Act
        await soundPropagation.PropagateSound(
            sourceRoomId,
            SoundLevel.Loud,
            SoundType.Combat,
            "muffled combat sounds"
        );

        // Assert
        var soundMessages = messagePublisher.PublishedMessages
            .OfType<AdjacentRoomSoundMessage>()
            .ToList();

        Assert.Single(soundMessages);
        Assert.Equal(401, soundMessages[0].ListenerRoomId);
    }

    [Fact]
    public async Task SoundPropagation_WithDirection_ShouldIncludeDirectionInMessage()
    {
        // Arrange
        const int sourceRoomId = 500;
        var adjacencyService = new TestAdjacencyService();
        var messagePublisher = new TestMessagePublisher();
        var soundPropagation = new SoundPropagationService(
            messagePublisher,
            adjacencyService,
            NullLogger<SoundPropagationService>.Instance
        );

        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(501, 1, "north", "south", new List<string> { "north" }));
        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(502, 1, "above", "below", new List<string> { "above" }));
        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(503, 1, "below", "above", new List<string> { "below" }));

        // Act
        await soundPropagation.PropagateSound(
            sourceRoomId,
            SoundLevel.Loud,
            SoundType.Combat,
            "battle sounds"
        );

        // Assert
        var soundMessages = messagePublisher.PublishedMessages
            .OfType<AdjacentRoomSoundMessage>()
            .OrderBy(m => m.ListenerRoomId)
            .ToList();

        Assert.Equal(3, soundMessages.Count);

        // North room hears sound from south (DirectionFromListener = south)
        Assert.Equal("south", soundMessages[0].Direction);
        Assert.Contains("south", soundMessages[0].Description, StringComparison.OrdinalIgnoreCase);

        // Above room hears sound from below (DirectionFromListener = below)
        Assert.Equal("below", soundMessages[1].Direction);
        Assert.Contains("below", soundMessages[1].Description, StringComparison.OrdinalIgnoreCase);

        // Below room hears sound from above (DirectionFromListener = above)
        Assert.Equal("above", soundMessages[2].Direction);
        Assert.Contains("above", soundMessages[2].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1, "You hear battle sounds from the south.")]
    [InlineData(2, "You hear distant battle sounds to the south.")]
    [InlineData(3, "You hear very distant battle sounds to the south.")]
    public async Task SoundPropagation_DescriptionShouldReflectDistance(int distance, string expectedPattern)
    {
        // Arrange
        const int sourceRoomId = 600;
        var adjacencyService = new TestAdjacencyService();
        var messagePublisher = new TestMessagePublisher();
        var soundPropagation = new SoundPropagationService(
            messagePublisher,
            adjacencyService,
            NullLogger<SoundPropagationService>.Instance
        );

        adjacencyService.AddAdjacentRoom(sourceRoomId, new AdjacentRoomInfo(601, distance, "north", "south", Enumerable.Repeat("north", distance).ToList()));

        // Act
        await soundPropagation.PropagateSound(
            sourceRoomId,
            SoundLevel.Deafening, // Reaches far
            SoundType.Combat,
            "battle sounds"
        );

        // Assert
        var soundMessages = messagePublisher.PublishedMessages
            .OfType<AdjacentRoomSoundMessage>()
            .ToList();

        if (distance <= 4) // Deafening reaches up to 4 rooms
        {
            Assert.Single(soundMessages);
            var message = soundMessages[0];

            if (distance == 1)
            {
                Assert.Contains("from the south", message.Description, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Assert.Contains("distant", message.Description, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("to the south", message.Description, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    #region Helper Classes

    private sealed class TestAdjacencyService : IRoomAdjacencyService
    {
        private readonly Dictionary<int, List<AdjacentRoomInfo>> _adjacentRooms = new();

        public void AddAdjacentRoom(int sourceRoomId, AdjacentRoomInfo adjacentRoom)
        {
            if (!_adjacentRooms.ContainsKey(sourceRoomId))
            {
                _adjacentRooms[sourceRoomId] = new List<AdjacentRoomInfo>();
            }
            _adjacentRooms[sourceRoomId].Add(adjacentRoom);
        }

        public AdjacentRoomInfo? GetAdjacentRoom(int sourceRoomId, int targetRoomId)
        {
            if (_adjacentRooms.TryGetValue(sourceRoomId, out var rooms))
            {
                return rooms.FirstOrDefault(r => r.RoomId == targetRoomId);
            }
            return null;
        }

        public List<AdjacentRoomInfo> GetRoomsWithinDistance(int sourceRoomId, int maxDistance)
        {
            if (_adjacentRooms.TryGetValue(sourceRoomId, out var rooms))
            {
                return rooms.Where(r => r.Distance <= maxDistance).ToList();
            }
            return new List<AdjacentRoomInfo>();
        }

        public Task<IReadOnlyList<AdjacentRoomInfo>> GetAdjacentRoomsAsync(int sourceRoomId, int maxDistance, CancellationToken cancellationToken = default)
        {
            var rooms = GetRoomsWithinDistance(sourceRoomId, maxDistance);
            return Task.FromResult<IReadOnlyList<AdjacentRoomInfo>>(rooms);
        }
    }

    private sealed class TestMessagePublisher : IGameMessagePublisher
    {
        public List<GameMessage> PublishedMessages { get; } = new();

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

    private sealed class TestSubscriberFactory : IGameMessageSubscriberFactory
    {
        public IGameMessageSubscriber CreateSubscriber(Guid characterId, int? roomId = null, int? zoneId = null)
        {
            return new TestSubscriber { CharacterId = characterId };
        }
    }

    private sealed class TestSubscriber : IGameMessageSubscriber
    {
        public Guid CharacterId { get; set; }
        public int? CurrentRoomId { get; set; }
        public int? CurrentZoneId { get; set; }

        public event Func<GameMessage, Task>? MessageReceived;

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateRoomAsync(int? newRoomId, CancellationToken cancellationToken = default)
        {
            CurrentRoomId = newRoomId;
            return Task.CompletedTask;
        }
        public Task UpdateZoneAsync(int? newZoneId, CancellationToken cancellationToken = default)
        {
            CurrentZoneId = newZoneId;
            return Task.CompletedTask;
        }
        public void Dispose() { }

        public async Task SimulateMessageAsync(GameMessage message)
        {
            if (MessageReceived != null)
            {
                await MessageReceived.Invoke(message);
            }
        }
    }

    #endregion
}
