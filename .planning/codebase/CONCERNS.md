# Codebase Concerns

**Analysis Date:** 2026-01-26

## Tech Debt

### Stub Project Placeholders

**Issue:** Two projects (`Mordecai.Data` and `Mordecai.BackgroundServices`) contain empty `Class1.cs` placeholder files that should be removed or properly implemented.

**Files:**
- `S:\src\rdl\mordecai-mud\Mordecai.Data\Class1.cs`
- `S:\src\rdl\mordecai-mud\Mordecai.BackgroundServices\Class1.cs`

**Impact:** Creates confusion about project purpose; violates clean code principles. These projects exist in the solution but lack substantive content or clear responsibilities.

**Fix approach:**
- Remove `Class1.cs` files or properly rename/repurpose the projects
- `Mordecai.Data` should contain repository implementations and data access abstractions (currently EF Core is handled directly in `Mordecai.Web`)
- `Mordecai.BackgroundServices` should contain the `SpawnerTickService` implementation but currently only has this one service

### Incomplete RoomService Stub Implementation

**Issue:** `Mordecai.Game\Services\RoomService.cs` contains multiple TODO comments and placeholder methods that return empty/null results. The service lacks a proper DbContext dependency.

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Game\Services\RoomService.cs` (lines 87, 124, 139, 158, 170)

**Impact:**
- Methods like `GetRoomAsync()`, `GetRoomExitsAsync()`, `GetCharactersInRoomAsync()` return null/empty collections
- Blocks room-based features that depend on these methods
- Game.Services layer cannot access database; only Web.Services has context access

**Fix approach:**
- Inject `IDbContextFactory<ApplicationDbContext>` into `RoomService`
- Complete all TODO implementations with actual database queries
- Consider whether `Mordecai.Game.Services` should depend on Web layer, or move service implementations to Web.Services

### Missing CombatService Skill Level Loading

**Issue:** Combat service placeholder comment at line 495 indicates skill levels are hardcoded rather than loaded from database.

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\CombatService.cs` (line 495)

**Impact:** Combat calculations don't use actual character skill levels, making balance testing impossible.

**Fix approach:** Implement skill loading from `CharacterSkills` table before combat resolution.

### Unimplemented Target Resolution Service

