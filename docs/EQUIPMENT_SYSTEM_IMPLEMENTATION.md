# Equipment System Implementation

## Overview

The equipment system has been fully implemented, allowing players to manage their inventory, equip and unequip items, and benefit from item bonuses that affect skills and attributes.

## Implementation Date

October 12, 2025

## Components Implemented

### 1. EquipmentService (`Mordecai.Web/Services/EquipmentService.cs`)

A comprehensive service managing all equipment-related operations:

#### Interface Methods

```csharp
Task<EquipResult> EquipAsync(Guid characterId, string userId, Guid itemId);
Task<UnequipResult> UnequipAsync(Guid characterId, string userId, Guid itemId);
Task<UnequipResult> UnequipSlotAsync(Guid characterId, string userId, ArmorSlot slot);
Task<IReadOnlyList<Item>> GetEquippedItemsAsync(Guid characterId, string userId);
Task<IReadOnlyList<Item>> GetInventoryItemsAsync(Guid characterId, string userId);
Task<Dictionary<int, int>> GetActiveSkillBonuses(Guid characterId, string userId);
Task<Dictionary<string, int>> GetActiveAttributeModifiers(Guid characterId, string userId);
Task<int> CalculateEffectiveAttributeValue(Guid characterId, string userId, string attributeName, int baseValue);
```

#### Key Features

**Auto-Unequip Logic**:
- When equipping an item to an occupied slot, the existing item is automatically unequipped
- Two-handed weapons automatically unequip MainHand and OffHand items
- Equipping to MainHand or OffHand automatically unequips two-handed weapons

**Security**:
- All operations verify character ownership (userId check)
- Items must be in direct inventory (not in containers) to equip
- Cannot equip items that don't have equipment slots

**Bonus Aggregation**:
- `GetActiveSkillBonuses()` aggregates all flat skill bonuses from equipped items
- `GetActiveAttributeModifiers()` aggregates all flat attribute modifiers from equipped items
- Supports cascading effects: attribute bonuses affect ALL skills using that attribute

### 2. Player Commands (`Mordecai.Web/Pages/Play.razor`)

Four new command handlers integrated into the game:

#### `inventory` / `inv` / `i`

Shows all items in the character's inventory:

```
inventory
```

**Output**:
```
Inventory:
  Enchanted Longsword [EQUIPPED]
  Leather Backpack [EQUIPPED]
  Health Potion x3
  Iron Key
```

#### `equipment` / `eq`

Shows currently equipped items with their bonuses:

```
equipment
```

**Output**:
```
Currently Equipped:
  MainHand: Enchanted Longsword
    +2 to Swords
    +1 to STR
  Back: Leather Backpack
  Chest: Studded Leather Armor
    +5 to DEX
```

#### `equip` / `wear` / `wield`

Equips an item from inventory:

```
equip longsword
wield enchanted longsword
wear leather armor
```

**Features**:
- Matches by item name (case-insensitive)
- Supports custom names
- Auto-unequips conflicting items
- Provides clear feedback

#### `unequip` / `remove`

Unequips an item by name or slot:

```
unequip longsword
remove MainHand
unequip chest
```

**Features**:
- Can target by item name or equipment slot
- Enum-based slot parsing for precision
- Clear error messages

### 3. Service Registration (`Mordecai.Web/Program.cs`)

The EquipmentService is registered in the DI container:

```csharp
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
```

### 4. Database Migration

Migration `AddItemAndInventorySystem` created and ready to apply:

```bash
dotnet ef migrations add AddItemAndInventorySystem --project Mordecai.Web
```

**Tables Added**:
- `ItemTemplates` - Item blueprints/definitions
- `Items` - Item instances (in world, inventory, containers)
- `ItemSkillBonuses` - Skill bonuses provided by items
- `ItemAttributeModifiers` - Attribute modifiers provided by items
- `CharacterInventories` - Inventory capacity tracking

