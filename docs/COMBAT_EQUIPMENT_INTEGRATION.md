# Combat Equipment Integration - Implementation Summary

**Status**: Equipment Integration Complete ✓
**Date**: January 5, 2026

## Overview

This document describes the integration of the item/equipment system with the combat system, allowing weapons and armor to affect combat calculations.

## What Was Implemented

### 1. Weapon Integration ✓

**Location**: `Mordecai.Web\Services\CombatService.cs:534-593`

The `GetWeaponSkillAsync` method now:
- Queries equipped weapons from the `Items` table
- Checks for MainHand, OffHand, or TwoHand slots
- Extracts weapon properties including:
  - `AttackValueModifier` - Added to attack roll
  - `BaseSuccessValueModifier` - Added to success value after hit
  - `DamageType` - Determines armor absorption type
  - `DamageClass` - Affects armor penetration
- Applies skill bonuses from weapon ItemSkillBonus entries
- Handles broken weapons (prevents use if `IsBroken = true`)
- Falls back to "Unarmed Combat" if no weapon equipped

**Combat Flow Changes**:
1. Attack skill = Base skill + Weapon AV modifier + Off-hand penalty (-2) + Timed penalties
2. Success value = (Attack roll - Defense roll) + Weapon SV modifier + Physicality bonus
3. Damage type from weapon determines which armor absorption applies

### 2. Armor Integration ✓

**Location**: `Mordecai.Web\Services\CombatService.cs:786-907`

The `ApplyDefensesAsync` method now:
- Queries all equipped armor pieces
- Filters armor covering the hit location (Head, Torso, Arms, Legs)
- Calculates total absorption from all armor layers:
  - Uses damage-type-specific absorption (Bashing, Cutting, Piercing, etc.)
  - Applies layer priority ordering
  - Handles weapon class vs armor class scaling
    - Higher class weapons penetrate lower class armor
    - Absorption reduced by (Weapon Class - Armor Class)
- Ignores broken armor (`IsBroken = true`)
- Reduces success value by total absorption

**Hit Location Coverage**:
- Armor coverage determined by `HitLocationCoverage` property or inferred from slot
- Supports comma-separated location lists (e.g., "Head,Torso,Arms")
- Handles common aliases (chest → torso, arms → arm, etc.)

### 3. Dodge Modifier Integration ✓

**Location**: `Mordecai.Web\Services\CombatService.cs:621-685`

The `GetDefenseSkillAsync` and `GetEquipmentDodgeModifierAsync` methods now:
- Apply dodge modifiers from equipped armor (`ArmorProperties.DodgeModifier`)
- Apply dodge modifiers from equipped weapons (`WeaponProperties.DodgeModifier`)
- Only apply when using dodge defense (not when parrying)
- Ignore broken equipment

**Examples**:
- Heavy armor: `-2 dodge modifier` (harder to move)
- Light rapier: `+1 dodge modifier` (encourages agility)
- Broken chainmail: `0 modifier` (provides nothing)

### 4. Broken Equipment Handling ✓

Equipment can break when `CurrentDurability` reaches 0:
- **Broken weapons**: Cannot attack, message shown to attacker
- **Broken armor**: Provides no absorption or dodge modifiers
- **All broken items**: Provide no skill bonuses or attribute modifiers

## Code Changes Summary

### Modified Files

**Mordecai.Web\Services\CombatService.cs**
- Line 144-175: Updated attack calculation to use weapon stats
- Line 222-241: Added weapon SV modifier and damage type integration
- Line 534-593: Rewrote `GetWeaponSkillAsync` to return `WeaponInfo` with full stats
- Line 621-650: Updated `GetDefenseSkillAsync` to apply equipment dodge modifiers
- Line 652-685: Added `GetEquipmentDodgeModifierAsync` method
- Line 786-907: Rewrote `ApplyDefensesAsync` to calculate armor absorption
- Line 871-907: Added `ArmorCoversLocation` helper method
- Line 1132-1141: Added `WeaponInfo` class with damage type/class support

### New Classes

```csharp
private class WeaponInfo
{
    public string SkillName { get; set; }
    public int CurrentLevel { get; set; }
    public int AttackValueModifier { get; set; }
    public int SuccessValueModifier { get; set; }
    public DamageType DamageType { get; set; }
    public DamageClass DamageClass { get; set; }
    public bool IsBroken { get; set; }
}
```

## Testing Checklist

### Manual Testing Needed