**Issue:** `TargetResolutionService` contains TODOs for NPC/Mob entity lookup and proper room filtering (lines 63, 100, 127, 160).

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\TargetResolutionService.cs`

**Impact:**
- NPC and Mob targeting not implemented
- Room-character relationships incomplete
- Player-to-NPC combat will not resolve targets properly

**Fix approach:** Complete NPC/Mob entity implementations and implement room-aware target filtering.

## Test Coverage Gaps

### Limited Test Suite

**Issue:** Only 15 test files exist for a game with complex mechanics across 10 projects.

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Web.Tests\` (15 test files)

**Risk:**
- No test coverage for service layer in most projects
- Game mechanics (4dF dice, skill progression, combat resolution) lack comprehensive tests
- Database migration changes lack integration test validation
- Skill progression anti-abuse rules untested

**Priority:** High - Game mechanics correctness is critical

### Missing Integration Tests for Item System

**Issue:** Item and inventory system was recently added (as documented in `ITEM_IMPLEMENTATION_STATUS.md`) but database migration not yet applied and services not yet implemented.

**Files:** Items exist in entities but not yet in services layer

**Risk:**
- When item services are implemented, no tests will validate integration with existing systems
- Equipment bonuses might not cascade correctly to attributes/skills
- Inventory capacity validation untested

**Priority:** High - Must be tested before item features go live

## Missing Critical Features

### Item System Incomplete

**Issue:** Item system entities defined but core services not yet implemented.

**Status:** Foundation complete, Phase 2-7 (services, commands, UI, integration) not started.

**Files:**
- Entities: `S:\src\rdl\mordecai-mud\Mordecai.Game\Entities\ItemEntities.cs`
- DbContext: `S:\src\rdl\mordecai-mud\Mordecai.Web\Data\ApplicationDbContext.cs` (lines 28-35)
- Status doc: `docs\ITEM_IMPLEMENTATION_STATUS.md`

**Impact:**
- Players cannot pick up, drop, or manage items
- Equipment system exists but not wired to combat
- No inventory UI
- Game progression severely limited without items/equipment

**Priority:** High - Blocks core gameplay

**Required Actions:**
1. Create database migration: `dotnet ef migrations add AddItemAndInventorySystem`
2. Implement `IItemService`, `IInventoryService`, `IContainerService`, `IEquipmentService`
3. Add command handlers for item interaction
4. Wire equipment bonuses into skill/attribute calculation
5. Create UI for inventory management

### Skill Progression Anti-Abuse Not Implemented

**Issue:** Specification defines anti-abuse mechanics in `SKILL_PROGRESSION_ANTI_ABUSE.md` but no implementation exists in code.

**Specified mechanics (lines 16-97 of spec):**
- Hourly usage diminishing returns (soft caps at 50/100/150 uses/hour)
- Context-aware validation (same target, same location detection)
- Skill use cooldown per target/context (30s-120s depending on type)

**Files:** Not found in codebase - missing entirely

**Impact:** Players can automate skill grinding with no penalty. Skill-based progression system breaks down without abuse prevention.

**Priority:** High - Must be implemented before gameplay becomes exploitable

**Fix approach:** Implement in `SkillService` using tracking in `SkillUsageLog` table.

## Architecture Issues

### Layered Service Dependency Mismatch

**Issue:** `Mordecai.Game` project is meant for domain models but contains service implementations (`RoomService`) that cannot access the database without depending on `Mordecai.Web`.

**Files:**
- `Mordecai.Game\Services\RoomService.cs` - lacks DbContext
- `Mordecai.Web\Services\*` - all services access database directly

**Impact:**
- Game layer cannot function independently
- Unclear separation of concerns
- Makes testing difficult (can't test Game services in isolation)

**Fix approach:**
Either move all service implementations to `Mordecai.Web.Services` (recommended), or inject `IDbContextFactory` into Game services and accept the circular dependency.

### Nested Exception Handling in WorldService

**Issue:** `WorldService.GetRoomByIdAsync()` has nested try-catch blocks (lines 119-141) that catch identical exceptions.

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\WorldService.cs` (lines 119-141)

**Impact:** Redundant code, harder to reason about error handling.

**Fix approach:** Remove inner try-catch and handle single exception at outer level.

### Multiple DbContext Instances in Same Request

**Issue:** Several service methods create multiple DbContext instances in a single method (e.g., `WorldService.GetStartingRoomAsync()` lines 45, 63).

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\WorldService.cs` (lines 37-113)

**Impact:**
- Potential inconsistent reads if data changes between context instances
- Inefficient connection usage
- Can trigger spurious conflicts in optimistic concurrency

**Fix approach:** Reuse single context instance throughout async method or accept cost if queries are intentionally sequential.

## Fragile Areas

### Skill Definition Caching Issue

**Issue:** `CharacterService` uses cached `_cachedFocusSkillDefinitionId` and `_cachedDriveSkillDefinitionId` fields (lines 32-33) that are instance-scoped but service is likely registered as scoped per-request.

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\CharacterService.cs` (lines 32-33)

**Impact:**
- Cache lifetime unclear - may cache indefinitely or per-request
- If service lifetime changes, subtle bugs may appear
- Other services may reinitialize cache independently

**Fix approach:**
- Move to static readonly lazy-initialized properties if truly static
- Or pass skill definition IDs as constructor parameters
- Or fetch fresh each time (prefer simplicity over premature caching)

### Equipment Service Slot Inference Heuristics

