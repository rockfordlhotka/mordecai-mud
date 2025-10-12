# Carrying Capacity Analysis

## Why Exponential Scaling?

The Physicality attribute is generated using **4dF** (four Fudge dice), which creates a **bell curve distribution** centered on 10. This means:

- **10 is the most common value** (mode of the distribution)
- Values further from 10 become increasingly rare
- The probability drops off significantly for extreme values

Therefore, carrying capacity should **reward rare high values** and **penalize rare low values** more dramatically than a linear formula would.

## Formula Comparison

### Old Linear Formula

```
Max Weight = Physicality × 10 lbs
Max Volume = Physicality × 2 cu.ft.
```

**Problem**: Doesn't reflect the rarity of extreme values. A Physicality of 14 (very rare) only provides 40% more capacity than 10 (common).

### New Exponential Formula

```
Max Weight = 50 lbs × (1.15 ^ (Physicality - 10))
Max Volume = 10 cu.ft. × (1.15 ^ (Physicality - 10))
```

**Benefit**: Properly rewards rare high values and penalizes rare low values. A Physicality of 14 provides 75% more capacity than 10.

## Capacity Comparison Table

| Physicality | Probability | Old Weight | New Weight | Old Volume | New Volume | % Change |
|-------------|-------------|------------|------------|------------|------------|----------|
| 6 (very low)| ~1.2%       | 60 lbs     | **29 lbs** | 12 cu.ft.  | **6 cu.ft.**  | **-48%** |
| 7           | ~4.9%       | 70 lbs     | **33 lbs** | 14 cu.ft.  | **7 cu.ft.**  | **-53%** |
| 8 (low)     | ~12.3%      | 80 lbs     | **38 lbs** | 16 cu.ft.  | **8 cu.ft.**  | **-53%** |
| 9           | ~19.8%      | 90 lbs     | **43 lbs** | 18 cu.ft.  | **9 cu.ft.**  | **-52%** |
| **10 (avg)**| **23.5%**   | **100 lbs**| **50 lbs** | **20 cu.ft.** | **10 cu.ft.** | **-50%** |
| 11          | ~19.8%      | 110 lbs    | **58 lbs** | 22 cu.ft.  | **12 cu.ft.** | **-47%** |
| 12 (high)   | ~12.3%      | 120 lbs    | **66 lbs** | 24 cu.ft.  | **13 cu.ft.** | **-45%** |
| 13          | ~4.9%       | 130 lbs    | **76 lbs** | 26 cu.ft.  | **15 cu.ft.** | **-42%** |
| 14 (very high)| ~1.2%     | 140 lbs    | **87 lbs** | 28 cu.ft.  | **17 cu.ft.** | **-38%** |

## Relative Capacity Differences

The key insight is how much **more** or **less** capacity extreme values provide compared to average:

| Physicality | Old vs. Avg (10) | New vs. Avg (10) | Notes |
|-------------|------------------|------------------|-------|
| 6           | -40%             | **-42%**         | Very weak, struggles with basics |
| 8           | -20%             | **-24%**         | Below average, limited carrying |
| 10          | 0% (baseline)    | 0% (baseline)    | Average character |
| 12          | +20%             | **+32%**         | Strong, noticeable advantage |
| 14          | +40%             | **+75%**         | Very strong, dramatic advantage |

## Why 1.15 Scaling Factor?

The scaling factor of **1.15** (15% per point) was chosen to:

1. **Provide meaningful differentiation** between attribute values
2. **Reward rare high values** significantly (PHY 14 is ~75% more than PHY 10)
3. **Penalize rare low values** appropriately (PHY 6 is ~42% less than PHY 10)
4. **Maintain reasonable ranges** (29 to 87 lbs for PHY 6-14)
5. **Keep average capacity moderate** (50 lbs at PHY 10)

The scaling could be adjusted if needed:
- **1.10** (10% per point) = gentler curve, less dramatic differences
- **1.20** (20% per point) = steeper curve, more extreme differences
- **1.15** (15% per point) = balanced middle ground ✓

## Gameplay Impact

### Low Physicality (6-8)

**29-38 lbs capacity**
- Can carry basic equipment (weapon, light armor, small backpack)
- Must make careful choices about what to bring
- May need to leave treasure behind or make multiple trips
- Encourages lighter weapons (daggers vs. greatswords)
- Magical Bags of Holding become extremely valuable

### Average Physicality (9-11)

