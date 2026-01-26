---
phase: 01-target-resolution
plan: 01
subsystem: api
tags: [ef-core, linq, target-resolution, npc, activespawn, disambiguation]

# Dependency graph
requires:
  - phase: existing-codebase
    provides: ActiveSpawn entities with CurrentRoomId, NpcTemplate with Name
provides:
  - TargetResolutionService with real NPC queries
  - FindNpcInRoomAsync with disambiguation support
  - NpcResolutionResult discriminated union types
  - Comprehensive test coverage for target resolution
affects: [02-combat-sessions, combat-commands]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Discriminated union result types (NpcResolutionResult)
    - Prefix matching with ToLower().StartsWith()
    - Numeric suffix disambiguation ("goblin 2" syntax)

key-files:
  created:
    - Mordecai.Web/Data/DesignTimeDbContextFactory.cs
    - Mordecai.Web.Tests/TargetResolutionServiceTests.cs
  modified:
    - Mordecai.Web/Services/TargetResolutionService.cs
    - Mordecai.Web/Data/ApplicationDbContext.cs

key-decisions:
  - "Prefix-only matching per CONTEXT.md (no Contains matching)"
  - "1-based numeric suffix for disambiguation (matches MUD conventions)"
  - "ActiveSpawns ordered by Name then Id for consistent disambiguation ordering"

patterns-established:
  - "NpcResolutionResult discriminated union: NpcFound, NpcNotFound, MultipleNpcsFound"
  - "ParseSearchInput for numeric suffix extraction"
  - "Always filter by IsActive=true for ActiveSpawn queries"

# Metrics
duration: 9min
completed: 2026-01-26
---

# Phase 1 Plan 1: Target Resolution Summary

**Real NPC target resolution via ActiveSpawn entities with case-insensitive prefix matching and numeric disambiguation**

## Performance

- **Duration:** 9 min
- **Started:** 2026-01-26T17:55:38Z
- **Completed:** 2026-01-26T18:04:35Z
- **Tasks:** 3
- **Files modified:** 4 (plus 1 created)

## Accomplishments

- FindNpcInRoomAsync queries real ActiveSpawn entities instead of simulated data
- Prefix matching enables "gob" to find "goblin" (case-insensitive)
- Disambiguation with numeric suffix - "goblin 2" selects second match
- 12 integration tests verify all target resolution scenarios
- GetAllTargetsInRoomAsync returns real NPCs from database

## Task Commits

Each task was committed atomically:

1. **Task 1: Add CurrentRoomId index and result types** - `54d25c2` (feat)
2. **Task 2: Implement real NPC queries with disambiguation** - `96f69c2` (feat)
3. **Task 3: Add integration tests for target resolution** - `43f902e` (test)

## Files Created/Modified

- `Mordecai.Web/Services/TargetResolutionService.cs` - Real NPC queries with FindNpcInRoomAsync and disambiguation
- `Mordecai.Web/Data/ApplicationDbContext.cs` - Added HasIndex for ActiveSpawn.CurrentRoomId
- `Mordecai.Web/Data/DesignTimeDbContextFactory.cs` - Design-time factory for migrations without credentials
- `Mordecai.Web.Tests/TargetResolutionServiceTests.cs` - 12 integration tests for target resolution
- `Mordecai.Web.Tests/CombatSoundPropagationTests.cs` - Fixed pre-existing broken test

## Decisions Made

- **Index already existed:** CurrentRoomId index (IX_ActiveSpawns_CurrentRoomId) was created in AddSpawnerSystem migration, no new migration needed
- **Ordered disambiguation:** NPCs ordered by Name then Id for consistent, predictable disambiguation ordering
- **Prefix-only:** Per CONTEXT.md, only prefix matching (StartsWith), not Contains

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fixed pre-existing broken test in CombatSoundPropagationTests**
- **Found during:** Task 1 (during build for migration)
- **Issue:** CombatSoundPropagationTests instantiated CombatMessageBroadcastService with wrong constructor signature (service was refactored to use IServiceScopeFactory)
- **Fix:** Removed unused CombatMessageBroadcastService instantiation from test (test was calling SoundPropagationService directly anyway)
- **Files modified:** Mordecai.Web.Tests/CombatSoundPropagationTests.cs
- **Verification:** Build succeeds, all tests pass
- **Committed in:** 54d25c2 (Task 1 commit)

**2. [Rule 3 - Blocking] Created DesignTimeDbContextFactory for migrations**
- **Found during:** Task 1 (migration creation failed without database credentials)
- **Issue:** dotnet ef migrations add failed because Program.cs throws if Database:Password is missing
- **Fix:** Added IDesignTimeDbContextFactory implementation with dummy connection string
- **Files modified:** Mordecai.Web/Data/DesignTimeDbContextFactory.cs (created)
- **Verification:** Migration commands work without database
- **Committed in:** 54d25c2 (Task 1 commit)

**3. [Rule 1 - Bug] Index already existed, removed empty migration**
- **Found during:** Task 1 (migration contained only Identity column changes, no index)
- **Issue:** IX_ActiveSpawns_CurrentRoomId was already created in AddSpawnerSystem migration
- **Fix:** Removed unnecessary migration files, restored model snapshot
- **Files modified:** Removed migration files, restored ApplicationDbContextModelSnapshot.cs
- **Verification:** git log shows no migration commit
- **Committed in:** Not committed (reverted before commit)

---

**Total deviations:** 3 auto-fixed (1 bug discovery, 2 blocking issues)
**Impact on plan:** All auto-fixes necessary for correctness and build. No scope creep.

## Issues Encountered

- Database password requirement for EF Core design-time - solved with DesignTimeDbContextFactory
- Index already existed from prior migration - verified and documented (no new migration needed)

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Target resolution fully functional with real ActiveSpawn entities
- FindNpcInRoomAsync returns discriminated union enabling callers to handle all cases
- Ready for Phase 2 (Combat Sessions) to use target resolution for combat commands
- No blockers or concerns

---
*Phase: 01-target-resolution*
*Completed: 2026-01-26*
