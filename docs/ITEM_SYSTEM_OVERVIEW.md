# Item System Overview

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         ITEM SYSTEM                              │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────┐         ┌──────────────────┐
│  ItemTemplate   │◄───────┤  ItemSkillBonus   │
│  (Blueprint)    │         └──────────────────┘
│                 │         ┌──────────────────┐
│ - Name          │◄───────┤ ItemAttribute-   │
│ - Weight        │         │ Modifier         │
│ - Volume        │         └──────────────────┘
│ - IsContainer   │
└────────┬────────┘
         │ 1:N
         │
         ▼
┌─────────────────┐
│   Item          │         ┌──────────────────┐
│   (Instance)    │◄───────┤  Room            │
│                 │ N:1     └──────────────────┘
│ - StackSize     │
│ - Durability    │         ┌──────────────────┐
│ - IsEquipped    │◄───────┤  Character       │
│ - Location:     │ N:1     │                  │
│   • Room        │         │ Inventory:       │
│   • Character   │         │  MaxWeight       │
│   • Container   │◄──┐     │  MaxVolume       │
└─────────────────┘   │     └──────────────────┘
         │            │
         │ Self-ref   │
         │ (nesting)  │
         └────────────┘
```

## Item Location States

```
┌──────────────┐
│   In Room    │  CurrentRoomId = 123, OwnerCharacterId = null
└──────┬───────┘
       │ pickup
       ▼
┌──────────────┐
│  In Inventory│  OwnerCharacterId = abc, CurrentRoomId = null
└──────┬───────┘
       │ equip
       ▼
┌──────────────┐
│   Equipped   │  IsEquipped = true, EquippedSlot = MainHand
└──────┬───────┘
       │ unequip
       ▼
┌──────────────┐
│  In Inventory│
└──────┬───────┘
       │ put in container
       ▼
┌──────────────┐
│ In Container │  ContainerItemId = xyz
└──────┬───────┘
       │ take from container
       ▼
┌──────────────┐
│  In Inventory│
└──────┬───────┘
       │ drop
       ▼
┌──────────────┐
│   In Room    │
└──────────────┘
```

## Container Hierarchy Example

```
Character (Physicality = 12, MaxWeight = ~66 lbs, MaxVolume = ~13 cu.ft.)
│
├─ Large Backpack (weight: 5 lbs, capacity: 50 lbs / 10 cu.ft.)
│  ├─ Small Pouch (weight: 0.5 lbs, capacity: 5 lbs / 1 cu.ft.)
│  │  ├─ Gold Coins (×100, weight: 2 lbs, volume: 0.1 cu.ft.)
│  │  └─ Gems (×3, weight: 0.3 lbs, volume: 0.05 cu.ft.)
│  ├─ Rope (50 ft, weight: 10 lbs, volume: 2 cu.ft.)
│  ├─ Torch (×5, weight: 5 lbs, volume: 1 cu.ft.)
│  └─ Rations (×10, weight: 10 lbs, volume: 2 cu.ft.)
│
├─ Belt Pouch (weight: 0.3 lbs, capacity: 3 lbs / 0.5 cu.ft.)
│  ├─ Lockpicks (weight: 0.2 lbs)
│  └─ Small Key (weight: 0.1 lbs)
│
├─ Quiver (weight: 1 lb, capacity: 5 lbs / 2 cu.ft., restriction: "Arrow,Bolt")
│  └─ Arrows (×30, weight: 3 lbs, volume: 0.3 cu.ft.)
│
└─ Equipped Items (not in containers)
   ├─ Longsword (MainHand, weight: 4 lbs)
   ├─ Shield (OffHand, weight: 6 lbs)
   ├─ Leather Armor (Chest, weight: 10 lbs)
   └─ Boots (Feet, weight: 2 lbs)

Total Inventory Weight:
  Backpack: 5 + (0.5 + 2.3) + 10 + 5 + 10 = 32.8 lbs
  Belt Pouch: 0.3 + 0.3 = 0.6 lbs
  Quiver: 1 + 3 = 4 lbs
  Equipped: 4 + 6 + 10 + 2 = 22 lbs
  TOTAL: 59.4 lbs / ~66 lbs (90% capacity - getting heavy!)
```

## Capacity Calculations

### Character Base Capacity

Uses exponential scaling to reflect 4dF bell curve distribution:

```
Physicality = 12

Max Weight = 50 × (1.15 ^ (12 - 10)) = 50 × 1.3225 = ~66.1 lbs
Max Volume = 10 × (1.15 ^ (12 - 10)) = 10 × 1.3225 = ~13.2 cu.ft.

Comparison:
  PHY  6: ~29 lbs, ~6 cu.ft.  (very weak, rare roll)
  PHY 10:  50 lbs, 10 cu.ft.  (average, most common)
  PHY 14: ~87 lbs, ~17 cu.ft. (very strong, rare roll)
```

### Item with Container

```
Backpack
  Base Weight: 5 lbs
  Base Volume: 1 cu.ft.
  
  Contains:
    - Rope: 10 lbs
    - Food: 5 lbs
  
  Total Weight = 5 + 10 + 5 = 20 lbs
  Total Volume = 1 + 2 + 1 = 4 cu.ft.
