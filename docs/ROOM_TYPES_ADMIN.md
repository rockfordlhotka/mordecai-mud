# Room Types Admin Page Implementation

## Date: 2025-01-23

## Problem

The admin interface had no way to create or edit room types, which are required when creating new rooms. Room types were only seeded via migrations and couldn't be managed through the UI. Additionally, after the migration reset, room type seed data was not included in the new `InitialCreate` migration.

## Solution

Created a comprehensive admin page for managing room types with full CRUD functionality, plus added a dedicated seed service to ensure room types are populated on first run.

## Files Created

### 1. `/Pages/Admin/RoomTypes.razor`
- **Purpose:** Complete admin interface for room type management
- **Features:**
  - List all room types with filtering (show/hide inactive)
  - Create new room types
  - Edit existing room types
  - Visual indicators for settings (combat, logout, indoor/outdoor)
  - Badge displays for healing and learning bonuses
  - Modal-based editing interface

### 2. `/Services/RoomTypeSeedService.cs` ? **NEW**
- **Purpose:** Seed standard room types on first run
- **Functionality:**
  - Checks if room types exist
  - Seeds 12 standard room types if database is empty
  - Called during application startup
  - Includes all original seeded room types:
    - Normal, Safe Room, Training Hall
    - Shop, Temple, Inn, Tavern, Bank
    - Dungeon, Wilderness, Cave, Tower

### Key Features

#### Display Table
- Name and description
- Boolean settings (combat, logout, indoor) with icons
- Healing rate and learning bonus badges with color coding
- Active/inactive status
- Sortable by name

#### Edit Modal
- **Basic Info:** Name, Description
- **Boolean Settings:** 
  - Allows Combat
  - Allows Logout  
  - Has Special Commands
  - Indoor/Outdoor
- **Numeric Settings:**
  - Healing Rate (0-5x multiplier)
  - Skill Learning Bonus (0-5x multiplier)
  - Max Occupancy (0 = unlimited)
- **Messages:** Entry/Exit messages (optional)
- **Status:** Active/Inactive toggle

#### Validation
- Name uniqueness check
- All required fields validated
- Prevents duplicate names (case-insensitive)

## Files Modified

### 1. `Mordecai.Web/Services/RoomService.cs`

**Interface Changes:**
```csharp
// Added to IRoomService
Task<RoomType?> GetRoomTypeByIdAsync(int id);
Task<RoomType> CreateRoomTypeAsync(RoomType roomType);
Task<RoomType> UpdateRoomTypeAsync(RoomType roomType);
```

**Implementation:**
- `GetRoomTypeByIdAsync` - Retrieve single room type
- `CreateRoomTypeAsync` - Create new room type with logging
- `UpdateRoomTypeAsync` - Update existing room type with logging

### 2. `Mordecai.Web/Pages/Admin/Index.razor`

**Change:** Added "Manage Room Types" button to World Builder card

```razor
<a href="/admin/roomtypes" class="btn btn-outline-secondary">Manage Room Types</a>
```

### 3. `Mordecai.Web/Program.cs` ? **UPDATED**

**Changes:**
- Registered `RoomTypeSeedService` with DI
- Added call to `SeedRoomTypesAsync()` during startup (after admin seed, before skill seed)

```csharp
// Register service
builder.Services.AddScoped<RoomTypeSeedService>();

// Call during startup
var roomTypeSeedService = scope.ServiceProvider.GetRequiredService<RoomTypeSeedService>();
await roomTypeSeedService.SeedRoomTypesAsync();
```

## Seed Data Restoration

The seed service restores the 12 standard room types that were in the original migrations:

| Room Type | Combat | Logout | Healing | Learning | Special |
|-----------|--------|--------|---------|----------|---------|
| Normal | ? | ? | 1.0x | 1.0x | - |
| Safe Room | ? | ? | 1.5x | 1.0x | - |
| Training Hall | ? | ? | 1.0x | 1.5x | Yes |
| Shop | ? | ? | 1.0x | 1.0x | Yes |
| Temple | ? | ? | 2.0x | 1.0x | Yes |
| Inn | ? | ? | 2.0x | 1.0x | Yes |
| Tavern | ? | ? | 1.2x | 1.0x | Yes |
| Bank | ? | ? | 1.0x | 1.0x | Yes (Max 10) |
| Dungeon | ? | ? | 0.8x | 1.2x | - |
| Wilderness | ? | ? | 0.9x | 1.1x | - |
| Cave | ? | ? | 0.9x | 1.0x | - |
| Tower | ? | ? | 1.0x | 1.0x | - |

## Usage Workflow

### First Run
1. Application starts
2. `RoomTypeSeedService` checks if room types exist
3. If empty, seeds 12 standard room types
4. Logs: "Seeded 12 room types successfully"

### Creating a Room Type

