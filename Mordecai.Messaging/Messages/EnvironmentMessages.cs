namespace Mordecai.Messaging.Messages;

/// <summary>
/// Published for zone-wide environmental effects (weather, time of day, etc.)
/// Future implementation - requires ZoneId property on GameMessage base class
/// </summary>
public sealed record ZoneEnvironmentMessage : GameMessage
{
    public ZoneEnvironmentMessage(
        int? zoneId,
        string effectType,
        string description,
        TimeSpan? duration = null
    )
    {
        ZoneId = zoneId;
        EffectType = effectType;
        Description = description;
        Duration = duration;
    }

    public string EffectType { get; init; }
    public string Description { get; init; }
    public TimeSpan? Duration { get; init; }

    public override int? RoomId => null; // Zone messages don't use RoomId
    public override int? ZoneId { get; init; }
}

/// <summary>
/// Published for room-specific environmental effects
/// </summary>
public sealed record RoomEnvironmentMessage(
    int SourceRoomId,
    string EffectType,
    string Description,
    TimeSpan? Duration = null,
    bool IsHarmful = false
) : GameMessage
{
    public override int? RoomId => SourceRoomId;
}

/// <summary>
/// Published when a zone-wide event occurs
/// Future implementation - requires ZoneId property on GameMessage base class
/// </summary>
public sealed record ZoneEventMessage : GameMessage
{
    public ZoneEventMessage(
        int? zoneId,
        string eventType,
        string eventName,
        string description,
        MessagePriority priority = MessagePriority.Normal
    )
    {
        ZoneId = zoneId;
        EventType = eventType;
        EventName = eventName;
        Description = description;
        Priority = priority;
    }

    public string EventType { get; init; }
    public string EventName { get; init; }
    public string Description { get; init; }
    public MessagePriority Priority { get; init; }

    public override int? RoomId => null;
    public override int? ZoneId { get; init; }
}

/// <summary>
/// Types of environmental effects
/// </summary>
public enum EnvironmentEffectType
{
    Weather,        // Rain, snow, fog, storms
    Temperature,    // Hot, cold, comfortable
    Light,          // Bright, dim, dark, pitch black
    Sound,          // Noisy, quiet, echoing
    Smell,          // Pleasant, foul, smoky
    TimeOfDay,      // Dawn, day, dusk, night
    Atmosphere,     // Peaceful, tense, eerie, welcoming
    Magic,          // High magic, low magic, anti-magic
    Hazard          // Fire, poison gas, radiation, etc.
}

/// <summary>
/// Types of zone-wide events
/// </summary>
public enum ZoneEventType
{
    Boss,           // Zone boss spawn
    Festival,       // Celebration or event
    Invasion,       // Monster invasion
    Weather,        // Major weather event
    Quest,          // Zone-wide quest
    PvPChange,      // PvP flag change
    Lockdown,       // Zone closed or restricted
    Discovery       // New area discovered
}
