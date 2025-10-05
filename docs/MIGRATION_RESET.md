# Migration Reset - Clean Slate Approach

## Date: 2025-01-23

## Decision

After encountering repeated migration conflicts and database schema mismatches, we decided to **reset all migrations** and start fresh with a single `InitialCreate` migration that captures the current complete model state.

## What We Did

1. **Deleted all existing migrations**
   ```bash
   Remove-Item "Mordecai.Web\Migrations" -Recurse -Force
   ```

2. **Deleted the database**
   ```bash
   Get-ChildItem "Mordecai.Web" -Filter "mordecai.db*" | Remove-Item -Force
   ```

3. **Created fresh initial migration**
   ```bash
   dotnet ef migrations add InitialCreate --project Mordecai.Web
   ```

## Why This Approach?

### Previous Issues
- Multiple overlapping migrations trying to add same columns
- Migration history table out of sync with actual schema
- Partially applied migrations causing duplicate column errors
- Complex migration dependencies difficult to unwind

### Benefits of Clean Slate
? **Single source of truth** - One migration reflects entire current model  
? **No conflicts** - No competing migrations trying to modify same columns  
? **Clean history** - Migration history accurately reflects database state  
? **Easier troubleshooting** - Simple to understand what the schema should be  
? **Fresh start** - No legacy baggage from failed migration attempts  

## Current State

### Migration Files
```
Mordecai.Web/Migrations/
??? 20251005051259_InitialCreate.cs
??? 20251005051259_InitialCreate.Designer.cs
??? ApplicationDbContextModelSnapshot.cs
```

### What InitialCreate Contains
- All Identity tables (AspNetUsers, AspNetRoles, etc.)
- Character system (Characters table with all attributes)
- World system (Zones, Rooms, RoomTypes, RoomExits)
- Skill system (SkillCategories, SkillDefinitions, CharacterSkills, SkillUsageLogs)
- Room Effects system (RoomEffectDefinitions, RoomEffectImpacts, RoomEffects, RoomEffectApplicationLogs)
- All indexes and foreign key relationships
- All data type configurations (precision, uniqueness, etc.)

## When to Reset Migrations Again

Consider resetting migrations if:
- ? You're in **production** with real data (DON'T reset!)
- ? You're in **early development** with no production data
- ? Migration history becomes **severely corrupted**
- ? Multiple **conflicting migrations** are impossible to reconcile
- ? You want to **simplify** before production deployment

## How to Reset Migrations (Future Reference)

```bash
# 1. Delete all migrations
Remove-Item "Mordecai.Web\Migrations" -Recurse -Force

# 2. Delete the database (dev only!)
Get-ChildItem "Mordecai.Web" -Filter "mordecai.db*" | Remove-Item -Force

# 3. Create fresh initial migration
dotnet ef migrations add InitialCreate --project Mordecai.Web

# 4. Run the app to create database
dotnet run --project Mordecai.AppHost
```

## Best Practices Going Forward

### Development
1. **Test migrations before committing**
   ```bash
   dotnet ef database update --project Mordecai.Web
   ```

2. **One migration per logical change**
   - Don't combine unrelated schema changes
   - Use descriptive migration names

3. **Review generated migrations**
   - Check for data loss warnings
   - Verify column types and constraints
   - Ensure indexes are created

4. **Keep model and database in sync**
   - Run migrations promptly after creating them
   - Don't manually modify database schema

### Production (Future)
1. **NEVER delete migrations that have been deployed**
2. **Always have rollback scripts ready**
3. **Test migrations on staging first**
4. **Backup before applying migrations**
5. **Use migration scripts for production** (not automatic on startup)

## Migration Strategy Comparison

### Old Approach (Multiple Migrations)
```
20250914035534_InitialCreate
20250915020842_SeedRoomTypes
20250915070106_AddCharacterAttributes
20250918184141_AddSkillSystemAndHealthTracking  ? Conflicts here
20250923015729_AddSkillsSystem                   ? And here
```
**Issues:** Overlapping changes, partial application, history mismatch

### New Approach (Single Migration)
```
20251005051259_InitialCreate  ? Everything in one place
```
**Benefits:** Clean, simple, accurate

## When NOT to Reset

? **Don't reset if:**
- Production database exists with real user data
- Other developers have applied your migrations
- Migrations have been tagged/released
- You need to preserve migration history for compliance

? **Safe to reset if:**
- Early development phase (no production deployment)
- Local development database only
- Team agrees on the reset
- No external dependencies on migration history

## Impact Assessment

### Development Impact
- ? Cleaner codebase
- ? Easier onboarding (one migration to understand)
- ? No migration conflicts
- ? Faster migration application

### Production Impact
- ?? Not applicable (no production deployment yet)
- ?? Document this decision for future reference
- ?? Plan proper migration strategy before production

## Alternative Considered

We could have:
1. Manually fixed each conflicting migration
2. Created compensating migrations to undo/redo changes
3. Manually synchronized migration history table

**Why we chose clean slate instead:**
- Faster (minutes vs hours of troubleshooting)
- Cleaner result (no migration cruft)
- More maintainable going forward
- No production data to preserve

## Verification

After reset, verify:
```bash
# List migrations (should show only InitialCreate)
dotnet ef migrations list --project Mordecai.Web

# Generate script to review what will be applied
dotnet ef migrations script --project Mordecai.Web

# Run app and verify database creates cleanly
dotnet run --project Mordecai.AppHost
```

## Related Documentation

- `MIGRATION_HISTORY_MISMATCH.md` - Original problem that led to this decision
- `PENDING_MODEL_CHANGES_WARNING.md` - Warning suppression (may no longer be needed)
- `APPLICATION_STARTUP_RESOLUTION.md` - Full journey to application startup

## Status

? **Complete**

Migrations reset to single `InitialCreate` migration capturing current complete model state.

---

**Completed:** 2025-01-23  
**Approach:** Delete all migrations, create fresh InitialCreate  
**Impact:** Development only (no production data affected)  
**Benefit:** Clean, maintainable migration history going forward
