using System.ComponentModel.DataAnnotations;

namespace Mordecai.Game.Entities;

/// <summary>
/// Represents an active combat session between one or more participants
/// </summary>
public class CombatSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int RoomId { get; set; }
    public virtual Room? Room { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }

    public bool IsActive { get; set; } = true;
    public string? EndReason { get; set; }

    public virtual ICollection<CombatParticipant> Participants { get; set; } = new List<CombatParticipant>();
}

/// <summary>
/// Represents a character or NPC participating in a combat session
/// </summary>
public class CombatParticipant
{
    [Key]
    public int Id { get; set; }

    public Guid CombatSessionId { get; set; }
    public virtual CombatSession? CombatSession { get; set; }

    /// <summary>
    /// Character ID if this is a player character (null for NPCs)
    /// </summary>
    public Guid? CharacterId { get; set; }
    public virtual Character? Character { get; set; }

    /// <summary>
    /// Active Spawn ID if this is a spawned NPC (null for players)
    /// </summary>
    public int? ActiveSpawnId { get; set; }
    public virtual ActiveSpawn? ActiveSpawn { get; set; }

    public string ParticipantName { get; set; } = string.Empty;

    /// <summary>
    /// Is this participant in parry mode (using weapon skill for defense)?
    /// </summary>
    public bool IsInParryMode { get; set; } = false;

    /// <summary>
    /// Timestamp when ranged weapon was last fired (for cooldown tracking)
    /// </summary>
    public DateTimeOffset? LastRangedAttack { get; set; }

    /// <summary>
    /// Timed AV penalties from failed physicality checks
    /// Key: penalty amount, Value: expiration time
    /// </summary>
    public string? TimedPenaltiesJson { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LeftAt { get; set; }

    public bool IsActive { get; set; } = true;
    public string? LeaveReason { get; set; }
}

/// <summary>
/// Tracks timed attack value penalties from combat
/// </summary>
public class TimedPenalty
{
    public int PenaltyAmount { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// Represents a single combat action (attack, defense, skill check, etc.)
/// </summary>
public class CombatActionLog
{
    [Key]
    public int Id { get; set; }

    public Guid CombatSessionId { get; set; }
    public virtual CombatSession? CombatSession { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public int ActorParticipantId { get; set; }
    public virtual CombatParticipant? ActorParticipant { get; set; }

    public int? TargetParticipantId { get; set; }
    public virtual CombatParticipant? TargetParticipant { get; set; }

    public CombatActionType ActionType { get; set; }

    public int? AttackRoll { get; set; }
    public int? DefenseRoll { get; set; }
    public int? SuccessValue { get; set; }

    public int? DamageDealt { get; set; }
    public int? FatigueDamage { get; set; }
    public int? VitalityDamage { get; set; }
    public int? WoundsInflicted { get; set; }

    public HitLocation? HitLocation { get; set; }
    public DamageType? DamageType { get; set; }

    public string? Description { get; set; }
}

/// <summary>
/// Types of combat actions
/// </summary>
public enum CombatActionType
{
    MeleeAttack,
    RangedAttack,
    Dodge,
    Parry,
    ShieldBlock,
    ArmorAbsorb,
    Knockback,
    DualWield,
    Flee,
    PhysicalityCheck,
    EnterParryMode,
    ExitParryMode
}

/// <summary>
/// Hit location for damage targeting
/// </summary>
public enum HitLocation
{
    Head,
    Torso,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg
}