**43-58 lbs capacity**
- Can carry standard adventuring gear
- Room for some treasure and supplies
- Can use most weapons and medium armor
- Typical adventurer experience

### High Physicality (12-14)

**66-87 lbs capacity**
- Can carry heavy armor and large weapons
- Plenty of room for treasure
- Can help party members by carrying extra supplies
- Can use the heaviest equipment effectively
- Noticeable advantage in dungeon crawling

## Examples

### Weak Character (PHY 6)

**Max Capacity: ~29 lbs**

Typical loadout that maxes them out:
- Leather armor (10 lbs)
- Short sword (3 lbs)
- Small backpack (2 lbs)
  - Rations ×3 (3 lbs)
  - Waterskin (2 lbs)
  - Rope (5 lbs)
  - Torch ×2 (2 lbs)
- Small pouch (0.5 lbs)
  - Gold coins (1.5 lbs)

**Total: 29 lbs (100% capacity)**

This character is at their limit with basic gear and can't pick up much treasure!

### Average Character (PHY 10)

**Max Capacity: 50 lbs**

Typical loadout:
- Chain mail (25 lbs)
- Longsword (4 lbs)
- Shield (6 lbs)
- Backpack (3 lbs)
  - Rations ×5 (5 lbs)
  - Waterskin (2 lbs)
  - Rope (5 lbs)
  - Torch ×3 (3 lbs)
- Belt pouch (0.5 lbs)
  - Gold coins (2.5 lbs)

**Total: 56 lbs (112% capacity - slightly overweight)**

This character would need to drop something or use a container to manage their load.

### Strong Character (PHY 14)

**Max Capacity: ~87 lbs**

Typical loadout:
- Plate armor (45 lbs)
- Greatsword (6 lbs)
- Large backpack (5 lbs)
  - Rations ×10 (10 lbs)
  - Waterskin ×2 (4 lbs)
  - Rope (5 lbs)
  - Torch ×5 (5 lbs)
  - Treasure (10 lbs)
- Belt pouches ×2 (1 lb)
  - Gold coins ×200 (4 lbs)
  - Gems (2 lbs)

**Total: 97 lbs - WAIT, that's over!**

Actually recalculating:
- Plate + Sword + Pack = 56 lbs
- Contents = 34 lbs
**Total: 90 lbs** (103% - still a bit over!)

Even the strong character must make choices, but they have much more flexibility.

## Magical Container Impact

A **Bag of Holding** with 0.1 weight reduction is even more valuable now:

### Average Character (PHY 10) with Bag of Holding

**Max Capacity: 50 lbs**

- Bag of Holding (2 lbs base)
  - 100 lbs of treasure inside
  - Effective weight: 2 + (100 × 0.1) = **12 lbs**
- Chain mail (25 lbs)
- Longsword (4 lbs)
- Shield (6 lbs)
- Belt pouch with gold (3 lbs)

**Total: 50 lbs** - perfectly balanced with massive treasure capacity!

Without the magical bag, they could only carry 50 lbs total. With it, they effectively carry 150+ lbs of items.

## Design Notes

The exponential formula creates:

1. **Meaningful attribute differences** - High Physicality is valuable
2. **Trade-off decisions** - Even strong characters must choose gear carefully
3. **Magic item value** - Bags of Holding, Strength potions, etc. are highly desirable
4. **Character specialization** - Some characters are porters, others are not
5. **Realistic feel** - A person with PHY 6 really is much weaker than average

## Alternative Formulas Considered

### Option 1: Power Function
```
Max Weight = 10 × (Physicality ^ 1.2)
```
- PHY 6: 47 lbs
- PHY 10: 79 lbs  
- PHY 14: 120 lbs

**Rejected**: Numbers too high, less dramatic difference

### Option 2: Quadratic
```
Max Weight = (Physicality - 5) ^ 2
```
- PHY 6: 1 lb (too low!)
- PHY 10: 25 lbs
- PHY 14: 81 lbs

**Rejected**: Too extreme at low end

### Option 3: Exponential with 1.15 ✓
```
Max Weight = 50 × (1.15 ^ (Physicality - 10))
```
- PHY 6: 29 lbs ✓
- PHY 10: 50 lbs ✓
- PHY 14: 87 lbs ✓

**Selected**: Balanced, reflects 4dF distribution, creates meaningful choices

---

**Implementation Status**: ✅ Implemented in `ItemEntities.cs` → `CharacterInventory.CalculateMaxWeight/Volume`
