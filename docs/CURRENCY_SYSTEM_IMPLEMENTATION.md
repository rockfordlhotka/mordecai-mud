# Currency System Implementation

**Implementation Date**: October 14, 2025

## Overview

The Mordecai MUD economy now includes a four-tier currency system as specified in the game design document. All currency is tracked at the character level and item costs are stored in the database as copper pieces.

## Currency Denominations

| Currency | Symbol | Exchange Rate |
|----------|--------|---------------|
| Copper   | cp     | Base unit (1 cp) |
| Silver   | sp     | 20 cp = 1 sp |
| Gold     | gp     | 20 sp = 400 cp = 1 gp |
| Platinum | pp     | 20 gp = 8000 cp = 1 pp |

## Database Changes

### Character Table

Added four new columns to the `Characters` table:

- `CopperCoins` (integer, default: 0)
- `SilverCoins` (integer, default: 0)
- `GoldCoins` (integer, default: 0)
- `PlatinumCoins` (integer, default: 0)

**Migration**: `20251014235236_AddCharacterCurrency`

### Item Template Table

The existing `Value` column stores item costs as copper pieces (no migration needed).

## Code Changes

### 1. Character Entity (`Mordecai.Game/Entities/SkillEntities.cs`)

Added currency properties:

```csharp
// Currency (stored as individual coin counts)
public int CopperCoins { get; set; } = 0;
public int SilverCoins { get; set; } = 0;
public int GoldCoins { get; set; } = 0;
public int PlatinumCoins { get; set; } = 0;
```

Added calculated properties:

```csharp
/// <summary>
/// Calculates total wealth in copper pieces
/// </summary>
[NotMapped]
public int TotalCopperValue => CopperCoins + (SilverCoins * 20) + (GoldCoins * 400) + (PlatinumCoins * 8000);

/// <summary>
/// Calculates weight of carried coins in pounds (100 coins = 1 pound)
/// </summary>
[NotMapped]
public decimal CoinWeight => (CopperCoins + SilverCoins + GoldCoins + PlatinumCoins) / 100.0m;
```

### 2. ApplicationDbContext (`Mordecai.Web/Data/ApplicationDbContext.cs`)

Added configuration for currency columns with default values:

```csharp
// Configure currency properties with default values
entity.Property(c => c.CopperCoins).HasDefaultValue(0);
entity.Property(c => c.SilverCoins).HasDefaultValue(0);
entity.Property(c => c.GoldCoins).HasDefaultValue(0);
entity.Property(c => c.PlatinumCoins).HasDefaultValue(0);
```

### 3. CurrencyService (`Mordecai.Web/Services/CurrencyService.cs`)

Created a new service for currency operations:

**Interface Methods**:

- `FormatCopperValue(int)` - Convert copper to display string
- `FormatCoins(int, int, int, int)` - Format individual coin counts
- `ConvertCopperToCoins(int)` - Convert copper to optimized coin counts
- `ConvertCoinsToCopper(int, int, int, int)` - Convert coins to total copper
- `TryDeductCost(Character, int)` - Attempt to deduct cost from character
- `AddCurrency(Character, int, int, int, int)` - Add coins to character
- `GetTotalCopper(Character)` - Get character's total wealth
- `CanAfford(Character, int)` - Check if character can afford cost

**Key Features**:

- Automatic coin optimization (minimizes coin count)
- Safe arithmetic operations
- Clear formatting for display
- Support for adding/deducting currency

**Example Usage**:

```csharp
// Check if character can afford an item
if (currencyService.CanAfford(character, itemCost))
{
    // Deduct the cost
    if (currencyService.TryDeductCost(character, itemCost))
    {
        // Give the item
    }
}

// Display formatted currency
var display = currencyService.FormatCoins(
    character.CopperCoins, 
    character.SilverCoins, 
    character.GoldCoins, 
    character.PlatinumCoins
);
// Output: "5 gold, 3 silver, 12 copper"
```

## Currency Economy Rules

### Coin Weight

- 100 coins of any type = 1 pound
- Weight affects character carrying capacity
- Total coin weight = (Copper + Silver + Gold + Platinum) / 100

### Item Pricing

As specified in the game design document:

| Category | Price Range |
|----------|-------------|
| Food and Drink | 1-50 cp |
| Basic Supplies | 5-100 cp |
| Common Weapons/Armor | 50 cp - 10 sp |
| Quality Weapons/Armor | 10 sp - 5 gp |
| Masterwork Equipment | 5-20 gp |
| Enchanted Items (minor) | 20-100 gp |
| Enchanted Items (major) | 100+ gp |

### Merchant Behavior

