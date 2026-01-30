# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-26)

**Core value:** Players can engage in real-time combat with NPCs in rooms, with NPCs fighting back autonomously using the same skill-based 4dF mechanics as players, creating immersive tactical combat until death or flee.
**Current focus:** NPC Combat System COMPLETE 🎉

## Current Position

Phase: 5 of 5 (Combat Polish) - COMPLETE
Plan: 1 of 1 in current phase
Status: ROADMAP COMPLETE
Last activity: 2026-01-30 - Verified 05-01 (already implemented)

Progress: [##########] 100%

## Performance Metrics

**Velocity:**
- Total plans completed: 5
- Average duration: 12 min
- Total execution time: 1 hour

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-target-resolution | 1 | 9 min | 9 min |
| 02-combat-orchestration | 1 | 20 min | 20 min |
| 03-messaging-round-tick | 1 | 15 min | 15 min |
| 04-npc-ai-behavior | 1 | 15 min | 15 min |
| 05-combat-polish | 1 | 5 min | 5 min (verification) |

**Recent Trend:**
- Last 5 plans: 9 min, 20 min, 15 min, 15 min, 5 min
- Trend: Complete - roadmap finished

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

| Date | Phase | Decision | Rationale |
|------|-------|----------|-----------|
| 2026-01-26 | 01-01 | Prefix-only matching | Per CONTEXT.md, no Contains matching for NPC names |
| 2026-01-26 | 01-01 | 1-based disambiguation index | "goblin 2" matches MUD conventions |
| 2026-01-26 | 01-01 | ActiveSpawns ordered by Name then Id | Consistent disambiguation ordering |
| 2026-01-30 | 02-01 | Join target's session when target in combat | Prevents duplicate sessions for same NPC |
| 2026-01-30 | 02-01 | Explicit state init (IsInParryMode=false, JoinedAt=UtcNow) | Ensure consistent participant state |
| 2026-01-30 | 03-01 | Extend HealthTickBackgroundService for NPCs | Reuse existing 3s tick, no new service |
| 2026-01-30 | 03-01 | NPC max VIT/FAT from template attributes | Same formula as Characters: VIT=(STR*2)-5, FAT=(END+WIL)-5 |
| 2026-01-30 | 04-01 | INpcAiService for AI decisions | Separate service for testability, called from tick |
| 2026-01-30 | 04-01 | AI priority: flee → defense → attack | Survival instinct first, then optimize defense, then counterattack |
| 2026-01-30 | 04-01 | Default flee threshold 25% VIT | Configurable via NpcTemplate.BehaviorConfig JSON |
| 2026-01-30 | 04-01 | Parry mode when FAT < 3 | Conserve stamina by switching to no-cost parry defense |

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-01-30 20:55 UTC
Stopped at: Completed 02-01-PLAN.md (Phase 2 complete)
Resume file: None
