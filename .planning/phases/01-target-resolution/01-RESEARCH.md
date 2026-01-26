# Phase 1: Target Resolution - Research

**Researched:** 2026-01-26
**Domain:** NPC targeting and string-to-entity resolution in .NET 9 / EF Core
**Confidence:** HIGH

## Summary

This phase implements NPC targeting within rooms so players can issue commands like "attack goblin" and have them resolve to the correct ActiveSpawn entity. The codebase already has the infrastructure: `ActiveSpawn` entities with `CurrentRoomId`, `NpcTemplate` with display names, and a `TargetResolutionService` that currently uses simulated data instead of real queries.

The implementation is straightforward database query work with string matching. The existing `TargetResolutionService` architecture is sound and only needs its NPC lookup methods replaced with real EF Core queries against `ActiveSpawns`. The decisions from CONTEXT.md (case-insensitive, prefix-only matching, disambiguation via "goblin 2" syntax) align with standard MUD targeting patterns.

**Primary recommendation:** Extend `TargetResolutionService` with real `ActiveSpawn` queries using EF Core, add a `CurrentRoomId` index to `ActiveSpawns` table, and implement disambiguation result types to handle multiple matches.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Entity Framework Core | 9.x | ORM and database queries | Already used throughout codebase |
| Microsoft.Extensions.Logging | 9.x | Logging infrastructure | Already configured in TargetResolutionService |
| StringComparison.OrdinalIgnoreCase | N/A | Case-insensitive string matching | .NET standard, culture-invariant |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Linq | N/A | Query composition | LINQ-to-Entities for EF Core queries |
| None additional required | - | - | Phase uses existing stack |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| LINQ queries | Raw SQL | LINQ preferred for maintainability; raw SQL only if performance issues arise |
| StringComparison.OrdinalIgnoreCase | ToLower() | Ordinal is more efficient, avoids allocation |
| EF Core Include | Explicit joins | Include is cleaner; explicit joins only for complex projections |

**Installation:**
No additional packages required. All dependencies already in project.

## Architecture Patterns

### Existing Service Structure
```
Mordecai.Web/Services/
├── TargetResolutionService.cs    # THIS IS WHERE CHANGES GO
├── SpawnerService.cs             # Manages ActiveSpawn lifecycle (reference)
└── CharacterService.cs           # Character queries (reference pattern)
```

### Pattern 1: Result Type for Disambiguation
**What:** Return a discriminated result that distinguishes success, no-match, and multiple-matches
**When to use:** Any target resolution that may find 0, 1, or N matches
**Example:**
```csharp
// Source: Standard pattern for discriminated results in C#
public abstract record TargetResolutionResult;

public sealed record TargetFound(CommunicationTarget Target) : TargetResolutionResult;

public sealed record TargetNotFound(string SearchTerm) : TargetResolutionResult;

public sealed record MultipleTargetsFound(
    string SearchTerm,
    IReadOnlyList<CommunicationTarget> Matches
) : TargetResolutionResult;
```

### Pattern 2: Prefix Matching with EF Core
**What:** Query using `StartsWith` with case-insensitive comparison
**When to use:** Matching NPC names from user input
**Example:**
```csharp
// Source: EF Core 9 documentation
// Generates: WHERE LOWER(Name) LIKE 'goblin%'
var matches = await _context.ActiveSpawns
    .AsNoTracking()
    .Include(asp => asp.NpcTemplate)
    .Where(asp => asp.IsActive
        && asp.CurrentRoomId == roomId
        && asp.NpcTemplate.Name.ToLower().StartsWith(searchTerm))
    .ToListAsync();
```

### Pattern 3: Index-First Filtering
**What:** Filter by indexed columns first, then apply string predicates
**When to use:** All ActiveSpawn queries to leverage existing indexes
**Example:**
```csharp
// Good: Filter by indexed columns first
.Where(asp => asp.IsActive && asp.CurrentRoomId == roomId)  // Uses index
.Where(asp => asp.NpcTemplate.Name.ToLower().StartsWith(searchTerm))  // String match

// After adding CurrentRoomId index, this becomes efficient
```

### Anti-Patterns to Avoid
- **Contains matching:** Don't use `Contains()` - CONTEXT.md specifies prefix-only matching
- **Returning first match silently:** Don't auto-select when multiple match - require disambiguation
- **Hardcoded room IDs:** Always use the player's current room from context
- **Forgetting AsNoTracking:** Read-only queries should use AsNoTracking for performance

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Case-insensitive comparison | Custom toLower logic | `StringComparison.OrdinalIgnoreCase` or EF `ToLower()` | Built into .NET, handles edge cases |
| Numeric suffix parsing | Regex or manual string parsing | Simple `string.Split(' ')` + `int.TryParse` | "goblin 2" is trivially splittable |
| Index creation | Manual SQL scripts | EF Core migration with `HasIndex()` | Maintains migration history |
| Async enumeration | Manual Task handling | `ToListAsync()` / `FirstOrDefaultAsync()` | EF Core's async is properly implemented |

**Key insight:** This phase is essentially "replace simulated data with real queries." The service architecture and interfaces already exist.

## Common Pitfalls

### Pitfall 1: Missing Index on CurrentRoomId
**What goes wrong:** ActiveSpawn queries scan entire table instead of using index
**Why it happens:** ActiveSpawn entity has CurrentRoomId FK but NO index defined
**How to avoid:** Add migration with `entity.HasIndex(asp => asp.CurrentRoomId)`
**Warning signs:** Slow queries when many NPCs exist across world

