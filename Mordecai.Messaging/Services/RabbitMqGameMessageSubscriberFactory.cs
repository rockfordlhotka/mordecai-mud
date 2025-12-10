using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mordecai.Messaging.Services;

/// <summary>
/// Factory for creating RabbitMQ-based game message subscribers
/// </summary>
public sealed class RabbitMqGameMessageSubscriberFactory : IGameMessageSubscriberFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public RabbitMqGameMessageSubscriberFactory(
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _loggerFactory = loggerFactory;
    }

    public IGameMessageSubscriber CreateSubscriber(Guid characterId, int? initialRoomId = null, int? initialZoneId = null)
    {
        var logger = _loggerFactory.CreateLogger<RabbitMqGameMessageSubscriber>();
        return new RabbitMqGameMessageSubscriber(characterId, initialRoomId, initialZoneId, _configuration, logger);
    }
}
