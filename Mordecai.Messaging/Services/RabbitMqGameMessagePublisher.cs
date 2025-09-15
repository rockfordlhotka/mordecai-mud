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
    private readonly IConnection _connection;
    private readonly IModel _channel;
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
            // Get RabbitMQ connection string from configuration (Aspire will provide this)
            var connectionString = configuration.GetConnectionString("messaging") 
                ?? throw new InvalidOperationException("RabbitMQ connection string 'messaging' not found");

            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
                DispatchConsumersAsync = true
            };

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Game Message Publisher");
            throw;
        }
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : GameMessage
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqGameMessagePublisher));

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

            // RabbitMQ.Client operations are sync, but we add a Task.Yield to be properly async
            await Task.Yield();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message {MessageType}", typeof(T).Name);
            throw;
        }
    }

    public async Task PublishBatchAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : GameMessage
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqGameMessagePublisher));

        try
        {
            // Process all messages in the batch
            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await PublishAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message batch");
            throw;
        }
    }

    private static string GetRoutingKey<T>(T message) where T : GameMessage
    {
        var messageType = typeof(T).Name.ToLowerInvariant();
        
        // Create routing keys for topic-based routing
        // Format: {category}.{type}.{room_id}
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