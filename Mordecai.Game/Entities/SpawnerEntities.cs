using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mordecai.Game.Services;

namespace Mordecai.Game.Entities;

/// <summary>
/// Behavior pattern for spawner spawn selection
/// </summary>
public enum SpawnBehavior
{
    /// <summary>
    /// Always spawns the same NPC type (single entry in spawn table)
    /// </summary>
    Fixed = 0,

    /// <summary>
    /// Randomly selects from spawn table with equal probability
    /// </summary>
    Random = 1,

    /// <summary>
    /// Randomly selects based on weighted probability
    /// </summary>
    Weighted = 2
}

/// <summary>
/// Type of spawner placement
/// </summary>
public enum SpawnerType
{
    /// <summary>
    /// Spawner is fixed to a single room
    /// </summary>
    RoomBound = 0,

    /// <summary>
    /// Spawner roams through rooms in an area/zone (future implementation)
    /// </summary>
    AreaRoaming = 1
}

/// <summary>
/// Reason for NPC despawn
/// </summary>
public enum DespawnReason
{
    Death = 0,
    Timeout = 1,
    AdminCommand = 2,
    SpawnerDisabled = 3,
    SystemShutdown = 4
}

/// <summary>
/// Template defining NPC characteristics and behavior
/// Similar to ItemTemplate but for creatures/NPCs
/// </summary>
public class NpcTemplate
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
    /// Short description shown in room listings
    /// </summary>
    [StringLength(200)]
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// Level/difficulty of this NPC (affects stats and rewards)
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// Base attributes for this NPC type
    /// </summary>
    public int Strength { get; set; } = 0;
    public int Endurance { get; set; } = 0;
    public int Coordination { get; set; } = 0;
    public int Quickness { get; set; } = 0;
    public int Intelligence { get; set; } = 0;
    public int Willpower { get; set; } = 0;
    public int Charisma { get; set; } = 0;

    /// <summary>
    /// Whether this NPC is hostile by default
    /// </summary>
    public bool IsHostile { get; set; } = false;

    /// <summary>
    /// Whether this NPC will assist other nearby NPCs of the same template
    /// </summary>
    public bool IsGroupAssist { get; set; } = false;

    /// <summary>
    /// Whether this NPC can move between rooms
    /// </summary>
    public bool CanWander { get; set; } = false;

    /// <summary>
    /// JSON field for AI behavior configuration
    /// </summary>
    [StringLength(4000)]
    public string? BehaviorConfig { get; set; }

    /// <summary>
    /// JSON field for loot table configuration
    /// </summary>
    [StringLength(4000)]
    public string? LootConfig { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<SpawnerNpcEntry> SpawnerEntries { get; set; } = new List<SpawnerNpcEntry>();
}

/// <summary>
/// Template defining spawner behavior and rules
/// Reusable configuration for what/how to spawn
/// </summary>
public class SpawnerTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// How NPCs are selected from the spawn table
    /// </summary>
    public SpawnBehavior SpawnBehavior { get; set; } = SpawnBehavior.Fixed;

    /// <summary>
    /// Minimum seconds between spawn attempts
    /// </summary>
    public int SpawnIntervalMin { get; set; } = 300; // 5 minutes default

    /// <summary>
    /// Maximum seconds between spawn attempts (for randomization)
    /// </summary>
    public int SpawnIntervalMax { get; set; } = 600; // 10 minutes default

    /// <summary>
    /// Maximum number of NPCs this spawner can have active at once
    /// </summary>
    public int MaxActiveCreatures { get; set; } = 1;

    /// <summary>
    /// Whether to respawn NPCs when they die
    /// </summary>
    public bool RespawnOnDeath { get; set; } = true;

    /// <summary>
    /// JSON field for spawn conditions (serialized SpawnConditions)
    /// </summary>
    [StringLength(2000)]
    public string? ConditionsJson { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<SpawnerNpcEntry> SpawnTable { get; set; } = new List<SpawnerNpcEntry>();
    public virtual ICollection<SpawnerInstance> Instances { get; set; } = new List<SpawnerInstance>();
}

/// <summary>
/// Entry in a spawner's spawn table defining possible NPCs to spawn
/// </summary>
public class SpawnerNpcEntry
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SpawnerTemplateId { get; set; }

    [Required]
    public int NpcTemplateId { get; set; }

    /// <summary>
    /// Weight for weighted random selection (higher = more likely)
    /// Ignored for Fixed and Random spawn behaviors
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// Minimum level for level-scaled NPCs (0 = use NpcTemplate default)
    /// </summary>
    public int MinLevel { get; set; } = 0;

    /// <summary>
    /// Maximum level for level-scaled NPCs (0 = use NpcTemplate default)
    /// </summary>
    public int MaxLevel { get; set; } = 0;

    // Navigation properties
    [ForeignKey(nameof(SpawnerTemplateId))]
    public virtual SpawnerTemplate SpawnerTemplate { get; set; } = null!;

    [ForeignKey(nameof(NpcTemplateId))]
    public virtual NpcTemplate NpcTemplate { get; set; } = null!;
}

