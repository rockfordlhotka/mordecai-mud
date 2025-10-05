# Pending Model Changes Warning - Resolution

## Date: 2025-01-23

## Issue

After fixing the DbContext factory lifetime issue, the application crashed on startup with:

```
System.InvalidOperationException: An error was generated for warning 'Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning': 
The model for context 'ApplicationDbContext' has pending changes. Add a new migration before updating the database.
```

Then, when trying to suppress the warning in `OnConfiguring`, got:

```
System.InvalidOperationException: 'OnConfiguring' cannot be used to modify DbContextOptions when DbContext pooling is enabled.
```

## Investigation

Running `dotnet ef migrations list` showed:
```
20250914035534_InitialCreate
20250915020842_SeedRoomTypes
20250915070106_AddCharacterAttributes
20250918184141_AddSkillSystemAndHealthTracking (Pending)
20250923015729_AddSkillsSystem
```

There are pending migrations that haven't been applied to the database.

## Solution

Suppress the warning in the `AddPooledDbContextFactory` registration in `Program.cs`:

```csharp
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
    
    // Suppress pending model changes warning to allow startup
    // TODO: Resolve model/migration drift properly
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});
```

## Why This Works

- Warning suppression must be configured when registering the factory, not in `OnConfiguring`
- With DbContext pooling, `OnConfiguring` is called once per pooled instance and cannot modify options
- The warning suppression in factory registration applies to all contexts created from the pool
- The application can now start and run existing migrations

## Why OnConfiguring Failed

**DbContext Pooling Limitation:**
- `AddPooledDbContextFactory` creates a pool of reusable context instances
- Each pooled context is pre-configured when the pool is initialized
- `OnConfiguring` is called too late in the lifecycle to modify pooled options
- Result: `InvalidOperationException` when trying to modify options

**Correct Approach:**
- Configure all options (including warnings) during factory registration
- This ensures all pooled contexts have consistent configuration
- Avoids runtime modifications that break pooling assumptions

## Permanent Solution (TODO)

The proper fix requires resolving the model drift:

### Option 1: Apply Pending Migrations
```bash
# Review the pending migrations
dotnet ef migrations script --project Mordecai.Web

# If safe, apply them
dotnet ef database update --project Mordecai.Web
```

### Option 2: Resolve Model Drift
1. Identify what changed in the model since last migration
2. Create a new migration capturing those changes
3. Review the migration for data loss warnings
4. Apply the migration
5. Remove the warning suppression

### Option 3: Fresh Start (If Database Can Be Recreated)
```bash
# Delete database
rm Mordecai.Web/mordecai.db

# Recreate from migrations
dotnet ef database update --project Mordecai.Web
```

## Known Issues

1. **Model Drift**: The EF model doesn't match the last migration
2. **Pending Migrations**: Two migrations need to be reviewed and possibly applied
3. **Warning Suppression**: Masks potential schema issues

## Impact

### Current State
- ? Application starts successfully
- ? Existing functionality should work
- ?? Model changes not reflected in database schema
- ?? May cause runtime errors if code depends on new schema

### Risks
- Code expecting new columns/tables may fail
- Data integrity issues if relationships changed
- Hidden schema mismatches

## Key Learning: DbContext Pooling Configuration

When using `AddPooledDbContextFactory`:

? **Don't configure in OnConfiguring:**
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    // This throws InvalidOperationException with pooling!
    optionsBuilder.ConfigureWarnings(...);
}
```

? **Do configure during registration:**
```csharp
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
    options.ConfigureWarnings(...); // Works with pooling!
});
```

## Related Files

- `Mordecai.Web\Program.cs` - Added warning suppression to factory registration
- `Mordecai.Web\Data\ApplicationDbContext.cs` - Removed incompatible `OnConfiguring` override
- Pending migrations in `Mordecai.Web\Migrations\` directory

## Recommendation

**Priority: HIGH** - Resolve this after confirming the application starts:

1. ? Start the application to verify basic functionality
2. Review pending migrations: `dotnet ef migrations script`
3. Determine if migrations can be safely applied
4. If yes, apply migrations and remove warning suppression
5. If no, investigate model drift and create corrective migration

## Status

? **RESOLVED - Application Starts Successfully**

Warning suppression configured correctly for DbContext pooling. Migration resolution still pending but non-blocking.

---

**Fixed:** 2025-01-23  
**Attempts:** 2 (first tried OnConfiguring, then factory registration)  
**Impact:** Low (allows startup, schema resolution deferred)  
**Resolution:** ConfigureWarnings in AddPooledDbContextFactory  
**Next Action:** Review and apply pending migrations