- **Common Merchants**: Deal in copper and silver
- **Specialty Merchants**: Deal in silver and gold
- **Wealthy Merchants**: Deal primarily in gold
- **Black Market Traders**: Accept any currency

### Currency Rarity

- **Copper**: Most common, everyday transactions
- **Silver**: Quality goods, professional services
- **Gold**: Rare items, enchanted equipment
- **Platinum**: Exceedingly rare (1-3 coins from high-level NPCs)

## Coin Optimization

The CurrencyService automatically optimizes coin storage to minimize the total number of coins:

**Example**:
```
Input: 850 copper
Output: 2 gold, 2 silver, 10 copper
```

This is important for:
- Reducing weight (fewer total coins)
- Realistic merchant behavior (giving change)
- Player convenience

## Future Enhancements

### Phase 1: Basic Commerce (To Implement)
- [ ] Shop command (`buy`, `sell`, `list`)
- [ ] Give/trade currency between players
- [ ] Drop/pick up currency in rooms
- [ ] Bank system for secure storage
- [ ] Currency exchange at banks (with fees)

### Phase 2: Advanced Economy
- [ ] Dynamic pricing based on supply/demand
- [ ] Merchant inventories
- [ ] Quest rewards in currency
- [ ] Loot drops from NPCs
- [ ] Taxation systems
- [ ] Guild banks

### Phase 3: Economic Events
- [ ] Market crashes/booms
- [ ] Inflation/deflation
- [ ] Regional currency variations
- [ ] Black market exchanges

## Testing Checklist

### Unit Tests Needed

- [ ] CurrencyService.ConvertCopperToCoins
  - All denomination combinations
  - Edge cases (0, negative, max int)
  - Optimization validation
  
- [ ] CurrencyService.ConvertCoinsToCopper
  - All denomination combinations
  - Overflow protection
  
- [ ] CurrencyService.TryDeductCost
  - Successful deduction
  - Insufficient funds
  - Exact amount
  - Coin optimization after deduction
  
- [ ] CurrencyService.AddCurrency
  - Adding single denomination
  - Adding multiple denominations
  - Coin optimization after addition
  
- [ ] Character.TotalCopperValue
  - Various coin combinations
  - Edge cases
  
- [ ] Character.CoinWeight
  - Weight calculation accuracy
  - Impact on carrying capacity

### Integration Tests Needed

- [ ] Character creation with starting currency
- [ ] Item purchase transaction flow
- [ ] Currency transfer between players
- [ ] Bank deposit/withdrawal
- [ ] NPC loot drop with currency
- [ ] Database persistence of currency

## Documentation Updates

- [x] Specification updated (`docs/MORDECAI_SPECIFICATION.md`)
- [x] Currency system README created (`docs/CURRENCY_SYSTEM_IMPLEMENTATION.md`)
- [x] Database design updated (implicit via migration)
- [ ] API documentation for CurrencyService
- [ ] Player guide for currency system

## Migration Instructions

### For Existing Characters

When the migration is applied, all existing characters will have:
- CopperCoins = 0
- SilverCoins = 0
- GoldCoins = 0
- PlatinumCoins = 0

**Recommended**: Run a data migration script to give existing characters starting currency based on their level or skill progression.

### For New Characters

New characters can be granted starting currency in the character creation flow:

```csharp
var newCharacter = new Character
{
    // ... other properties ...
    CopperCoins = 100,  // 100 copper starting money
    SilverCoins = 0,
    GoldCoins = 0,
    PlatinumCoins = 0
};
```

## Dependencies

The currency system has no new external dependencies. It relies on:

- Existing Character entity
- Existing ItemTemplate entity
- Standard .NET math operations

## Performance Considerations

- All currency operations are in-memory calculations (no database queries)
- Coin optimization is O(1) time complexity
- Character.TotalCopperValue is a calculated property (no database hit)
- Consider caching optimized coin counts if frequently accessed

## Security Considerations

- Currency values are stored server-side (characters table)
- All transactions should be validated server-side
- Use transactions for multi-step operations (buy + inventory update)
- Log large currency transfers for audit trail
- Implement rate limiting for currency commands

## Summary

The currency system implementation is complete and ready for use. The foundation supports:

? Four-tier currency system (copper, silver, gold, platinum)
? Character wealth tracking
? Automatic coin optimization
? Item cost storage (as copper)
? Comprehensive currency service API
? Weight impact on carrying capacity
? Extensible for future commerce features

**Next Steps**: Implement shop commands, trading, and NPC loot drops to make the currency system functional in gameplay.

---

**Related Documents**:
- [Game Specification](../MORDECAI_SPECIFICATION.md) - Full economy design
- [Database Design](DATABASE_DESIGN.md) - Schema details
- [Item System](ITEM_IMPLEMENTATION_STATUS.md) - Item and inventory system

