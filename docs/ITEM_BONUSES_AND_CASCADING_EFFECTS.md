# Item Bonuses and Cascading Effects

## Overview

Items can provide bonuses to characters through two mechanisms:

1. **Skill Bonuses** - Direct bonuses to specific skills (`ItemSkillBonus`)
2. **Attribute Modifiers** - Bonuses to base attributes (`ItemAttributeModifier`)

**Critical Understanding**: Attribute modifiers have **cascading effects** because they affect the base attribute, which in turn affects ALL skills that use that attribute in their ability score calculation.

## How Ability Scores Work

From the skill system, a character's ability with a skill is calculated as:

```
Ability Score (AS) = Related Attribute + Current Skill Level - 5
```

For example, if a character has:
- Physicality (STR) = 12
- Swords skill level = 5

Their Swords ability score = 12 + 5 - 5 = **12**

## Item Bonus Types

### 1. Skill Bonuses (ItemSkillBonus)

**Affects**: A single specific skill

**Bonus Types**:
- `FlatBonus` - Added directly to the skill's effective level
- `PercentageBonus` - Multiplies the skill's effectiveness (10 = 10% bonus)
- `CooldownReduction` - Reduces the skill's cooldown time in seconds

**Example - Sword with +2 Swords Bonus:**

```csharp
var magicSwordBonus = new ItemSkillBonus
{
    ItemTemplateId = magicSword.Id,
    SkillDefinitionId = swordsSkill.Id,
    BonusType = "FlatBonus",
    BonusValue = 2
};
```

**Effect**:
- Base: STR 12 + Swords 5 - 5 = AS 12
- With item: STR 12 + (Swords 5 + 2) - 5 = AS **14**

**Only affects Swords skill**

### 2. Attribute Modifiers (ItemAttributeModifier)

**Affects**: ALL skills that use that attribute

**Modifier Types**:
- `FlatBonus` - Added to the base attribute
- `PercentageBonus` - Multiplies the attribute (10 = 10% bonus)

**Example - Belt of Giants (+3 Physicality):**

```csharp
var beltModifier = new ItemAttributeModifier
{
    ItemTemplateId = beltOfGiants.Id,
    AttributeName = "STR",
    ModifierType = "FlatBonus",
    ModifierValue = 3
};
```

**Cascading Effect**:

Character has Physicality 12, and these skills:
- Swords (level 5, uses STR)
- Axes (level 3, uses STR)
- Athletics (level 4, uses STR)

**Without belt**:
- Swords AS: 12 + 5 - 5 = 12
- Axes AS: 12 + 3 - 5 = 10
- Athletics AS: 12 + 4 - 5 = 11

**With Belt of Giants (+3 STR)**:
- Effective Physicality: 12 + 3 = **15**
- Swords AS: 15 + 5 - 5 = **15** (+3)
- Axes AS: 15 + 3 - 5 = **13** (+3)
- Athletics AS: 15 + 4 - 5 = **14** (+3)

**Every STR-based skill improved by +3!**

## Stacking Multiple Bonuses

### Example: Equipped Items

Character has:
- Base Physicality: 10
- Swords skill level: 6

**Equipped items**:

1. **Enchanted Longsword** (+2 to Swords skill)
2. **Gauntlets of Strength** (+2 to Physicality)
3. **Belt of the Warrior** (+1 to Physicality)

**Calculation**:

```
Effective Physicality = 10 + 2 (gauntlets) + 1 (belt) = 13

Swords Ability Score:
  = Effective Physicality + (Swords Level + Sword Bonus) - 5
  = 13 + (6 + 2) - 5
  = 13 + 8 - 5
  = 16

Base (no items): 10 + 6 - 5 = 11
With items: 16

Total improvement: +5 (from +3 attribute and +2 skill bonus)
```

## Strategic Implications

### Attribute Items are More Valuable

**One +3 Physicality item** affects:
- All weapon skills (Swords, Axes, Maces, Polearms, etc.)
- Physical utility skills (Athletics, Climb, Swim, etc.)
- Carrying capacity (exponential scaling!)

