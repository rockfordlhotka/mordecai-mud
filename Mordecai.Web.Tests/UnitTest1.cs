using Mordecai.Web.Services;
using Xunit;

namespace Mordecai.Web.Tests;

public class VitalityActionRestrictionTests
{
    [Fact]
    public void EvaluateActionRestriction_AllowsAction_WhenVitalityIsHealthy()
    {
        var restriction = VitalityEffectRules.EvaluateActionRestriction(5);

        Assert.True(restriction.CanAttemptAction);
        Assert.Null(restriction.FailureMessage);
        Assert.False(restriction.FocusCheck.HasValue);
    }
}
