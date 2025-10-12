# Item and Inventory System - Implementation Status

**Status**: Foundation Complete ✓  
**Date**: October 12, 2025

## What's Been Completed

### 1. Entity Definitions ✓

Created `Mordecai.Game\Entities\ItemEntities.cs` with:

- **ItemTemplate** - Item blueprints with 30+ properties
- **Item** - Item instances with location, state, ownership tracking
- **ItemSkillBonus** - Skill bonuses from equipped items
- **ItemAttributeModifier** - Attribute bonuses from equipped items
- **CharacterInventory** - Character capacity tracking

**Key Features Implemented:**
- Weight and volume system
- Container system with type restrictions
- Equipment slots (15 different slots)
- Item binding (BindOnPickup, BindOnEquip)
- Durability tracking
- Stackable items
- Rarity tiers
- Custom properties (JSON) for flexibility
- Recursive weight/volume calculations

### 2. Database Integration ✓

Updated `Mordecai.Web\Data\ApplicationDbContext.cs`:

- Added DbSets for all item entities
- Configured entity relationships and foreign keys
- Added indexes for performance
- Set up precision for decimal fields
- Configured cascading delete behaviors

### 3. Documentation ✓

Created comprehensive documentation:

- **ITEM_SYSTEM_README.md** - Full system documentation with examples
- **ITEM_SYSTEM_QUICK_REF.md** - Quick reference for developers
- **DATABASE_DESIGN.md** - Updated with item schema
- **MORDECAI_SPECIFICATION.md** - Updated with item/inventory design

## What Needs to Be Done Next

### Phase 1: Database Migration (REQUIRED FIRST)

```bash
# Create migration for item system
cd Mordecai.Web
dotnet ef migrations add AddItemAndInventorySystem

# Review the generated migration
# Apply migration
dotnet ef database update
```

**Note**: This will add ~5 new tables to the database.

### Phase 2: Core Services

Create service layer in `Mordecai.Web\Services\`:

#### IItemService & ItemService

```csharp
public interface IItemService
{
    // Template management
    Task<ItemTemplate?> GetItemTemplateAsync(int templateId);
    Task<IReadOnlyList<ItemTemplate>> GetAllItemTemplatesAsync();
    Task<ItemTemplate> CreateItemTemplateAsync(ItemTemplate template);
    
    // Instance management
    Task<Item?> GetItemAsync(Guid itemId);
    Task<Item> SpawnItemAsync(int templateId, int? roomId = null, Guid? ownerId = null);
    Task<bool> DestroyItemAsync(Guid itemId);
    
    // Item queries
    Task<IReadOnlyList<Item>> GetItemsInRoomAsync(int roomId);
    Task<IReadOnlyList<Item>> GetCharacterItemsAsync(Guid characterId);
    Task<IReadOnlyList<Item>> GetEquippedItemsAsync(Guid characterId);
}
```

#### IInventoryService & InventoryService

```csharp
public interface IInventoryService
{
    // Capacity management
    Task<CharacterInventory> GetOrCreateInventoryAsync(Guid characterId);
    Task UpdateInventoryCapacityAsync(Guid characterId);
    Task<(decimal currentWeight, decimal currentVolume)> GetCurrentUsageAsync(Guid characterId);
    
    // Capacity checks
    Task<bool> CanCarryItemAsync(Guid characterId, Guid itemId);
    Task<bool> IsOverweightAsync(Guid characterId);
    
    // Item organization
    Task<bool> PickUpItemAsync(Guid characterId, Guid itemId);
    Task<bool> DropItemAsync(Guid characterId, Guid itemId, int roomId);
}
```

#### IContainerService & ContainerService

```csharp
public interface IContainerService
{
    // Container queries
    Task<bool> IsContainerAsync(Guid itemId);
    Task<IReadOnlyList<Item>> GetContainerContentsAsync(Guid containerId);
    
    // Container operations
    Task<bool> CanStoreInContainerAsync(Guid containerId, Guid itemId);
    Task<bool> PutItemInContainerAsync(Guid itemId, Guid containerId);
    Task<bool> TakeItemFromContainerAsync(Guid itemId);
    
