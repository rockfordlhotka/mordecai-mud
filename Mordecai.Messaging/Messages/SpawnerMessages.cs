using Mordecai.Game.Entities;

namespace Mordecai.Messaging.Messages;

/// <summary>
/// Published when an NPC is spawned into the world
/// </summary>
public sealed record NpcSpawnedEvent(
    Guid NpcId,
    int NpcTemplateId,
    string NpcName,
    int SpawnedInRoomId,
    int? SpawnerInstanceId = null
) : GameMessage
{
    public override int? RoomId => SpawnedInRoomId;
}

/// <summary>
/// Published when an NPC is despawned/removed from the world
/// </summary>
public sealed record NpcDespawnedEvent(
    Guid NpcId,
    int NpcTemplateId,
    string NpcName,
    int? LastRoomId,
    DespawnReason Reason,
    int? SpawnerInstanceId = null
) : GameMessage
{
    public override int? RoomId => LastRoomId;
}

/// <summary>
/// Published when an NPC moves between rooms
/// </summary>
public sealed record NpcMovedEvent(
    Guid NpcId,
    int NpcTemplateId,
    string NpcName,
    int FromRoomId,
    int ToRoomId,
    string Direction
) : GameMessage
{
    public override int? RoomId => ToRoomId;
}

/// <summary>
/// Published when a spawner instance is enabled or disabled
/// </summary>
public sealed record SpawnerStateChangedEvent(
    int SpawnerInstanceId,
    int SpawnerTemplateId,
    string SpawnerName,
    bool IsEnabled,
    int? RoomId = null,
    int? ZoneId = null
) : GameMessage
{
    public override int? RoomId { get; init; } = RoomId;
    public override int? ZoneId { get; init; } = ZoneId;
}
