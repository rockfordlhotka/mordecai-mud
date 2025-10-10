using Mordecai.Messaging.Messages;
using Mordecai.Messaging.Services;
using System.Collections.Concurrent;
using System.Linq;

namespace Mordecai.Web.Services;

/// <summary>
/// Captures metadata for a character currently connected to the game client.
/// </summary>
public sealed record ActiveCharacterInfo(Guid CharacterId, string CharacterName, string? UserDisplayName, string? UserId);

/// <summary>
/// Manages message subscriptions for connected characters and provides events for Blazor components to subscribe to
/// </summary>
public sealed class CharacterMessageBroadcastService : IDisposable
{
    private readonly IGameMessageSubscriberFactory _subscriberFactory;
    private readonly ILogger<CharacterMessageBroadcastService> _logger;
    
    // Track active subscriptions by character ID
    private readonly ConcurrentDictionary<Guid, IGameMessageSubscriber> _activeSubscriptions = new();
    
    // Track which characters have active UI components listening
    private readonly ConcurrentDictionary<Guid, int> _activeListeners = new();

    // Track metadata about active characters for presence queries
    private readonly ConcurrentDictionary<Guid, ActiveCharacterInfo> _activeCharacters = new();

    public CharacterMessageBroadcastService(
        IGameMessageSubscriberFactory subscriberFactory,
        ILogger<CharacterMessageBroadcastService> logger)
    {
        _subscriberFactory = subscriberFactory;
        _logger = logger;
    }

    /// <summary>
    /// Event fired when a message is received for a character
    /// </summary>
    public event Action<Guid, string>? MessageReceived;