    // Validation
    Task<(bool fits, string? reason)> ValidateContainerStorageAsync(Guid containerId, Guid itemId);
}
```

#### IEquipmentService & EquipmentService

```csharp
public interface IEquipmentService
{
    // Equipment management
    Task<bool> EquipItemAsync(Guid characterId, Guid itemId, ArmorSlot? slot = null);
    Task<bool> UnequipItemAsync(Guid characterId, Guid itemId);
    Task<bool> UnequipSlotAsync(Guid characterId, ArmorSlot slot);
    
    // Equipment queries
    Task<Item?> GetEquippedItemInSlotAsync(Guid characterId, ArmorSlot slot);
    Task<bool> IsSlotAvailableAsync(Guid characterId, ArmorSlot slot);
    
    // Bonus calculations
    Task<Dictionary<int, decimal>> GetActiveSkillBonusesAsync(Guid characterId);
    Task<Dictionary<string, int>> GetActiveAttributeModifiersAsync(Guid characterId);
}
```

### Phase 3: Commands

Create command handlers in `Mordecai.Web\Services\Commands\`:

#### Item Interaction Commands

- **Get/Take** - Pick up item from room
- **Drop** - Drop item in room
- **Inventory (I)** - Show inventory
- **Equipment (Eq)** - Show equipped items
- **Equip** - Equip an item
- **Unequip** - Unequip an item
- **Put** - Put item in container
- **Get (from container)** - Take item from container
- **Look (in container)** - View container contents
- **Examine/Inspect** - View item details

Example command structure:

```csharp
public class GetItemCommand
{
    public Guid CharacterId { get; set; }
    public string ItemIdentifier { get; set; } // Name or ID
    public int? FromRoomId { get; set; }
    public Guid? FromContainerId { get; set; }
}

public class GetItemCommandHandler
{
    private readonly IItemService _itemService;
    private readonly IInventoryService _inventoryService;
    private readonly IMessagePublisher _messagePublisher;
    
    public async Task<CommandResult> HandleAsync(GetItemCommand command);
}
```

### Phase 4: Admin UI

Enhance `Mordecai.Web\Pages\Admin\Items.razor`:

- **Item Template Creator** - Form to create item templates
- **Item Template Browser** - List/search all templates
- **Item Spawner** - Spawn items in rooms or to characters
- **Container Designer** - Special UI for designing containers
- **Equipment Stats Editor** - Edit skill bonuses and attribute modifiers

### Phase 5: Player UI

Enhance `Mordecai.Web\Pages\Play.razor`:

Add panels for:

- **Inventory Display** - List of carried items with weight/volume
- **Equipment Paper Doll** - Visual equipment slots
- **Container View** - When opening containers
- **Item Details Panel** - When examining items

### Phase 6: Messaging Integration

Define messages in `Mordecai.Messaging\Messages\ItemMessages.cs`:

```csharp
public sealed record ItemSpawned(Guid ItemId, int? RoomId, DateTimeOffset OccurredAt);
public sealed record ItemPickedUp(Guid ItemId, Guid CharacterId, DateTimeOffset OccurredAt);
public sealed record ItemDropped(Guid ItemId, Guid CharacterId, int RoomId, DateTimeOffset OccurredAt);
public sealed record ItemEquipped(Guid ItemId, Guid CharacterId, ArmorSlot Slot, DateTimeOffset OccurredAt);
public sealed record ItemUnequipped(Guid ItemId, Guid CharacterId, ArmorSlot Slot, DateTimeOffset OccurredAt);
public sealed record ItemDestroyed(Guid ItemId, string Reason, DateTimeOffset OccurredAt);
```

### Phase 7: Seed Data

Create `Mordecai.Web\Services\ItemSeedService.cs`:

Seed starter items:
- Basic weapons (training sword, club, dagger)
- Basic armor (leather, cloth)
- Containers (backpack, small pouch, quiver)
- Consumables (healing potion, bread, water)
- Keys for tutorial area

### Phase 8: Integration with Existing Systems

#### Character Creation
- Give starting items to new characters (clothes, basic weapon)
- Initialize CharacterInventory

#### Room Descriptions
- Show items in room: "You see: a rusty sword, a wooden chest"
- Hook into existing room description system

#### Combat System
- Use equipped weapon stats in combat calculations
- Apply equipment skill bonuses
- Handle weapon durability damage

#### Skill System
- Apply ItemSkillBonus when calculating ability scores
- Apply ItemAttributeModifier when calculating attributes

## Testing Checklist

### Unit Tests (Mordecai.Web.Tests)

- [ ] Item weight/volume calculations
- [ ] Container capacity validation
- [ ] Container type restrictions
- [ ] Equipment slot validation
- [ ] Item binding logic
- [ ] Inventory capacity calculations

### Integration Tests

- [ ] Spawn item in room
- [ ] Pick up item
- [ ] Drop item
- [ ] Equip item
- [ ] Unequip item
- [ ] Put item in container
- [ ] Take item from container
- [ ] Nested containers
- [ ] Overweight scenarios
- [ ] Over-volume scenarios

## Performance Considerations

### Indexes
All necessary indexes already configured in ApplicationDbContext.

### Caching
Consider caching:
- ItemTemplates (static data, rarely changes)
- CharacterInventory capacity (recalculate only when Physicality changes)

### Eager Loading
Always use `.Include()` for:
- Item → ItemTemplate (almost always needed)
- Item → ContainedItems when working with containers

### Avoid N+1 Queries
```csharp
// BAD
foreach (var item in items) {
    var template = await context.ItemTemplates.FindAsync(item.ItemTemplateId);
}

