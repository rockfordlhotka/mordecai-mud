namespace Mordecai.Game.Entities;

/// <summary>
/// Magic schools for mana management
/// Each school has its own mana pool and recovery skill
/// </summary>
public enum MagicSchool
{
    Fire = 1,
    Healing = 2,
    Lightning = 3,
    Illusion = 4
}

/// <summary>
/// Represents a character's mana pool for a specific magic school
/// </summary>
public class CharacterManaPool
{
    public int Id { get; set; }

    /// <summary>
    /// The character who owns this mana pool
    /// </summary>
    public Guid CharacterId { get; set; }

    /// <summary>
    /// The magic school this pool belongs to
    /// </summary>
    public MagicSchool School { get; set; }

    /// <summary>
    /// Current mana available for casting
    /// </summary>
    public int CurrentMana { get; set; }

    /// <summary>
    /// Maximum mana capacity for this school
    /// Determined by: Base (10) + WIL + School Recovery Skill
    /// </summary>
    public int MaxMana { get; set; }

    /// <summary>
    /// Last time mana was regenerated for this pool
    /// </summary>
    public DateTimeOffset LastRegenAt { get; set; }

    /// <summary>
    /// When mana gathering started (null if not gathering)
    /// Used for concentration-based mana gathering
    /// </summary>
    public DateTimeOffset? GatheringStartedAt { get; set; }

    // Navigation properties
    public Character? Character { get; set; }
}

/// <summary>
/// Result of a mana operation (consume, gather, regenerate)
/// </summary>
public class ManaOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public MagicSchool School { get; set; }
    public int PreviousMana { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    public int AmountChanged { get; set; }
}

/// <summary>
/// Summary of a character's mana across all schools
/// </summary>
public class CharacterManaSummary
{
    public Guid CharacterId { get; set; }
    public Dictionary<MagicSchool, ManaPoolInfo> Pools { get; set; } = new();
    public int TotalCurrentMana => Pools.Values.Sum(p => p.CurrentMana);
    public int TotalMaxMana => Pools.Values.Sum(p => p.MaxMana);
}

/// <summary>
/// Info about a single mana pool
/// </summary>
public class ManaPoolInfo
{
    public MagicSchool School { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    public decimal RegenPerMinute { get; set; }
    public bool IsGathering { get; set; }
    public decimal PercentFull => MaxMana > 0 ? (decimal)CurrentMana / MaxMana * 100 : 0;
}
