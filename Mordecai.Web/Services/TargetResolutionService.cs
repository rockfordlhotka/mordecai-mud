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

            // Try NPC match using real ActiveSpawn entities
            var npcResult = await FindNpcInRoomAsync(searchName, roomId);
            if (npcResult is NpcFound found)
            {
                _logger.LogDebug("Found NPC {TargetName} in room {RoomId}", found.Target.Name, roomId);
                return found.Target;
            }
            // Note: For now, multiple matches returns null (caller should use FindNpcInRoomAsync directly for disambiguation)

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
            // Get all characters in the room
            var charactersQuery = _context.Characters
                .AsNoTracking()
                .Where(c => c.CurrentRoomId == roomId);
            
            if (excludeCharacterId.HasValue)
            {
                charactersQuery = charactersQuery.Where(c => c.Id != excludeCharacterId.Value);
            }
            
            var charactersInRoom = await charactersQuery
                .Select(c => new CommunicationTarget(c.Id, c.Name, TargetType.Character, true))
                .ToListAsync();

            targets.AddRange(charactersInRoom);

            // Get all active NPCs in the room
            var npcsInRoom = await _context.ActiveSpawns
                .AsNoTracking()
                .Where(asp => asp.IsActive && asp.CurrentRoomId == roomId)
                .Include(asp => asp.NpcTemplate)
                .Select(asp => new CommunicationTarget(
                    asp.NpcId,
                    asp.NpcTemplate.Name,
                    TargetType.Npc,
                    true))
                .ToListAsync();

            targets.AddRange(npcsInRoom);

            return targets.OrderBy(t => t.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting targets in room {RoomId}", roomId);
            return Array.Empty<CommunicationTarget>();
        }
    }

    /// <summary>
    /// Finds NPC(s) matching the search term in the specified room.
    /// Supports prefix matching (case-insensitive) and numeric disambiguation suffix.
    /// </summary>
    /// <param name="searchInput">Target string (e.g., "goblin", "gob", "goblin 2")</param>
    /// <param name="roomId">Room ID to search within</param>
    /// <returns>Resolution result indicating found, not found, or multiple matches</returns>
    public async Task<NpcResolutionResult> FindNpcInRoomAsync(string searchInput, int roomId)
    {
        if (string.IsNullOrWhiteSpace(searchInput))
            return new NpcNotFound(string.Empty);

        // Parse input for numeric suffix: "goblin 2" -> searchTerm="goblin", index=2
        var (searchTerm, disambiguationIndex) = ParseSearchInput(searchInput.Trim());
        var normalizedSearchTerm = searchTerm.ToLowerInvariant();

        try
        {
            // Query ActiveSpawns with filters
            var matchingSpawns = await _context.ActiveSpawns
                .AsNoTracking()
                .Where(asp => asp.IsActive && asp.CurrentRoomId == roomId)
                .Include(asp => asp.NpcTemplate)
                .Where(asp => asp.NpcTemplate.Name.ToLower().StartsWith(normalizedSearchTerm))
                .OrderBy(asp => asp.NpcTemplate.Name)
                .ThenBy(asp => asp.Id)
                .ToListAsync();

            if (matchingSpawns.Count == 0)
            {
                _logger.LogDebug("No NPC matching '{SearchTerm}' found in room {RoomId}", searchTerm, roomId);
                return new NpcNotFound(searchTerm);
            }

            // Handle disambiguation
            if (disambiguationIndex.HasValue)
            {
                // User specified index (1-based)
                var index = disambiguationIndex.Value - 1; // Convert to 0-based
                if (index >= 0 && index < matchingSpawns.Count)
                {
                    var selected = matchingSpawns[index];
                    var target = new CommunicationTarget(
                        selected.NpcId,
                        selected.NpcTemplate.Name,
                        TargetType.Npc,
                        true);
                    _logger.LogDebug("Disambiguated to NPC '{NpcName}' (index {Index}) in room {RoomId}",
                        target.Name, disambiguationIndex.Value, roomId);
                    return new NpcFound(target);
                }
                else
                {
                    _logger.LogDebug("Invalid disambiguation index {Index} for '{SearchTerm}' (only {Count} matches) in room {RoomId}",
                        disambiguationIndex.Value, searchTerm, matchingSpawns.Count, roomId);
                    return new NpcNotFound($"{searchTerm} {disambiguationIndex.Value}");
                }
            }

            // Single match - return directly
            if (matchingSpawns.Count == 1)
            {
                var spawn = matchingSpawns[0];
                var target = new CommunicationTarget(
                    spawn.NpcId,
                    spawn.NpcTemplate.Name,
                    TargetType.Npc,
                    true);
                _logger.LogDebug("Found single NPC '{NpcName}' in room {RoomId}", target.Name, roomId);
                return new NpcFound(target);
            }

            // Multiple matches - return disambiguation list
            var matches = matchingSpawns
                .Select(asp => new CommunicationTarget(
                    asp.NpcId,
                    asp.NpcTemplate.Name,
                    TargetType.Npc,
                    true))
                .ToList();

            _logger.LogDebug("Multiple NPCs ({Count}) matching '{SearchTerm}' found in room {RoomId}",
                matches.Count, searchTerm, roomId);
            return new MultipleNpcsFound(searchTerm, matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding NPC '{SearchInput}' in room {RoomId}", searchInput, roomId);
            return new NpcNotFound(searchTerm);
        }
    }

    /// <summary>
    /// Parses search input for disambiguation suffix.
    /// "goblin" -> ("goblin", null)
    /// "goblin 2" -> ("goblin", 2)
    /// "goblin warrior" -> ("goblin warrior", null)
    /// "goblin warrior 2" -> ("goblin warrior", 2)
    /// </summary>
    private static (string SearchTerm, int? Index) ParseSearchInput(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return (string.Empty, null);

        if (parts.Length == 1)
            return (parts[0], null);

        // Check if last token is numeric
        if (int.TryParse(parts[^1], out var index) && index > 0)
        {
            // Last token is a positive integer - it's a disambiguation index
            var searchTerm = string.Join(' ', parts[..^1]);
            return (searchTerm, index);
        }

        // Last token is not numeric - entire input is the search term
        return (input, null);
    }

    private async Task<CommunicationTarget?> FindCharacterByNameAsync(
        string searchName, 
        int roomId, 
        Guid? excludeCharacterId, 
        bool exactMatch)
    {
        var query = _context.Characters
            .AsNoTracking()
            .Where(c => c.CurrentRoomId == roomId);

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