using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

public interface IRoomService
{
    Task<IEnumerable<Room>> GetAllRoomsAsync();
    Task<IEnumerable<Room>> GetRoomsByZoneAsync(int zoneId);
    Task<Room?> GetRoomByIdAsync(int id);
    Task<Room> CreateRoomAsync(Room room);
    Task<Room> UpdateRoomAsync(Room room);
    Task<bool> DeleteRoomAsync(int id);
    Task<bool> RoomExistsAsync(int id);
    Task<bool> RoomNameExistsInZoneAsync(string name, int zoneId, int? excludeId = null);
    Task<IEnumerable<Room>> GetActiveRoomsAsync();
    Task<IEnumerable<Room>> GetActiveRoomsByZoneAsync(int zoneId);
    Task<IEnumerable<RoomType>> GetAllRoomTypesAsync();
    Task<IEnumerable<RoomType>> GetActiveRoomTypesAsync();
    Task<RoomType?> GetRoomTypeByIdAsync(int id);
    Task<RoomType> CreateRoomTypeAsync(RoomType roomType);
    Task<RoomType> UpdateRoomTypeAsync(RoomType roomType);
    Task<int> GetRoomCountByZoneAsync(int zoneId);
    Task<bool> HasCharactersInRoomAsync(int roomId);
    Task<(bool HasExits, int ExitCount)> HasExitsAsync(int roomId);
}

public class RoomService : IRoomService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoomService> _logger;

    public RoomService(ApplicationDbContext context, ILogger<RoomService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Room>> GetAllRoomsAsync()
    {
        return await _context.Rooms
            .Include(r => r.Zone)
            .Include(r => r.RoomType)
            .OrderBy(r => r.Zone.Name)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Room>> GetRoomsByZoneAsync(int zoneId)
    {
        return await _context.Rooms
            .Include(r => r.Zone)
            .Include(r => r.RoomType)
            .Where(r => r.ZoneId == zoneId)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Room?> GetRoomByIdAsync(int id)
    {
        return await _context.Rooms
            .Include(r => r.Zone)
            .Include(r => r.RoomType)
            .Include(r => r.ExitsFromHere)
                .ThenInclude(e => e.ToRoom)
            .Include(r => r.ExitsToHere)
                .ThenInclude(e => e.FromRoom)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Room> CreateRoomAsync(Room room)
    {
        _logger.LogInformation("Creating new room: {RoomName} in zone {ZoneId} by {CreatedBy}", 
            room.Name, room.ZoneId, room.CreatedBy);
        
        room.CreatedAt = DateTimeOffset.UtcNow;
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Room created successfully with ID: {RoomId}", room.Id);
        return room;
    }

    public async Task<Room> UpdateRoomAsync(Room room)
    {
        _logger.LogInformation("Updating room: {RoomId} - {RoomName}", room.Id, room.Name);
        
        _context.Rooms.Update(room);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Room updated successfully: {RoomId}", room.Id);
        return room;
    }

    public async Task<bool> DeleteRoomAsync(int id)
    {
        _logger.LogInformation("Attempting to delete room: {RoomId}", id);
        
        var room = await _context.Rooms
            .Include(r => r.ExitsFromHere)
            .Include(r => r.ExitsToHere)
            .FirstOrDefaultAsync(r => r.Id == id);
            
        if (room == null)
        {
            _logger.LogWarning("Room not found for deletion: {RoomId}", id);
            return false;
        }

        // Check if room has any characters (would need Character entity with CurrentRoomId)
        // For now, we'll skip this check until Character entity is updated
        
        // Remove all exits to and from this room
        var allExits = room.ExitsFromHere.Concat(room.ExitsToHere).ToList();
        if (allExits.Any())
        {
            _logger.LogInformation("Removing {ExitCount} exits connected to room {RoomId}", allExits.Count, id);
            _context.RoomExits.RemoveRange(allExits);
        }

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Room deleted successfully: {RoomId}", id);
        return true;
    }

    public async Task<bool> RoomExistsAsync(int id)
    {
        return await _context.Rooms.AnyAsync(r => r.Id == id);
    }

    public async Task<bool> RoomNameExistsInZoneAsync(string name, int zoneId, int? excludeId = null)
    {
        var query = _context.Rooms.Where(r => r.Name.ToLower() == name.ToLower() && r.ZoneId == zoneId);
        
        if (excludeId.HasValue)
        {
            query = query.Where(r => r.Id != excludeId.Value);
        }
        
        return await query.AnyAsync();
    }

    public async Task<IEnumerable<Room>> GetActiveRoomsAsync()
    {
        return await _context.Rooms
            .Include(r => r.Zone)
            .Include(r => r.RoomType)
            .Where(r => r.IsActive)
            .OrderBy(r => r.Zone.Name)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Room>> GetActiveRoomsByZoneAsync(int zoneId)
    {
        return await _context.Rooms
            .Include(r => r.Zone)
            .Include(r => r.RoomType)
            .Where(r => r.ZoneId == zoneId && r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<RoomType>> GetAllRoomTypesAsync()
    {
        return await _context.RoomTypes
            .OrderBy(rt => rt.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<RoomType>> GetActiveRoomTypesAsync()
    {
        return await _context.RoomTypes
            .Where(rt => rt.IsActive)
            .OrderBy(rt => rt.Name)
            .ToListAsync();
    }

    public async Task<RoomType?> GetRoomTypeByIdAsync(int id)
    {
        return await _context.RoomTypes
            .FirstOrDefaultAsync(rt => rt.Id == id);
    }

    public async Task<RoomType> CreateRoomTypeAsync(RoomType roomType)
    {
        _logger.LogInformation("Creating new room type: {RoomTypeName}", roomType.Name);
        
        _context.RoomTypes.Add(roomType);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Room type created successfully with ID: {RoomTypeId}", roomType.Id);
        return roomType;
    }

    public async Task<RoomType> UpdateRoomTypeAsync(RoomType roomType)
    {
        _logger.LogInformation("Updating room type: {RoomTypeId} - {RoomTypeName}", roomType.Id, roomType.Name);
        
        _context.RoomTypes.Update(roomType);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Room type updated successfully: {RoomTypeId}", roomType.Id);
        return roomType;
    }

    public async Task<int> GetRoomCountByZoneAsync(int zoneId)
    {
        return await _context.Rooms
            .CountAsync(r => r.ZoneId == zoneId && r.IsActive);
    }

    public async Task<bool> HasCharactersInRoomAsync(int roomId)
    {
        // TODO: Implement when Character entity has CurrentRoomId
        // return await _context.Characters.AnyAsync(c => c.CurrentRoomId == roomId && c.IsOnline);
        await Task.CompletedTask;
        return false;
    }

    public async Task<(bool HasExits, int ExitCount)> HasExitsAsync(int roomId)
    {
        var exitCount = await _context.RoomExits
            .CountAsync(e => (e.FromRoomId == roomId || e.ToRoomId == roomId) && e.IsActive);
        
        return (exitCount > 0, exitCount);
    }
}