**Issue:** `EquipmentService` uses hardcoded lookup tables and heuristics to infer equipment slots from item names (lines 28-88).

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\EquipmentService.cs` (lines 28-88)

**Impact:**
- Slot assignment fragile to item naming variations
- No validation that inferred slot matches item category
- Will fail silently if heuristics don't match actual item

**Safe modification:**
- Always explicitly store `EquipSlot` in item template
- Use heuristics only as fallback with logging
- Add validation test for each new armor piece

**Test coverage:** Add tests for slot inference with various item names.

### Hardcoded Damage Class Values

**Issue:** `CombatService` and tests use hardcoded `DamageClass.Class1` values without validation.

**Files:**
- `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\CombatService.cs` (line 509)
- Multiple test files: `CombatEquipmentIntegrationTests.cs`, `EquipmentServiceItemTests.cs`

**Impact:**
- Damage calculations may not match item definitions
- No validation that weapon damage class is valid
- Equipment bonuses might not apply if classes don't match

**Safe modification:**
- Load damage class from equipped weapon
- Validate against item template definition
- Add assertion tests for class matching

## Security Considerations

### RabbitMQ Graceful Degradation Risk

**Issue:** RabbitMQ publisher and subscriber intentionally continue if RabbitMQ is unavailable (marked as "offline mode" in code).

**Files:**
- `S:\src\rdl\mordecai-mud\Mordecai.Messaging\Services\RabbitMqGameMessagePublisher.cs` (lines 48-52)
- `S:\src\rdl\mordecai-mud\Mordecai.Messaging\Services\RabbitMqGameMessageSubscriber.cs` (lines 56-73)

**Risk:**
- Silent message loss during RabbitMQ outages
- Players broadcast combat/chat that never reaches others
- Game state inconsistency between servers if ever scaled
- No alerting when messaging fails

**Current mitigation:** Logging at WARNING level when offline.

**Recommendations:**
- Make offline mode explicit in configuration (opt-in flag)
- Consider circuit breaker pattern with monitored health checks
- Add metrics for failed message publishes
- Test message loss scenarios explicitly

### No Input Validation on Game Commands

**Issue:** Skill checks, combat actions, and movement commands lack explicit input validation for bounds.

**Files:** Services in `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\` generally use try-catch, but no validation on:
- Skill IDs/levels before use
- Target IDs before combat resolution
- Room IDs before movement

**Risk:**
- Invalid data could crash services (caught by try-catch but logged as error)
- Boundary conditions (negative levels, non-existent IDs) not explicitly handled
- No consistent validation layer

**Fix approach:**
- Create validation helper in `GameActionService` or separate validator
- Validate all inputs before business logic
- Return structured error result instead of catching exceptions

### Unused Project Dependencies

**Issue:** `Mordecai.Data` and `Mordecai.BackgroundServices` projects exist but lack clear dependencies or usage.

**Impact:** Adds maintenance burden and confusion about architecture.

## Performance Bottlenecks

### Multiple Sequential DbContext Queries

**Issue:** `WorldService.GetStartingRoomAsync()` performs 2-3 sequential database queries as fallback chain (lines 46-93).

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\WorldService.cs` (lines 37-113)

**Current performance:** Acceptable for startup, but shows pattern of sequential fallback queries.

**Improvement path:**
- Combine into single query with OR conditions
- Cache starting room in configuration at startup
- Return immediately on first match rather than three attempts

### Equipment Service's Slot Lookup Cost

**Issue:** `EquipmentService` performs full heuristic lookup on every equipment slot assignment.

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\EquipmentService.cs` (lines 98-135+)

**Current performance:** Not critical for 10-50 player scale, but scales poorly if items per character increase.

**Improvement path:**
- Cache inferred slots in item template
- Pre-calculate during item creation
- Use explicit slot storage instead of inference

### Combat Service N+1 Query Risk

**Issue:** `CombatService.PerformMeleeAttackAsync()` calls `GetWeaponSkillAsync()` (line 145) which may load skills sequentially.

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\CombatService.cs` (line 145)

**Current performance:** Depends on skill loading implementation (TODO at line 495).

**Improvement path:** Ensure skill loading uses `.Include()` for related data, not sequential queries.

### CombatService Code Complexity

