# Plan 04-01 Summary: NPC AI Behavior Implementation

**Status:** ✅ Complete
**Duration:** 15 min
**Date:** 2026-01-30

## Objectives Achieved

### 1. INpcAiService Interface & Implementation
Created new service for NPC AI decision-making:
- `INpcAiService` interface in `Mordecai.Game/Services/`
- `NpcAiService` implementation in `Mordecai.Web/Services/`
- Registered as scoped service in Program.cs

### 2. AI Decision Priority System
Implemented priority-based decision loop:
1. **Flee check** (survival instinct) - Check if VIT below threshold
2. **Defense mode** - Switch to parry when FAT is low
3. **Counterattack** - Attack a player target in the session

### 3. Flee Logic (AI-03, AI-04)
- Default threshold: 25% of max VIT
- Configurable via `NpcTemplate.BehaviorConfig` JSON
- Uses existing `CombatService.FleeFromCombatAsync()`
- Publishes CombatAction message when NPC flees

### 4. Defense Mode Selection (AI-02)
- Switches to parry mode when FAT < 3
- Parry costs no FAT (conserves stamina)
- Dodge costs 1 FAT per defense

### 5. Counterattack Logic (AI-01)
- Selects first player target in combat session
- Uses `CombatService.PerformMeleeAttackAsync()`
- NPC attacks automatically each tick

### 6. Health Tick Integration (TICK-05)
- Added `ProcessNpcAiDecisionsAsync()` to tick loop
- Queries all active combat sessions with NPC participants
- Executes AI for each NPC in combat

## Requirements Completed

| ID | Requirement | Status |
|----|-------------|--------|
| AI-01 | NPC counterattacks automatically | ✅ |
| AI-02 | NPC selects defense mode | ✅ |
| AI-03 | NPC attempts flee at low VIT | ✅ |
| AI-04 | Flee uses skill check | ✅ (via CombatService) |
| TICK-05 | Execute NPC AI each tick | ✅ |

## Files Created

- `Mordecai.Game/Services/INpcAiService.cs` - Interface definition
- `Mordecai.Web/Services/NpcAiService.cs` - Implementation with NpcBehaviorConfig
- `Mordecai.Web.Tests/NpcAiTests.cs` - 6 integration tests

## Files Modified

- `Mordecai.Web/Services/HealthTickBackgroundService.cs` - Added AI tick processing
- `Mordecai.Web/Program.cs` - Registered INpcAiService

## Tests Added (6 total)

1. `DecideAndAct_NpcAtLowVit_AttemptsFlee`
2. `DecideAndAct_NpcWithLowFat_SwitchesToParryMode`
3. `DecideAndAct_NpcWithHighFat_UsesDodgeMode`
4. `GetFleeThreshold_DefaultThreshold_Returns25Percent`
5. `GetFleeThreshold_CustomThreshold_ReturnsConfiguredValue`
6. `DecideAndAct_NpcHealthy_AttacksPlayer`

## Architecture Notes

### NPC Behavior Configuration
NPCs can be customized via `NpcTemplate.BehaviorConfig` JSON:
```json
{
  "FleeThreshold": 0.10,    // 10% VIT to flee
  "NeverFlee": false,       // Fights to the death
  "AggressionLevel": 5      // Future use
}
```

### AI Processing Order in Tick Loop
1. Character pending health (existing)
2. NPC pending health (Phase 3)
3. Timed penalty expiration (Phase 3)
4. **NPC AI decisions (new)**

### Test Infrastructure
Tests use stub implementations for isolation:
- `StubCombatService` - Returns configurable flee/attack results
- `StubMessagePublisher` - Captures published messages
- `TestLogger<T>` - No-op logger for tests

## Total Tests: 102 passing

All existing tests continue to pass after Phase 4 implementation.

## Next Phase

Ready for **Phase 5: Combat Polish** which includes:
- MECH-01: Timed AV penalties for failed attacks (SV < -3)
- MULT-01: Multi-combatant session tracking

This is the final phase of the NPC Combat System roadmap.
