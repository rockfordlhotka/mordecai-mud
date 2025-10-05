# DI Lifetime Issue - Final Resolution

## The Problem

The application was crashing with:
```
Cannot consume scoped service 'DbContextOptions' from singleton 'IDbContextPool'
```

## Root Cause Analysis

The issue was having **TWO competing DbContext registrations**:

```csharp
// Registration 1: Creates SCOPED DbContextOptions
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Registration 2: Tries to create SINGLETON pool that needs DbContextOptions
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
```

### Why This Fails

1. `AddDbContext` registers `DbContextOptions<ApplicationDbContext>` with **scoped** lifetime
2. `AddPooledDbContextFactory` creates a **singleton** pool
3. The singleton pool tries to consume the scoped options
4. ? **DI Rule Violation**: Singletons cannot consume scoped services

## The Solution

**Remove the duplicate registration** - use ONLY the pooled factory:

```csharp
// Single source of truth - pooled factory with its own options
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Add scoped resolver for services that expect direct DbContext injection
builder.Services.AddScoped<ApplicationDbContext>(sp => 
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
```

### Why This Works

1. **Pooled Factory**: Creates singleton pool with internal singleton `DbContextOptions`
2. **No Conflicts**: No competing scoped options registration
3. **Scoped Resolver**: Provides `ApplicationDbContext` instances for Identity/migrations
4. **Best of Both Worlds**: Factory pattern for services + direct injection for framework components

## What Services Get What

| Service Type | Gets What | How |
|-------------|-----------|-----|
| `RoomEffectService` | `IDbContextFactory<ApplicationDbContext>` | Constructor injection |
| ASP.NET Identity | `ApplicationDbContext` | Scoped resolver creates from factory |
| Migrations | `ApplicationDbContext` | Scoped resolver creates from factory |
| Background Services | `IDbContextFactory<ApplicationDbContext>` | Constructor injection |

## Key Insight

You **cannot** use both `AddDbContext` and `AddPooledDbContextFactory` together. Choose one:

### Option A: Regular DbContext (Simpler, Good Enough)
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
// No factory available
```

### Option B: Pooled Factory (Better Performance) ? **We Use This**
```csharp
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Add resolver for direct injection needs
builder.Services.AddScoped<ApplicationDbContext>(sp => 
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
```

## Testing Verification

```bash
# Build succeeds
dotnet build

# Application starts without DI errors
dotnet run --project Mordecai.AppHost
```

## Lessons Learned

1. **Read error messages carefully**: "Cannot consume scoped from singleton" = lifetime mismatch
2. **Don't duplicate registrations**: One registration per service type
3. **Understand factory patterns**: Factories create instances, they don't share options
4. **Document assumptions**: Why we chose pooled factory (performance + factory pattern)

## Status

? **RESOLVED** - Application starts successfully

---

**Issue Duration**: ~30 minutes  
**Root Cause**: Duplicate DbContext registrations  
**Solution**: Single pooled factory registration + scoped resolver  
**Impact**: Unblocked application startup
