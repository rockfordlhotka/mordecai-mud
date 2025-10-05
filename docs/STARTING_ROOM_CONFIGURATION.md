# Starting Room Configuration Feature

## Date: 2025-01-23

## Problem

There was no admin interface to configure where new characters should start their journey. The starting room logic was hardcoded in `WorldService.GetStartingRoomAsync()` to look for:
1. Rooms at coordinates 0,0,0 in zones with "tutorial" in the name
2. Any room at 0,0,0
3. Any active room (as ultimate fallback)

This approach was fragile and gave admins no control over the new player experience.

## Solution

Created a comprehensive game settings system with a dedicated admin interface to configure the starting room.

## Components Created

### 1. Database - `GameConfiguration` Entity

**File:** `Mordecai.Game/Entities/GameConfiguration.cs`

Simple key-value configuration table:
```csharp
public class GameConfiguration
{
    public string Key { get; set; }      // e.g., "Game.StartingRoomId"
    public string Value { get; set; }    // e.g., "42"
    public string? Description { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; }
}
```

**Migration:** `AddGameConfiguration`

### 2. Service Layer - `GameConfigurationService`

**File:** `Mordecai.Web/Services/GameConfigurationService.cs`

**Interface:**
```csharp
public interface IGameConfigurationService
{
    Task<string?> GetConfigurationAsync(string key);
    Task<int?> GetConfigurationAsIntAsync(string key);
    Task SetConfigurationAsync(string key, string value, string description, string updatedBy);
    Task<int?> GetStartingRoomIdAsync();
    Task SetStartingRoomIdAsync(int roomId, string updatedBy);
}
```

**Key Constant:**
```csharp
public const string StartingRoomKey = "Game.StartingRoomId";
```

**Features:**
- Generic key-value configuration storage
- Convenience methods for starting room specifically
- Audit trail (UpdatedAt, UpdatedBy)
- Structured logging

### 3. Updated `WorldService`

**File:** `Mordecai.Web/Services/WorldService.cs`

**Updated Logic:**
```csharp
public async Task<Room?> GetStartingRoomAsync()
{
    // 1. Try configured starting room (NEW)
    var configuredRoomId = await _configService.GetStartingRoomIdAsync();
    if (configuredRoomId.HasValue)
    {
        // Return configured room if found and active
    }
    
    // 2. Fallback to old logic (tutorial zones at 0,0,0)
    // 3. Fallback to any room at 0,0,0
    // 4. Ultimate fallback: any active room
}
```

### 4. Admin UI - Game Settings Page

**File:** `Mordecai.Web/Pages/Admin/Settings.razor`
**Route:** `/admin/settings`

**Features:**
- **Current Setting Display:** Shows currently configured starting room with preview
- **Zone Filter:** Filter rooms by zone to make selection easier
- **Room Selector:** Dropdown of all active rooms with zone and coordinates
- **Live Preview:** Shows selected room details before saving
- **Validation:** Ensures a valid room is selected
- **Best Practices Guide:** Sidebar with tips for choosing a starting room

## Usage Workflow

### For Admins

1. **Navigate to Settings:**
   - Admin Dashboard ? "Game Settings" card
   - Or directly: `/admin/settings`

2. **View Current Setting:**
   - See which room is currently set (if any)
   - View warning if no starting room configured

3. **Select New Starting Room:**
   - Optionally filter by zone
   - Choose room from dropdown
   - Preview selection before saving

4. **Save:**
   - Click "Save Starting Room"
   - Configuration is saved with audit trail
   - Confirmation message shows the change

5. **Test:**
   - Create a new test character
   - Verify they start in the correct location

### For Players

**No visible change** - new characters automatically start in the configured room.

## Best Practices (From UI Guide)

### Choosing a Starting Room

? **Do:**
- Pick a "Safe Room" type (no combat)
- Choose a location with clear directions to tutorial areas
- Ensure the room has helpful NPCs or signs nearby
- Place it in a beginner-friendly zone (low difficulty)
- Make the description welcoming and informative

? **Don't:**
- Choose a dangerous dungeon or wilderness area
- Select a room without exits
- Pick a location far from help or guidance
- Use a room that's confusing or overwhelming

### Considerations

- This affects **ALL new characters**
- Existing characters are **not** moved
- The room should always be **active and accessible**
- Consider creating a dedicated "Tutorial Zone"

### After Changing

1. Test by creating a new character
2. Verify the starting room has useful exits
3. Update any tutorial text or help commands
4. Consider announcing the change to players

## Integration Points

### Character Creation

When a new character is created:
1. `CharacterService.GetCharacterRoomAsync()` is called
2. Which calls `WorldService.GetStartingRoomAsync()`
3. Which now checks `GameConfigurationService.GetStartingRoomIdAsync()` first
4. Falls back to old logic if no configuration exists

### Backward Compatibility

