# Item System Quick Reference

## Key Entity Types

- **ItemTemplate** - Blueprint for items (design-time)
- **Item** - Actual item instance (runtime)
- **ItemSkillBonus** - Skill bonuses from equipment
- **ItemAttributeModifier** - Attribute bonuses from equipment
- **CharacterInventory** - Character capacity tracking

## Item Location States

An item instance is ALWAYS in exactly one of these states:

1. **In Room**: `CurrentRoomId` set, others null
2. **In Inventory**: `OwnerCharacterId` set, `CurrentRoomId` null
3. **In Container**: `ContainerItemId` set (may also have `OwnerCharacterId`)

## Carrying Capacity Formula

Exponential scaling (reflects 4dF bell curve):

```
Max Weight = 50 lbs × (1.15 ^ (Physicality - 10))
Max Volume = 10 cu.ft. × (1.15 ^ (Physicality - 10))

Examples: PHY 6 = ~29 lbs | PHY 10 = 50 lbs | PHY 14 = ~87 lbs
```

## Common Item Types

| Type | Description | Examples |
|------|-------------|----------|
| Weapon | Combat items | Swords, bows, staves |
| Armor | Protective gear | Helmets, chest plates |
| Container | Holds other items | Bags, quivers, chests |
| Consumable | Single-use | Potions, scrolls |
| Food/Drink | Restores hunger/thirst | Bread, water |
| Treasure | Valuable items | Gold, gems |
| Key | Opens locks | Door keys, chest keys |
| Magic | Enchanted items | Wands, artifacts |

## Equipment Slots

```
Head, Neck, Shoulders, Chest, Back, Wrists, Hands, Waist, Legs, Feet,
FingerLeft, FingerRight, MainHand, OffHand, TwoHand
```

## Container Properties

```csharp
IsContainer = true
ContainerMaxWeight = 50m    // Max weight it can hold
ContainerMaxVolume = 10m    // Max volume it can hold
ContainerAllowedTypes = ""  // Empty = any, or "Weapon,Armor", "Arrow,Bolt"
```

## Item Bonus Types

**Skill Bonuses:**
- `FlatBonus` - +X to skill level
- `PercentageBonus` - +X% effectiveness
- `CooldownReduction` - -X seconds cooldown

**Attribute Modifiers:**
- `FlatBonus` - +X to attribute
- `PercentageBonus` - +X% to attribute

## Common Queries

### Get all items in a character's inventory

```csharp
var items = await context.Items
    .Include(i => i.ItemTemplate)
    .Where(i => i.OwnerCharacterId == characterId)
    .ToListAsync();
```

### Get all equipped items

```csharp
var equipped = await context.Items
    .Include(i => i.ItemTemplate)
    .Where(i => i.OwnerCharacterId == characterId && i.IsEquipped)
    .ToListAsync();
```

### Get items in a room

```csharp
var roomItems = await context.Items
    .Include(i => i.ItemTemplate)
    .Where(i => i.CurrentRoomId == roomId)
    .ToListAsync();
```

### Get items in a container

```csharp
var contents = await context.Items
    .Include(i => i.ItemTemplate)
    .Where(i => i.ContainerItemId == containerId)
    .ToListAsync();
```

### Calculate total inventory weight

```csharp
var totalWeight = await context.Items
    .Include(i => i.ItemTemplate)
    .Where(i => i.OwnerCharacterId == characterId)
    .SumAsync(i => i.ItemTemplate.Weight * i.StackSize);
```

### Check if container has space

```csharp
var container = await context.Items
    .Include(i => i.ItemTemplate)
    .Include(i => i.ContainedItems)
        .ThenInclude(ci => ci.ItemTemplate)
    .FirstOrDefaultAsync(i => i.Id == containerId);

var currentWeight = container.ContainedItems.Sum(i => i.TotalWeight);
var currentVolume = container.ContainedItems.Sum(i => i.TotalVolume);

bool canFit = 
    currentWeight + newItem.TotalWeight <= container.ItemTemplate.ContainerMaxWeight &&
    currentVolume + newItem.TotalVolume <= container.ItemTemplate.ContainerMaxVolume;
```

### Check container type restrictions

```csharp
bool isAllowed = true;
if (!string.IsNullOrEmpty(container.ItemTemplate.ContainerAllowedTypes))
{
    var allowedTypes = container.ItemTemplate.ContainerAllowedTypes
        .Split(',')
        .Select(t => t.Trim());
    
    isAllowed = allowedTypes.Contains(newItem.ItemTemplate.ItemType.ToString());
}
```

## Item State Transitions

```
Spawned (in room)
  ↓ pickup
Inventory (character owns, not equipped)
  ↓ equip
Equipped (in slot, providing bonuses)
  ↓ unequip
Inventory
  ↓ put in container
Container (nested storage)
  ↓ take from container
Inventory
  ↓ drop
Spawned (back in room)
```

## Binding Rules

- **BindOnPickup**: Item binds when `OwnerCharacterId` is first set
- **BindOnEquip**: Item binds when `IsEquipped` is first set to true
- **IsBound** = true means item cannot be traded or dropped

## Durability

- Items with `HasDurability = true` have `MaxDurability`
- Instances track `CurrentDurability`
- When `CurrentDurability <= 0`, item is broken (`IsBroken` property)
- Broken items may need repair or become unusable

## Rarity Tiers

1. Common (white/gray)
2. Uncommon (green)
3. Rare (blue)
4. Epic (purple)
5. Legendary (orange/gold)

## Important Calculated Properties

On `Item` entity:

```csharp
TotalWeight  // Weight including all contained items (recursive)
TotalVolume  // Volume including all contained items (recursive)
EffectiveName // CustomName ?? ItemTemplate.Name
IsBroken     // CurrentDurability <= 0
```

## Service Layer Responsibilities

Future services to implement:

- **ItemService** - CRUD operations, spawning, moving items
- **InventoryService** - Capacity checks, organization
- **ContainerService** - Container management, nesting validation
- **EquipmentService** - Equipping, unequipping, slot management
- **ItemBonusService** - Calculate active bonuses from equipped items

## See Also

- [ITEM_SYSTEM_README.md](./ITEM_SYSTEM_README.md) - Full documentation
- [ItemEntities.cs](./ItemEntities.cs) - Entity definitions
