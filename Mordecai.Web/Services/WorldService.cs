using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

public interface IWorldService
{
    Task<Room?> GetStartingRoomAsync();
    Task<Room?> GetRoomByIdAsync(int roomId);
    Task<Room?> GetRoomByCoordinatesAsync(int x, int y, int z, string? zoneName = null);
    Task<RoomExit?> GetExitFromRoomAsync(int fromRoomId, string direction);
    Task<Room?> GetRoomByExitAsync(int fromRoomId, string direction);
    Task<string> GetRoomDescriptionAsync(int roomId, bool isNight = false);
    Task<IReadOnlyList<RoomExit>> GetExitsFromRoomAsync(int roomId);
    Task<IReadOnlyList<RoomExit>> GetHiddenExitsFromRoomAsync(int roomId);
    Task<bool> CanMoveFromRoomAsync(int fromRoomId, string direction);
    Task<IReadOnlyList<int>> GetOccupiedRoomsAsync(CancellationToken cancellationToken = default);
}

public class WorldService : IWorldService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorldService> _logger;
    private readonly IGameConfigurationService _configService;

    public WorldService(
        ApplicationDbContext context, 
        ILogger<WorldService> logger,
        IGameConfigurationService configService)
    {
        _context = context;
        _logger = logger;
        _configService = configService;
    }

    public async Task<Room?> GetStartingRoomAsync()
    {
        try
        {
            // First, try to get the configured starting room
            var configuredRoomId = await _configService.GetStartingRoomIdAsync();
            if (configuredRoomId.HasValue)
            {
                var configuredRoom = await _context.Rooms
                    .Include(r => r.Zone)
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.Id == configuredRoomId.Value && r.IsActive && r.Zone.IsActive);

                if (configuredRoom != null)
                {
                    _logger.LogInformation("Starting room found from configuration: {RoomName} (ID: {RoomId}) in zone {ZoneName}",
                        configuredRoom.Name, configuredRoom.Id, configuredRoom.Zone.Name);
                    return configuredRoom;
                }

                _logger.LogWarning("Configured starting room ID {RoomId} not found or inactive, falling back to discovery",
                    configuredRoomId.Value);
            }

            // Fallback 1: Look for a room at coordinates 0,0,0 in a zone named "Tutorial" or similar
            var startingRoom = await _context.Rooms
                .Include(r => r.Zone)
                .Include(r => r.RoomType)
                .Where(r => r.IsActive && r.Zone.IsActive)
                .Where(r => r.X == 0 && r.Y == 0 && r.Z == 0)
                .Where(r => r.Zone.Name.ToLower().Contains("tutorial") || 
                           r.Zone.Name.ToLower().Contains("starting") ||
                           r.Zone.Name.ToLower().Contains("newbie") ||
                           r.Zone.Name.ToLower().Contains("begin"))
                .FirstOrDefaultAsync();

            if (startingRoom == null)
            {
                // Fallback 2: find any room at 0,0,0
                startingRoom = await _context.Rooms
                    .Include(r => r.Zone)
                    .Include(r => r.RoomType)
                    .Where(r => r.IsActive && r.Zone.IsActive)
                    .Where(r => r.X == 0 && r.Y == 0 && r.Z == 0)
                    .FirstOrDefaultAsync();
            }

            if (startingRoom == null)
            {
                // Ultimate fallback: find any active room
                startingRoom = await _context.Rooms
                    .Include(r => r.Zone)
                    .Include(r => r.RoomType)
                    .Where(r => r.IsActive && r.Zone.IsActive)
                    .FirstOrDefaultAsync();
            }

            if (startingRoom != null)
            {
                _logger.LogInformation("Starting room found via fallback: {RoomName} (ID: {RoomId}) in zone {ZoneName}",
                    startingRoom.Name, startingRoom.Id, startingRoom.Zone.Name);
            }
            else
            {
                _logger.LogWarning("No starting room found! The world may not have any rooms created yet.");
            }

            return startingRoom;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding starting room");
            return null;
        }
    }

    public async Task<Room?> GetRoomByIdAsync(int roomId)
    {
        try
        {
            return await _context.Rooms
                .Include(r => r.Zone)
                .Include(r => r.RoomType)
                .Include(r => r.ExitsFromHere.Where(e => e.IsActive))
                    .ThenInclude(e => e.ToRoom)
                        .ThenInclude(r => r.Zone)
                .Where(r => r.Id == roomId && r.IsActive && r.Zone.IsActive)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room {RoomId}", roomId);
            return null;
        }
    }

    public async Task<Room?> GetRoomByCoordinatesAsync(int x, int y, int z, string? zoneName = null)
    {
        try
        {
            var query = _context.Rooms
                .Include(r => r.Zone)
                .Include(r => r.RoomType)
                .Where(r => r.IsActive && r.Zone.IsActive)
                .Where(r => r.X == x && r.Y == y && r.Z == z);

            if (!string.IsNullOrEmpty(zoneName))
            {
                query = query.Where(r => r.Zone.Name.ToLower() == zoneName.ToLower());
            }

            return await query.FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room at coordinates ({X},{Y},{Z}) in zone {ZoneName}", 
                x, y, z, zoneName ?? "any");
            return null;
        }
    }

    public async Task<RoomExit?> GetExitFromRoomAsync(int fromRoomId, string direction)
    {
        try
        {
            return await _context.RoomExits
                .Include(e => e.ToRoom)
                    .ThenInclude(r => r.Zone)
                .Include(e => e.ToRoom)
                    .ThenInclude(r => r.RoomType)
                .Where(e => e.FromRoomId == fromRoomId && 
                           e.Direction.ToLower() == direction.ToLower() && 
                           e.IsActive &&
                           e.ToRoom.IsActive && 
                           e.ToRoom.Zone.IsActive)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exit {Direction} from room {RoomId}", direction, fromRoomId);
            return null;
        }
    }

    public async Task<Room?> GetRoomByExitAsync(int fromRoomId, string direction)
    {
        try
        {
            var exit = await GetExitFromRoomAsync(fromRoomId, direction);
            return exit?.ToRoom;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting destination room via {Direction} from room {RoomId}", direction, fromRoomId);
            return null;
        }
    }

    public async Task<string> GetRoomDescriptionAsync(int roomId, bool isNight = false)
    {
        try
        {
            var room = await GetRoomByIdAsync(roomId);
            if (room == null)
            {
                return "You are in an unknown location.";
            }

            var description = room.GetDescription(isNight);

            // Add exits information with descriptions (only non-hidden exits)
            var exits = await GetExitsFromRoomAsync(roomId);
            var visibleExits = exits.Where(e => !e.IsHidden).ToList();
            
            if (visibleExits.Any())
            {
                var exitDescriptions = new List<string>();
                
                foreach (var exit in visibleExits.OrderBy(e => e.Direction))
                {
                    var exitDesc = exit.GetExitDescription(isNight);
                    if (!string.IsNullOrEmpty(exitDesc))
                    {
                        // Include both direction and description
                        exitDescriptions.Add($"{exit.Direction} - {exitDesc}");
                    }
                    else
                    {
                        // Just the direction if no description
                        exitDescriptions.Add(exit.Direction);
                    }
                }
                
                description += $"\n\nObvious exits: {string.Join(", ", exitDescriptions)}";
            }
            else
            {
                description += "\n\nThere are no obvious exits.";
            }

            return description;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room description for room {RoomId}", roomId);
            return "You are in an unknown location.";
        }
    }

    public async Task<IReadOnlyList<RoomExit>> GetExitsFromRoomAsync(int roomId)
    {
        try
        {
            return await _context.RoomExits
                .Include(e => e.ToRoom)
                    .ThenInclude(r => r.Zone)
                .Where(e => e.FromRoomId == roomId && 
                           e.IsActive && 
                           e.ToRoom.IsActive && 
                           e.ToRoom.Zone.IsActive)
                .OrderBy(e => e.Direction)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exits from room {RoomId}", roomId);
            return Array.Empty<RoomExit>();
        }
    }

    public async Task<IReadOnlyList<RoomExit>> GetHiddenExitsFromRoomAsync(int roomId)
    {
        try
        {
            return await _context.RoomExits
                .Include(e => e.ToRoom)
                    .ThenInclude(r => r.Zone)
                .Where(e => e.FromRoomId == roomId && 
                           e.IsHidden && 
                           e.IsActive && 
                           e.ToRoom.IsActive && 
                           e.ToRoom.Zone.IsActive)
                .OrderBy(e => e.Direction)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hidden exits from room {RoomId}", roomId);
            return Array.Empty<RoomExit>();
        }
    }

    public async Task<bool> CanMoveFromRoomAsync(int fromRoomId, string direction)
    {
        try
        {
            var exit = await GetExitFromRoomAsync(fromRoomId, direction);
            return exit != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if can move {Direction} from room {RoomId}", direction, fromRoomId);
            return false;
        }
    }

    public async Task<IReadOnlyList<int>> GetOccupiedRoomsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Characters
                .Where(c => c.CurrentRoomId.HasValue)
                .Select(c => c.CurrentRoomId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting occupied rooms");
            return Array.Empty<int>();
        }
    }
}