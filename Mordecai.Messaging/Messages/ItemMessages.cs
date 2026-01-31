using System;
using Mordecai.Game.Entities;

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

/// <summary>
/// Published when an item is spawned into the game world.
/// </summary>
public sealed record ItemSpawned(
    Guid ItemId,
    int ItemTemplateId,
    string ItemName,
    int? LocationRoomId,
    Guid? OwnerCharacterId,
    int StackSize
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}

/// <summary>
/// Published when an item is destroyed/removed from the game world.
/// </summary>
public sealed record ItemDestroyed(
    Guid ItemId,
    string ItemName,
    int? LocationRoomId,
    Guid? OwnerCharacterId,
    string Reason
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}

/// <summary>
/// Published when a character equips an item.
/// </summary>
public sealed record ItemEquipped(
    Guid CharacterId,
    string CharacterName,
    int LocationRoomId,
    Guid ItemId,
    string ItemName,
    ArmorSlot Slot
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}

/// <summary>
/// Published when a character unequips an item.
/// </summary>
public sealed record ItemUnequipped(
    Guid CharacterId,
    string CharacterName,
    int LocationRoomId,
    Guid ItemId,
    string ItemName,
    ArmorSlot Slot
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}

/// <summary>
/// Published when an item is placed into a container.
/// </summary>
public sealed record ItemStoredInContainer(
    Guid CharacterId,
    string CharacterName,
    int LocationRoomId,
    Guid ItemId,
    string ItemName,
    Guid ContainerId,
    string ContainerName
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}

/// <summary>
/// Published when an item is removed from a container.
/// </summary>
public sealed record ItemRemovedFromContainer(
    Guid CharacterId,
    string CharacterName,
    int LocationRoomId,
    Guid ItemId,
    string ItemName,
    Guid ContainerId,
    string ContainerName
) : GameMessage
{
    public override int? RoomId => LocationRoomId;
}

/// <summary>
/// Published when an item's durability changes significantly (broken or repaired).
/// </summary>
public sealed record ItemDurabilityChanged(
    Guid ItemId,
    string ItemName,
    Guid? OwnerCharacterId,
    int PreviousDurability,
    int CurrentDurability,
    int MaxDurability,
    bool IsBroken
) : GameMessage
{
    public override IReadOnlyList<Guid>? TargetCharacterIds =>
        OwnerCharacterId.HasValue ? new[] { OwnerCharacterId.Value } : null;
}

/// <summary>
/// Published when a stack of items is split.
/// </summary>
public sealed record ItemStackSplit(
    Guid OriginalItemId,
    Guid NewItemId,
    string ItemName,
    Guid? OwnerCharacterId,
    int OriginalStackSize,
    int NewStackSize
) : GameMessage
{
    public override IReadOnlyList<Guid>? TargetCharacterIds =>
        OwnerCharacterId.HasValue ? new[] { OwnerCharacterId.Value } : null;
}

/// <summary>
/// Published when stacks of items are merged.
/// </summary>
public sealed record ItemStacksMerged(
    Guid SourceItemId,
    Guid TargetItemId,
    string ItemName,
    Guid? OwnerCharacterId,
    int FinalStackSize,
    bool SourceDestroyed
) : GameMessage
{
    public override IReadOnlyList<Guid>? TargetCharacterIds =>
        OwnerCharacterId.HasValue ? new[] { OwnerCharacterId.Value } : null;
}
