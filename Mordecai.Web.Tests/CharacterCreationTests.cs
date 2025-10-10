using System.Collections.Generic;
using System.Security.Cryptography;
using Xunit;
using Mordecai.Web.Services;
using Mordecai.Web.Models;

namespace Mordecai.Web.Tests;

public class DiceServiceTests
{
    private readonly IDiceService _diceService;

    public DiceServiceTests()
    {
        _diceService = new DiceService();
    }

    [Fact]
    public void Roll4dF_Should_Return_Value_Between_Minus4_And_Plus4()
    {
        // Act & Assert - Test many rolls to ensure range is correct
        for (int i = 0; i < 1000; i++)
        {
            var result = _diceService.Roll4dF();
            Assert.InRange(result, -4, 4);
        }
    }

    [Fact]
    public void Roll4dFWithModifier_Should_Respect_Bounds()
    {
        // Arrange
        int modifier = 10;
        int minValue = 6;
        int maxValue = 14;

        // Act & Assert - Test many rolls
        for (int i = 0; i < 1000; i++)
        {
            var result = _diceService.Roll4dFWithModifier(modifier, minValue, maxValue);
            Assert.InRange(result, minValue, maxValue);
        }
    }

    [Fact]
    public void RollExploding4dF_Should_AddAdditionalPluses_WhenMaximumRolled()
    {
        using var diceService = new DiceService(new StubRandomNumberGenerator(
            2, 2, 2, 2, // Initial roll: + + + + -> +4
            2, 3, 0, 4  // Exploding roll: + + blank - -> adds +2 and stops
        ));

        var result = diceService.RollExploding4dF();

        Assert.Equal(6, result);
    }

    [Fact]
    public void RollExploding4dF_Should_AddAdditionalMinuses_WhenMinimumRolled()
    {
        using var diceService = new DiceService(new StubRandomNumberGenerator(
            4, 4, 5, 5, // Initial roll: - - - - -> -4
            4, 5, 2, 0  // Exploding roll: - - + blank -> subtract 2 and stop
        ));

        var result = diceService.RollExploding4dF();

        Assert.Equal(-6, result);
    }

    private sealed class StubRandomNumberGenerator : RandomNumberGenerator
    {
        private readonly Queue<byte> _values;

        public StubRandomNumberGenerator(params byte[] values)
        {
            _values = new Queue<byte>(values);
        }

        public override void GetBytes(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (_values.Count == 0)
                {
                    throw new InvalidOperationException("Not enough random values provided for test.");
                }

                data[i] = _values.Dequeue();
            }
        }
    }
}

public class CharacterCreationServiceTests
{
    private readonly ICharacterCreationService _characterCreationService;
    private readonly IDiceService _diceService;

    public CharacterCreationServiceTests()
    {
        _diceService = new DiceService();
        _characterCreationService = new CharacterCreationService(_diceService);
    }

    [Theory]
    [InlineData("Human")]
    [InlineData("Elf")]
    [InlineData("Dwarf")]
    [InlineData("Halfling")]
    [InlineData("Orc")]
    public void GenerateRandomAttributes_Should_Respect_Species_Bounds(string species)
    {
        // Act
        var attributes = _characterCreationService.GenerateRandomAttributes(species);

        // Assert - Check each attribute is within bounds
        var attributeNames = new[] { "Physicality", "Dodge", "Drive", "Reasoning", "Awareness", "Focus", "Bearing" };
        var attributeValues = new[] { attributes.Physicality, attributes.Dodge, attributes.Drive, 
                                    attributes.Reasoning, attributes.Awareness, attributes.Focus, attributes.Bearing };

        for (int i = 0; i < attributeNames.Length; i++)
        {
            var (min, max) = _characterCreationService.GetAttributeBounds(species, attributeNames[i]);
            Assert.InRange(attributeValues[i], min, max);
        }
    }

    [Fact]
    public void Human_Attributes_Should_Be_Between_6_And_14()
    {
        // Act
        var attributes = _characterCreationService.GenerateRandomAttributes("Human");

        // Assert
        Assert.InRange(attributes.Physicality, 6, 14);
        Assert.InRange(attributes.Dodge, 6, 14);
        Assert.InRange(attributes.Drive, 6, 14);
        Assert.InRange(attributes.Reasoning, 6, 14);
        Assert.InRange(attributes.Awareness, 6, 14);
        Assert.InRange(attributes.Focus, 6, 14);
        Assert.InRange(attributes.Bearing, 6, 14);
    }

    [Fact]
    public void ValidateAttributeAdjustments_Should_Require_Same_Total()
    {
        // Arrange
        var originalAttributes = _characterCreationService.GenerateRandomAttributes("Human");
        var originalTotal = originalAttributes.Total;
        
        var modifiedAttributes = originalAttributes.Clone();
        modifiedAttributes.Physicality += 1; // Increase by 1 without decreasing another

        // Act
        var isValid = _characterCreationService.ValidateAttributeAdjustments(modifiedAttributes, originalTotal, "Human");

        // Assert
        Assert.False(isValid, "Validation should fail when total changes");
    }

    [Fact]
    public void ValidateAttributeAdjustments_Should_Pass_When_Total_Unchanged()
    {
        // Arrange
        var originalAttributes = _characterCreationService.GenerateRandomAttributes("Human");
        var originalTotal = originalAttributes.Total;
        
        var modifiedAttributes = originalAttributes.Clone();
        
        // Only modify if we can (attributes not at bounds)
        if (modifiedAttributes.Physicality < 14 && modifiedAttributes.Dodge > 6)
        {
            modifiedAttributes.Physicality += 1;
            modifiedAttributes.Dodge -= 1; // Keep total the same
        }

        // Act
        var isValid = _characterCreationService.ValidateAttributeAdjustments(modifiedAttributes, originalTotal, "Human");

        // Assert
        Assert.True(isValid, "Validation should pass when total is unchanged and attributes within bounds");
    }
}