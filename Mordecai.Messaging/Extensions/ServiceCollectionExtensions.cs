using Microsoft.Extensions.DependencyInjection;
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
        services.AddSingleton<IGameMessagePublisher, RabbitMqGameMessagePublisher>();
        services.AddSingleton<IGameMessageSubscriberFactory, RabbitMqGameMessageSubscriberFactory>();
        
        return services;
    }
}