- [ ] Attack with equipped weapon vs unarmed
- [ ] Verify weapon attack/success modifiers apply correctly
- [ ] Test broken weapon prevents attacks
- [ ] Attack unarmored target (full damage)
- [ ] Attack armored target (reduced damage from absorption)
- [ ] Test layered armor (multiple pieces covering same location)
- [ ] Verify class scaling (Class 3 weapon vs Class 1 armor)
- [ ] Test dodge modifiers from armor/weapons
- [ ] Verify parry mode ignores dodge modifiers
- [ ] Test damage type interactions (Cutting vs Cutting absorption, etc.)

### Unit Test Scenarios (Future)

- Weapon AV/SV modifier calculations
- Armor absorption calculations
- Hit location coverage detection
- Damage class scaling
- Broken equipment handling
- Dodge modifier aggregation

## Combat Formulas (Updated)

### Attack Value
```
Attack Value = Base Skill
             + Weapon Skill Bonuses
             + Weapon AV Modifier
             + Off-Hand Penalty (-2 if dual wielding)
             + Timed Penalties
             + 4dF+
```

### Success Value
```
Success Value = (Attack Value - Defense Value)
              + Physicality Bonus (from RVS)
              + Weapon SV Modifier
```

### Damage After Armor
```
Absorption = Sum of (Armor Absorption for Damage Type - Class Modifier)
           where Class Modifier = max(0, Weapon Class - Armor Class)

Final SV = max(0, Success Value - Total Absorption)
```

### Defense Value (Dodge Mode)
```
Defense Value = Base Dodge Skill
              + Equipment Dodge Modifiers (weapons + armor)
              + 4dF+
```

## Dependencies

The combat-equipment integration requires:
- **Item System**: ItemTemplate, Item, WeaponTemplateProperties, ArmorTemplateProperties
- **Database**: Items table with IsEquipped, EquippedSlot, IsBroken fields
- **Equipment Service**: Existing but not required for combat calculations

## Performance Considerations

### Database Queries Per Attack

Each melee attack now performs:
1. Query for attacker's equipped weapon (MainHand/OffHand/TwoHand)
2. Query for defender's equipped weapon (if parrying)
3. Query for defender's equipped armor (all pieces)
4. Query for defender's equipment dodge modifiers

**Optimization Opportunities**:
- Cache equipped items per character (invalidate on equip/unequip)
- Use `.AsNoTracking()` for read-only queries
- Pre-load equipment during combat session creation
- Aggregate equipment stats into CombatParticipant entity

### Indexes Recommended

Existing indexes should cover these queries:
- `IX_Items_OwnerCharacterId_IsEquipped` (already exists)
- `IX_Items_EquippedSlot` (check if exists)

## Future Enhancements

### Short Term
- [ ] Durability damage on weapon/armor during combat
- [ ] Shield blocking mechanics (separate from armor absorption)
- [ ] Weapon range validation (melee vs ranged)
- [ ] Ammunition tracking for ranged weapons

### Long Term
- [ ] Weapon skill requirements (penalties if below minimum)
- [ ] Two-weapon fighting bonuses
- [ ] Armor encumbrance penalties
- [ ] Weapon special abilities (knockback, bleeding, etc.)
- [ ] Armor set bonuses
- [ ] Quality modifiers (masterwork, poor condition)

## Known Limitations

1. **No Durability Damage**: Equipment doesn't degrade during combat yet
2. **No Skill Requirements**: Characters can use any weapon/armor regardless of skill
3. **No Weight Penalties**: Heavy armor/weapons don't affect fatigue costs
4. **Simple Class Scaling**: Linear reduction, not percentage-based
5. **No Special Abilities**: Weapon/armor special effects not implemented

## Integration Testing

### Recommended Test Scenario

1. Create two test characters
2. Spawn weapon templates (sword, dagger, mace) with different stats
3. Spawn armor templates (leather, chainmail, plate) with different absorption
4. Equip character A with sword + leather armor
5. Equip character B with mace + chainmail armor
6. Initiate combat and verify:
   - Weapon stats affect attack values
   - Armor absorbs appropriate damage
   - Broken equipment is handled correctly
   - Dodge modifiers apply appropriately

## References

- **Game Specification**: `docs/MORDECAI_SPECIFICATION.md` (Combat section)
- **Item System**: `Mordecai.Game/Entities/ITEM_SYSTEM_README.md`
- **Database Schema**: `docs/DATABASE_DESIGN.md`
- **Combat Entities**: `Mordecai.Game/Entities/CombatEntities.cs`
- **Item Entities**: `Mordecai.Game/Entities/ItemEntities.cs`

---

**Implementation Complete**: January 5, 2026
**Next Step**: Test combat with spawned weapons and armor