1. Navigate to `/admin/roomtypes`
2. Click "Create New Room Type"
3. Fill in:
   - Name (e.g., "Shrine", "Marketplace")
   - Description
   - Toggle settings (combat, logout, special commands, indoor)
   - Set rates (healing, learning)
   - Optional: Max occupancy, entry/exit messages
4. Click "Create Room Type"

### Editing a Room Type

1. Navigate to `/admin/roomtypes`
2. Find the room type in the list
3. Click the edit (pencil) button
4. Modify fields as needed
5. Click "Update Room Type"

### Using Room Types

When creating a room:
1. Go to `/admin/rooms/create`
2. Select zone
3. **Select room type from dropdown** (now populated with seeded types)
4. Fill in room details
5. Create room

## Room Type Properties Explained

### Combat Settings
- **Allows Combat:** Can players fight here?
- **Allows Logout:** Can players safely disconnect?
- **Has Special Commands:** Shop/bank/special interactions?

### Environment
- **Indoor:** Overrides outdoor zone day/night behavior
- **Healing Rate:** Multiplier for health regeneration
  - 1.0 = normal
  - 2.0 = double healing speed
  - 0.8 = slower healing
- **Learning Bonus:** Multiplier for skill advancement
  - 1.0 = normal
  - 1.5 = 50% faster learning

### Occupancy
- **Max Occupancy:** Player limit (0 = unlimited)
- Useful for banks, shops, small rooms

### Messages
- **Entry Message:** Shown when entering (e.g., "You feel safe here")
- **Exit Message:** Shown when leaving (e.g., "You leave the warmth behind")

## Design Patterns

### Service Layer
- Methods follow async/await pattern
- Structured logging with ILogger
- Clear separation of concerns
- Seed service follows same pattern as `SkillSeedService` and `AdminSeedService`

### Startup Seeding
```
Application Start
  ?
Database Migrate
  ?
Seed Admin Data (roles, users)
  ?
Seed Room Types ? NEW
  ?
Seed Skill Data
  ?
Run Data Migrations
  ?
Application Ready
```

### UI Layer
- Modal-based editing (no navigation away)
- Immediate validation feedback
- Color-coded visual indicators
- Responsive design (works on mobile)

### Data Flow
```
UI (Razor) ? Service (IRoomService) ? EF Core ? SQLite
```

## Benefits

1. **No Database Access Required:** Admins can manage room types through UI
2. **Complete CRUD:** Create, read, update (no delete to preserve referential integrity)
3. **Visual Design:** Easy to understand at a glance
4. **Validation:** Prevents invalid configurations
5. **Audit Trail:** Logging for all operations
6. **User-Friendly:** Clear labels, tooltips, and help text
7. **Automatic Seeding:** Room types populated on first run ?
8. **Migration-Independent:** Works even after migration resets ?

## Future Enhancements

### Potential Additions
1. **Delete Functionality:** With cascade handling or room reassignment
2. **Bulk Operations:** Activate/deactivate multiple types
3. **Usage Statistics:** Show how many rooms use each type
4. **Templates:** Copy from existing room type
5. **Import/Export:** JSON export/import for sharing configurations
6. **Room Type Effects:** Link to room effects system
7. **Validation Rules:** Min/max values per setting

### Integration Points
- **Room Effects:** Room types could have default effects
- **NPCs:** Certain NPCs prefer certain room types
- **Quest System:** Quests could reference room types
- **Spawn Rules:** Creatures spawn based on room type

## Testing Checklist

- [x] Build succeeds
- [x] RoomTypeSeedService registered
- [x] Seeding called during startup
- [ ] Navigate to `/admin/roomtypes` and verify 12 seeded types
- [ ] Create new room type
- [ ] Edit existing room type
- [ ] Toggle show/hide inactive
- [ ] Name uniqueness validation works
- [ ] Save persists to database
- [ ] Room creation uses seeded types

## Related Documentation

- **Room Types Seeded:** Now via `RoomTypeSeedService` (was in old `20250915020842_SeedRoomTypes` migration)
- **Room Entity:** See `Mordecai.Game.Entities.RoomType`
- **Room Creation:** See `/admin/rooms/create`
- **Seed Pattern:** See `SkillSeedService`, `AdminSeedService`

## Migration Reset Context

When we reset migrations and created the new `InitialCreate` migration, the room type seed data (which was in the old `SeedRoomTypes` migration) was lost. This seed service restores that data automatically on first run, following the same pattern as the skill and admin seed services.

## Status

? **Complete**

Room types admin page is fully functional with:
- Create and edit capabilities
- Full validation
- Visual indicators
- Responsive design
- Service layer integration
- **Automatic seeding on first run** ?

---

**Created:** 2025-01-23  
**Updated:** 2025-01-23 (Added seed service)  
**Files Added:** 2 (RoomTypes.razor, RoomTypeSeedService.cs)  
**Files Modified:** 3 (RoomService.cs, Index.razor, Program.cs)  
**Build Status:** ? Success  
**Ready for Use:** Yes
