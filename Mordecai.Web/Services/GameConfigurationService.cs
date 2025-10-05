using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

public interface IGameConfigurationService
{
    Task<string?> GetConfigurationAsync(string key);
    Task<int?> GetConfigurationAsIntAsync(string key);
    Task SetConfigurationAsync(string key, string value, string description, string updatedBy);
    Task<int?> GetStartingRoomIdAsync();
    Task SetStartingRoomIdAsync(int roomId, string updatedBy);
}

public class GameConfigurationService : IGameConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GameConfigurationService> _logger;

    public const string StartingRoomKey = "Game.StartingRoomId";

    public GameConfigurationService(ApplicationDbContext context, ILogger<GameConfigurationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetConfigurationAsync(string key)
    {
        try
        {
            var config = await _context.GameConfigurations
                .FirstOrDefaultAsync(gc => gc.Key == key);

            return config?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration for key {Key}", key);
            return null;
        }
    }

    public async Task<int?> GetConfigurationAsIntAsync(string key)
    {
        var value = await GetConfigurationAsync(key);
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (int.TryParse(value, out var intValue))
        {
            return intValue;
        }

        _logger.LogWarning("Configuration key {Key} value '{Value}' could not be parsed as int", key, value);
        return null;
    }

    public async Task SetConfigurationAsync(string key, string value, string description, string updatedBy)
    {
        try
        {
            var config = await _context.GameConfigurations
                .FirstOrDefaultAsync(gc => gc.Key == key);

            if (config == null)
            {
                config = new GameConfiguration
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    UpdatedBy = updatedBy
                };
                _context.GameConfigurations.Add(config);
            }
            else
            {
                config.Value = value;
                config.Description = description;
                config.UpdatedAt = DateTimeOffset.UtcNow;
                config.UpdatedBy = updatedBy;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Configuration {Key} set to {Value} by {UpdatedBy}", key, value, updatedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration {Key} to {Value}", key, value);
            throw;
        }
    }

    public async Task<int?> GetStartingRoomIdAsync()
    {
        return await GetConfigurationAsIntAsync(StartingRoomKey);
    }

    public async Task SetStartingRoomIdAsync(int roomId, string updatedBy)
    {
        await SetConfigurationAsync(
            StartingRoomKey,
            roomId.ToString(),
            "The room ID where new characters start their journey",
            updatedBy
        );
    }
}
