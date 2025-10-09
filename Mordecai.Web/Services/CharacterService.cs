using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

public interface ICharacterService
{
    Task<Character?> GetCharacterByIdAsync(Guid characterId, string userId);
    Task<int?> GetCharacterCurrentRoomAsync(Guid characterId, string userId);
    Task<bool> SetCharacterRoomAsync(Guid characterId, string userId, int roomId);
    Task<bool> CharacterExistsAsync(Guid characterId, string userId);
    Task<bool> DeleteCharacterAsync(Guid characterId, string userId);
    Task<bool> EnsureCharacterHasStartingSkillsAsync(Guid characterId, string userId);
}

public class CharacterService : ICharacterService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IWorldService _worldService;
    private readonly SkillService _skillService;
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IWorldService worldService,
        SkillService skillService,
        ILogger<CharacterService> logger)
    {
        _contextFactory = contextFactory;
        _worldService = worldService;
        _skillService = skillService;
        _logger = logger;
    }

    public async Task<Character?> GetCharacterByIdAsync(Guid characterId, string userId)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();
            return await context.Characters
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
            await using var context = _contextFactory.CreateDbContext();
            return await context.Characters
                .AnyAsync(c => c.Id == characterId && c.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if character {CharacterId} exists for user {UserId}", 
                characterId, userId);
            return false;
        }
    }

    public async Task<bool> EnsureCharacterHasStartingSkillsAsync(Guid characterId, string userId)
    {
        try
        {
            // Verify the character belongs to the user
            var character = await GetCharacterByIdAsync(characterId, userId);
            if (character == null)
            {
                _logger.LogWarning("Attempted to initialize skills for non-existent character {CharacterId} for user {UserId}", 
                    characterId, userId);
                return false;
            }

            // Check if character already has skills
            await using var context = _contextFactory.CreateDbContext();
            var existingSkillsCount = await context.CharacterSkills
                .Where(cs => cs.CharacterId == characterId)
                .CountAsync();

            if (existingSkillsCount > 0)
            {
                _logger.LogDebug("Character {CharacterId} already has {SkillCount} skills, skipping initialization", 
                    characterId, existingSkillsCount);
                return true; // Already has skills, nothing to do
            }

            // Character has no skills, initialize them
            _logger.LogInformation("Initializing starting skills for existing character {CharacterId} ({CharacterName})", 
                characterId, character.Name);
            
            await _skillService.InitializeCharacterSkillsAsync(characterId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring character {CharacterId} has starting skills", characterId);
            return false;
        }
    }

    public async Task<bool> DeleteCharacterAsync(Guid characterId, string userId)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();
            var character = await context.Characters
                .Where(c => c.Id == characterId && c.UserId == userId)
                .FirstOrDefaultAsync();

            if (character == null)
            {
                _logger.LogWarning("Attempted to delete non-existent character {CharacterId} for user {UserId}", 
                    characterId, userId);
                return false;
            }

            // Start a transaction to ensure all related data is deleted consistently
            await using var ctx = _contextFactory.CreateDbContext();
            using var transaction = await ctx.Database.BeginTransactionAsync();
            
            try
            {
                // Delete related character skills first (foreign key constraint)
                var characterSkills = await ctx.CharacterSkills
                    .Where(cs => cs.CharacterId == characterId)
                    .ToListAsync();
                
                if (characterSkills.Any())
                {
                    ctx.CharacterSkills.RemoveRange(characterSkills);
                    _logger.LogInformation("Deleting {Count} character skills for character {CharacterId}", 
                        characterSkills.Count, characterId);
                }

                // TODO: Add other related data deletion as the game expands:
                // - Character inventory items
                // - Character active effects
                // - Character quest progress
                // - Character social relationships
                // - Character achievements
                // - etc.

                // Delete the character itself
                ctx.Characters.Remove(character);
                
                // Save all changes
                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation("Successfully deleted character {CharacterName} ({CharacterId}) for user {UserId}", 
                    character.Name, characterId, userId);
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during character deletion transaction for {CharacterId}", characterId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting character {CharacterId} for user {UserId}", characterId, userId);
            return false;
        }
    }
}