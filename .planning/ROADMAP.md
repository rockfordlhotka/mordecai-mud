# Roadmap: Mordecai MUD - NPC Combat System

## Overview

This roadmap delivers real-time NPC combat through five phases: first enabling NPCs to be targeted, then managing combat session lifecycles, adding messaging and continuous round processing, implementing NPC AI behavior, and finally polishing with penalties and multi-combatant tracking. Each phase builds on the existing combat foundation (CombatService, equipment integration, 4dF mechanics) to create autonomous NPCs that fight back using identical mechanics to players.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Target Resolution** - Enable NPCs to be targeted for combat via ActiveSpawn entities
- [x] **Phase 2: Combat Orchestration** - Manage combat session lifecycle (create, track, end)
- [x] **Phase 3: Messaging & Round Tick** - Broadcast combat to room and process health pools continuously
- [x] **Phase 4: NPC AI Behavior** - NPCs make autonomous combat decisions (attack, defend, flee)
- [x] **Phase 5: Combat Polish** - Add timed penalties and multi-combatant session tracking
- [x] **Phase 6: Combat UI** - Visual feedback for combat (health bars, combat log, status indicators)

## Phase Details

### Phase 1: Target Resolution
**Goal**: Players can target NPCs in their room for combat commands
**Depends on**: Nothing (first phase)
**Requirements**: TGT-01, TGT-02, TGT-03
**Success Criteria** (what must be TRUE):
  1. "attack goblin" command finds the correct ActiveSpawn NPC in the player's room
  2. ActiveSpawn entities have CurrentRoomId populated and queryable
  3. TargetResolutionService returns real NPC data instead of simulated/hardcoded values
**Plans**: 1 plan

Plans:
- [x] 01-01-PLAN.md - Implement real NPC target resolution with disambiguation

### Phase 2: Combat Orchestration
**Goal**: Combat sessions manage participant state from first attack to death/flee
**Depends on**: Phase 1
**Requirements**: ORCH-01, ORCH-02, ORCH-03, ORCH-04
**Success Criteria** (what must be TRUE):
  1. Attacking an NPC creates a CombatSession if none exists for that NPC
  2. CombatParticipant records track attacker and defender with combat state (parry mode, penalties)
  3. Combat session ends automatically when a participant dies or successfully flees
  4. Subsequent attacks against same NPC reuse existing combat session
**Plans**: TBD

Plans:
- [x] 02-01: Combat session lifecycle implementation

### Phase 3: Messaging & Round Tick
**Goal**: Combat actions broadcast to room and health pools drain continuously
**Depends on**: Phase 2
**Requirements**: MSG-01, MSG-02, MSG-03, TICK-01, TICK-02, TICK-03, TICK-04
**Success Criteria** (what must be TRUE):
  1. All players in room see combat narration (attacks, hits, misses, flees) via zone-scoped messages
  2. Affected participants receive personal messages with detailed damage breakdown
  3. CombatRoundTickService runs every 3 seconds processing all active combat sessions
  4. Pending FAT/VIT pools drain to actual values each tick (half per tick)
  5. Expired timed penalties are removed each tick
**Plans**: TBD

Plans:
- [x] 03-01: Combat messaging and NPC health tick

### Phase 4: NPC AI Behavior
**Goal**: NPCs fight back autonomously using same mechanics as players
**Depends on**: Phase 3
**Requirements**: AI-01, AI-02, AI-03, AI-04, TICK-05
**Success Criteria** (what must be TRUE):
  1. When attacked, NPCs counterattack the player automatically
  2. NPCs select appropriate defense mode (dodge/parry/shield) based on FAT and equipment
  3. NPCs attempt to flee when VIT drops below threshold (modified by personality)
  4. NPC flee attempts use skill checks and can be intercepted
  5. Each tick executes NPC AI decisions for all NPCs in active combat
**Plans**: TBD

Plans:
- [x] 04-01: NPC AI behavior implementation

### Phase 5: Combat Polish
**Goal**: Complete combat loop with timed penalties and multi-combatant tracking
**Depends on**: Phase 4
**Requirements**: MECH-01, MULT-01
**Success Criteria** (what must be TRUE):
  1. Failed attacks with SV < -3 apply timed AV penalties per damage table
  2. CombatSession can track multiple participants (for future group combat)
**Plans**: TBD

Plans:
- [x] 05-01: Verification (already implemented)

### Phase 6: Combat UI
**Goal**: Visual feedback for combat makes the system usable and immersive
**Depends on**: Phase 5
**Requirements**: UI-01, UI-02, UI-03
**Success Criteria** (what must be TRUE):
  1. Health bars show current (blue), pending damage (red), pending healing (green)
  2. Combat panel appears when in combat, collapses when peaceful
  3. Combat messages are color-coded by type (hits, misses, flee)
  4. Status indicators show parry mode, exhausted, wounded states
  5. NPC health displayed as descriptive text in messages
**Plans**: 1 plan

Plans:
- [x] 06-01: Combat UI components and integration

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5 -> 6

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Target Resolution | 1/1 | Complete | 2026-01-26 |
| 2. Combat Orchestration | 1/1 | Complete | 2026-01-30 |
| 3. Messaging & Round Tick | 1/1 | Complete | 2026-01-30 |
| 4. NPC AI Behavior | 1/1 | Complete | 2026-01-30 |
| 5. Combat Polish | 1/1 | Complete | 2026-01-30 |
| 6. Combat UI | 1/1 | Complete | 2026-01-31 |

**ALL PHASES COMPLETE** - NPC Combat System roadmap finished

---
*Roadmap created: 2026-01-26*
*Last updated: 2026-01-31*
