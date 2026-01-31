using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Types of spell effects
/// </summary>
public enum SpellEffectType
{
    /// <summary>
    /// Damage a specific target (Fire Bolt, Lightning Strike)
    /// </summary>
    TargetedDamage,

    /// <summary>
    /// Heal a specific target (Heal, Restore)
    /// </summary>
    TargetedHealing,

    /// <summary>
    /// Apply a buff to self (Strength, Shield, Invisibility)
    /// </summary>
    SelfBuff,

    /// <summary>
    /// Apply a buff to a target (Bless, Haste)
    /// </summary>
    TargetedBuff,

    /// <summary>
    /// Apply a debuff to a target (Weaken, Slow)
    /// </summary>
    TargetedDebuff,

    /// <summary>
    /// Damage all enemies in room (Fireball, Earthquake)
    /// </summary>
    AreaDamage,

    /// <summary>
    /// Heal all allies in room (Mass Heal)
    /// </summary>
    AreaHealing,

    /// <summary>
    /// Create a persistent room effect (Wall of Fire, Fog Cloud, Entangle)
    /// </summary>
    RoomEffect
}

/// <summary>
/// Result of a spell cast attempt
/// </summary>
public class SpellCastResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Dice roll result (4dF)
    /// </summary>
    public int DiceRoll { get; set; }

    /// <summary>
    /// Caster's ability score (attribute + skill - 5)
    /// </summary>
    public int AbilityScore { get; set; }

    /// <summary>
    /// Total success value (ability score + dice roll)
    /// </summary>
    public int SuccessValue { get; set; }

    /// <summary>
    /// Target's defense value (if applicable)
    /// </summary>
    public int? TargetDefenseValue { get; set; }

    /// <summary>
    /// Final effect value (damage, healing, etc.)
    /// </summary>
    public int EffectValue { get; set; }

    /// <summary>
    /// Whether the spell was a critical success (exploding dice +4)
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// Whether the spell was a fumble (exploding dice -4)
    /// </summary>
    public bool IsFumble { get; set; }

    /// <summary>
    /// Mana consumed by this cast
    /// </summary>
    public int ManaConsumed { get; set; }

    /// <summary>
    /// Fatigue consumed by this cast
    /// </summary>
    public int FatigueConsumed { get; set; }

    /// <summary>
    /// Effects applied to targets
    /// </summary>
    public List<SpellEffectApplication> AppliedEffects { get; set; } = new();

    /// <summary>
    /// Room effect created (if spell type is RoomEffect)
    /// </summary>
    public RoomEffect? CreatedRoomEffect { get; set; }

    /// <summary>
    /// Skill progression information (if skill was advanced)
    /// </summary>
    public SkillProgressionResult? SkillProgression { get; set; }

    /// <summary>
    /// Whether the caster leveled up the spell skill
    /// </summary>
    public bool SkillLeveledUp => SkillProgression?.DidLevelUp ?? false;
}

/// <summary>
/// Represents a single effect application from a spell
/// </summary>
public class SpellEffectApplication
{
    public Guid TargetId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public bool IsPlayer { get; set; }
    public string EffectType { get; set; } = string.Empty;
    public int EffectValue { get; set; }
    public bool ResistanceApplied { get; set; }
    public int? ResistanceRoll { get; set; }
    public CharacterEffect? AppliedEffect { get; set; }
}

/// <summary>
/// Information needed to cast a spell
/// </summary>
public class SpellCastRequest
{
    public Guid CasterId { get; set; }
    public int SpellSkillId { get; set; }

    /// <summary>
    /// Target character ID (for targeted spells)
    /// </summary>
    public Guid? TargetCharacterId { get; set; }

    /// <summary>
    /// Target NPC ID (for targeted spells)
    /// </summary>
    public Guid? TargetNpcId { get; set; }

    /// <summary>
    /// Target room ID (for room effect spells)
    /// </summary>
    public int? TargetRoomId { get; set; }

    /// <summary>
    /// Override intensity (for scaling spells)
    /// </summary>
    public decimal? IntensityOverride { get; set; }
}

/// <summary>
/// Information about a spell's properties and costs
/// </summary>
public class SpellInfo
{
    public int SkillDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MagicSchool School { get; set; }
    public int ManaCost { get; set; }
    public int FatigueCost { get; set; } = 1;
    public SpellEffectType EffectType { get; set; }
    public decimal CooldownSeconds { get; set; }
    public bool UsesExplodingDice { get; set; }
    public int? BaseDamage { get; set; }
    public int? BaseHealing { get; set; }
    public int? EffectDefinitionId { get; set; }
    public int? RoomEffectDefinitionId { get; set; }
    public int? DurationSeconds { get; set; }
}

/// <summary>
/// Service for casting spells and managing spell effects
/// </summary>
public interface ISpellCastingService
{
    /// <summary>
    /// Casts a spell from a character
    /// </summary>
    /// <param name="request">Spell cast request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the spell cast attempt</returns>
    Task<SpellCastResult> CastSpellAsync(SpellCastRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a character can cast a specific spell (has mana, not on cooldown, not silenced)
    /// </summary>
    /// <param name="characterId">Character attempting to cast</param>
    /// <param name="spellSkillId">Spell skill definition ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the spell can be cast, false otherwise with reason</returns>
    Task<(bool CanCast, string? Reason)> CanCastSpellAsync(
        Guid characterId,
        int spellSkillId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a spell skill
    /// </summary>
    /// <param name="spellSkillId">Spell skill definition ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Spell information or null if not a valid spell skill</returns>
    Task<SpellInfo?> GetSpellInfoAsync(int spellSkillId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all spell skills known by a character
    /// </summary>
    /// <param name="characterId">Character ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of known spell skills with current levels</returns>
    Task<IReadOnlyList<(SpellInfo Spell, int Level)>> GetKnownSpellsAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all spell skills for a specific magic school known by a character
    /// </summary>
    /// <param name="characterId">Character ID</param>
    /// <param name="school">Magic school to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of known spell skills in that school</returns>
    Task<IReadOnlyList<(SpellInfo Spell, int Level)>> GetKnownSpellsBySchoolAsync(
        Guid characterId,
        MagicSchool school,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a character is on cooldown for a specific spell
    /// </summary>
    /// <param name="characterId">Character ID</param>
    /// <param name="spellSkillId">Spell skill definition ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Remaining cooldown in seconds, or 0 if ready</returns>
    Task<decimal> GetSpellCooldownRemainingAsync(
        Guid characterId,
        int spellSkillId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Learns a new spell skill for a character (grants level 0)
    /// </summary>
    /// <param name="characterId">Character ID</param>
    /// <param name="spellSkillId">Spell skill definition ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if spell was learned, false if already known or invalid</returns>
    Task<bool> LearnSpellAsync(
        Guid characterId,
        int spellSkillId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available spell skills that can be learned
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all spell skill definitions</returns>
    Task<IReadOnlyList<SpellInfo>> GetAllSpellsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all spell skills in a specific magic school
    /// </summary>
    /// <param name="school">Magic school</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of spell skills in that school</returns>
    Task<IReadOnlyList<SpellInfo>> GetSpellsBySchoolAsync(
        MagicSchool school,
        CancellationToken cancellationToken = default);
}
