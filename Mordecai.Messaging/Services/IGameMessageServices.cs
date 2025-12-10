using Mordecai.Messaging.Messages;

namespace Mordecai.Messaging.Services;

/// <summary>
/// Service for publishing messages to the game's RabbitMQ pub/sub system
/// </summary>
public interface IGameMessagePublisher
{
    /// <summary>
    /// Publishes a game message to all subscribers
    /// </summary>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : GameMessage;
    
    /// <summary>
    /// Publishes multiple messages in a batch
    /// </summary>
    Task PublishBatchAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : GameMessage;
}

/// <summary>
/// Service for subscribing to game messages for a specific character
/// </summary>
public interface IGameMessageSubscriber : IDisposable
{
    /// <summary>
    /// Character ID this subscriber is for
    /// </summary>
    Guid CharacterId { get; }
    
    /// <summary>
    /// Current room ID of the character (for filtering room-based messages)
    /// </summary>
    int? CurrentRoomId { get; set; }
    
    /// <summary>
    /// Current zone ID of the character (for filtering zone-based messages)
    /// </summary>
    int? CurrentZoneId { get; set; }

    /// <summary>
    /// Event fired when a relevant message is received
    /// </summary>
    event Func<GameMessage, Task>? MessageReceived;
    
    /// <summary>
    /// Starts the subscription
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the subscription
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the room subscriptions when a character moves
    /// </summary>
    Task UpdateRoomAsync(int? newRoomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the zone subscriptions when a character moves
    /// </summary>
    Task UpdateZoneAsync(int? newZoneId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating message subscribers
/// </summary>
public interface IGameMessageSubscriberFactory
{
    /// <summary>
    /// Creates a new subscriber for the specified character
    /// </summary>
    IGameMessageSubscriber CreateSubscriber(Guid characterId, int? initialRoomId = null, int? initialZoneId = null);
}