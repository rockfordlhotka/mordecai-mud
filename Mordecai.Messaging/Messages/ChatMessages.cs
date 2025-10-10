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
    TargetType? TargetType = null,
    SoundLevel SoundLevel = SoundLevel.Quiet
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
/// Published when sounds are heard from adjacent rooms
/// </summary>
public sealed record AdjacentRoomSoundMessage(
    int SourceRoomId,
    int ListenerRoomId,
    string Direction,
    SoundLevel SoundLevel,
    SoundType SoundType,
    string Description,
    string? CharacterName = null,
    string? DetailedMessage = null
) : GameMessage
{
    public override int? RoomId => ListenerRoomId;
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
/// Sound volume levels that determine propagation distance
/// </summary>
public enum SoundLevel
{
    Silent = 0,      // No sound (whisper to self)
    Quiet = 1,       // Normal conversation (adjacent rooms hear muffled sound)
    Normal = 2,      // Regular speech (adjacent rooms hear clearly)
    Loud = 3,        // Yelling/shouting (1 room away hears words, 2 rooms away hears sound)
    VeryLoud = 4,    // Combat, spells (2 rooms away hears description, 3 rooms away hears distant sound)
    Deafening = 5    // Explosions, dragon roars (3+ rooms away can hear)
}

/// <summary>
/// Categories of sounds for appropriate descriptions
/// </summary>
public enum SoundType
{
    Speech,          // Talking, yelling, shouting
    Combat,          // Fighting, weapon clashes
    Magic,           // Spell casting, magical effects
    Movement,        // Footsteps, running, objects moving
    Environmental,   // Wind, water, ambient sounds
    Music,           // Singing, instruments
    Animal,          // Creature sounds
    Mechanical,      // Doors, mechanisms, traps
    Destruction      // Breaking, explosions, collapse
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