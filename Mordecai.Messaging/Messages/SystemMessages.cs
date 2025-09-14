namespace Mordecai.Messaging.Messages;

/// <summary>
/// Published for system announcements and notifications
/// </summary>
public sealed record SystemMessage(
    string Message,
    MessagePriority Priority = MessagePriority.Normal,
    MessageCategory Category = MessageCategory.System
) : GameMessage
{
    public override int? RoomId => null; // System messages are global by default
}

/// <summary>
/// Published when an admin performs actions that should be logged/announced
/// </summary>
public sealed record AdminAction(
    string AdminName,
    string Action,
    string Details,
    int? AffectedRoomId = null,
    Guid? AffectedCharacterId = null
) : GameMessage
{
    public override int? RoomId => AffectedRoomId;
}

/// <summary>
/// Published for error conditions that players should be aware of
/// </summary>
public sealed record ErrorMessage(
    Guid CharacterId,
    string ErrorDescription,
    string? Context = null
) : GameMessage
{
    public override IReadOnlyList<Guid>? TargetCharacterIds => [CharacterId]; // Only send to the affected character
}