using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mordecai.Web.Services;

/// <summary>
/// Background service for processing room effects periodically
/// </summary>
public class RoomEffectBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RoomEffectBackgroundService> _logger;
    private const int ProcessingIntervalSeconds = 10; // Process room effects every 10 seconds

    public RoomEffectBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<RoomEffectBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Room Effect Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRoomEffects(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(ProcessingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing room effects");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait 1 minute before retrying on error
            }
        }

        _logger.LogInformation("Room Effect Background Service stopped");
    }

    private async Task ProcessRoomEffects(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var roomEffectService = scope.ServiceProvider.GetRequiredService<IRoomEffectService>();
        var worldService = scope.ServiceProvider.GetRequiredService<IWorldService>();

        try
        {
            // First, clean up expired effects
            await roomEffectService.CleanupExpiredEffectsAsync(cancellationToken);

            // Get all rooms that have characters currently in them
            var occupiedRooms = await worldService.GetOccupiedRoomsAsync(cancellationToken);

            // Process periodic effects for each occupied room
            foreach (var roomId in occupiedRooms)
            {
                await roomEffectService.ProcessPeriodicEffectsAsync(roomId, cancellationToken);
            }

            _logger.LogDebug("Processed room effects for {RoomCount} occupied rooms", occupiedRooms.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessRoomEffects");
            throw;
        }
    }
}