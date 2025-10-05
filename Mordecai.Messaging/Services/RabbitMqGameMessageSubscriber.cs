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
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName;
    private readonly string _queueName;
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
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Create character-specific queue (temporary, auto-delete when character disconnects)
            _channel.QueueDeclare(
                queue: _queueName,
                durable: false,
                exclusive: false,
                autoDelete: true);

            _logger.LogDebug("Created message subscriber for character {CharacterId}", CharacterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Game Message Subscriber for character {CharacterId}", CharacterId);
            throw;
        }
    }

    private static ConnectionFactory CreateConnectionFactory(IConfiguration configuration)
    {
        // Try connection string first (for Aspire local development)
        var connectionString = configuration.GetConnectionString("messaging");
        
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return new ConnectionFactory
            {
                Uri = new Uri(connectionString),
                DispatchConsumersAsync = true
            };
        }

        // Fall back to individual configuration values (for Kubernetes/production)
        // Priority: Environment variables > Configuration
        var host = configuration["RabbitMQ:Host"] 
            ?? configuration["RABBITMQ_HOST"] 
            ?? "localhost";
        
        var port = int.Parse(configuration["RabbitMQ:Port"] 
            ?? configuration["RABBITMQ_PORT"] 
            ?? "5672");
        
        var username = configuration["RabbitMQ:Username"] 
            ?? configuration["RABBITMQ_USERNAME"] 
            ?? "guest";
        
        var password = configuration["RabbitMQ:Password"] 
            ?? configuration["RABBITMQ_PASSWORD"] 
            ?? "guest";

        var virtualHost = configuration["RabbitMQ:VirtualHost"] 
            ?? configuration["RABBITMQ_VIRTUALHOST"] 
            ?? "/";

        return new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = username,
            Password = password,
            VirtualHost = virtualHost,
            DispatchConsumersAsync = true
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started || _disposed)
            return;

        try
        {
            // Bind to all relevant routing keys
            await BindToRoutingKeysAsync().ConfigureAwait(false);

            // Set up consumer
            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.Received += OnMessageReceivedAsync;

            _channel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: _consumer);

            _started = true;
            _logger.LogInformation("Started message subscription for character {CharacterId} in room {RoomId}", 
                CharacterId, CurrentRoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start message subscription for character {CharacterId}", CharacterId);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_started || _disposed)
            return;

        try
        {
            if (_consumer != null)
            {
                _consumer.Received -= OnMessageReceivedAsync;
            }

            _started = false;
            _logger.LogInformation("Stopped message subscription for character {CharacterId}", CharacterId);
            
            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping message subscription for character {CharacterId}", CharacterId);
        }
    }

    private Task BindToRoutingKeysAsync()
    {
        // Bind to global messages
        _channel.QueueBind(_queueName, _exchangeName, "system.*.global");
        _channel.QueueBind(_queueName, _exchangeName, "chat.globalchatmessage.*");

        // Bind to room-specific messages if in a room
        if (CurrentRoomId.HasValue)
        {
            BindToRoomMessagesAsync(CurrentRoomId.Value);
        }

        // Bind to character-specific messages (errors, private tells, etc.)
        _channel.QueueBind(_queueName, _exchangeName, "*.*.global");
        
        return Task.CompletedTask;
    }

    private Task BindToRoomMessagesAsync(int roomId)
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
        return Task.CompletedTask;
    }

    private Task UnbindFromRoomMessagesAsync(int roomId)
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
            try
            {
                _channel.QueueUnbind(_queueName, _exchangeName, routingKey, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to unbind from routing key {RoutingKey}", routingKey);
            }
        }
        
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            var messageType = eventArgs.BasicProperties.Type;
            var messageBody = Encoding.UTF8.GetString(eventArgs.Body.Span);
            
            var gameMessage = DeserializeMessage(messageType, messageBody);
            if (gameMessage != null && ShouldProcessMessage(gameMessage))
            {
                if (MessageReceived != null)
                {
                    await MessageReceived(gameMessage).ConfigureAwait(false);
                }
            }

            // Acknowledge the message
            _channel.BasicAck(eventArgs.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for character {CharacterId}", CharacterId);
            
            // Reject the message (don't requeue to avoid infinite loops)
            _channel.BasicNack(eventArgs.DeliveryTag, false, false);
        }
    }

    private GameMessage? DeserializeMessage(string messageType, string messageBody)
    {
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
        // If message has specific target character IDs, check if this character is included
        if (message.TargetCharacterIds?.Any() == true)
        {
            return message.TargetCharacterIds.Contains(CharacterId);
        }

        // If message has a room ID, check if this character is in that room
        if (message.RoomId.HasValue)
        {
            return CurrentRoomId == message.RoomId.Value;
        }

        // Global messages (no room ID, no specific targets) are always processed
        return true;
    }

    public async Task UpdateRoomAsync(int? newRoomId, CancellationToken cancellationToken = default)
    {
        if (CurrentRoomId == newRoomId || !_started)
            return;

        try
        {
            // Unbind from old room if we were in one
            if (CurrentRoomId.HasValue)
            {
                await UnbindFromRoomMessagesAsync(CurrentRoomId.Value).ConfigureAwait(false);
            }

            // Bind to new room if we're entering one
            if (newRoomId.HasValue)
            {
                await BindToRoomMessagesAsync(newRoomId.Value).ConfigureAwait(false);
            }

            CurrentRoomId = newRoomId;
            _logger.LogDebug("Updated character {CharacterId} room subscription from {OldRoom} to {NewRoom}", 
                CharacterId, CurrentRoomId, newRoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update room subscription for character {CharacterId}", CharacterId);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            if (_started)
            {
                // Use synchronous version in Dispose to avoid blocking
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