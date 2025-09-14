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
    private readonly TargetResolutionService _targetResolution;
    private readonly ILogger<GameActionService> _logger;

    public GameActionService(
        IGameMessagePublisher messagePublisher,
        TargetResolutionService targetResolution,
        ILogger<GameActionService> logger)
    {
        _messagePublisher = messagePublisher;
        _targetResolution = targetResolution;
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
    /// Handles sending a chat message, with optional targeting
    /// </summary>
    public async Task<string> HandleChatMessageAsync(
        Guid characterId,
        string characterName,
        int roomId,
        string message,
        ChatType chatType = ChatType.Say,
        string? targetName = null)
    {
        try
        {
            CommunicationTarget? target = null;
            
            // If a target name is provided, try to resolve it
            if (!string.IsNullOrWhiteSpace(targetName))
            {
                if (!TargetResolutionService.IsValidTargetName(targetName))
                {
                    return "Invalid target name.";
                }

                target = await _targetResolution.FindTargetInRoomAsync(targetName, roomId, characterId);
                if (target == null)
                {
                    return $"There is no '{targetName}' here.";
                }
            }

            // Create and publish the chat message
            var chatMessage = new ChatMessage(
                characterId, 
                characterName, 
                roomId, 
                message, 
                chatType,
                target?.Id,
                target?.Name,
                target?.Type);

            await _messagePublisher.PublishAsync(chatMessage);

            // Return feedback message for the speaker
            var feedback = target != null 
                ? $"You {GetChatVerb(chatType)} to {target.Name}: {message}"
                : $"You {GetChatVerb(chatType)}: {message}";

            _logger.LogDebug("Published {ChatType} message from character {CharacterId} in room {RoomId}{TargetInfo}", 
                chatType, characterId, roomId, target != null ? $" targeting {target.Name}" : "");

            return feedback;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish chat message for character {CharacterId}", characterId);
            return "An error occurred while sending your message.";
        }
    }

    /// <summary>
    /// Gets all available targets in a room for auto-completion or help
    /// </summary>
    public async Task<IReadOnlyList<CommunicationTarget>> GetAvailableTargetsAsync(
        int roomId, 
        Guid? excludeCharacterId = null)
    {
        try
        {
            return await _targetResolution.GetAllTargetsInRoomAsync(roomId, excludeCharacterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available targets in room {RoomId}", roomId);
            return Array.Empty<CommunicationTarget>();
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

    private static string GetChatVerb(ChatType chatType) => chatType switch
    {
        ChatType.Say => "say",
        ChatType.Whisper => "whisper",
        ChatType.Yell => "yell",
        ChatType.Tell => "tell",
        ChatType.Emote => "emote",
        _ => "say"
    };
}