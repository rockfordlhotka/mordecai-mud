namespace Mordecai.Messaging.Messages;

/// <summary>
/// Published for zone-wide environmental effects (weather, time of day, etc.)
/// Future implementation - requires ZoneId property on GameMessage base class
/// </summary>
public sealed record ZoneEnvironmentMessage(
    int ZoneId,
    string EffectType,
    string Description,
    TimeSpan? Duration = null
) : GameMessage
{
    public override int? RoomId => null; // Zone messages don't use RoomId
    // Future: public int ZoneId { get; init; } on GameMessage base class
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
public sealed record ZoneEventMessage(
    int ZoneId,
    string EventType,
    string EventName,
    string Description,
    MessagePriority Priority = MessagePriority.Normal
) : GameMessage
{
    public override int? RoomId => null;
    // Future: public int ZoneId { get; init; } on GameMessage base class
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