**One +3 Swords skill item** affects:
- Only the Swords skill

### Trade-offs and Build Diversity

**Specialized Build** (Swordmaster):
- Sword with +5 Swords
- Armor with +3 Swords
- Ring with +2 Swords
- **Total**: +10 to Swords only

**Generalist Build** (Warrior):
- Belt with +3 Physicality
- Gauntlets with +2 Physicality
- **Total**: +5 to ALL Physicality-based skills

### Negative Modifiers (Cursed/Poor Quality)

Items can have negative bonuses:

```csharp
// Rusty sword - harder to use effectively
var rustySwordPenalty = new ItemSkillBonus
{
    SkillDefinitionId = swordsSkill.Id,
    BonusType = "FlatBonus",
    BonusValue = -2  // Penalty!
};

// Cursed gauntlets - weaken the wearer
var cursedGauntlets = new ItemAttributeModifier
{
    AttributeName = "STR",
    ModifierType = "FlatBonus",
    ModifierValue = -3  // Curse penalty!
};
```

The cursed gauntlets penalty cascades to **all** Physicality-based skills!

## Conditional Bonuses

Both bonus types support an optional `Condition` field:

```csharp
var dayBlade = new ItemSkillBonus
{
    SkillDefinitionId = swordsSkill.Id,
    BonusType = "FlatBonus",
    BonusValue = 5,
    Condition = "IsDaytime"  // Only active during day
};

var vampireAmulet = new ItemAttributeModifier
{
    AttributeName = "STR",
    ModifierType = "FlatBonus",
    ModifierValue = 3,
    Condition = "IsNighttime"  // Only active at night
};
```

## Percentage Bonuses

### Skill Percentage Bonus

Applied to the **final effectiveness** of the skill (use case: critical hits, damage multipliers):

```csharp
var criticalRing = new ItemSkillBonus
{
    SkillDefinitionId = swordsSkill.Id,
    BonusType = "PercentageBonus",
    BonusValue = 20  // 20% more effective
};
```

### Attribute Percentage Bonus

Applied to the **base attribute value**:

```csharp
var titanGrip = new ItemAttributeModifier
{
    AttributeName = "STR",
    ModifierType = "PercentageBonus",
    ModifierValue = 15  // 15% stronger
};

// Physicality 10 → 10 × 1.15 = 11.5 → 12 (rounded)
```

## Example Item Configurations

### Legendary Weapon (Multiple Bonuses)

```csharp
var excalibur = new ItemTemplate
{
    Name = "Excalibur",
    ItemType = ItemType.Weapon,
    WeaponType = WeaponType.Sword,
    Rarity = "Legendary"
};

// Direct skill bonus
new ItemSkillBonus
{
    ItemTemplateId = excalibur.Id,
    SkillDefinitionId = swordsSkill.Id,
    BonusType = "FlatBonus",
    BonusValue = 5
};

// Attribute bonus (affects all STR skills)
new ItemAttributeModifier
{
    ItemTemplateId = excalibur.Id,
    AttributeName = "STR",
    ModifierType = "FlatBonus",
    ModifierValue = 2
};

// Cooldown reduction
new ItemSkillBonus
{
    ItemTemplateId = excalibur.Id,
    SkillDefinitionId = powerStrikeSkill.Id,
    BonusType = "CooldownReduction",
    BonusValue = 2  // 2 seconds faster
};
```

**Total effect**:
- Swords skill: +5 direct + 2 from attribute = **+7 effective**
- All other STR skills: **+2**
- Power Strike cooldown: **-2 seconds**

### Ring of Intelligence

```csharp
var intelligenceRing = new ItemAttributeModifier
{
    ItemTemplateId = ringTemplate.Id,
    AttributeName = "INT",
    ModifierType = "FlatBonus",
    ModifierValue = 2
};
```

**Affects ALL INT-based skills**:
- All spell skills that use Reasoning
- Crafting skills
- Lore/knowledge skills
- Puzzle-solving abilities

### Armor Set with Mixed Bonuses

