# Mordecai MUD - NPC Combat System

## What This Is

Completing the player-vs-NPC combat system for Mordecai MUD, a skill-based text-based multi-user dungeon. This project connects existing combat mechanics (attack resolution, damage calculation, equipment integration) with NPC AI behavior, real-time target resolution, and continuous 3-second round processing to create fully functional autonomous NPC combat.

## Core Value

Players can engage in real-time combat with NPCs in rooms, with NPCs fighting back autonomously using the same skill-based 4dF mechanics as players, creating immersive tactical combat until death or flee.

## Requirements

### Validated

- ✓ Combat calculation engine (attack, defense, damage resolution) — existing (CombatService)
- ✓ Equipment integration (weapons/armor affect combat, bonuses apply) — existing (COMBAT_EQUIPMENT_INTEGRATION.md)
- ✓ Character attributes and skills system — existing
- ✓ Combat entities (CombatSession, CombatParticipant, CombatActionLog, NPC/spawner entities) — existing
- ✓ Command handlers (attack, flee, parry) — existing (GameActionService)
- ✓ Weapon/armor durability tracking in database — existing
- ✓ 4dF dice mechanics — existing (DiceService)
- ✓ Message broadcasting infrastructure (RabbitMQ, scoped messages) — existing
- ✓ NPC spawning system — existing (SpawnerTickService, ActiveSpawn entities)
- ✓ Hit location system (d12 roll) — existing in damage calculations
- ✓ Armor absorption mechanics — existing

### Active

- [ ] NPC target resolution - Replace simulated NPCs with real ActiveSpawn entity lookups in TargetResolutionService
- [ ] Room-character position tracking - Track which ActiveSpawns are in which rooms for accurate targeting
- [ ] NPC AI behavior system - Decision-making for attack/defend/parry/flee based on health and personality
- [ ] Combat round tick service - 3-second background service for FAT recovery, pool drainage, effect expiration, NPC AI execution
- [ ] Combat session orchestration - Manage session lifecycle (create on first attack, add joiners, end on death/flee)
- [ ] NPC autonomous combat - NPCs counterattack automatically when attacked or when AI decides
- [ ] Combat messaging - Scoped messages (global room announcements + personal damage details)
- [ ] Skill level loading - Load real character skill levels from CharacterSkills table (remove hardcoded values)
- [ ] Armor durability damage - Reduce armor durability when it absorbs damage
- [ ] Flee skill check - NPC flee attempts use skill check, can be pursued/intercepted
- [ ] Timed penalty system - Failed attacks with negative SV apply temporary AV penalties
- [ ] Multiple combatant support - Additional players/NPCs can join ongoing combat sessions

### Out of Scope

- Magic/spell combat — Deferred to separate magic system implementation
- Ranged combat with NPCs — Foundation exists but AI behavior deferred
- Group tactics/formations — Beyond initial combat system
- Advanced NPC abilities (special attacks, skills) — Start with basic melee only
- PvP combat improvements — Already functional, separate concern
- Status effects beyond timed penalties — Future enhancement
- Boss mechanics/scripted encounters — Future enhancement
- Combat logs/history UI — Basic logging exists, enhanced UI deferred
- Loot drops on NPC death — Separate loot system feature
- Experience/skill gain from combat — Skill progression already implemented separately

## Context

**Brownfield codebase with substantial combat foundation:**
- CombatService (1,185 lines) implements full attack/defense/damage flow per MORDECAI_SPECIFICATION.md
- TargetResolutionService exists but uses simulated NPCs (lines 63, 100, 127, 160 marked TODO)
- Combat entities fully defined (CombatSession, CombatParticipant, ActiveSpawn with health tracking)
- NPC templates include attributes, personality traits (IsHostile, IsGroupAssist, CanWander, BehaviorConfig)
- Equipment integration complete (weapons add AV/SV bonuses, armor provides absorption by damage type)

**Architecture:**
- Event-driven via RabbitMQ for all combat messages (broadcast to room, personal damage details)
- Blazor Server provides real-time UI updates
- Background services pattern established (SpawnerTickService model for CombatRoundTickService)
- Scoped dependency injection for services with database access

**Game mechanics:**
- Continuous combat (not turn-based) with 3-second rounds in background
- FAT-based action throttling (players can spam commands but FAT costs create natural limits)
- Attack flow: AS + 4dF + weapon bonus → AV vs TV → SV → hit location (d12) → armor absorption → damage pools
- Health system: Pending damage pools drain half per tick (3 seconds), wounds cause ongoing FAT damage
- NPCs use identical mechanics to players (attributes → derived stats, skill checks, equipment effects)

**Known issues to address (from CONCERNS.md):**
- Skill levels hardcoded in CombatService line 495 (need database lookup)
- TargetResolutionService incomplete NPC lookup (lines 63, 100, 127, 160)
- No room-character relationship tracking (characters not associated with current room)
- No NPC AI implementation (NPCs don't fight back)
- No continuous round processing (one-shot attacks only)

## Constraints

- **Tech stack**: .NET 9, Blazor Server, PostgreSQL, RabbitMQ, EF Core — Must follow established patterns
- **Architecture**: Service layer pattern (Game/Services), message-driven updates, background services for ticks
- **Scale**: Optimized for 10-50 concurrent players, intimate community MUD
- **Combat mechanics**: Must precisely follow MORDECAI_SPECIFICATION.md formulas (4dF, AS/TV/SV calculations, damage tables)
- **Compatibility**: Cannot break existing character/skill/equipment systems
- **Performance**: 3-second tick must handle all active combat sessions without lag

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Continuous rounds (3 sec tick) vs turn-based | Matches spec, FAT throttling provides pacing | — Pending |
| Simple AI (behavior flags) vs complex decision trees | Start simple, personality-driven (IsHostile, flee threshold) | — Pending |
| NPC actions in round tick vs event-driven | Background tick simplifies state management | — Pending |
| Real-time messaging via RabbitMQ | Matches existing architecture, broadcasts to all in room | — Pending |
| ActiveSpawn entity for NPCs vs separate Npc table | ActiveSpawn already tracks health/location, reuse it | — Pending |

---
*Last updated: 2026-01-26 after initialization*
