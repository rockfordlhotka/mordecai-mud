namespace Mordecai.Messaging.Messages;

/// <summary>
/// Published when a character speaks in a room
/// </summary>
public sealed record ChatMessage(
    Guid CharacterId,
    string CharacterName,
    int SourceRoomId,
    string Message,
    ChatType ChatType = ChatType.Say,
    Guid? TargetId = null,
    string? TargetName = null,
    TargetType? TargetType = null
) : GameMessage
{
    public override int? RoomId => SourceRoomId;
    
    /// <summary>
    /// True if this message is directed at a specific target
    /// </summary>
    public bool IsTargeted => TargetId.HasValue && !string.IsNullOrEmpty(TargetName);
};

/// <summary>
/// Published for out-of-character chat or global channels
/// </summary>
public sealed record GlobalChatMessage(
    Guid CharacterId,
    string CharacterName,
    string Message,
    string Channel = "ooc"
) : GameMessage
{
    public override int? RoomId => null; // Global messages don't have room scope
};

/// <summary>
/// Published when a character emotes
/// </summary>
public sealed record EmoteMessage(
    Guid CharacterId,
    string CharacterName,
    int SourceRoomId,
    string EmoteText,
    bool IsTargeted = false,
    Guid? TargetCharacterId = null,
    string? TargetCharacterName = null
) : GameMessage
{
    public override int? RoomId => SourceRoomId;
};

/// <summary>
/// Types of chat communication
/// </summary>
public enum ChatType
{
    Say,
    Whisper,
    Yell,
    Tell,
    Emote
}

/// <summary>
/// Types of entities that can be targeted in communication
/// </summary>
public enum TargetType
{
    Character,
    Npc,
    Mob
}