using System.Text.Json.Serialization;

namespace Mordecai.Messaging.Messages;

/// <summary>
/// Base class for all game messages that flow through the pub/sub system
/// </summary>
public abstract record GameMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Room ID where this message is relevant (null for global messages)
    /// </summary>
    public virtual int? RoomId { get; init; }

    /// <summary>
    /// Zone ID where this message is relevant (null for global or room-specific messages)
    /// </summary>
    public virtual int? ZoneId { get; init; }
    
    /// <summary>
    /// Character IDs that should receive this message (null means all characters in the room/global scope)
    /// </summary>
    public virtual IReadOnlyList<Guid>? TargetCharacterIds { get; init; }
}

/// <summary>
/// Message priority levels for routing and display
/// </summary>
public enum MessagePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Categories for message filtering and display styling
/// </summary>
public enum MessageCategory
{
    Movement,
    Chat,
    Combat,
    Skill,
    System,
    Admin,
    Emote,
    Look,
    Inventory,
    Trade,
    Environment
}