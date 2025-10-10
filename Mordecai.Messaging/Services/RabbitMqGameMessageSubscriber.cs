using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mordecai.Messaging.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Mordecai.Messaging.Services;

/// <summary>
/// RabbitMQ-based implementation of game message subscriber for individual characters
/// </summary>
public sealed class RabbitMqGameMessageSubscriber : IGameMessageSubscriber
{
    private readonly ILogger<RabbitMqGameMessageSubscriber> _logger;
    private readonly IConnection? _connection;
    private IModel? _channel;
    private readonly string _exchangeName;
    private string _queueName = string.Empty;
    private readonly JsonSerializerOptions _jsonOptions;
    private AsyncEventingBasicConsumer? _consumer;
    private bool _disposed;
    private bool _started;

    public Guid CharacterId { get; }
    public int? CurrentRoomId { get; set; }

    public event Func<GameMessage, Task>? MessageReceived;

    public RabbitMqGameMessageSubscriber(
        Guid characterId,
        int? initialRoomId,
        IConfiguration configuration,
        ILogger<RabbitMqGameMessageSubscriber> logger)
    {
        CharacterId = characterId;
        CurrentRoomId = initialRoomId;
        _logger = logger;
        _exchangeName = "mordecai.game.events";
        _queueName = $"character.{characterId}";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        try
        {
            var factory = CreateConnectionFactory(configuration);

            // Try to create connection, but gracefully handle failures
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Ensure the exchange exists (topic exchange)
                _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true);

                // Let the server create a unique, temporary queue for this subscriber
                _queueName = _channel.QueueDeclare().QueueName;

                _logger.LogDebug("Created message subscriber for character {CharacterId} with queue {QueueName}", CharacterId, _queueName);
            }
            catch (Exception rabbitEx)
            {
                _logger.LogWarning(rabbitEx, "Could not connect to RabbitMQ for character {CharacterId}. Subscriber will operate in offline mode.", CharacterId);
                // Don't rethrow - allow the service to start without RabbitMQ for development
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Game Message Subscriber for character {CharacterId}", CharacterId);
            throw;
        }
        }
    private static ConnectionFactory CreateConnectionFactory(IConfiguration configuration)
    {
        var host = configuration["RABBITMQ_HOST"] 
            ?? configuration["RabbitMQ:Host"] 
            ?? "localhost";

        var portString = configuration["RABBITMQ_PORT"] ?? configuration["RabbitMQ:Port"] ?? "5672";
        if (!int.TryParse(portString, out var port))
        {
            port = 5672;
        }

        var username = configuration["RABBITMQ_USERNAME"] 
            ?? configuration["RabbitMQ:Username"] 
            ?? "guest";

        var password = configuration["RABBITMQ_PASSWORD"] 
            ?? configuration["RabbitMQ:Password"] 
            ?? "guest";

        var virtualHost = configuration["RABBITMQ_VIRTUALHOST"] 
            ?? configuration["RabbitMQ:VirtualHost"] 
            ?? "/";

        return new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = username,
            Password = password,
            VirtualHost = virtualHost,
            DispatchConsumersAsync = true,
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            AutomaticRecoveryEnabled = true
        };
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started || _disposed)
            return Task.CompletedTask;

        // If RabbitMQ is not available, just mark as started
        if (_connection == null || _channel == null)
        {
            _logger.LogWarning("RabbitMQ connection not available for character {CharacterId}. Operating in offline mode.", CharacterId);
            _started = true;
            return Task.CompletedTask;
        }

        try
        {
            // Bind to all relevant routing keys
            BindToRoutingKeys();


            // Set up async consumer so it works with DispatchConsumersAsync = true on the connection factory
            _consumer = new AsyncEventingBasicConsumer(_channel);

            // Wire the async handler which will deserialize, filter and invoke the MessageReceived callback
            _consumer.Received += OnMessageReceivedAsync;

            // Use manual ack=false so the async handler can ack/nack messages after processing
            _channel.BasicConsume(_queueName, autoAck: false, consumer: _consumer);

            _started = true;
            _logger.LogInformation("Started message subscription for character {CharacterId} in room {RoomId}", 
                CharacterId, CurrentRoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start message subscription for character {CharacterId}", CharacterId);
            // Don't rethrow - allow the game to continue without messaging
            _started = true;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_started || _disposed)
            return Task.CompletedTask;

        try
        {
            if (_consumer != null)
            {
                _consumer.Received -= OnMessageReceivedAsync;
            }

            _started = false;
            _logger.LogInformation("Stopped message subscription for character {CharacterId}", CharacterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping message subscription for character {CharacterId}", CharacterId);
        }

        return Task.CompletedTask;
    }

    private void BindToRoutingKeys()
    {
        if (_channel == null)
            return;

        try
        {
            // Bind to global messages
            _channel.QueueBind(_queueName, _exchangeName, "system.*.global");
            _channel.QueueBind(_queueName, _exchangeName, "chat.globalchatmessage.*");

            // Bind to room-specific messages if in a room
            if (CurrentRoomId.HasValue)
            {
                BindToRoomMessages(CurrentRoomId.Value);
            }

            // Bind to character-specific messages (errors, private tells, etc.)
            _channel.QueueBind(_queueName, _exchangeName, "*.*.global");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to bind to routing keys for character {CharacterId}", CharacterId);
        }
    }

    private void BindToRoomMessages(int roomId)
    {
        if (_channel == null)
            return;

        try
        {
            var roomRoutingKeys = new[]
            {
                $"movement.*.{roomId}",
                $"chat.*.{roomId}",
                $"combat.*.{roomId}",
                $"skill.*.{roomId}"
            };

            foreach (var routingKey in roomRoutingKeys)
            {
                _channel.QueueBind(_queueName, _exchangeName, routingKey);
            }

            _logger.LogDebug("Bound character {CharacterId} to room {RoomId} messages", CharacterId, roomId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to bind to room messages for character {CharacterId}", CharacterId);
        }
    }

    private void UnbindFromRoomMessages(int roomId)
    {
        if (_channel == null)
            return;

        var roomRoutingKeys = new[]
        {
            $"movement.*.{roomId}",
            $"chat.*.{roomId}",
            $"combat.*.{roomId}",
            $"skill.*.{roomId}"
        };

        foreach (var routingKey in roomRoutingKeys)
        {
            try
            {
                _channel.QueueUnbind(_queueName, _exchangeName, routingKey, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to unbind from routing key {RoutingKey}", routingKey);
            }
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            var messageType = eventArgs.BasicProperties?.Type;
            var bodyArray = eventArgs.Body.ToArray();
            var messageBody = Encoding.UTF8.GetString(bodyArray);
            
            var gameMessage = DeserializeMessage(messageType, messageBody);
            if (gameMessage != null && ShouldProcessMessage(gameMessage))
            {
                if (MessageReceived != null)
                {
                    await MessageReceived(gameMessage).ConfigureAwait(false);
                }
            }

            // Acknowledge the message
            _channel?.BasicAck(eventArgs.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for character {CharacterId}", CharacterId);
            
            // Reject the message (don't requeue to avoid infinite loops)
            _channel?.BasicNack(eventArgs.DeliveryTag, false, false);
        }
    }

    private GameMessage? DeserializeMessage(string? messageType, string messageBody)
    {
        if (string.IsNullOrEmpty(messageType))
            return null;

        try
        {
            return messageType switch
            {
                nameof(PlayerMoved) => JsonSerializer.Deserialize<PlayerMoved>(messageBody, _jsonOptions),
                nameof(PlayerLeft) => JsonSerializer.Deserialize<PlayerLeft>(messageBody, _jsonOptions),
                nameof(PlayerJoined) => JsonSerializer.Deserialize<PlayerJoined>(messageBody, _jsonOptions),
                nameof(PlayerDisconnected) => JsonSerializer.Deserialize<PlayerDisconnected>(messageBody, _jsonOptions),
                nameof(ChatMessage) => JsonSerializer.Deserialize<ChatMessage>(messageBody, _jsonOptions),
                nameof(GlobalChatMessage) => JsonSerializer.Deserialize<GlobalChatMessage>(messageBody, _jsonOptions),
                nameof(EmoteMessage) => JsonSerializer.Deserialize<EmoteMessage>(messageBody, _jsonOptions),
                nameof(CombatStarted) => JsonSerializer.Deserialize<CombatStarted>(messageBody, _jsonOptions),
                nameof(CombatAction) => JsonSerializer.Deserialize<CombatAction>(messageBody, _jsonOptions),
                nameof(CombatEnded) => JsonSerializer.Deserialize<CombatEnded>(messageBody, _jsonOptions),
                nameof(HealthChanged) => JsonSerializer.Deserialize<HealthChanged>(messageBody, _jsonOptions),
                nameof(SkillExperienceGained) => JsonSerializer.Deserialize<SkillExperienceGained>(messageBody, _jsonOptions),
                nameof(SkillUsed) => JsonSerializer.Deserialize<SkillUsed>(messageBody, _jsonOptions),
                nameof(SkillLearned) => JsonSerializer.Deserialize<SkillLearned>(messageBody, _jsonOptions),
                nameof(SystemMessage) => JsonSerializer.Deserialize<SystemMessage>(messageBody, _jsonOptions),
                nameof(AdminAction) => JsonSerializer.Deserialize<AdminAction>(messageBody, _jsonOptions),
                nameof(ErrorMessage) => JsonSerializer.Deserialize<ErrorMessage>(messageBody, _jsonOptions),
                nameof(ZoneEnvironmentMessage) => JsonSerializer.Deserialize<ZoneEnvironmentMessage>(messageBody, _jsonOptions),
                nameof(RoomEnvironmentMessage) => JsonSerializer.Deserialize<RoomEnvironmentMessage>(messageBody, _jsonOptions),
                nameof(ZoneEventMessage) => JsonSerializer.Deserialize<ZoneEventMessage>(messageBody, _jsonOptions),
                _ => null
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message of type {MessageType}", messageType);
            return null;
        }
    }

    private bool ShouldProcessMessage(GameMessage message)
    {
        if (message.TargetCharacterIds?.Any() == true)
        {
            return message.TargetCharacterIds.Contains(CharacterId);
        }

        if (message.RoomId.HasValue)
        {
            return CurrentRoomId == message.RoomId.Value;
        }

        return true;
    }

    public Task UpdateRoomAsync(int? newRoomId, CancellationToken cancellationToken = default)
    {
        if (CurrentRoomId == newRoomId || !_started)
            return Task.CompletedTask;

        try
        {
            if (CurrentRoomId.HasValue)
            {
                UnbindFromRoomMessages(CurrentRoomId.Value);
            }

            if (newRoomId.HasValue)
            {
                BindToRoomMessages(newRoomId.Value);
            }

            CurrentRoomId = newRoomId;
            _logger.LogDebug("Updated character {CharacterId} room subscription from {OldRoom} to {NewRoom}", 
                CharacterId, CurrentRoomId, newRoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update room subscription for character {CharacterId}", CharacterId);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            if (_started)
            {
                _started = false;
                if (_consumer != null)
                {
                    _consumer.Received -= OnMessageReceivedAsync;
                }
            }
            
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ Game Message Subscriber for character {CharacterId}", CharacterId);
        }

        _disposed = true;
    }
}