/// <summary>
/// Instance of a spawner placed in the world
/// Connects spawner templates to specific locations
/// </summary>
public class SpawnerInstance
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SpawnerTemplateId { get; set; }

    /// <summary>
    /// Type of spawner placement (room-bound or area-roaming)
    /// </summary>
    public SpawnerType Type { get; set; } = SpawnerType.RoomBound;

    /// <summary>
    /// Room ID if this is a room-bound spawner
    /// </summary>
    public int? RoomId { get; set; }

    /// <summary>
    /// Zone ID if this is an area-roaming spawner
    /// </summary>
    public int? ZoneId { get; set; }

    /// <summary>
    /// Current room location for area-roaming spawners
    /// </summary>
    public int? CurrentRoomId { get; set; }

    /// <summary>
    /// Whether this spawner instance is currently active
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Last time this spawner successfully spawned an NPC
    /// </summary>
    public DateTimeOffset? LastSpawnTime { get; set; }

    /// <summary>
    /// Next scheduled spawn time
    /// </summary>
    public DateTimeOffset? NextSpawnTime { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SpawnerTemplateId))]
    public virtual SpawnerTemplate SpawnerTemplate { get; set; } = null!;

    [ForeignKey(nameof(RoomId))]
    public virtual Room? Room { get; set; }

    [ForeignKey(nameof(ZoneId))]
    public virtual Zone? Zone { get; set; }

    [ForeignKey(nameof(CurrentRoomId))]
    public virtual Room? CurrentRoom { get; set; }

    public virtual ICollection<ActiveSpawn> ActiveSpawns { get; set; } = new List<ActiveSpawn>();
}

/// <summary>
/// Tracks NPCs spawned by spawners for management and cleanup
/// Links spawned NPCs back to their spawner for respawn logic
/// </summary>
public class ActiveSpawn
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID of the spawned NPC (will reference Npc entity when implemented)
    /// For now, this is a placeholder GUID
    /// </summary>
    [Required]
    public Guid NpcId { get; set; }

    [Required]
    public int SpawnerInstanceId { get; set; }

    [Required]
    public int NpcTemplateId { get; set; }

    /// <summary>
    /// When this NPC was spawned
    /// </summary>
    public DateTimeOffset SpawnedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Current room location of the spawned NPC
    /// </summary>
    public int? CurrentRoomId { get; set; }

    /// <summary>
    /// Whether this NPC is still alive/active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this spawn was deactivated (death, despawn, etc.)
    /// </summary>
    public DateTimeOffset? DeactivatedAt { get; set; }

    /// <summary>
    /// Reason for deactivation
    /// </summary>
    public DespawnReason? DespawnReason { get; set; }

    /// <summary>
    /// Current fatigue (stamina) value for this NPC
    /// </summary>
    public int CurrentFatigue { get; set; }

    /// <summary>
    /// Current vitality (health) value for this NPC
    /// </summary>
    public int CurrentVitality { get; set; }

    /// <summary>
    /// Pending fatigue damage to be applied over time
    /// </summary>
    public int PendingFatigueDamage { get; set; } = 0;

    /// <summary>
    /// Pending vitality damage to be applied over time
    /// </summary>
    public int PendingVitalityDamage { get; set; } = 0;

    /// <summary>
    /// Current number of wounds (long-term injuries)
    /// </summary>
    public int CurrentWounds { get; set; } = 0;

    // Navigation properties
    [ForeignKey(nameof(SpawnerInstanceId))]
    public virtual SpawnerInstance SpawnerInstance { get; set; } = null!;

    [ForeignKey(nameof(NpcTemplateId))]
    public virtual NpcTemplate NpcTemplate { get; set; } = null!;

    [ForeignKey(nameof(CurrentRoomId))]
    public virtual Room? CurrentRoom { get; set; }
}

/// <summary>
/// Value object for spawn conditions
/// Serialized to JSON in SpawnerTemplate.ConditionsJson
/// </summary>
public class SpawnConditions
{
    /// <summary>
    /// Don't spawn if any players are present in the room
    /// </summary>
    public bool BlockIfPlayersPresent { get; set; } = false;

    /// <summary>
    /// Don't spawn if any other creatures are present in the room
    /// </summary>
    public bool BlockIfCreaturesPresent { get; set; } = false;

    /// <summary>
    /// Maximum number of creatures allowed in the room before blocking spawn
    /// </summary>
    public int? MaxCreaturesInRoom { get; set; }

    /// <summary>
    /// Required time of day for spawning (null = any time)
    /// </summary>
    public TimeOfDay? RequiredTimeOfDay { get; set; }

    /// <summary>
    /// Minimum seconds that must pass since last spawn attempt (cooldown)
    /// </summary>
    public int? MinTimeSinceLastSpawn { get; set; }

    /// <summary>
    /// Probability of spawn attempt succeeding (0.0-1.0)
    /// Rolled on each spawn attempt after all other conditions pass
    /// </summary>
    public float SpawnChance { get; set; } = 1.0f;
}