    /// <summary>
    /// Registers interest from a Blazor component for a character's messages
    /// </summary>
    public async Task RegisterCharacterListenerAsync(
        Guid characterId,
        int? currentRoomId = null,
        string? characterName = null,
        string? userDisplayName = null,
        string? userId = null)
    {
        try
        {
            // Increment listener count
            _activeListeners.AddOrUpdate(characterId, 1, (_, count) => count + 1);

            // Create subscription if this is the first listener
            if (!_activeSubscriptions.ContainsKey(characterId))
            {
                var subscriber = _subscriberFactory.CreateSubscriber(characterId, currentRoomId);
                subscriber.MessageReceived += async (message) => await OnMessageReceivedAsync(characterId, message);
                
                await subscriber.StartAsync();
                _activeSubscriptions[characterId] = subscriber;
                
                _logger.LogInformation("Started message subscription for character {CharacterId}", characterId);
            }
            else
            {
                // Update room if needed
                if (currentRoomId.HasValue && _activeSubscriptions.TryGetValue(characterId, out var existingSubscriber))
                {
                    existingSubscriber.CurrentRoomId = currentRoomId;
                }
            }

            // Update active character metadata
            UpdateActiveCharacterInfo(characterId, characterName, userDisplayName, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register character listener for {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// Unregisters interest from a Blazor component for a character's messages
    /// </summary>
    public async Task UnregisterCharacterListenerAsync(Guid characterId)
    {
        try
        {
            if (!_activeListeners.TryGetValue(characterId, out var currentCount))
                return;

            var newCount = currentCount - 1;
            
            if (newCount <= 0)
            {
                // No more listeners, remove subscription
                _activeListeners.TryRemove(characterId, out _);
                
                if (_activeSubscriptions.TryRemove(characterId, out var subscriber))
                {
                    await subscriber.StopAsync();
                    subscriber.Dispose();
                    
                    _logger.LogInformation("Stopped message subscription for character {CharacterId}", characterId);
                }

                _activeCharacters.TryRemove(characterId, out _);
            }
            else
            {
                // Still have listeners, just decrement count
                _activeListeners[characterId] = newCount;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister character listener for {CharacterId}", characterId);
        }
    }

    /// <summary>
    /// Gets a snapshot of currently active characters.
    /// </summary>
    public IReadOnlyCollection<ActiveCharacterInfo> GetActiveCharacters()
        => _activeCharacters.Values.ToArray();

    /// <summary>
    /// Updates the room ID for a character's subscription
    /// </summary>
    public void UpdateCharacterRoom(Guid characterId, int? newRoomId)
    {
        if (_activeSubscriptions.TryGetValue(characterId, out var subscriber))
        {
            subscriber.CurrentRoomId = newRoomId;
            _logger.LogDebug("Updated character {CharacterId} room to {RoomId}", characterId, newRoomId);
        }
    }

    /// <summary>
    /// Handles messages received from the message bus and fires events for Blazor components
    /// </summary>
    private Task OnMessageReceivedAsync(Guid characterId, GameMessage message)
    {
        try
        {
            // Format the message for display
            var displayMessage = FormatMessageForDisplay(message);
            if (string.IsNullOrEmpty(displayMessage))
                return Task.CompletedTask;

            // Fire the event on the UI thread
            MessageReceived?.Invoke(characterId, displayMessage);
            
            _logger.LogDebug("Broadcasted message for character {CharacterId}: {Message}", characterId, displayMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast message to character {CharacterId}", characterId);
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Formats game messages for display in the client
    /// </summary>
    private static string FormatMessageForDisplay(GameMessage message)
    {
        return message switch
        {
            PlayerMoved moved => $"{moved.CharacterName} arrives from the {GetOppositeDirection(moved.Direction)}.",
            PlayerLeft left => $"{left.CharacterName} leaves {left.Direction}.",
            PlayerJoined joined => $"{joined.CharacterName} enters the game.",
            PlayerDisconnected disconnected => $"{disconnected.CharacterName} disconnects from the game.",
            
            ChatMessage chat when chat.IsTargeted => FormatTargetedChatMessage(chat),
            ChatMessage chat => $"{chat.CharacterName} {GetChatVerb(chat.ChatType)}, \"{chat.Message}\"",
            GlobalChatMessage globalChat => $"[{globalChat.Channel.ToUpper()}] {globalChat.CharacterName}: {globalChat.Message}",
            EmoteMessage emote when emote.IsTargeted && emote.TargetCharacterName != null => 
                $"{emote.CharacterName} {emote.EmoteText} {emote.TargetCharacterName}.",
            EmoteMessage emote => $"{emote.CharacterName} {emote.EmoteText}.",
            
            CombatStarted combat => $"{combat.InitiatorName} attacks {combat.TargetName}!",
            CombatAction action when action.IsHit => 
                $"{action.AttackerName} {action.ActionDescription} {action.DefenderName} for {action.Damage} damage!",
            CombatAction action => $"{action.AttackerName} {action.ActionDescription} {action.DefenderName} but misses!",
            CombatEnded ended when ended.WinnerName != null => $"Combat ends. {ended.WinnerName} is victorious!",
            CombatEnded ended => $"Combat ends. {ended.EndReason}",
            
            SkillExperienceGained skillXp when skillXp.LeveledUp => 
                $"Your {skillXp.SkillName} skill increases! You are now level {skillXp.NewLevel}.",
            SkillExperienceGained skillXp => $"You gain {skillXp.ExperienceGained} experience in {skillXp.SkillName}.",
            SkillUsed skillUsed => $"{skillUsed.CharacterName} uses {skillUsed.SkillName}.",
            SkillLearned skillLearned => $"You have learned the {skillLearned.SkillName} skill!",
            
            SystemMessage system => $"[SYSTEM] {system.Message}",
            AdminAction admin => $"[ADMIN] {admin.AdminName}: {admin.Action}",
            ErrorMessage error => $"[ERROR] {error.ErrorDescription}",
            
            _ => string.Empty
        };
    }

    private static string FormatTargetedChatMessage(ChatMessage chat)
    {
        if (!chat.IsTargeted || string.IsNullOrEmpty(chat.TargetName))
            return $"{chat.CharacterName} {GetChatVerb(chat.ChatType)}, \"{chat.Message}\"";

        var verb = GetChatVerb(chat.ChatType);
        var preposition = chat.ChatType switch
        {
            ChatType.Say => "to",
            ChatType.Whisper => "to",
            ChatType.Yell => "at",
            ChatType.Tell => "to",
            _ => "to"
        };

        var targetDescription = chat.TargetType switch
        {
            Mordecai.Messaging.Messages.TargetType.Character => chat.TargetName,
            Mordecai.Messaging.Messages.TargetType.Npc => $"the {chat.TargetName}",
            Mordecai.Messaging.Messages.TargetType.Mob => $"the {chat.TargetName}",
            _ => chat.TargetName
        };

        return $"{chat.CharacterName} {verb}s {preposition} {targetDescription}, \"{chat.Message}\"";
    }

    private static string GetChatVerb(ChatType chatType) => chatType switch
    {
        ChatType.Say => "says",
        ChatType.Whisper => "whispers",
        ChatType.Yell => "yells",
        ChatType.Tell => "tells you",
        ChatType.Emote => "emotes",
        _ => "says"
    };

    private static string GetOppositeDirection(string direction) => direction.ToLowerInvariant() switch
    {
        "north" => "south",
        "south" => "north", 
        "east" => "west",
        "west" => "east",
        "northeast" => "southwest",
        "northwest" => "southeast",
        "southeast" => "northwest",
        "southwest" => "northeast",
        "up" => "below",
        "down" => "above",
        _ => "somewhere"
    };

    public void Dispose()
    {
        foreach (var subscriber in _activeSubscriptions.Values)
        {
            try
            {
                subscriber.StopAsync().GetAwaiter().GetResult();
                subscriber.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing message subscriber");
            }
        }
        
        _activeSubscriptions.Clear();
        _activeListeners.Clear();
        _activeCharacters.Clear();
    }

    private void UpdateActiveCharacterInfo(Guid characterId, string? characterName, string? userDisplayName, string? userId)
    {
        _activeCharacters.AddOrUpdate(
            characterId,
            _ => CreateActiveCharacterInfo(characterId, characterName, userDisplayName, userId),
            (_, existing) =>
            {
                var updatedCharacterName = string.IsNullOrWhiteSpace(characterName)
                    ? existing.CharacterName
                    : characterName.Trim();

                var updatedDisplayName = string.IsNullOrWhiteSpace(userDisplayName)
                    ? existing.UserDisplayName
                    : userDisplayName.Trim();

                var updatedUserId = string.IsNullOrWhiteSpace(userId)
                    ? existing.UserId
                    : userId.Trim();

                return existing with
                {
                    CharacterName = string.IsNullOrWhiteSpace(updatedCharacterName) ? existing.CharacterName : updatedCharacterName,
                    UserDisplayName = updatedDisplayName,
                    UserId = updatedUserId
                };
            });
    }

    private static ActiveCharacterInfo CreateActiveCharacterInfo(Guid characterId, string? characterName, string? userDisplayName, string? userId)
    {
        var name = string.IsNullOrWhiteSpace(characterName) ? "Unknown" : characterName.Trim();
        var displayName = string.IsNullOrWhiteSpace(userDisplayName) ? null : userDisplayName.Trim();
        var normalizedUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();

        return new ActiveCharacterInfo(characterId, name, displayName, normalizedUserId);
    }
}