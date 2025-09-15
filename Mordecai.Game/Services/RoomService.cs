using Microsoft.Extensions.Logging;
using Mordecai.Game.Entities;
using Mordecai.Game.Services;

namespace Mordecai.Game.Services;

/// <summary>
/// Service for room-related operations including description formatting with day/night support
/// </summary>
public interface IRoomService
{
    /// <summary>
    /// Gets a room by ID with all necessary related data
    /// </summary>
    Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the formatted room description including day/night variations
    /// </summary>
    Task<string> GetRoomDescriptionAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the formatted room description for a specific room with time of day context
    /// </summary>
    string GetRoomDescription(Room room, TimeOfDay timeOfDay);

    /// <summary>
    /// Gets all available exits from a room with descriptions
    /// </summary>
    Task<IReadOnlyList<RoomExitInfo>> GetRoomExitsAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets exit information for a specific direction from a room
    /// </summary>
    Task<RoomExitInfo?> GetExitInDirectionAsync(int roomId, string direction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets characters currently in the specified room
    /// </summary>
    Task<IReadOnlyList<string>> GetCharactersInRoomAsync(int roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a character can move to a target room
    /// </summary>
    Task<(bool CanMove, string? Reason)> CanMoveToRoomAsync(int fromRoomId, int toRoomId, Guid characterId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a room exit
/// </summary>
public sealed record RoomExitInfo(
    int ExitId,
    string Direction,
    int ToRoomId,
    string ToRoomName,
    string? ExitDescription,
    bool IsHidden,
    bool RequiresSkill,
    string? SkillRequiredName,
    decimal SkillLevelRequired
);

/// <summary>
/// Implementation of room service
/// </summary>
public class RoomService : IRoomService
{
    private readonly IGameTimeService _gameTimeService;
    private readonly ILogger<RoomService> _logger;

    public RoomService(
        IGameTimeService gameTimeService,
        ILogger<RoomService> logger)
    {
        _gameTimeService = gameTimeService;
        _logger = logger;
    }

    // Note: These methods would need a DbContext injected in a real implementation
    // For now, I'll create placeholder implementations that can be completed when the DbContext is available

    public async Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement when DbContext is available
        // return await _context.Rooms
        //     .Include(r => r.Zone)
        //     .Include(r => r.RoomType)
        //     .FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive, cancellationToken);
        
        _logger.LogWarning("GetRoomAsync not yet implemented - requires DbContext integration");
        return null;
    }

    public async Task<string> GetRoomDescriptionAsync(int roomId, CancellationToken cancellationToken = default)
    {
        var room = await GetRoomAsync(roomId, cancellationToken);
        if (room == null)
        {
            return "You are in an unknown location.";
        }

        return GetRoomDescription(room, _gameTimeService.CurrentTimeOfDay);
    }

    public string GetRoomDescription(Room room, TimeOfDay timeOfDay)
    {
        var isNight = !_gameTimeService.IsDaylight;
        var description = room.GetDescription(isNight);
        
        // Add time-based atmospheric details for outdoor areas
        if (room.UsesDayNightDescriptions)
        {
            description = AddAtmosphericDetails(description, timeOfDay, room.Zone?.WeatherType ?? "Clear");
        }

        return description;
    }

    public async Task<IReadOnlyList<RoomExitInfo>> GetRoomExitsAsync(int roomId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement when DbContext is available
        // var exits = await _context.RoomExits
        //     .Include(e => e.ToRoom)
        //     .Where(e => e.FromRoomId == roomId && e.IsActive)
        //     .ToListAsync(cancellationToken);
        
        // var isNight = !_gameTimeService.IsDaylight;
        // return exits.Select(exit => new RoomExitInfo(
        //     exit.Id,
        //     exit.Direction,
        //     exit.ToRoomId,
        //     exit.ToRoom.Name,
        //     exit.GetExitDescription(isNight),
        //     exit.IsHidden,
        //     exit.SkillRequired.HasValue,
        //     exit.SkillRequired.HasValue ? "Unknown Skill" : null, // TODO: Look up skill name
        //     exit.SkillLevelRequired
        // )).ToList();

        _logger.LogWarning("GetRoomExitsAsync not yet implemented - requires DbContext integration");
        return Array.Empty<RoomExitInfo>();
    }

    public async Task<RoomExitInfo?> GetExitInDirectionAsync(int roomId, string direction, CancellationToken cancellationToken = default)
    {
        var exits = await GetRoomExitsAsync(roomId, cancellationToken);
        return exits.FirstOrDefault(e => e.Direction.Equals(direction, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<string>> GetCharactersInRoomAsync(int roomId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement when Character entity has CurrentRoomId and DbContext is available
        // return await _context.Characters
        //     .Where(c => c.CurrentRoomId == roomId && c.IsOnline)
        //     .Select(c => c.Name)
        //     .ToListAsync(cancellationToken);

        _logger.LogWarning("GetCharactersInRoomAsync not yet implemented - requires DbContext integration");
        return Array.Empty<string>();
    }

    public async Task<(bool CanMove, string? Reason)> CanMoveToRoomAsync(int fromRoomId, int toRoomId, Guid characterId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement skill checks, room capacity, etc.
        // For now, allow all movement
        await Task.CompletedTask;
        return (true, null);
    }

    /// <summary>
    /// Adds atmospheric details based on time of day and weather
    /// </summary>
    private static string AddAtmosphericDetails(string baseDescription, TimeOfDay timeOfDay, string weatherType)
    {
        var atmosphericDetail = GetAtmosphericDetail(timeOfDay, weatherType);
        
        if (!string.IsNullOrEmpty(atmosphericDetail))
        {
            return $"{baseDescription.TrimEnd('.')}. {atmosphericDetail}";
        }
        
        return baseDescription;
    }

    /// <summary>
    /// Gets atmospheric detail text based on time and weather
    /// </summary>
    private static string GetAtmosphericDetail(TimeOfDay timeOfDay, string weatherType)
    {
        var lightingDetails = timeOfDay switch
        {
            TimeOfDay.Night => "The area is shrouded in darkness",
            TimeOfDay.Dawn => "The first light of dawn illuminates the area",
            TimeOfDay.Morning => "Morning sunlight bathes the area",
            TimeOfDay.Midday => "Bright sunlight floods the area",
            TimeOfDay.Afternoon => "Afternoon light casts long shadows",
            TimeOfDay.Evening => "The warm evening light creates a golden glow",
            TimeOfDay.Dusk => "Twilight shadows begin to deepen",
            _ => ""
        };

        var weatherDetails = weatherType.ToLowerInvariant() switch
        {
            "rainy" => ", and rain falls steadily",
            "stormy" => ", while thunder rumbles in the distance",
            "foggy" => ", though thick fog limits visibility",
            "snowy" => ", as snowflakes drift down",
            "cloudy" => ", though clouds obscure much of the sky",
            _ => ""
        };

        return $"{lightingDetails}{weatherDetails}.";
    }
}