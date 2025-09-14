namespace Mordecai.Messaging.Messages;

/// <summary>
/// Published when a character speaks in a room
/// </summary>
public sealed record ChatMessage(
    Guid CharacterId,
    string CharacterName,
    int SourceRoomId,
    string Message,
    ChatType ChatType = ChatType.Say
) : GameMessage
{
    public override int? RoomId => SourceRoomId;
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