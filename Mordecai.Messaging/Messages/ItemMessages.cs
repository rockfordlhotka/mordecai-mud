using System;

namespace Mordecai.Messaging.Messages;

/// <summary>
/// Published when a character drops an item into a room so nearby players are notified.
/// </summary>
public sealed record ItemDropped(
    Guid CharacterId,
    string CharacterName,
    int LocationRoomId,
    string ItemName,
    int StackSize,
    string? CustomName = null
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}

/// <summary>
/// Published when a character picks up an item from the room.
/// </summary>
public sealed record ItemPickedUp(
    Guid CharacterId,
    string CharacterName,
    int LocationRoomId,
    string ItemName,
    int StackSize,
    string? CustomName = null
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}