### Pitfall 2: Eager Loading Without Need
**What goes wrong:** Loading full NpcTemplate graph when only Name is needed
**Why it happens:** Using `.Include(asp => asp.NpcTemplate)` reflexively
**How to avoid:** Use `.Select()` projection for lightweight queries
**Warning signs:** Large memory footprint during queries

### Pitfall 3: Non-Atomic Disambiguation State
**What goes wrong:** NPCs despawn between disambiguation prompt and selection
**Why it happens:** User selects "goblin 2" but second goblin died
**How to avoid:** Re-query on selection, handle gracefully with "That target is no longer valid"
**Warning signs:** Null reference exceptions when resolving disambiguated targets

### Pitfall 4: Suffix Parsing Confusion
**What goes wrong:** "goblin warrior 2" parsed as name="goblin warrior", index=2 OR name="goblin", rest="warrior 2"
**Why it happens:** Multi-word names with numeric suffix
**How to avoid:** Per CONTEXT.md, only check if LAST token is numeric
**Warning signs:** Unable to target NPCs with names ending in numbers (rare but possible)

### Pitfall 5: Forgetting IsActive Filter
**What goes wrong:** Targeting dead/despawned NPCs
**Why it happens:** ActiveSpawn.IsActive=false when despawned but record may exist
**How to avoid:** ALWAYS include `.Where(asp => asp.IsActive)` in queries
**Warning signs:** Targeting returns entities that shouldn't be targetable

## Code Examples

Verified patterns from existing codebase:

### Existing TargetResolutionService Pattern
```csharp
// Source: Mordecai.Web/Services/TargetResolutionService.cs (lines 41-86)
// Current implementation shows the interface - replace simulated with real

public async Task<CommunicationTarget?> FindTargetInRoomAsync(
    string targetName,
    int roomId,
    Guid? excludeCharacterId = null)
{
    // 1. First try character match (KEEP THIS)
    // 2. Then try NPC match (REPLACE SIMULATED WITH REAL)
    // 3. Return null if not found
}
```

### ActiveSpawn Query Pattern
```csharp
// Source: Mordecai.Web/Services/SpawnerService.cs (lines 270-271)
// Existing pattern for querying ActiveSpawns by room

var hasCreatures = await _context.ActiveSpawns
    .AnyAsync(asp => asp.CurrentRoomId == roomId && asp.IsActive, cancellationToken);
```

### NpcTemplate Name Access
```csharp
// Source: Mordecai.Game/Entities/SpawnerEntities.cs (lines 63-77)
// NpcTemplate has Name and ShortDescription for display

public class NpcTemplate
{
    public string Name { get; set; }           // "goblin warrior"
    public string ShortDescription { get; set; } // "a fierce goblin warrior"
}
```

### Character Room Query Pattern
```csharp
// Source: Mordecai.Web/Data/ApplicationDbContext.cs (lines 66-72)
// Character has CurrentRoomId with index - same pattern for ActiveSpawn

entity.HasIndex(c => c.CurrentRoomId);
entity.HasOne(c => c.CurrentRoom)
    .WithMany()
    .HasForeignKey(c => c.CurrentRoomId);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Simulated NPC data | Query ActiveSpawn entities | This phase | Real NPCs targetable |
| No room index | Indexed CurrentRoomId | This phase | Efficient room-scoped queries |
| Single-match return | Disambiguation result type | This phase | Handle multiple matches per CONTEXT.md |

**Deprecated/outdated:**
- `GetSimulatedTargetsInRoom()` method in TargetResolutionService - will be removed
- Hardcoded room-to-NPC mapping (roomId switch statement) - replaced with database

## Open Questions

Things that couldn't be fully resolved:

1. **Numeric suffix in NPC names**
   - What we know: CONTEXT.md says "goblin 2" means second goblin
   - What's unclear: What if an NPC is named "Goblin 2" (with "2" as part of name)?
   - Recommendation: Treat trailing numeric as index only if multiple matches exist. If exact match "Goblin 2" exists, prefer it.

2. **Target type in combat context**
   - What we know: TargetType enum has Character, Npc, Mob
   - What's unclear: When resolving for combat, should service distinguish Npc vs Mob?
   - Recommendation: Use Npc for all ActiveSpawn entities. Mob distinction is semantic (hostile by default) not structural.

3. **Online/offline status for NPCs**
   - What we know: CommunicationTarget has IsOnline field
   - What's unclear: NPCs don't have online/offline concept
   - Recommendation: Always set IsOnline=true for NPCs, or introduce nullable for non-applicable

## Sources

### Primary (HIGH confidence)
- Mordecai.Web/Services/TargetResolutionService.cs - existing service architecture
- Mordecai.Web/Services/SpawnerService.cs - ActiveSpawn query patterns
- Mordecai.Game/Entities/SpawnerEntities.cs - ActiveSpawn and NpcTemplate structure
- Mordecai.Web/Data/ApplicationDbContext.cs - existing index patterns
- .planning/phases/01-target-resolution/01-CONTEXT.md - locked implementation decisions

### Secondary (MEDIUM confidence)
- EF Core 9 documentation patterns (ToLower, StartsWith behavior in SQL generation)

### Tertiary (LOW confidence)
- None - all findings verified against existing codebase

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - using existing project dependencies
- Architecture: HIGH - extending existing service, clear patterns in codebase
- Pitfalls: HIGH - identified from code review and standard .NET/EF Core knowledge

**Research date:** 2026-01-26
**Valid until:** No expiration - patterns are stable .NET/EF Core fundamentals
