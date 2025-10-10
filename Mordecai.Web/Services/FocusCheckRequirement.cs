using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Represents the parameters needed to evaluate a conditional Focus skill check.
/// </summary>
internal readonly record struct FocusCheckRequirement(
    int TargetValue,
    string FailureMessage,
    string ResourceLabel,
    int ResourceValue,
    SkillUsageType UsageType,
    string ContextTag);
