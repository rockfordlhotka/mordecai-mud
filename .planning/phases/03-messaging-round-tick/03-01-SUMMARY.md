# Plan 03-01 Summary: Combat Messaging and NPC Health Tick

**Status:** ✅ Complete
**Duration:** 15 min
**Date:** 2026-01-30

## Objectives Achieved

### 1. NPC Health Tick Processing
Extended `HealthTickBackgroundService` to process ActiveSpawn (NPC) entities alongside Characters:
- Added `ProcessNpcPendingHealthAsync()` to main tick loop
- Calculates max VIT from template: `(Strength × 2) - 5`
- Calculates max FAT from template: `(Endurance + Willpower) - 5`
- Processes pending damage same as Characters (drain half per tick, min 1)
- Broadcasts NpcStatChanged messages for UI updates

### 2. Timed Penalty Expiration
Added automatic cleanup of expired combat penalties:
- Added `ProcessTimedPenaltiesAsync()` to tick loop
- Queries participants with non-empty `TimedPenaltiesJson`
- Deserializes, filters out expired penalties, serializes back
- Saves changes only when penalties actually removed

### 3. Combat Messaging (Already Implemented)
Verified existing messaging infrastructure meets requirements:
- CombatAction messages published for all hits/misses (MSG-01 ✓)
- Messages include damage breakdown (MSG-02 ✓)
- Messages scoped via RoomId property (MSG-03 ✓)

## Requirements Completed

| ID | Requirement | Status |
|----|-------------|--------|
| TICK-01 | 3-second background tick | ✅ Pre-existing |
| TICK-03 | Drain pending health pools | ✅ Extended for NPCs |
| TICK-04 | Remove expired timed penalties | ✅ New |
| MSG-01 | Room-wide combat broadcasts | ✅ Pre-existing |
| MSG-02 | Personal damage messages | ✅ Pre-existing |
| MSG-03 | RabbitMQ scoped messaging | ✅ Pre-existing |

## Requirements Deferred

| ID | Requirement | Reason |
|----|-------------|--------|
| TICK-02 | FAT recovery in combat | Needs "last damage received" tracking |
| TICK-05 | NPC AI decisions | Phase 4 scope |

## Files Modified

- `Mordecai.Web/Services/HealthTickBackgroundService.cs`
  - Added `ProcessNpcPendingHealthAsync()` method
  - Added `ProcessTimedPenaltiesAsync()` method  
  - Added NPC FAT/VIT processing helpers

## Tests Added

- `Mordecai.Web.Tests/HealthTickNpcTests.cs` (4 tests)
  - `ProcessNpcPendingHealth_DrainsPendingDamage`
  - `ProcessNpcPendingHealth_RespectsMaxValues`
  - `ProcessTimedPenalties_RemovesExpired`
  - `ProcessTimedPenalties_KeepsActive`

## Architecture Notes

### NPC VIT/FAT Calculation
NPCs use same formulas as Characters but derive from NpcTemplate attributes:
- Max VIT = `(NpcTemplate.Strength × 2) - 5`
- Max FAT = `(NpcTemplate.Endurance + NpcTemplate.Willpower) - 5`

### Tick Processing Order
1. Character pending health (existing)
2. NPC pending health (new)
3. Timed penalty expiration (new)

### Test Infrastructure
Tests use stub implementations (no Moq):
- `StubDiceService` for deterministic rolls
- `StubMessagePublisher` to capture messages
- In-memory SQLite database

## Next Phase

Ready for **Phase 4: NPC AI Behavior** which includes:
- AI-01: NPC counterattacks when attacked
- AI-02: NPC defense mode selection
- AI-03: NPC flee behavior at low VIT
- AI-04: Flee skill checks
- TICK-05: Execute NPC AI decisions each tick