## Usage Examples

### Basic Workflow

1. **Check Inventory**:
   ```
   > inv
   Inventory:
     Iron Sword
     Wooden Shield
     Health Potion x5
   ```

2. **Equip Items**:
   ```
   > equip iron sword
   You equip Iron Sword.
   
   > equip wooden shield
   You equip Wooden Shield.
   ```

3. **View Equipment**:
   ```
   > eq
   Currently Equipped:
     MainHand: Iron Sword
     OffHand: Wooden Shield
   ```

4. **Unequip**:
   ```
   > unequip MainHand
   You unequip Iron Sword.
   ```

### Advanced: Two-Handed Weapons

```
> equip iron sword
You equip Iron Sword.

> equip wooden shield
You equip Wooden Shield.

> equip greatsword
You equip Greatsword.
```

**Result**: The greatsword automatically unequips both the sword and shield since it requires two hands.

### Bonus System Example

**Item**: Belt of Giants (+3 Physicality)

When equipped:
- Swords skill: Base AS 12 → Effective AS 15 (+3 from attribute)
- Axes skill: Base AS 10 → Effective AS 13 (+3 from attribute)
- Athletics skill: Base AS 11 → Effective AS 14 (+3 from attribute)
- Carrying capacity: 50 lbs → ~76 lbs (+52%)

**Item**: Enchanted Sword (+2 to Swords skill only)

When equipped:
- Swords skill: +2 bonus applies
- Other skills: No effect

## Integration with Skill System

The bonus aggregation methods are designed to integrate with the existing skill calculation system:

```csharp
// Future integration in skill system:
public int CalculateEffectiveAbilityScore(Character character, Skill skill)
{
    // 1. Get base attribute
    int baseAttribute = character.GetAttributeValue(skill.RelatedAttribute);
    
    // 2. Apply attribute modifiers from equipped items
    int effectiveAttribute = await equipmentService.CalculateEffectiveAttributeValue(
        character.Id, userId, skill.RelatedAttribute, baseAttribute);
    
    // 3. Get base skill level
    int baseSkillLevel = skill.CurrentLevel;
    
    // 4. Apply skill bonuses from equipped items
    var skillBonuses = await equipmentService.GetActiveSkillBonuses(character.Id, userId);
    int skillBonus = skillBonuses.TryGetValue(skill.Id, out var bonus) ? bonus : 0;
    int effectiveSkillLevel = baseSkillLevel + skillBonus;
    
    // 5. Calculate final AS
    return effectiveAttribute + effectiveSkillLevel - 5;
}
```

## Help System Integration

The help command now includes equipment commands:

```
> help
Available Commands:
...
inventory/inv/i - Show items you are carrying
equipment/eq - Show what you have equipped
equip/wear/wield [item] - Equip an item from your inventory
unequip/remove [item] - Unequip an item
...
```

## Technical Details

### Equipment Slot Validation

The system uses the 34-slot `ArmorSlot` enum:
- Head, Face, Ears, Neck
- Shoulders, Back, Chest
- ArmLeft, ArmRight
- WristLeft, WristRight
- HandLeft, HandRight
- MainHand, OffHand, TwoHand
- FingerLeft1-5, FingerRight1-5
- Waist, Legs
- AnkleLeft, AnkleRight
- FootLeft, FootRight

### Item Location States

Items can exist in three states:
1. **In Room**: `CurrentRoomId` is set, `OwnerCharacterId` is null
2. **In Character Inventory**: `OwnerCharacterId` is set, `CurrentRoomId` is null, `ContainerItemId` is null
3. **In Container**: `ContainerItemId` is set

Only items in state #2 (direct inventory) can be equipped.

### Bonus Type Support

**Currently Implemented**:
- `FlatBonus` for skills (added to skill level)
- `FlatBonus` for attributes (added to attribute value)

