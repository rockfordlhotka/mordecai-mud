using System.Security.Cryptography;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for rolling Fudge dice (4dF) and managing dice-related calculations
/// </summary>
public interface IDiceService
{
    /// <summary>
    /// Rolls 4dF (four Fudge dice) returning a value between -4 and +4
    /// </summary>
    int Roll4dF();

    /// <summary>
    /// Rolls 4dF with a modifier, ensuring result stays within min/max bounds
    /// </summary>
    int Roll4dFWithModifier(int modifier, int minValue = 1, int maxValue = 20);

    /// <summary>
    /// Calculates the total of multiple 4dF rolls
    /// </summary>
    int RollMultiple4dF(int count);
}

public class DiceService : IDiceService
{
    private readonly RandomNumberGenerator _rng;

    public DiceService()
    {
        _rng = RandomNumberGenerator.Create();
    }

    public int Roll4dF()
    {
        int total = 0;
        
        // Roll 4 Fudge dice
        for (int i = 0; i < 4; i++)
        {
            total += RollSingleFudgeDie();
        }

        return total;
    }

    public int Roll4dFWithModifier(int modifier, int minValue = 1, int maxValue = 20)
    {
        int roll = Roll4dF();
        int result = roll + modifier;
        
        // Clamp to bounds
        return Math.Max(minValue, Math.Min(maxValue, result));
    }

    public int RollMultiple4dF(int count)
    {
        int total = 0;
        for (int i = 0; i < count; i++)
        {
            total += Roll4dF();
        }
        return total;
    }

    private int RollSingleFudgeDie()
    {
        // Fudge die has 6 faces: 2 blanks (0), 2 plus (+1), 2 minus (-1)
        byte[] randomBytes = new byte[1];
        _rng.GetBytes(randomBytes);
        int roll = randomBytes[0] % 6;

        return roll switch
        {
            0 or 1 => 0,  // Blank faces
            2 or 3 => 1,  // Plus faces
            4 or 5 => -1, // Minus faces
            _ => 0
        };
    }

    public void Dispose()
    {
        _rng?.Dispose();
    }
}