```csharp
// Helmet: +1 Awareness
// Chest: +2 Physicality
// Gloves: +3 to Swords
// Boots: +1 Dodge

// Net effect on Swords:
// = Base STR + Base Swords + Chest Bonus (STR) + Gloves Bonus (Swords) - 5
// = +2 from attribute, +3 from skill = +5 total to Swords AS
```

## Implementation Considerations

### When Calculating Ability Scores

```csharp
public int CalculateEffectiveAbilityScore(Character character, Skill skill)
{
    // 1. Get base attribute
    int baseAttribute = character.GetAttributeValue(skill.RelatedAttribute);
    
    // 2. Apply attribute modifiers from equipped items
    int attributeModifiers = GetEquippedAttributeModifiers(character, skill.RelatedAttribute);
    int effectiveAttribute = baseAttribute + attributeModifiers;
    
    // 3. Get base skill level
    int baseSkillLevel = skill.CurrentLevel;
    
    // 4. Apply skill bonuses from equipped items
    int skillBonuses = GetEquippedSkillBonuses(character, skill.Id);
    int effectiveSkillLevel = baseSkillLevel + skillBonuses;
    
    // 5. Calculate final AS
    return effectiveAttribute + effectiveSkillLevel - 5;
}
```

### Caching Strategy

Attribute modifiers affect multiple skills, so:

1. **Cache equipped item bonuses** when items are equipped/unequipped
2. **Invalidate cache** when equipment changes
3. **Recalculate on-demand** for non-combat situations
4. **Pre-calculate for combat** to avoid per-action overhead

### Display to Players

Show the breakdown:

```
Swords Ability Score: 16
  Base Physicality: 10
  + Belt of Giants: +3 (STR)
  + Gauntlets: +2 (STR)
  = Effective STR: 15
  
  Base Swords Level: 6
  + Enchanted Sword: +2
  = Effective Swords: 8
  
  Final AS: 15 + 8 - 5 = 16
```

## Carrying Capacity Impact

Don't forget: Physicality bonuses also affect carrying capacity (exponential)!

```
Base PHY 10: 50 lbs capacity
With +3 from belt: PHY 13 = ~76 lbs capacity (+52% capacity!)
```

This is a **huge** side benefit of Physicality-boosting items.

## Database Queries

### Get All Active Bonuses for Character

```csharp
// Get equipped items with bonuses
var equippedItems = await context.Items
    .Include(i => i.ItemTemplate)
        .ThenInclude(it => it.SkillBonuses)
    .Include(i => i.ItemTemplate)
        .ThenInclude(it => it.AttributeModifiers)
    .Where(i => i.OwnerCharacterId == characterId && i.IsEquipped)
    .ToListAsync();

// Extract all skill bonuses
var skillBonuses = equippedItems
    .SelectMany(i => i.ItemTemplate.SkillBonuses)
    .GroupBy(b => b.SkillDefinitionId)
    .ToDictionary(g => g.Key, g => g.Sum(b => b.BonusValue));

// Extract all attribute modifiers
var attributeMods = equippedItems
    .SelectMany(i => i.ItemTemplate.AttributeModifiers)
    .GroupBy(m => m.AttributeName)
    .ToDictionary(g => g.Key, g => g.Sum(m => m.ModifierValue));
```

## Summary

**Key Takeaways**:

1. ✅ **Skill bonuses** affect only one skill
2. ✅ **Attribute modifiers cascade** to ALL skills using that attribute
3. ✅ **Stacking works** - multiple items can provide bonuses to the same skill/attribute
4. ✅ **Negative bonuses** (cursed items, poor quality) are supported
5. ✅ **Conditional bonuses** can be time/situation dependent
6. ✅ **Percentage bonuses** multiply effectiveness
7. ✅ **Attribute bonuses are more valuable** due to cascading effects
8. ✅ **Carrying capacity** benefits from Physicality bonuses (exponential scaling!)

This creates rich strategic choices for players in item selection and character builds!

---

**Status**: Entity definitions complete ✓ | Service implementation pending (future phase)
