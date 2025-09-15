using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

public interface IZoneService
{
    Task<IEnumerable<Zone>> GetAllZonesAsync();
    Task<Zone?> GetZoneByIdAsync(int id);
    Task<Zone> CreateZoneAsync(Zone zone);
    Task<Zone> UpdateZoneAsync(Zone zone);
    Task<bool> DeleteZoneAsync(int id);
    Task<bool> ZoneExistsAsync(int id);
    Task<bool> ZoneNameExistsAsync(string name, int? excludeId = null);
    Task<IEnumerable<Zone>> GetActiveZonesAsync();
    Task<int> GetRoomCountForZoneAsync(int zoneId);
}

public class ZoneService : IZoneService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ZoneService> _logger;

    public ZoneService(ApplicationDbContext context, ILogger<ZoneService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Zone>> GetAllZonesAsync()
    {
        return await _context.Zones
            .OrderBy(z => z.Name)
            .ToListAsync();
    }

    public async Task<Zone?> GetZoneByIdAsync(int id)
    {
        return await _context.Zones
            .Include(z => z.Rooms)
            .FirstOrDefaultAsync(z => z.Id == id);
    }

    public async Task<Zone> CreateZoneAsync(Zone zone)
    {
        _logger.LogInformation("Creating new zone: {ZoneName} by {CreatedBy}", zone.Name, zone.CreatedBy);
        
        zone.CreatedAt = DateTimeOffset.UtcNow;
        _context.Zones.Add(zone);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Zone created successfully with ID: {ZoneId}", zone.Id);
        return zone;
    }

    public async Task<Zone> UpdateZoneAsync(Zone zone)
    {
        _logger.LogInformation("Updating zone: {ZoneId} - {ZoneName}", zone.Id, zone.Name);
        
        _context.Zones.Update(zone);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Zone updated successfully: {ZoneId}", zone.Id);
        return zone;
    }

    public async Task<bool> DeleteZoneAsync(int id)
    {
        _logger.LogInformation("Attempting to delete zone: {ZoneId}", id);
        
        var zone = await _context.Zones
            .Include(z => z.Rooms)
            .FirstOrDefaultAsync(z => z.Id == id);
            
        if (zone == null)
        {
            _logger.LogWarning("Zone not found for deletion: {ZoneId}", id);
            return false;
        }

        // Check if zone has rooms
        if (zone.Rooms.Any())
        {
            _logger.LogWarning("Cannot delete zone {ZoneId} - contains {RoomCount} rooms", id, zone.Rooms.Count);
            throw new InvalidOperationException($"Cannot delete zone '{zone.Name}' because it contains {zone.Rooms.Count} rooms. Delete all rooms first.");
        }

        _context.Zones.Remove(zone);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Zone deleted successfully: {ZoneId}", id);
        return true;
    }

    public async Task<bool> ZoneExistsAsync(int id)
    {
        return await _context.Zones.AnyAsync(z => z.Id == id);
    }

    public async Task<bool> ZoneNameExistsAsync(string name, int? excludeId = null)
    {
        var query = _context.Zones.Where(z => z.Name.ToLower() == name.ToLower());
        
        if (excludeId.HasValue)
        {
            query = query.Where(z => z.Id != excludeId.Value);
        }
        
        return await query.AnyAsync();
    }

    public async Task<IEnumerable<Zone>> GetActiveZonesAsync()
    {
        return await _context.Zones
            .Where(z => z.IsActive)
            .OrderBy(z => z.Name)
            .ToListAsync();
    }

    public async Task<int> GetRoomCountForZoneAsync(int zoneId)
    {
        return await _context.Rooms
            .CountAsync(r => r.ZoneId == zoneId && r.IsActive);
    }
}