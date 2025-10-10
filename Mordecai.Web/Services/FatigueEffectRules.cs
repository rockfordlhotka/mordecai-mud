using System.Collections.Generic;

namespace Mordecai.Web.Services;

/// <summary>
/// Provides lookups for fatigue-related reactive effects.
/// </summary>
internal static class FatigueEffectRules
{
    private static readonly IReadOnlyDictionary<int, LowFatigueFocusCheck> FocusChecksByFatigue =
        new Dictionary<int, LowFatigueFocusCheck>
        {
            [3] = new LowFatigueFocusCheck(5, "You force yourself to stay upright, but you can't muster the focus to act."),
            [2] = new LowFatigueFocusCheck(7, "Your vision swims as exhaustion overtakes you."),
            [1] = new LowFatigueFocusCheck(12, "You sway on your feet and blackness creeps at the edge of your sight."),
        };

    /// <summary>
    /// Attempts to get a Focus skill check requirement for the supplied available fatigue.
    /// </summary>
    /// <param name="availableFatigue">Current usable fatigue (after pending damage).</param>
    /// <param name="check">The focus check details when a rule exists.</param>
    /// <returns>True when a focus check should be performed; otherwise false.</returns>
    public static bool TryGetFocusCheck(int availableFatigue, out LowFatigueFocusCheck check)
    {
        if (FocusChecksByFatigue.TryGetValue(availableFatigue, out var result))
        {
            check = result;
            return true;
        }

        check = default;
        return false;
    }
}

internal readonly record struct LowFatigueFocusCheck(int TargetValue, string FailureMessage);