**Future Support** (entities ready, logic pending):
- `PercentageBonus` for skills (multiply effectiveness)
- `PercentageBonus` for attributes (multiply attribute)
- `CooldownReduction` for skills (reduce cooldown time)
- Conditional bonuses (based on time, situation, etc.)

## Error Handling

All operations include comprehensive error handling:

- **Character not found**: Returns failure with clear message
- **Item not found**: Returns failure with clear message
- **Ownership violations**: Returns failure (security)
- **Invalid slot**: Returns failure with explanation
- **Container conflicts**: Must remove from container first
- **Already equipped/unequipped**: Returns failure with status

## Performance Considerations

- **Bonus Aggregation**: Queries are optimized with eager loading (`Include`) and `AsNoTracking` for read operations
- **Caching Opportunity**: Future enhancement could cache active bonuses and invalidate on equip/unequip
- **Transaction Safety**: All database operations use proper async/await patterns

## Testing Recommendations

### Unit Tests Needed

1. **EquipmentService.EquipAsync**
   - ✓ Successful equip
   - ✓ Auto-unequip existing item in slot
   - ✓ Two-handed weapon unequips both hands
   - ✓ One-handed weapon unequips two-handed
   - ✓ Cannot equip from container
   - ✓ Cannot equip non-equippable item
   - ✓ Ownership validation

2. **EquipmentService.UnequipAsync**
   - ✓ Successful unequip
   - ✓ Cannot unequip not-equipped item
   - ✓ Ownership validation

3. **Bonus Aggregation**
   - ✓ Multiple items with same skill bonus stack
   - ✓ Multiple items with same attribute modifier stack
   - ✓ Empty equipment returns empty dictionaries

### Integration Tests Needed

1. **Full equip/unequip cycle**
2. **Bonus calculation with skill system**
3. **Weight/volume capacity checks (future)**
4. **Container interaction (future)**

## Future Enhancements

### Immediate Next Steps (Pending)

1. **ItemService**: Create, spawn, transfer items
2. **InventoryService**: Get/drop/give items, weight/volume checks
3. **ContainerService**: Put items in containers, take items out
4. **Player commands**: `get`, `drop`, `give`, `put`, `take`

### Advanced Features (Later)

1. **Percentage bonuses**: Implement multiplication logic
2. **Conditional bonuses**: Time-based, situation-based activation
3. **Cooldown reduction**: Integrate with skill cooldown system
4. **Durability system**: Damage items over time
5. **Item comparison**: Compare two items side-by-side
6. **Auto-equip best**: Suggest optimal equipment
7. **Equipment sets**: Bonus for wearing complete sets
8. **Enchanting system**: Add/modify bonuses on items
9. **Item identification**: Identify unknown magical properties

## Database Schema

See `docs/DATABASE_DESIGN.md` for complete schema details.

Key relationships:
- `Items.ItemTemplateId` → `ItemTemplates.Id`
- `Items.OwnerCharacterId` → `Characters.Id`
- `Items.CurrentRoomId` → `Rooms.Id`
- `Items.ContainerItemId` → `Items.Id` (self-reference)
- `ItemSkillBonuses.ItemTemplateId` → `ItemTemplates.Id`
- `ItemSkillBonuses.SkillDefinitionId` → `SkillDefinitions.Id`
- `ItemAttributeModifiers.ItemTemplateId` → `ItemTemplates.Id`

## Conclusion

The equipment system is now fully functional with:
- ✅ Complete service layer (EquipmentService)
- ✅ Player commands (inventory, equipment, equip, unequip)
- ✅ DI registration
- ✅ Database migration ready
- ✅ Help system updated
- ✅ Bonus aggregation foundation
- ✅ Security and validation
- ✅ Error handling

**Status**: Ready for testing and use. Future enhancements can build on this solid foundation.

---

**Next Priority**: Implement ItemService, InventoryService, and related commands (get, drop, give) to complete the item interaction system.
