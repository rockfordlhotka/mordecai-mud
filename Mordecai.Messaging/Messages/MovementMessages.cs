namespace Mordecai.Messaging.Messages;

/// <summary>
/// Published when a character moves between rooms
/// </summary>
public sealed record PlayerMoved(
    Guid CharacterId,
    string CharacterName,
    int FromRoomId,
    int ToRoomId,
    string Direction
) : GameMessage
{
    public override int? RoomId => ToRoomId; // Target room receives the arrival message
}

/// <summary>
/// Published when a character leaves a room (for those remaining in the room)
/// </summary>
public sealed record PlayerLeft(
    Guid CharacterId,
    string CharacterName,
    int SourceRoomId,
    string Direction
) : GameMessage
{
    public override int? RoomId => SourceRoomId;
}

/// <summary>
/// Published when a character joins the game/logs in
/// </summary>
public sealed record PlayerJoined(
    Guid CharacterId,
    string CharacterName,
    int StartingRoomId
) : GameMessage
{
    public override int? RoomId => StartingRoomId;
}

/// <summary>
/// Published when a character leaves the game/logs out
/// </summary>
public sealed record PlayerDisconnected(
    Guid CharacterId,
    string CharacterName,
    int LastRoomId
) : GameMessage
{
    public override int? RoomId => LastRoomId;
}