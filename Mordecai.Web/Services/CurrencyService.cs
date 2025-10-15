using Mordecai.Game.Entities;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for currency conversion and display operations
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Converts a copper value to a formatted display string (e.g., "5 gold, 3 silver, 12 copper")
    /// </summary>
    string FormatCopperValue(int copperValue);

    /// <summary>
    /// Converts individual coin counts to a formatted display string
    /// </summary>
    string FormatCoins(int copper, int silver, int gold, int platinum);

    /// <summary>
    /// Converts copper value to coin counts (optimized for fewest coins)
    /// </summary>
    (int copper, int silver, int gold, int platinum) ConvertCopperToCoins(int copperValue);

    /// <summary>
    /// Converts coin counts to total copper value
    /// </summary>
    int ConvertCoinsToCopper(int copper, int silver, int gold, int platinum);

    /// <summary>
    /// Attempts to deduct a cost from character's coins. Returns true if successful.
    /// </summary>
    bool TryDeductCost(Character character, int costInCopper);

    /// <summary>
    /// Adds coins to character's currency
    /// </summary>
    void AddCurrency(Character character, int copper = 0, int silver = 0, int gold = 0, int platinum = 0);

    /// <summary>
    /// Gets the character's total wealth in copper
    /// </summary>
    int GetTotalCopper(Character character);

    /// <summary>
    /// Validates that a character has enough currency for a purchase
    /// </summary>
    bool CanAfford(Character character, int costInCopper);
}

public class CurrencyService : ICurrencyService
{
    public const int CopperPerSilver = 20;
    public const int SilverPerGold = 20;
    public const int GoldPerPlatinum = 20;

    public const int CopperPerGold = CopperPerSilver * SilverPerGold; // 400
    public const int CopperPerPlatinum = CopperPerGold * GoldPerPlatinum; // 8000

    public string FormatCopperValue(int copperValue)
    {
        var (copper, silver, gold, platinum) = ConvertCopperToCoins(copperValue);
        return FormatCoins(copper, silver, gold, platinum);
    }

    public string FormatCoins(int copper, int silver, int gold, int platinum)
    {
        var parts = new List<string>();

        if (platinum > 0)
            parts.Add($"{platinum} platinum");
        if (gold > 0)
            parts.Add($"{gold} gold");
        if (silver > 0)
            parts.Add($"{silver} silver");
        if (copper > 0 || parts.Count == 0) // Always show copper if no other coins
            parts.Add($"{copper} copper");

        return string.Join(", ", parts);
    }

    public (int copper, int silver, int gold, int platinum) ConvertCopperToCoins(int copperValue)
    {
        if (copperValue < 0)
            return (0, 0, 0, 0);

        var platinum = copperValue / CopperPerPlatinum;
        copperValue %= CopperPerPlatinum;

        var gold = copperValue / CopperPerGold;
        copperValue %= CopperPerGold;

        var silver = copperValue / CopperPerSilver;
        copperValue %= CopperPerSilver;

        return (copperValue, silver, gold, platinum);
    }

    public int ConvertCoinsToCopper(int copper, int silver, int gold, int platinum)
    {
        return copper + (silver * CopperPerSilver) + (gold * CopperPerGold) + (platinum * CopperPerPlatinum);
    }

    public bool TryDeductCost(Character character, int costInCopper)
    {
        if (!CanAfford(character, costInCopper))
            return false;

        var totalCopper = GetTotalCopper(character);
        var remainingCopper = totalCopper - costInCopper;

        // Convert back to optimized coin counts
        var (copper, silver, gold, platinum) = ConvertCopperToCoins(remainingCopper);

        character.CopperCoins = copper;
        character.SilverCoins = silver;
        character.GoldCoins = gold;
        character.PlatinumCoins = platinum;

        return true;
    }

    public void AddCurrency(Character character, int copper = 0, int silver = 0, int gold = 0, int platinum = 0)
    {
        var totalCopperToAdd = ConvertCoinsToCopper(copper, silver, gold, platinum);
        var currentTotal = GetTotalCopper(character);
        var newTotal = currentTotal + totalCopperToAdd;

        // Convert to optimized coin counts
        var (c, s, g, p) = ConvertCopperToCoins(newTotal);

        character.CopperCoins = c;
        character.SilverCoins = s;
        character.GoldCoins = g;
        character.PlatinumCoins = p;
    }

    public int GetTotalCopper(Character character)
    {
        return ConvertCoinsToCopper(
            character.CopperCoins,
            character.SilverCoins,
            character.GoldCoins,
            character.PlatinumCoins
        );
    }

    public bool CanAfford(Character character, int costInCopper)
    {
        return GetTotalCopper(character) >= costInCopper;
    }
}
