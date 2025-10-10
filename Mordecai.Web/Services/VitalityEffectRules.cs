using System;
using System.Collections.Generic;

namespace Mordecai.Web.Services;

/// <summary>
/// Provides rule lookups derived from a character's current vitality.
/// </summary>
internal static class VitalityEffectRules
{
    private static readonly IReadOnlyDictionary<int, VitalityFocusCheckSeed> FocusChecks =
        new Dictionary<int, VitalityFocusCheckSeed>
        {
            [3] = new VitalityFocusCheckSeed(7, "Pain grips every nerve; you cannot force your body to respond."),
            [2] = new VitalityFocusCheckSeed(12, "You hover on the edge of death and your limbs refuse to move."),
        };

    /// <summary>
    /// Determines the effective vitality that can be spent on actions after pending damage is resolved.
    /// </summary>
    public static int CalculateAvailableVitality(int currentVitality, int pendingVitalityDamage)
    {
        var pendingDamage = Math.Max(0, pendingVitalityDamage);
        return Math.Max(0, currentVitality - pendingDamage);
    }

    /// <summary>
    /// Evaluates whether a character may attempt actions at the supplied vitality.
    /// </summary>
    public static VitalityActionRestriction EvaluateActionRestriction(int availableVitality)
    {
        if (availableVitality <= 0)
        {
            return VitalityActionRestriction.Blocked("You have died.");
        }

        if (availableVitality == 1)
        {
            return VitalityActionRestriction.Blocked("You are too grievously injured to move.");
        }

        if (FocusChecks.TryGetValue(availableVitality, out var focusCheck))
        {
            return VitalityActionRestriction.RequiresFocusCheck(focusCheck);
        }

        return VitalityActionRestriction.Allowed;
    }

    /// <summary>
    /// Provides the passive fatigue recovery interval based on the available vitality.
    /// </summary>
    public static TimeSpan? GetFatigueRegenInterval(int availableVitality, TimeSpan baseInterval)
    {
        return availableVitality switch
        {
            <= 1 => null,
            2 => TimeSpan.FromHours(1),
            3 => TimeSpan.FromMinutes(30),
            4 => TimeSpan.FromMinutes(1),
            _ => baseInterval,
        };
    }

    internal readonly record struct VitalityFocusCheckSeed(int TargetValue, string FailureMessage);
}

internal readonly record struct VitalityActionRestriction(bool CanAttemptAction, VitalityEffectRules.VitalityFocusCheckSeed? FocusCheck, string? FailureMessage)
{
    public static readonly VitalityActionRestriction Allowed = new(true, null, null);

    public static VitalityActionRestriction Blocked(string failureMessage) => new(false, null, failureMessage);

    public static VitalityActionRestriction RequiresFocusCheck(VitalityEffectRules.VitalityFocusCheckSeed focusCheck) => new(true, focusCheck, null);
}
