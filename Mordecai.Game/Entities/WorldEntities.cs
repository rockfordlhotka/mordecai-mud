using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mordecai.Game.Entities;

/// <summary>
/// Tracks whether a doorway blocks passage or sound.
/// </summary>
public enum DoorState
{
    None = 0,
    Open = 1,
    Closed = 2
}

/// <summary>
/// Represents a game world zone containing multiple rooms
/// </summary>
public class Zone
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Difficulty level of the zone (affects spawns, skill requirements, etc.)
    /// </summary>
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// Whether this zone is primarily outdoors (affects day/night descriptions)
    /// </summary>
    public bool IsOutdoor { get; set; } = true;

    /// <summary>
    /// Weather type for the zone (affects descriptions and atmospheric events)
    /// </summary>
    [StringLength(50)]
    public string WeatherType { get; set; } = "Clear";

    /// <summary>
    /// User who created this zone
    /// </summary>
    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}

/// <summary>
/// Represents room type templates with behaviors and properties
/// </summary>
public class RoomType
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether combat is allowed in rooms of this type
    /// </summary>
    public bool AllowsCombat { get; set; } = true;

    /// <summary>
    /// Whether players can safely log out in rooms of this type
    /// </summary>
    public bool AllowsLogout { get; set; } = true;

    /// <summary>
    /// Whether this room type has special commands available
    /// </summary>
    public bool HasSpecialCommands { get; set; } = false;

    /// <summary>
    /// Multiplier for health/mana recovery in rooms of this type
    /// </summary>
    public decimal HealingRate { get; set; } = 1.0m;

    /// <summary>
    /// Multiplier for skill advancement in rooms of this type
    /// </summary>
    public decimal SkillLearningBonus { get; set; } = 1.0m;

    /// <summary>
    /// Maximum number of players allowed in rooms of this type (0 = unlimited)
    /// </summary>
    public int MaxOccupancy { get; set; } = 0;

    /// <summary>
    /// Whether rooms of this type are primarily indoors (affects day/night descriptions)
    /// </summary>
    public bool IsIndoor { get; set; } = false;

    /// <summary>
    /// Message displayed when entering rooms of this type
    /// </summary>
    [StringLength(200)]
    public string? EntryMessage { get; set; }

    /// <summary>
    /// Message displayed when leaving rooms of this type
    /// </summary>
    [StringLength(200)]
    public string? ExitMessage { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}

/// <summary>
/// Represents an individual room in the game world
/// </summary>
public class Room
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ZoneId { get; set; }

    [Required]
    public int RoomTypeId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Primary description used during day or when time doesn't matter
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Alternative description used during night (optional)
    /// Only used if the zone is outdoor or room type allows day/night variation
    /// </summary>
    [StringLength(2000)]
    public string? NightDescription { get; set; }

    /// <summary>
    /// Additional entrance description when arriving from another room (optional)
    /// </summary>
    [StringLength(500)]
    public string? EntryDescription { get; set; }

    /// <summary>
    /// Alternative entrance description for night time (optional)
    /// </summary>
    [StringLength(500)]
    public string? NightEntryDescription { get; set; }

    /// <summary>
    /// Additional exit description when leaving to another room (optional)
    /// </summary>
    [StringLength(500)]
    public string? ExitDescription { get; set; }

    /// <summary>
    /// Alternative exit description for night time (optional)
    /// </summary>
    [StringLength(500)]
    public string? NightExitDescription { get; set; }

    /// <summary>
    /// X coordinate for mapping
    /// </summary>
    public int X { get; set; } = 0;

    /// <summary>
    /// Y coordinate for mapping
    /// </summary>
    public int Y { get; set; } = 0;

    /// <summary>
    /// Z coordinate for mapping (floors, levels)
    /// </summary>
    public int Z { get; set; } = 0;

    /// <summary>
    /// Override zone/room type settings for day/night descriptions
    /// Null = use zone/room type settings, true/false = force enable/disable
    /// </summary>
    public bool? OverrideDayNightDescriptions { get; set; }

    /// <summary>
    /// User who created this room
    /// </summary>
    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// JSON field for room-specific custom properties and overrides
    /// </summary>
    [StringLength(4000)]
    public string? CustomProperties { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ZoneId))]
    public virtual Zone Zone { get; set; } = null!;

    [ForeignKey(nameof(RoomTypeId))]
    public virtual RoomType RoomType { get; set; } = null!;

    public virtual ICollection<RoomExit> ExitsFromHere { get; set; } = new List<RoomExit>();
    public virtual ICollection<RoomExit> ExitsToHere { get; set; } = new List<RoomExit>();

    /// <summary>
    /// Determines whether this room should use day/night descriptions
    /// </summary>
    public bool UsesDayNightDescriptions
    {
        get
        {
            // Check for room-specific override first
            if (OverrideDayNightDescriptions.HasValue)
                return OverrideDayNightDescriptions.Value;

            // Then check if room type is indoor (indoor rooms typically don't change)
            if (RoomType?.IsIndoor == true)
                return false;

            // Finally, check if zone is outdoor
            return Zone?.IsOutdoor == true;
        }
    }

    /// <summary>
    /// Gets the appropriate description based on current time of day
    /// </summary>
    public string GetDescription(bool isNight = false)
    {
        if (UsesDayNightDescriptions && isNight && !string.IsNullOrEmpty(NightDescription))
        {
            return NightDescription;
        }
        return Description;
    }

    /// <summary>
    /// Gets the appropriate entry description based on current time of day
    /// </summary>
    public string? GetEntryDescription(bool isNight = false)
    {
        if (UsesDayNightDescriptions && isNight && !string.IsNullOrEmpty(NightEntryDescription))
        {
            return NightEntryDescription;
        }
        return EntryDescription;
    }

    /// <summary>
    /// Gets the appropriate exit description based on current time of day
    /// </summary>
    public string? GetExitDescription(bool isNight = false)
    {
        if (UsesDayNightDescriptions && isNight && !string.IsNullOrEmpty(NightExitDescription))
        {
            return NightExitDescription;
        }
        return ExitDescription;
    }
}

