using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mordecai.Messaging.Messages;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Mordecai.Messaging.Services;

/// <summary>
/// RabbitMQ-based implementation of the game message publisher
/// </summary>
public sealed class RabbitMqGameMessagePublisher : IGameMessagePublisher, IDisposable
{
    private readonly ILogger<RabbitMqGameMessagePublisher> _logger;
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly string _exchangeName;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public RabbitMqGameMessagePublisher(
        IConfiguration configuration,
        ILogger<RabbitMqGameMessagePublisher> logger)
    {
        _logger = logger;
        _exchangeName = "mordecai.game.events";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        try
        {
            var factory = CreateConnectionFactory(configuration);

            // Try to create connection, but gracefully handle failures
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare the exchange for game events (topic exchange for routing)
                _channel.ExchangeDeclare(
                    exchange: _exchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                _logger.LogInformation("RabbitMQ Game Message Publisher initialized successfully");
            }
            catch (Exception rabbitEx)
            {
                _logger.LogWarning(rabbitEx, "Could not connect to RabbitMQ. Publisher will operate in offline mode.");
                // Don't rethrow - allow the service to start without RabbitMQ for development
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Game Message Publisher");
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

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : GameMessage
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqGameMessagePublisher));

        // If RabbitMQ is not available, log and continue
        if (_connection == null || !_connection.IsOpen || _channel == null || !_channel.IsOpen)
        {
            _logger.LogWarning("RabbitMQ connection not available. Message {MessageType} will not be published.", typeof(T).Name);
            return;
        }

        try
        {
            var routingKey = GetRoutingKey(message);
            var messageBody = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);

            var properties = _channel.CreateBasicProperties();
            properties.MessageId = message.MessageId.ToString();
            properties.Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds());
            properties.Type = typeof(T).Name;
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: messageBody);

            _logger.LogDebug("Published message {MessageType} with routing key {RoutingKey}", 
                typeof(T).Name, routingKey);

            await Task.Yield();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message {MessageType}", typeof(T).Name);
            // Don't rethrow - let the application continue even if messaging fails
        }
    }

    public async Task PublishBatchAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : GameMessage
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqGameMessagePublisher));

        try
        {
            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await PublishAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message batch");
            // Don't rethrow - let the application continue
        }
    }

    private static string GetRoutingKey<T>(T message) where T : GameMessage
    {
        var messageType = typeof(T).Name.ToLowerInvariant();
        var category = GetMessageCategory(message);
        var roomPart = message.RoomId?.ToString() ?? "global";
        
        return $"{category}.{messageType}.{roomPart}";
    }

    private static string GetMessageCategory<T>(T message) where T : GameMessage
    {
        return message switch
        {
            PlayerMoved or PlayerLeft or PlayerJoined or PlayerDisconnected => "movement",
            ChatMessage or GlobalChatMessage or EmoteMessage => "chat",
            CombatStarted or CombatAction or CombatEnded or HealthChanged => "combat",
            SkillExperienceGained or SkillUsed or SkillLearned => "skill",
            SystemMessage or AdminAction or ErrorMessage => "system",
            ZoneEnvironmentMessage or RoomEnvironmentMessage or ZoneEventMessage => "environment",
            _ => "general"
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ Game Message Publisher");
        }

        _disposed = true;
    }
}
