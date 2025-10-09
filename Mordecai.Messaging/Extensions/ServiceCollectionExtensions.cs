using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Mordecai.Messaging.Services;

namespace Mordecai.Messaging.Extensions;

/// <summary>
/// Extension methods for registering messaging services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the game messaging services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddGameMessaging(this IServiceCollection services)
    {
        // Prefer RabbitMQ-based implementations when available. Fall back to stubs only if RabbitMQ components fail at runtime.
        services.AddSingleton<IGameMessagePublisher, RabbitMqGameMessagePublisher>();
        services.AddSingleton<IGameMessageSubscriberFactory, RabbitMqGameMessageSubscriberFactory>();
        
        return services;
    }
}