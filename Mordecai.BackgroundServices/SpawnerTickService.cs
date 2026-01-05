using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mordecai.Game.Services;

namespace Mordecai.BackgroundServices;

/// <summary>
/// Background service that periodically processes spawners to spawn NPCs
/// </summary>
public class SpawnerTickService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SpawnerTickService> _logger;

    // Process spawners every 10 seconds
    private const int ProcessingIntervalSeconds = 10;

    public SpawnerTickService(
        IServiceProvider serviceProvider,
        ILogger<SpawnerTickService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Spawner tick background service started");

        // Wait a bit before starting to allow other services to initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSpawnersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing spawners");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(ProcessingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Spawner tick background service stopped");
    }

    private async Task ProcessSpawnersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var spawnerService = scope.ServiceProvider.GetRequiredService<ISpawnerService>();

        try
        {
            await spawnerService.ProcessSpawnersAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in spawner processing");
        }

        // Periodically clean up old inactive spawns (once per hour)
        if (DateTime.UtcNow.Minute == 0)
        {
            try
            {
                await spawnerService.CleanupInactiveSpawnsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up inactive spawns");
            }
        }
    }
}
