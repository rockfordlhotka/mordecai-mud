using Mordecai.Messaging.Messages;
using Mordecai.Messaging.Services;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for handling game actions and publishing appropriate messages
/// This demonstrates how game logic can integrate with the messaging system
/// </summary>
public class GameActionService
{
    private readonly IGameMessagePublisher _messagePublisher;
    private readonly ILogger<GameActionService> _logger;

    public GameActionService(
        IGameMessagePublisher messagePublisher,
        ILogger<GameActionService> logger)
    {
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    /// <summary>
    /// Example: Handles a character moving between rooms
    /// </summary>
    public async Task HandleCharacterMovementAsync(
        Guid characterId, 
        string characterName, 
        int fromRoomId, 
        int toRoomId, 
        string direction)
    {
        try
        {
            // Publish message for characters remaining in the old room
            var leftMessage = new PlayerLeft(characterId, characterName, fromRoomId, direction);
            await _messagePublisher.PublishAsync(leftMessage);

            // Publish message for characters in the new room
            var arrivedMessage = new PlayerMoved(characterId, characterName, fromRoomId, toRoomId, direction);
            await _messagePublisher.PublishAsync(arrivedMessage);

            _logger.LogDebug("Published movement messages for character {CharacterId} moving from room {FromRoom} to {ToRoom}", 
                characterId, fromRoomId, toRoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish movement messages for character {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// Example: Handles sending a chat message
    /// </summary>
    public async Task HandleChatMessageAsync(
        Guid characterId,
        string characterName,
        int roomId,
        string message,
        ChatType chatType = ChatType.Say)
    {
        try
        {
            var chatMessage = new ChatMessage(characterId, characterName, roomId, message, chatType);
            await _messagePublisher.PublishAsync(chatMessage);

            _logger.LogDebug("Published chat message from character {CharacterId} in room {RoomId}", 
                characterId, roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chat message for character {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// Example: Handles a skill usage event
    /// </summary>
    public async Task HandleSkillUsageAsync(
        Guid characterId,
        string characterName,
        int roomId,
        int skillDefinitionId,
        string skillName,
        string usageDescription,
        bool success,
        int experienceGained,
        int newLevel,
        bool leveledUp)
    {
        try
        {
            // Publish skill usage message (visible to others in room)
            var skillUsedMessage = new SkillUsed(
                characterId, characterName, roomId, skillDefinitionId, 
                skillName, usageDescription, success);
            await _messagePublisher.PublishAsync(skillUsedMessage);

            // If experience was gained, publish that too (only to the character)
            if (experienceGained > 0)
            {
                var xpMessage = new SkillExperienceGained(
                    characterId, characterName, skillDefinitionId, 
                    skillName, experienceGained, newLevel, leveledUp);
                await _messagePublisher.PublishAsync(xpMessage);
            }

            _logger.LogDebug("Published skill usage messages for character {CharacterId} using skill {SkillName}", 
                characterId, skillName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish skill usage messages for character {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// Example: Handles system announcements
    /// </summary>
    public async Task HandleSystemAnnouncementAsync(
        string message, 
        MessagePriority priority = MessagePriority.Normal)
    {
        try
        {
            var systemMessage = new SystemMessage(message, priority);
            await _messagePublisher.PublishAsync(systemMessage);

            _logger.LogInformation("Published system announcement: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish system announcement");
            throw;
        }
    }

    /// <summary>
    /// Example: Handles admin actions
    /// </summary>
    public async Task HandleAdminActionAsync(
        string adminName,
        string action,
        string details,
        int? affectedRoomId = null,
        Guid? affectedCharacterId = null)
    {
        try
        {
            var adminMessage = new AdminAction(adminName, action, details, affectedRoomId, affectedCharacterId);
            await _messagePublisher.PublishAsync(adminMessage);

            _logger.LogInformation("Published admin action: {AdminName} - {Action}", adminName, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish admin action for {AdminName}", adminName);
            throw;
        }
    }
}