**Issue:** `CombatService` is 1,185 lines with multiple complex methods handling attack resolution, damage calculation, and state management.

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.Web\Services\CombatService.cs`

**Impact:** Hard to test individual mechanics; likely to accumulate bugs.

**Scaling risk:** Each new combat feature (status effects, special abilities, environmental damage) adds complexity.

**Improvement path:**
- Extract damage calculation to `IDamageCalculator` service
- Extract state management to separate service
- Use strategy pattern for different attack types
- Target: max 300 lines per class

## Scaling Limits

### SQLite for Production

**Issue:** Development database is SQLite, with documented plan to migrate to PostgreSQL.

**Current capacity:** SQLite adequate for 10-50 concurrent players with modest world size.

**Limit:**
- ~1,000 concurrent connections cause degradation
- Single file bottleneck for multiple server instances
- No replication/failover support

**Scaling path:**
- Switch to PostgreSQL for production (documented)
- Implement connection pooling
- Consider sharding by zone if world grows beyond 10,000 rooms

### RabbitMQ Single Instance

**Issue:** Development uses single RabbitMQ instance via Aspire.

**Current capacity:** Adequate for 10-50 players; RabbitMQ can handle thousands of msg/sec.

**Limit:** Single point of failure; no clustering/mirroring in development setup.

**Scaling path:** Configure RabbitMQ clustering for production (documented in `KUBERNETES_DEPLOYMENT.md`).

### Spawner Tick Service Timer-Based

**Issue:** `SpawnerTickService` (mentioned in `SpawnerTickService.cs`) likely uses `Timer` or `Task.Delay` for world ticks.

**Files:** `S:\src\rdl\mordecai-mud\Mordecai.BackgroundServices\SpawnerTickService.cs`

**Current performance:**
- Adequate for 10-50 players
- Single background service handles all spawner ticks
- No coordination with other servers

**Scaling limit:** Cannot scale horizontally without distributed timer coordination.

**Scaling path:**
- Use Hangfire or Quartz.NET for distributed job scheduling
- Ensure only one instance runs per tick
- Add jitter to prevent thundering herd

## Dependencies at Risk

### RabbitMQ Client Version Not Specified

**Issue:** No explicit version constraint found for `RabbitMQ.Client` in project files.

**Impact:** Could pull outdated or incompatible version if dependency graph shifts.

**Mitigation:** Ensure `Mordecai.Messaging.csproj` pins to specific stable version.

### Entity Framework Core Shadow Property Usage

**Issue:** If using Shadow Properties for database-only fields, documentation should clarify.

**Impact:** Code readability; domain model doesn't show all persisted state.

**Recommendation:** Review shadow property usage in `OnModelCreating` and document clearly.

## Missing Critical Features

### Room Effects System Incomplete

**Issue:** Database schema includes `RoomEffectDefinition`, `RoomEffectImpact`, and `RoomEffect` entities, but no UI or service layer implementation.

**Files:**
- Entities: Listed in `ApplicationDbContext.cs` (lines 22-26)
- Status: Not mentioned in `ITEM_IMPLEMENTATION_STATUS.md`

**Impact:** Rooms cannot have effects (poison gas, illusions, etc.) even though schema supports it.

**Priority:** Medium - not critical for MVP but needed for world immersion.

## Notes for Future Phases

### Anti-Abuse Implementation Must Precede Public Launch

The skill progression system will be heavily exploited without anti-abuse mechanics. `SKILL_PROGRESSION_ANTI_ABUSE.md` documents required features that must be implemented before players can grind indefinitely.

### Item System Integration Touches Many Systems

Once item services are implemented, they integrate with:
- Combat (weapons/armor)
- Skills (skill bonuses)
- Attributes (attribute modifiers)
- Inventory management
- Character creation (starting items)
- All of these need comprehensive integration tests

### Database Migration Strategy

Current migrations follow PostgreSQL path. If switching databases, ensure:
- Migration history remains intact
- Rollback capability is tested
- Performance characteristics understood for each backend

---

*Concerns audit: 2026-01-26*
