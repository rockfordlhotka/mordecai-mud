using System.Reflection;
using Mordecai.Game.Entities;
using Mordecai.Web.Services;
using Xunit;

namespace Mordecai.Web.Tests;

public sealed class HealthTickBackgroundServiceTests
{
    [Fact]
    public void ApplyPassiveVitalityRegen_ShouldNotHealBeforeInterval()
    {
        // Arrange
        var character = CreateCharacter();
        character.CurrentVitality = character.MaxVitality - 2;
        var last = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(30);
        character.LastVitalityRegenAt = last;

        // Act
        var updated = HealthTickBackgroundService.ApplyPassiveVitalityRegen(character, last + TimeSpan.FromMinutes(30), character.MaxVitality);

        // Assert
        Assert.False(updated);
        Assert.Equal(character.MaxVitality - 2, character.CurrentVitality);
        Assert.Equal(last, character.LastVitalityRegenAt);
    }

    [Fact]
    public void ApplyPassiveVitalityRegen_ShouldHealOnePointAfterHour()
    {
        // Arrange
        var character = CreateCharacter();
        character.CurrentVitality = character.MaxVitality - 3;
        var last = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        character.LastVitalityRegenAt = last;

        // Act
        var now = last + TimeSpan.FromHours(1);
        var updated = HealthTickBackgroundService.ApplyPassiveVitalityRegen(character, now, character.MaxVitality);

        // Assert
        Assert.True(updated);
        Assert.Equal(character.MaxVitality - 2, character.CurrentVitality);
        Assert.Equal(now, character.LastVitalityRegenAt);
    }

    [Fact]
    public void ApplyPassiveVitalityRegen_ShouldCapHealingToMissingAmount()
    {
        // Arrange
        var character = CreateCharacter();
        character.CurrentVitality = character.MaxVitality - 2;
        var last = DateTimeOffset.UtcNow - TimeSpan.FromHours(3);
        character.LastVitalityRegenAt = last;

        // Act
        var now = last + TimeSpan.FromHours(3);
        var updated = HealthTickBackgroundService.ApplyPassiveVitalityRegen(character, now, character.MaxVitality);

        // Assert
        Assert.True(updated);
        Assert.Equal(character.MaxVitality, character.CurrentVitality);
        Assert.Equal(now, character.LastVitalityRegenAt);
    }

    [Fact]
    public void ApplyPassiveVitalityRegen_ShouldResetTimerWhenAtMax()
    {
        // Arrange
        var character = CreateCharacter();
        var last = DateTimeOffset.UtcNow - TimeSpan.FromHours(5);
        character.CurrentVitality = character.MaxVitality;
        character.LastVitalityRegenAt = last;

        // Act
        var now = last + TimeSpan.FromMinutes(10);
        var updated = HealthTickBackgroundService.ApplyPassiveVitalityRegen(character, now, character.MaxVitality);

        // Assert
        Assert.True(updated);
        Assert.Equal(character.MaxVitality, character.CurrentVitality);
        Assert.Equal(now, character.LastVitalityRegenAt);
    }

    [Fact]
    public void ApplyPassiveVitalityRegen_ShouldClearTimerWhenDead()
    {
        // Arrange
        var character = CreateCharacter();
        var last = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        character.CurrentVitality = 0;
        character.LastVitalityRegenAt = last;

        // Act
        var updated = HealthTickBackgroundService.ApplyPassiveVitalityRegen(character, last + TimeSpan.FromMinutes(5), character.MaxVitality);

        // Assert
        Assert.True(updated);
        Assert.Null(character.LastVitalityRegenAt);
    }

    [Fact]
    public void ProcessFatiguePool_ShouldDiscardHealingWhenFatigueFull()
    {
        // Arrange
        var character = CreateCharacter();
        character.CurrentFatigue = character.MaxFatigue;
        character.PendingFatigueDamage = -4;
        character.PendingVitalityDamage = -3;

        // Act
        var updated = InvokeProcessFatiguePool(character);

        // Assert
        Assert.True(updated);
        Assert.Equal(0, character.PendingFatigueDamage);
        Assert.Equal(-3, character.PendingVitalityDamage);
    }

    [Fact]
    public void ProcessFatiguePool_ShouldNotConvertOverflowHealingIntoVitality()
    {
        // Arrange
        var character = CreateCharacter();
        character.CurrentFatigue = character.MaxFatigue - 1;
        character.PendingFatigueDamage = -4;
        character.PendingVitalityDamage = -2;

        // Act
        var updated = InvokeProcessFatiguePool(character);

        // Assert
        Assert.True(updated);
        Assert.Equal(character.MaxFatigue, character.CurrentFatigue);
        Assert.Equal(0, character.PendingFatigueDamage);
        Assert.Equal(-2, character.PendingVitalityDamage);
    }

    private static Character CreateCharacter()
    {
        return new Character
        {
            Physicality = 10,
            Drive = 10,
            CurrentVitality = 10,
            CurrentFatigue = 10
        };
    }

    private static bool InvokeProcessFatiguePool(Character character)
    {
        var method = typeof(HealthTickBackgroundService)
            .GetMethod("ProcessFatiguePool", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (bool)(method!.Invoke(null, new object[] { character }) ?? false);
    }
}