// GOOD
var items = await context.Items
    .Include(i => i.ItemTemplate)
    .Where(...)
    .ToListAsync();
```

## Future Enhancements (Post-MVP)

- Item crafting system
- Item enchantment/upgrade system
- Item sets (bonuses for wearing complete sets)
- Item identification (magical items need identifying)
- Cursed items
- Item quality tiers (masterwork, poor, etc.)
- Item decay over time
- Trading system between players
- Auction house
- Item transmog (appearance override)
- Item sockets for gems/runes

## Files Modified/Created

### Created
- `Mordecai.Game\Entities\ItemEntities.cs`
- `Mordecai.Game\Entities\ITEM_SYSTEM_README.md`
- `Mordecai.Game\Entities\ITEM_SYSTEM_QUICK_REF.md`
- `docs\ITEM_IMPLEMENTATION_STATUS.md` (this file)

### Modified
- `Mordecai.Web\Data\ApplicationDbContext.cs`
- `docs\DATABASE_DESIGN.md`
- `docs\MORDECAI_SPECIFICATION.md`

## Dependencies

The item system will interact with:

- **Character System** - Ownership, capacity based on Physicality
- **Room System** - Items in rooms
- **Skill System** - Equipment bonuses to skills
- **Combat System** - Weapon/armor stats
- **Messaging System** - Item events (pickup, drop, etc.)

## Notes

- All entity definitions follow the existing project conventions
- Nullable reference types are properly configured
- Database relationships use appropriate foreign keys and indexes
- The system is designed to be extensible via CustomProperties JSON fields
- Container nesting is supported but should be validated to prevent infinite loops
- Item binding is permanent once set

## Questions/Decisions Needed

1. Should there be a maximum nesting depth for containers? (Suggested: 5 levels)
2. How should broken items (durability = 0) behave? Unusable? Reduced effectiveness?
3. Should weight reduction occur as items break/wear?
4. How should the auction house/trading system work? (Future phase)
5. Should items have a "quality" modifier affecting their stats? (e.g., masterwork = +10%)

---

**Next Immediate Steps:**

1. ✅ Review this document
2. ⬜ Create database migration
3. ⬜ Apply migration
4. ⬜ Implement ItemService
5. ⬜ Implement InventoryService
6. ⬜ Create basic item commands (get, drop, inventory)
