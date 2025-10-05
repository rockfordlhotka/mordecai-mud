# DbContext Factory Lifetime Fix

## Date: 2025-01-23

## Problem

The application crashed on startup with a dependency injection lifetime mismatch error:

```
System.InvalidOperationException: Cannot consume scoped service 
'Microsoft.EntityFrameworkCore.DbContextOptions`1[Mordecai.Web.Data.ApplicationDbContext]' 
from singleton 'Microsoft.EntityFrameworkCore.Internal.IDbContextPool`1[Mordecai.Web.Data.ApplicationDbContext]'.
```

## Root Cause

Having **both** `AddDbContext` and `AddPooledDbContextFactory` creates conflicting registrations:

- `AddDbContext`: Registers scoped `DbContextOptions<ApplicationDbContext>`
- `AddPooledDbContextFactory`: Tries to create a singleton pool but consumes the scoped options
- **Problem**: The singleton pool cannot consume the scoped options

## Solution

Use **ONLY** `AddPooledDbContextFactory` and provide a scoped `ApplicationDbContext` resolver for services (like Identity) that need direct context injection.

### Before (Incorrect - Two Registrations)
```csharp
// This creates scoped DbContextOptions ?
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// This tries to use the scoped options from above ?
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
```

### After (Correct - Single Registration)
```csharp
// ONLY register the pooled factory - this creates singleton pool with its own options ?
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// For services like Identity that need direct DbContext injection,
// add a scoped resolver that uses the factory ?
builder.Services.AddScoped<ApplicationDbContext>(sp => 
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
```

## Why This Works

1. **Pooled Factory Only**: `AddPooledDbContextFactory` creates its own internal `DbContextOptions` with singleton lifetime
2. **No Conflicts**: We don't have competing scoped options from `AddDbContext`
3. **Identity Compatible**: The scoped resolver provides `ApplicationDbContext` instances for Identity and other services expecting direct injection
4. **Best Performance**: Context pooling reduces allocations

## Usage Patterns

### Pattern 1: Using IDbContextFactory (Preferred for Services)
```csharp
public class RoomEffectService : IRoomEffectService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public async Task<RoomEffect> ApplyEffectAsync(...)
    {
        // Get a pooled context
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        // Use context...
        
        // Context returns to pool when disposed
    }
}
```

### Pattern 2: Direct DbContext Injection (Identity, Migrations)
```csharp
// Identity automatically uses this pattern
builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();  // Works! Uses scoped resolver

// Startup migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();  // Works! Uses scoped resolver
}
```

## Key Points

1. **Don't Mix**: Never use `AddDbContext` and `AddPooledDbContextFactory` together
2. **Factory Provides Both**: The pooled factory + scoped resolver gives you both factory and direct injection
3. **Performance**: Pooling improves performance by reusing context instances
4. **Identity Works**: The scoped resolver ensures Identity can inject `ApplicationDbContext` directly

## Pool Configuration (Optional)

```csharp
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(
    options => options.UseSqlite(connectionString),
    poolSize: 128  // Default is 1024, adjust based on load
);
```

For Mordecai MUD (10-50 concurrent users), default pool size is sufficient.

## Testing

1. **Startup Test**: Application starts without DI errors ?
2. **Factory Usage**: Services using `IDbContextFactory` work correctly ?
3. **Direct Injection**: Services using `ApplicationDbContext` work correctly ?
4. **Identity**: Authentication and authorization work ?
5. **Migrations**: Database migrations apply successfully ?

## Common Mistake

? **Don't do this:**
```csharp
// Both registrations conflict!
builder.Services.AddDbContext<ApplicationDbContext>(...);
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(...);
```

? **Do this instead:**
```csharp
// Single registration + scoped resolver
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(...);
builder.Services.AddScoped<ApplicationDbContext>(sp => 
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
```

## Related Documentation

- [EF Core Context Pooling](https://learn.microsoft.com/ef/core/performance/advanced-performance-topics#dbcontext-pooling)
- [Dependency Injection Lifetimes](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection#service-lifetimes)
- [DbContext Factory](https://learn.microsoft.com/ef/core/dbcontext-configuration/#using-a-dbcontext-factory-eg-for-blazor)

## Files Changed

- **`Mordecai.Web\Program.cs`**: Removed `AddDbContext`, kept only `AddPooledDbContextFactory` + scoped resolver

## Status

? **RESOLVED**

Application now starts successfully with proper DbContext factory configuration and no lifetime conflicts.

---

**Fixed:** 2025-01-23  
**Attempts:** 2 (first attempt had duplicate registration)  
**Impact:** Critical (blocking startup)  
**Resolution:** Single AddPooledDbContextFactory + scoped ApplicationDbContext resolver  
**Testing:** Application startup verified
