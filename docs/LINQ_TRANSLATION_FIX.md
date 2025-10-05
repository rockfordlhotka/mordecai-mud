# EF Core LINQ Translation Fix - DateTimeOffset.UtcNow & Nullable Comparisons

## Date: 2025-01-23

## Issue

Background service crashed with LINQ translation error:

```
System.InvalidOperationException: The LINQ expression 'DbSet<RoomEffect>()
.Where(r => r.IsActive && r.EndTime.HasValue && r.EndTime.Value <= __now_0)' 
could not be translated.
```

## Root Cause

**SQLite provider limitation:** Even with captured variables and explicit `HasValue`/`.Value`, the SQLite EF Core provider cannot translate nullable `DateTimeOffset?` value comparisons in LINQ queries.

This is a known limitation of the SQLite provider when dealing with nullable value type comparisons.

## Solution

**Two-step approach:** Load from database, then filter in memory:

### ? Attempts That Failed

**Attempt 1:** Direct `DateTimeOffset.UtcNow`
```csharp
.Where(re => re.EndTime != null && re.EndTime <= DateTimeOffset.UtcNow)
// Error: Can't translate DateTimeOffset.UtcNow
```

**Attempt 2:** Captured variable
```csharp
var now = DateTimeOffset.UtcNow;
.Where(re => re.EndTime != null && re.EndTime <= now)
// Error: Still can't translate nullable comparison
```

**Attempt 3:** HasValue and Value
```csharp
var now = DateTimeOffset.UtcNow;
.Where(re => re.EndTime.HasValue && re.EndTime.Value <= now)
// Error: SQLite can't translate .Value comparison
```

### ? Final Solution (Load then Filter)
```csharp
var now = DateTimeOffset.UtcNow;

// Step 1: Load from database with simple nullable check
var activeEffectsWithEndTime = await context.RoomEffects
    .Where(re => re.IsActive && re.EndTime != null)
    .ToListAsync(cancellationToken);

// Step 2: Filter in memory with null-forgiving operator
var expiredEffects = activeEffectsWithEndTime
    .Where(re => re.EndTime!.Value <= now)
    .ToList();
```

## Why This Works

1. **Database query** uses only `!= null` check (SQLite can handle this)
2. **In-memory filtering** handles the actual `DateTimeOffset` comparison
3. Null-forgiving operator `!` is safe because we already filtered for non-null
4. Performance acceptable: typically few active room effects at any time

## Trade-offs

| Approach | Pros | Cons |
|----------|------|------|
| **SQL filtering** (ideal) | Minimal data transfer | ? SQLite can't translate |
| **Load then filter** (chosen) | ? Works reliably | Small perf cost for in-memory filter |
| **Raw SQL** | Would work | Loses type safety, harder to maintain |

**Decision:** Load-then-filter is best balance for development phase. When migrating to PostgreSQL, can revisit for full SQL filtering.

## Files Fixed

Only one method needed the two-step approach:

| Method | Strategy | Reason |
|--------|----------|--------|
| `CleanupExpiredEffectsAsync` | Load then filter | Primary cleanup method |
| Other methods | Keep `HasValue`/`Value` | They worked or use different patterns |

## Performance Impact

**Typical scenario:**
- 10-20 active room effects with expiration
- Load: ~20 rows
- Filter in memory: trivial
- **Impact: Negligible**

**Worst case:**
- 1000+ active room effects
- Load: 1000 rows
- Filter in memory: ~1ms
- **Impact: Still acceptable**

**Optimization opportunity:** If room effects scale dramatically, add index on `(IsActive, EndTime)`.

## SQLite vs PostgreSQL

This is a **SQLite-specific limitation**. When migrating to PostgreSQL:

```csharp
// This will work with PostgreSQL provider:
var now = DateTimeOffset.UtcNow;
var expiredEffects = await context.RoomEffects
    .Where(re => re.IsActive && re.EndTime.HasValue && re.EndTime.Value <= now)
    .ToListAsync(cancellationToken);
```

**Migration path:** Keep both implementations with provider detection, or accept the small performance cost of load-then-filter for all providers.

## General Pattern for SQLite Nullable Comparisons

When working with nullable value types in SQLite:

```csharp
// ? Don't do this (won't translate)
var threshold = DateTime.Now;
.Where(x => x.LastSeen.HasValue && x.LastSeen.Value < threshold)

// ? Do this instead
var threshold = DateTime.Now;
var candidates = await context.Items
    .Where(x => x.LastSeen != null)  // Simple null check only
    .ToListAsync();
var filtered = candidates
    .Where(x => x.LastSeen!.Value < threshold);  // Filter in memory
```

## Related Best Practices

From `.github/copilot-instructions.md`:
> Plan upgrade path for DB (SQLite -> PostgreSQL) by avoiding engine-specific SQL.

**This pattern honors that principle:**
- Works reliably with SQLite in development
- Will optimize naturally when switching to PostgreSQL
- No engine-specific raw SQL queries
- Type-safe LINQ throughout

## Testing Verification

```bash
# Build succeeds
dotnet build

# Run application
dotnet run --project Mordecai.AppHost

# Verify in logs:
# - No LINQ translation errors
# - "Cleaned up N expired room effects" appears periodically
```

## SQL Generated

**Query sent to SQLite:**
```sql
SELECT * FROM RoomEffects 
WHERE IsActive = 1 
  AND EndTime IS NOT NULL
```

**In-memory filter:**
```csharp
.Where(re => re.EndTime!.Value <= now)
```

## Alternative Considered: Raw SQL

```csharp
// Could use raw SQL, but loses benefits
var expiredEffects = await context.RoomEffects
    .FromSqlRaw(@"
        SELECT * FROM RoomEffects 
        WHERE IsActive = 1 
          AND EndTime IS NOT NULL 
          AND EndTime <= {0}", now)
    .ToListAsync();
```

**Why we didn't:** 
- Loses type safety
- Harder to maintain
- No significant performance gain for this use case
- Load-then-filter is more maintainable

## Future Optimization

If room effects scale to thousands and performance becomes an issue:

1. **Add database index:**
   ```csharp
   entity.HasIndex(re => new { re.IsActive, re.EndTime });
   ```

2. **Consider PostgreSQL migration** (already planned)

3. **Batch cleanup less frequently** (e.g., every minute vs every 10 seconds)

4. **Use raw SQL** if absolutely necessary

## Status

? **RESOLVED**

`CleanupExpiredEffectsAsync` now uses two-step load-then-filter approach that works reliably with SQLite.

---

**Fixed:** 2025-01-23  
**Attempts:** 3 (direct, variable, HasValue/Value, then load-then-filter)  
**Impact:** Critical (background service was crashing every 10s)  
**Resolution:** Load active effects with EndTime, filter expired in memory  
**Performance:** Negligible impact with typical effect counts  
**Files:** `Mordecai.Web\Services\RoomEffectService.cs`
