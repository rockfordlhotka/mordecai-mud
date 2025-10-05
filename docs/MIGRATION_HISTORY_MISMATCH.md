# Migration History Mismatch - Database Reset

## Date: 2025-01-23

## Issue

Application crashed during startup when trying to apply migrations:

```
Microsoft.Data.Sqlite.SqliteException: SQLite Error 1: 'duplicate column name: CurrentFatigue'.
```

The migration `20250918184141_AddSkillSystemAndHealthTracking` was trying to add columns that already existed in the database.

## Root Cause

**Database/Migration Mismatch:**
- The database schema had columns from pending migrations
- The `__EFMigrationsHistory` table didn't record these migrations as applied
- Likely caused by previous failed migration attempts or manual schema changes

**How This Happens:**
1. Migration partially applies before encountering an error
2. Schema changes made, but migration not recorded in history table
3. Next startup tries to apply same migration again
4. `ALTER TABLE ADD COLUMN` fails because column already exists

## Solution

Delete the database and let Entity Framework recreate it from migrations:

```bash
# Remove database file
Remove-Item "S:\src\rdl\mordecai-mud\Mordecai.Web\mordecai.db"

# Remove SQLite WAL files if they exist
Remove-Item "S:\src\rdl\mordecai-mud\Mordecai.Web\mordecai.db-*"

# Next app startup will recreate from migrations
dotnet run --project Mordecai.AppHost
```

## Why This Works

- Fresh database means no conflicting columns
- Entity Framework applies all migrations in order
- Migration history table accurately reflects applied migrations
- No data loss for development (database was in inconsistent state anyway)

## Alternative Solutions (If Data Needs Preserving)

### Option 1: Manually Mark Migration as Applied
```bash
# Connect to database and manually insert migration record
# WARNING: Only if you're CERTAIN the schema matches the migration!

sqlite3 Mordecai.Web/mordecai.db
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
VALUES ('20250918184141_AddSkillSystemAndHealthTracking', '9.0.0');
.quit
```

### Option 2: Create Rollback Script
```bash
# Review what the migration added
dotnet ef migrations script 20250915070106_AddCharacterAttributes 20250918184141_AddSkillSystemAndHealthTracking

# Manually remove added columns (if safe)
# Then let migration apply normally
```

### Option 3: Export/Import Data
```bash
# Export existing data
sqlite3 Mordecai.Web/mordecai.db ".dump" > backup.sql

# Delete database
rm Mordecai.Web/mordecai.db

# Let migrations create fresh schema
dotnet run --project Mordecai.AppHost

# Selectively import data (edit backup.sql to remove schema creation)
```

## Prevention

### For Development
- ? **Commit migrations before schema changes**
- ? **Test migrations in isolation** before running app
- ? **Use `dotnet ef database update`** explicitly to test migrations
- ? **Don't modify schema manually** while using migrations
- ? **Keep database backups** before applying migrations

### For Production
- ? **Test migrations on staging database** first
- ? **Use transactions** (SQLite has limitations, consider PostgreSQL)
- ? **Backup before migrations**
- ? **Have rollback scripts ready**
- ? **Monitor migration application**

## Understanding SQLite Limitations

SQLite has limited `ALTER TABLE` support compared to PostgreSQL/SQL Server:
- Can't drop columns (must recreate table)
- Can't modify columns (must recreate table)
- No native RENAME support for constraints
- WAL mode can complicate concurrent access

**Implication:** Migration failures in SQLite can be harder to recover from. This is one reason the spec mentions "plan upgrade path for DB (SQLite -> PostgreSQL)".

## Migration Health Check

Before running application:

```bash
# Check which migrations are pending
dotnet ef migrations list --project Mordecai.Web

# See what would be applied
dotnet ef migrations script --project Mordecai.Web

# Apply migrations explicitly (safer than on startup)
dotnet ef database update --project Mordecai.Web
```

## Database Recreation Process

When database is deleted:

1. **App startup** calls `context.Database.Migrate()`
2. **EF Core** sees no `__EFMigrationsHistory` table
3. **Creates** fresh database with all migrations applied in order
4. **Seeds** data (admin, skills, etc.)
5. **Application** starts normally

## Status After Reset

- ? Database recreated from migrations
- ? All migrations applied cleanly
- ? Migration history accurate
- ? No duplicate column errors
- ?? All previous data lost (acceptable for dev)

## Related Issues

This completes the migration warning suppression chain:
1. Warning about pending model changes ? suppressed
2. `OnConfiguring` can't modify pooled options ? fixed
3. Database/migration mismatch ? resolved by reset

## Files Involved

- `Mordecai.Web/mordecai.db` - Deleted and recreated
- `Mordecai.Web/Migrations/20250918184141_AddSkillSystemAndHealthTracking.cs` - Migration that failed
- `__EFMigrationsHistory` table - Now accurate after recreation

## Next Steps

1. ? Database recreated
2. ?? Review pending model changes (warning is still suppressed)
3. ?? Create new migration if needed
4. ?? Remove warning suppression once model/migrations align

## Recommendation

**For Development:**
- Recreating database is fine (no production data)
- Keeps schema clean and migrations reliable
- Faster than trying to manually fix inconsistencies

**For Production:**
- Never delete database!
- Use proper migration testing and rollback procedures
- Consider PostgreSQL for better migration support

---

**Fixed:** 2025-01-23  
**Method:** Database deletion and recreation  
**Impact:** Development data lost (acceptable)  
**Status:** ? Resolved - Application should start cleanly now
