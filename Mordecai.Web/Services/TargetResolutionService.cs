using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Messaging.Messages;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Represents a potential target for communication in a room
/// </summary>
public sealed record CommunicationTarget(
    Guid Id,
    string Name,
    TargetType Type,
    bool IsOnline = true
);

/// <summary>
/// Result of NPC target resolution
/// </summary>
public abstract record NpcResolutionResult;

/// <summary>
/// Single NPC found matching the search term
/// </summary>
public sealed record NpcFound(CommunicationTarget Target) : NpcResolutionResult;

/// <summary>
/// No NPC found matching the search term
/// </summary>
public sealed record NpcNotFound(string SearchTerm) : NpcResolutionResult;

/// <summary>
/// Multiple NPCs match - disambiguation required
/// </summary>
public sealed record MultipleNpcsFound(
    string SearchTerm,
    IReadOnlyList<CommunicationTarget> Matches
) : NpcResolutionResult;

/// <summary>
/// Service for resolving communication targets (characters, NPCs, mobs) by name within rooms
/// </summary>
public class TargetResolutionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TargetResolutionService> _logger;

    public TargetResolutionService(
        ApplicationDbContext context,
        ILogger<TargetResolutionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to find a target by name in the specified room
    /// </summary>
    /// <param name="targetName">Name to search for (case-insensitive, partial match supported)</param>
    /// <param name="roomId">Room ID to search within</param>
    /// <param name="excludeCharacterId">Character ID to exclude from search (typically the speaker)</param>
    /// <returns>The target if found, null otherwise</returns>
    public async Task<CommunicationTarget?> FindTargetInRoomAsync(
        string targetName, 
        int roomId, 
        Guid? excludeCharacterId = null)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        var searchName = targetName.Trim().ToLowerInvariant();

        try
        {
            // First, try to find a character by exact name match
            var exactCharacterMatch = await FindCharacterByNameAsync(searchName, roomId, excludeCharacterId, exactMatch: true);
            if (exactCharacterMatch != null)
                return exactCharacterMatch;

            // Then try partial character name match
            var partialCharacterMatch = await FindCharacterByNameAsync(searchName, roomId, excludeCharacterId, exactMatch: false);
            if (partialCharacterMatch != null)
                return partialCharacterMatch;

            // TODO: Add NPC and Mob searches when those entities are implemented
            // For now, we'll return placeholder data to demonstrate the system

            // Simulate some NPCs and mobs that might be in rooms for testing
            var simulatedTargets = GetSimulatedTargetsInRoom(roomId);
            var simulatedMatch = simulatedTargets.FirstOrDefault(t => 
                t.Name.ToLowerInvariant().Contains(searchName) ||
                t.Name.ToLowerInvariant().StartsWith(searchName));

            if (simulatedMatch != null)
            {
                _logger.LogDebug("Found simulated target {TargetName} of type {TargetType} in room {RoomId}", 
                    simulatedMatch.Name, simulatedMatch.Type, roomId);
                return simulatedMatch;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding target {TargetName} in room {RoomId}", targetName, roomId);
            return null;
        }
    }

    /// <summary>
    /// Gets all potential targets in a room (for auto-completion or listing)
    /// </summary>
    public async Task<IReadOnlyList<CommunicationTarget>> GetAllTargetsInRoomAsync(
        int roomId, 
        Guid? excludeCharacterId = null)
    {
        var targets = new List<CommunicationTarget>();

        try
        {
            // Get all characters in the room (for now, we'll simulate this since room-character relationships aren't implemented yet)
            // TODO: Replace with actual room-character lookup when room system is implemented
            var charactersInRoom = await _context.Characters
                .AsNoTracking()
                .Where(c => excludeCharacterId == null || c.Id != excludeCharacterId)
                .Select(c => new CommunicationTarget(c.Id, c.Name, TargetType.Character, true))
                .ToListAsync();

            targets.AddRange(charactersInRoom);

            // Add simulated NPCs and mobs
            targets.AddRange(GetSimulatedTargetsInRoom(roomId));

            return targets.OrderBy(t => t.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting targets in room {RoomId}", roomId);
            return Array.Empty<CommunicationTarget>();
        }
    }

    private async Task<CommunicationTarget?> FindCharacterByNameAsync(
        string searchName, 
        int roomId, 
        Guid? excludeCharacterId, 
        bool exactMatch)
    {
        // TODO: When room-character relationships are implemented, add proper room filtering
        // For now, search all characters since we don't have room positions tracked
        
        var query = _context.Characters.AsNoTracking();

        if (excludeCharacterId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCharacterId.Value);
        }

        Character? character;
        if (exactMatch)
        {
            character = await query.FirstOrDefaultAsync(c => c.Name.ToLower() == searchName);
        }
        else
        {
            character = await query.FirstOrDefaultAsync(c => c.Name.ToLower().StartsWith(searchName));
        }

        if (character != null)
        {
            _logger.LogDebug("Found character {CharacterName} (ID: {CharacterId}) in room {RoomId}", 
                character.Name, character.Id, roomId);
            
            return new CommunicationTarget(character.Id, character.Name, TargetType.Character, true);
        }

        return null;
    }

    /// <summary>
    /// Simulated targets for demonstration purposes
    /// TODO: Replace with actual NPC/Mob entities when implemented
    /// </summary>
    private static List<CommunicationTarget> GetSimulatedTargetsInRoom(int roomId)
    {
        return roomId switch
        {
            1 => new List<CommunicationTarget>
            {
                new(Guid.NewGuid(), "village guard", TargetType.Npc),
                new(Guid.NewGuid(), "merchant", TargetType.Npc),
                new(Guid.NewGuid(), "stray cat", TargetType.Mob),
            },
            2 => new List<CommunicationTarget>
            {
                new(Guid.NewGuid(), "innkeeper", TargetType.Npc),
                new(Guid.NewGuid(), "tavern wench", TargetType.Npc),
                new(Guid.NewGuid(), "drunk patron", TargetType.Npc),
            },
            3 => new List<CommunicationTarget>
            {
                new(Guid.NewGuid(), "forest sprite", TargetType.Mob),
                new(Guid.NewGuid(), "ancient oak", TargetType.Npc),
                new(Guid.NewGuid(), "woodland fox", TargetType.Mob),
            },
            _ => new List<CommunicationTarget>
            {
                new(Guid.NewGuid(), "mysterious figure", TargetType.Npc),
            }
        };
    }

    /// <summary>
    /// Validates if a target name is reasonable (not too long, no invalid characters, etc.)
    /// </summary>
    public static bool IsValidTargetName(string? targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return false;

        if (targetName.Length > 50)
            return false;

        // Basic validation - only letters, spaces, hyphens, and apostrophes
        return targetName.All(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c == '-' || c == '\'');
    }
}