```

### Capacity Check

```csharp
bool CanCarry(Character character, Item item)
{
    var currentWeight = character.GetTotalInventoryWeight();
    var currentVolume = character.GetTotalInventoryVolume();
    
    return (currentWeight + item.TotalWeight <= character.MaxWeight) &&
           (currentVolume + item.TotalVolume <= character.MaxVolume);
}
```

## Equipment Bonuses Flow

```
Character
  Physicality = 10
  Swords Skill Level = 5
  
  ↓ equips
  
Magic Longsword
  ItemSkillBonus:
    - Skill: Swords
    - BonusType: FlatBonus
    - BonusValue: 2
  
  ItemAttributeModifier:
    - Attribute: STR
    - ModifierType: FlatBonus
    - ModifierValue: 1
  
  ↓ result
  
Character (with bonuses)
  Effective Physicality = 10 + 1 = 11
  Effective Swords Skill = 5 + 2 = 7
  Ability Score = 11 + 7 - 5 = 13
```

## Item Type Use Cases

| Type | Examples | Stackable? | Has Durability? | Can Equip? |
|------|----------|------------|-----------------|------------|
| Weapon | Sword, Bow, Dagger | ❌ | ✅ | ✅ |
| Armor | Helmet, Chest Plate | ❌ | ✅ | ✅ |
| Container | Backpack, Quiver | ❌ | ❌ | Maybe¹ |
| Consumable | Potion, Scroll | ✅ | ❌ | ❌ |
| Treasure | Gold, Gems | ✅ | ❌ | ❌ |
| Key | Door Key, Chest Key | ❌ | ❌ | ❌ |
| Magic | Wand, Ring | ❌ | ✅² | ✅ |
| Food/Drink | Bread, Water | ✅ | ❌ | ❌ |
| Tool | Lockpick, Torch | Maybe | ✅ | Maybe¹ |

¹ Some containers/tools may be wearable (backpacks on Back slot)
² Magic items may or may not have durability/charges

## Database Entity Counts

```
ItemTemplates:        ~100-500  (design-time, grows slowly)
Items:                ~10,000+  (runtime, grows with play)
ItemSkillBonuses:     ~50-200   (design-time, per template)
ItemAttributeMods:    ~50-200   (design-time, per template)
CharacterInventories: ~1 per character
```

## Query Patterns

### Most Common Queries

```sql
-- Get character's inventory
SELECT * FROM Items 
WHERE OwnerCharacterId = @characterId 
  AND ContainerItemId IS NULL;

-- Get equipped items
SELECT * FROM Items 
WHERE OwnerCharacterId = @characterId 
  AND IsEquipped = 1;

-- Get items in a room
SELECT * FROM Items 
WHERE CurrentRoomId = @roomId;

-- Get items in a container
SELECT * FROM Items 
WHERE ContainerItemId = @containerId;

-- Get item with full details
SELECT i.*, it.*, isb.*, iam.*
FROM Items i
JOIN ItemTemplates it ON i.ItemTemplateId = it.Id
LEFT JOIN ItemSkillBonuses isb ON it.Id = isb.ItemTemplateId
LEFT JOIN ItemAttributeModifiers iam ON it.Id = iam.ItemTemplateId
WHERE i.Id = @itemId;
```

## Service Responsibilities

```
ItemService
  - CRUD for ItemTemplates
  - Spawn/Destroy Items
  - Query items by location

InventoryService
  - Capacity checks
  - Pickup/Drop operations
  - Weight/Volume calculations

ContainerService
  - Container validation
  - Put/Take operations
  - Nesting checks

EquipmentService
  - Equip/Unequip operations
  - Slot management
  - Bonus calculations
```

## Integration Points

```
┌────────────────┐
│ Combat System  │──→ Uses equipped weapon stats
└────────────────┘   Applies equipment bonuses
                     Damages item durability

┌────────────────┐
│ Skill System   │──→ Reads ItemSkillBonus
└────────────────┘   Applies to ability scores

┌────────────────┐
│ Character Sys  │──→ Reads ItemAttributeModifier
└────────────────┘   Affects attributes/capacity

┌────────────────┐
│ Room System    │──→ Shows items in room
└────────────────┘   Handles item spawning

┌────────────────┐
│ Messaging      │──→ Publishes item events
└────────────────┘   (pickup, drop, equip, etc.)
```

## Next Steps Summary

1. **Create Database Migration** ← START HERE
   ```bash
   dotnet ef migrations add AddItemAndInventorySystem
   dotnet ef database update
   ```

2. **Implement Core Services**
   - ItemService (item CRUD)
   - InventoryService (capacity/pickup/drop)
   - ContainerService (container logic)
   - EquipmentService (equip/unequip)

3. **Create Commands**
   - get/take, drop, inventory, equipment, equip, unequip, put, examine

4. **Build Admin UI**
   - Item template creator
   - Item spawner

5. **Integrate with Play UI**
   - Inventory display
   - Equipment paper doll
   - Container viewer

---

See [ITEM_IMPLEMENTATION_STATUS.md](./ITEM_IMPLEMENTATION_STATUS.md) for detailed implementation plan.