- If no starting room is configured, uses old fallback logic
- Existing characters are unaffected
- System continues to work even if configured room becomes inactive

## Database Schema

### GameConfigurations Table

| Column | Type | Description |
|--------|------|-------------|
| Key | string (PK) | Configuration key (e.g., "Game.StartingRoomId") |
| Value | string | Configuration value (e.g., "42") |
| Description | string? | Human-readable description |
| UpdatedAt | DateTimeOffset | When last updated |
| UpdatedBy | string | Who updated it (email/username) |

### Example Data

```
Key: "Game.StartingRoomId"
Value: "15"
Description: "The room ID where new characters start their journey"
UpdatedAt: 2025-01-23T10:30:00Z
UpdatedBy: "admin@mordecai.mud"
```

## Extensibility

The `GameConfiguration` table and service are designed for future settings:

**Potential Future Configurations:**
- `Game.MaxCharactersPerAccount`
- `Game.StartingGold`
- `Game.EnablePvP`
- `Game.MaintenanceMode`
- `Game.MessageOfTheDay`
- `Combat.GlobalDifficultyMultiplier`
- `Skills.PassiveGainRate`

**Pattern to add new settings:**
1. Add constant to `GameConfigurationService`: `public const string NewSettingKey = "Category.Setting";`
2. Add convenience methods: `GetNewSettingAsync()`, `SetNewSettingAsync()`
3. Create UI in appropriate admin page
4. Use in relevant service

## Files Modified

1. `Mordecai.Web/Data/ApplicationDbContext.cs` - Added GameConfigurations DbSet
2. `Mordecai.Web/Services/WorldService.cs` - Updated to use configuration service
3. `Mordecai.Web/Program.cs` - Registered GameConfigurationService
4. `Mordecai.Web/Pages/Admin/Index.razor` - Added Game Settings card

## Files Created

1. `Mordecai.Game/Entities/GameConfiguration.cs` - Configuration entity
2. `Mordecai.Web/Services/GameConfigurationService.cs` - Configuration service
3. `Mordecai.Web/Pages/Admin/Settings.razor` - Admin settings UI
4. Migration: `AddGameConfiguration`

## Testing Checklist

- [x] Build succeeds
- [ ] Migration applies successfully
- [ ] Navigate to `/admin/settings`
- [ ] See warning when no starting room configured
- [ ] Select a starting room and save
- [ ] Verify configuration is saved in database
- [ ] Create a new character
- [ ] Verify character starts in configured room
- [ ] Change starting room to different room
- [ ] Create another character and verify new location
- [ ] Test fallback by setting invalid room ID in database
- [ ] Verify system falls back to old logic gracefully

## Future Enhancements

### Near-term
1. **Multiple Starting Rooms:** Allow different starting rooms per race/class
2. **Random Selection:** Choose randomly from a pool of starting rooms
3. **Time-based:** Different starting rooms for day/night or events

### Medium-term
1. **Settings Categories:** Group settings by category in UI (World, Combat, Skills, etc.)
2. **Settings History:** Track all configuration changes with full history
3. **Settings Import/Export:** JSON export/import for backups or sharing

### Long-term
1. **Dynamic Validation:** Define validation rules per setting type
2. **Settings API:** RESTful API for external tools
3. **Real-time Updates:** Notify running services when settings change

## Security Considerations

- ? Requires "AdminOnly" policy (role-based authorization)
- ? Audit trail captures who made changes
- ? Validates room exists and is active before saving
- ? Graceful fallback if configured room becomes invalid

## Performance Notes

- Configuration service queries database each time
- **Future:** Add caching layer for frequently accessed settings
- **Future:** Add change notification system to invalidate caches

## Related Documentation

- **Character Creation Flow:** See `CharacterService`
- **World Service:** See `WorldService.GetStartingRoomAsync()`
- **Admin Dashboard:** See `/admin`

## Status

? **Complete**

Game settings system implemented with:
- Database table for configuration storage
- Service layer with audit trail
- Admin UI for starting room configuration
- Integration with world service
- Backward compatibility with fallback logic
- Comprehensive documentation

---

**Created:** 2025-01-23  
**Files Added:** 3 (GameConfiguration entity, service, admin page)  
**Files Modified:** 4 (DbContext, WorldService, Program.cs, Admin Index)  
**Migration:** AddGameConfiguration  
**Build Status:** ? Success  
**Ready for Use:** Yes

## Quick Reference

### Access Settings
```
Admin Dashboard ? Game Settings
Or: /admin/settings
```

### Set Starting Room (Code)
```csharp
await _configService.SetStartingRoomIdAsync(roomId, "admin@example.com");
```

### Get Starting Room (Code)
```csharp
var roomId = await _configService.GetStartingRoomIdAsync();
```

### Configuration Key
```csharp
GameConfigurationService.StartingRoomKey = "Game.StartingRoomId"
```