/// <summary>
/// Represents connections between rooms (exits/entrances)
/// </summary>
public class RoomExit
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FromRoomId { get; set; }

    [Required]
    public int ToRoomId { get; set; }

    [Required]
    [StringLength(20)]
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Description of the exit (e.g., "a wooden door", "a narrow path")
    /// </summary>
    [StringLength(200)]
    public string? ExitDescription { get; set; }

    /// <summary>
    /// Alternative exit description for night time
    /// </summary>
    [StringLength(200)]
    public string? NightExitDescription { get; set; }

    /// <summary>
    /// Whether this exit is hidden and requires special detection
    /// </summary>
    public bool IsHidden { get; set; } = false;

    /// <summary>
    /// Target score required to reveal this hidden exit (used when IsHidden is true)
    /// </summary>
    public int HiddenTargetScore { get; set; } = 10;

    /// <summary>
    /// Skill required to use this exit (e.g., climbing, swimming)
    /// </summary>
    public int? SkillRequired { get; set; }

    /// <summary>
    /// Minimum skill level required to use this exit
    /// </summary>
    public decimal SkillLevelRequired { get; set; } = 0;

    /// <summary>
    /// Optional name shown when players interact with the door.
    /// </summary>
    [StringLength(120)]
    public string? DoorName { get; set; }

    /// <summary>
    /// Indicates whether a door is present and whether it blocks passage.
    /// </summary>
    public DoorState DoorState { get; set; } = DoorState.None;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(FromRoomId))]
    public virtual Room FromRoom { get; set; } = null!;

    [ForeignKey(nameof(ToRoomId))]
    public virtual Room ToRoom { get; set; } = null!;

    /// <summary>
    /// Gets the appropriate exit description based on current time of day
    /// </summary>
    public string? GetExitDescription(bool isNight = false)
    {
        var description = isNight && !string.IsNullOrEmpty(NightExitDescription)
            ? NightExitDescription
            : ExitDescription;

        var trimmed = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (!HasDoor)
        {
            return trimmed;
        }

        var stateText = DoorState switch
        {
            DoorState.Open => "open",
            DoorState.Closed => "closed",
            _ => null
        };

        if (string.IsNullOrEmpty(stateText))
        {
            return trimmed ?? GetDoorDisplayName();
        }

        if (string.IsNullOrEmpty(trimmed))
        {
            return $"{GetDoorDisplayName()} ({stateText})";
        }

        return trimmed.Contains($"({stateText})", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{trimmed} ({stateText})";
    }

    /// <summary>
    /// Returns true when a door is present on the exit.
    /// </summary>
    public bool HasDoor => DoorState != DoorState.None;

    /// <summary>
    /// Returns true when the door is both present and closed.
    /// </summary>
    public bool IsDoorClosed => DoorState == DoorState.Closed;

    /// <summary>
    /// Indicates whether the door blocks sound propagation.
    /// </summary>
    public bool BlocksSound => IsDoorClosed;

    /// <summary>
    /// Gets a friendly display name for the doorway.
    /// </summary>
    public string GetDoorDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(DoorName))
        {
            return DoorName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(ExitDescription))
        {
            return ExitDescription.Trim();
        }

        return "door";
    }
}