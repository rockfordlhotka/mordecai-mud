# Requirements: Mordecai MUD - NPC Combat System

**Defined:** 2026-01-26
**Core Value:** Players can engage in real-time combat with NPCs in rooms, with NPCs fighting back autonomously using the same skill-based 4dF mechanics as players, creating immersive tactical combat until death or flee.

## v1 Requirements

Requirements for initial NPC combat system. Each maps to roadmap phases.

### Target Resolution

- [ ] **TGT-01**: TargetResolutionService queries ActiveSpawn entities instead of simulated NPC data
- [ ] **TGT-02**: ActiveSpawn entities track CurrentRoomId for room-based targeting
- [ ] **TGT-03**: "attack goblin" command resolves to correct ActiveSpawn NPC in same room as player

### Combat Orchestration

- [ ] **ORCH-01**: Attacking an NPC creates CombatSession automatically if none exists
- [ ] **ORCH-02**: CombatParticipant entities created for attacker and defender when combat starts
- [ ] **ORCH-03**: Combat session ends when participant dies or successfully flees
- [ ] **ORCH-04**: Combat state tracked per participant (parry mode, timed penalties, last attack time)

### NPC AI Behavior

- [ ] **AI-01**: NPC counterattacks automatically when attacked by player
- [ ] **AI-02**: NPC selects defense mode (dodge/parry/shield) based on available FAT and equipment
- [ ] **AI-03**: NPC attempts flee when VIT drops below threshold (25-30% modified by personality)
- [ ] **AI-04**: NPC flee attempt uses skill check, can be intercepted/pursued

### Combat Round Tick

- [ ] **TICK-01**: CombatRoundTickService runs as BackgroundService with 3-second tick interval
- [ ] **TICK-02**: Each tick recovers 1 FAT for participants not taking damage this round
- [ ] **TICK-03**: Each tick drains half of pending FAT/VIT pools to actual health values
- [ ] **TICK-04**: Each tick removes expired timed AV penalties from participants
- [ ] **TICK-05**: Each tick executes NPC AI decisions (attack/defend/flee) for all active NPCs in combat

### Combat Messaging

- [ ] **MSG-01**: Global room messages broadcast all combat actions (attacks, hits, misses, flees) to everyone in room
- [ ] **MSG-02**: Personal messages send detailed damage breakdown (armor absorption, FAT/VIT damage, wounds) to affected participant
- [ ] **MSG-03**: Messages use RabbitMQ scoped messaging (MessageScope.Zone for room, MessageScope.Personal for details)

### Combat Mechanics

- [ ] **MECH-01**: Failed attacks with SV < -3 apply timed AV penalties to attacker per damage table

### Multiple Combatants

- [ ] **MULT-01**: CombatSession tracks all participants (players and NPCs) in same combat instance

## v2 Requirements

Deferred to future releases after core combat loop validated.

### Combat Mechanics (Deferred)

- **MECH-02**: Load real character skill levels from CharacterSkills table instead of hardcoded values
- **MECH-03**: Reduce armor durability when armor absorbs damage during combat

### Multiple Combatants (Deferred)

- **MULT-02**: Additional players can attack NPCs already in combat (join ongoing fight)
- **MULT-03**: NPCs with IsGroupAssist=true join combat when nearby ally is attacked

### Advanced NPC AI (Deferred)

- **AI-05**: NPCs use consumable items (potions, scrolls) during combat
- **AI-06**: NPCs coordinate tactics when multiple NPCs in same combat
- **AI-07**: NPC personality affects aggressiveness (attack frequency, risk tolerance)

### Combat UI (Deferred)

- **UI-01**: Combat log displays recent actions in dedicated UI panel
- **UI-02**: Health bars show current FAT/VIT for all visible combatants
- **UI-03**: Status indicators show combat mode (parry, fleeing, exhausted)

## Out of Scope

Explicitly excluded from this project. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Magic/spell combat | Separate magic system implementation, different mechanics |
| Ranged combat AI | Foundation exists but NPC ranged behavior needs separate design |
| PvP combat improvements | Already functional, not part of NPC focus |
| Advanced NPC abilities (special attacks) | Start with basic melee, add complexity later |
| Boss encounter mechanics | Requires scripting system beyond basic AI |
| Loot drops on NPC death | Separate loot system feature |
| Experience/skill gain from combat | Skill progression already implemented separately |
| Combat replay/history UI | Enhanced UI beyond basic requirements |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| TGT-01 | TBD | Pending |
| TGT-02 | TBD | Pending |
| TGT-03 | TBD | Pending |
| ORCH-01 | TBD | Pending |
| ORCH-02 | TBD | Pending |
| ORCH-03 | TBD | Pending |
| ORCH-04 | TBD | Pending |
| AI-01 | TBD | Pending |
| AI-02 | TBD | Pending |
| AI-03 | TBD | Pending |
| AI-04 | TBD | Pending |
| TICK-01 | TBD | Pending |
| TICK-02 | TBD | Pending |
| TICK-03 | TBD | Pending |
| TICK-04 | TBD | Pending |
| TICK-05 | TBD | Pending |
| MSG-01 | TBD | Pending |
| MSG-02 | TBD | Pending |
| MSG-03 | TBD | Pending |
| MECH-01 | TBD | Pending |
| MULT-01 | TBD | Pending |

**Coverage:**
- v1 requirements: 20 total
- Mapped to phases: 0 (awaiting roadmap)
- Unmapped: 20 ⚠️

---
*Requirements defined: 2026-01-26*
*Last updated: 2026-01-26 after initial definition*
