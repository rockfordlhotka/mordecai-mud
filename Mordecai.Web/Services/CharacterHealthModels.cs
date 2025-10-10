namespace Mordecai.Web.Services;

/// <summary>
/// Snapshot of a character's current and pending health state.
/// </summary>
public sealed record CharacterHealthSnapshot(
    int CurrentFatigue,
    int MaxFatigue,
    int PendingFatigueDamage,
    int CurrentVitality,
    int MaxVitality,
    int PendingVitalityDamage
);

/// <summary>
/// Result of attempting to mutate a character's health (for actions that consume fatigue, etc.).
/// </summary>
public sealed record CharacterHealthOperationResult(
    bool Success,
    string? FailureReason,
    CharacterHealthSnapshot? Snapshot
);

/// <summary>
/// Result payload for the Drive command action.
/// </summary>
public sealed record DriveCommandResult(
    bool Success,
    string Feedback,
    int TargetValue,
    int AbilityScore,
    int? DiceRoll,
    int? CheckTotal,
    int FatigueHealingAmount,
    int VitalityDamageAmount,
    CharacterHealthSnapshot? Snapshot
);
