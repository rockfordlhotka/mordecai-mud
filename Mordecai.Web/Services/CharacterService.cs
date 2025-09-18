using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

public interface ICharacterService
{
    Task<Character?> GetCharacterByIdAsync(Guid characterId, string userId);
    Task<int?> GetCharacterCurrentRoomAsync(Guid characterId, string userId);
    Task<bool> SetCharacterRoomAsync(Guid characterId, string userId, int roomId);
    Task<bool> CharacterExistsAsync(Guid characterId, string userId);
    Task<bool> DeleteCharacterAsync(Guid characterId, string userId);
}

public class CharacterService : ICharacterService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorldService _worldService;
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(
        ApplicationDbContext context, 
        IWorldService worldService,
        ILogger<CharacterService> logger)
    {
        _context = context;
        _worldService = worldService;
        _logger = logger;
    }

    public async Task<Character?> GetCharacterByIdAsync(Guid characterId, string userId)
    {
        try
        {
            return await _context.Characters
                .Where(c => c.Id == characterId && c.UserId == userId)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character {CharacterId} for user {UserId}", characterId, userId);
            return null;
        }
    }

    public async Task<int?> GetCharacterCurrentRoomAsync(Guid characterId, string userId)
    {
        try
        {
            var character = await GetCharacterByIdAsync(characterId, userId);
            if (character == null)
            {
                return null;
            }

            // TODO: For now, characters don't have a CurrentRoomId field yet
            // We'll need to add this to the Character entity in a future migration
            // For now, always start at the starting room
            var startingRoom = await _worldService.GetStartingRoomAsync();
            return startingRoom?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current room for character {CharacterId}", characterId);
            return null;
        }
    }

    public async Task<bool> SetCharacterRoomAsync(Guid characterId, string userId, int roomId)
    {
        try
        {
            var character = await GetCharacterByIdAsync(characterId, userId);
            if (character == null)
            {
                return false;
            }

            // TODO: Update character's CurrentRoomId when the field is added
            // For now, this is a placeholder that always succeeds
            _logger.LogDebug("Character {CharacterId} room set to {RoomId} (placeholder implementation)", 
                characterId, roomId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting room {RoomId} for character {CharacterId}", roomId, characterId);
            return false;
        }
    }

    public async Task<bool> CharacterExistsAsync(Guid characterId, string userId)
    {
        try
        {
            return await _context.Characters
                .AnyAsync(c => c.Id == characterId && c.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if character {CharacterId} exists for user {UserId}", 
                characterId, userId);
            return false;
        }
    }

    public async Task<bool> DeleteCharacterAsync(Guid characterId, string userId)
    {
        try
        {
            var character = await _context.Characters
                .Where(c => c.Id == characterId && c.UserId == userId)
                .FirstOrDefaultAsync();

            if (character == null)
            {
                _logger.LogWarning("Attempted to delete non-existent character {CharacterId} for user {UserId}", 
                    characterId, userId);
                return false;
            }

            _logger.LogInformation("Deleting character {CharacterName} ({CharacterId}) for user {UserId}", 
                character.Name, characterId, userId);

            _context.Characters.Remove(character);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted character {CharacterName} ({CharacterId})", 
                character.Name, characterId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting character {CharacterId} for user {UserId}", 
                characterId, userId);
            return false;
        }
    }
}