using System;
using Mordecai.Web.Services;
using Xunit;

namespace Mordecai.Web.Tests;

public class VitalityEffectRulesTests
{
    [Theory]
    [InlineData(8, 0, 8)]
    [InlineData(8, 3, 5)]
    [InlineData(2, 5, 0)]
    [InlineData(5, -2, 5)]
    public void CalculateAvailableVitality_Should_Subtract_Pending_Damage(int current, int pending, int expected)
    {
        var result = VitalityEffectRules.CalculateAvailableVitality(current, pending);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EvaluateActionRestriction_Should_Block_When_Vitality_Is_Zero()
    {
        var restriction = VitalityEffectRules.EvaluateActionRestriction(0);

        Assert.False(restriction.CanAttemptAction);
        Assert.Equal("You have died.", restriction.FailureMessage);
    }

    [Fact]
    public void EvaluateActionRestriction_Should_Require_Focus_Check_When_Vitality_Is_Three()
    {
        var restriction = VitalityEffectRules.EvaluateActionRestriction(3);

        Assert.True(restriction.CanAttemptAction);
        Assert.True(restriction.FocusCheck.HasValue);
        Assert.Equal(7, restriction.FocusCheck!.Value.TargetValue);
    }

    [Theory]
    [InlineData(5, 3)]
    [InlineData(4, 60)]
    [InlineData(3, 1800)]
    [InlineData(2, 3600)]
    public void GetFatigueRegenInterval_Should_Reflect_Vitality_State(int vitality, int expectedSeconds)
    {
        var baseInterval = TimeSpan.FromSeconds(3);
        var interval = VitalityEffectRules.GetFatigueRegenInterval(vitality, baseInterval);

        Assert.NotNull(interval);
        Assert.Equal(expectedSeconds, (int)interval!.Value.TotalSeconds);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void GetFatigueRegenInterval_Should_Return_Null_When_Regeneration_Disabled(int vitality)
    {
        var baseInterval = TimeSpan.FromSeconds(3);
        var interval = VitalityEffectRules.GetFatigueRegenInterval(vitality, baseInterval);

        Assert.Null(interval);
    }
}
