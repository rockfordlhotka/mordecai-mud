# Phase 2: Combat Orchestration - Context

**Gathered:** 2026-01-30
**Status:** Ready for planning

<domain>
## Phase Boundary

Manage combat session lifecycle from first attack through death or flee. CombatService already has session creation logic in `InitiateCombatAsync`, but Phase 2 ensures the full orchestration: auto-create when attacking NPC, track participant state properly, end on death/flee, and reuse existing session for subsequent attacks.

**What's in scope:**
- Combat session auto-creation when attacking NPC (already exists but needs verification)
- Participant state tracking (parry mode, timed penalties, last attack time)
- Session reuse for subsequent attacks against same NPC
- Combat ending conditions (death, flee)
- Session cleanup when participant dies

**What's out of scope (other phases):**
- Combat messaging/broadcasting to room (Phase 3)
- Combat round tick service (Phase 3)
- NPC AI counterattacks (Phase 4)
- Timed penalty application from failed attacks (Phase 5)
- Multi-combatant session tracking beyond 1v1 (Phase 5)

</domain>

<existing_infrastructure>
## What Already Exists

### CombatService (Mordecai.Web\Services\CombatService.cs)

**Working features:**
- `InitiateCombatAsync` - Creates session + participants when not in combat
- `GetActiveCombatSessionAsync` - Queries for active session by participant
- `EndCombatAsync` - Marks session inactive, publishes CombatEnded message
- `FleeFromCombatAsync` - Marks participant inactive, checks if session should end
- `PerformMeleeAttackAsync` - Full attack resolution with damage calculation
- Death check after damage application (calls EndCombatAsync)

**CombatSession entity:**
- Id (Guid), RoomId, StartedAt, EndedAt, IsActive, EndReason
- Participants collection

**CombatParticipant entity:**
- Id (int), CombatSessionId, CharacterId (nullable), ActiveSpawnId (nullable)
- ParticipantName, IsInParryMode, LastRangedAttack, TimedPenaltiesJson
- JoinedAt, LeftAt, LeaveReason, IsActive

### Existing Gap Analysis

**What works now:**
1. ✅ Attacking creates session if none exists (InitiateCombatAsync line 37-43)
2. ✅ Participants created for attacker and defender (lines 66-90)
3. ✅ Session ends when participant dies (line 1110)
4. ✅ Flee marks participant inactive and checks session end (lines 328-345)

**What needs work:**
1. ❓ Target's existing combat session check - if NPC already in combat, attacker should join that session
2. ❓ Participant state verification - ensure parry mode, timed penalties are properly initialized
3. ❓ Session reuse logic - attacks against same NPC reuse session (currently only checks attacker's session)

</existing_infrastructure>

<decisions>
## Implementation Decisions

### Session Joining Logic
- When player attacks NPC already in combat, player joins that NPC's existing session
- Do NOT create new session if target already has active combat
- One CombatSession can have multiple participants (future multi-combatant support)

### Participant State Initialization
- IsInParryMode defaults to false (must explicitly enable)
- TimedPenaltiesJson starts as empty JSON array "[]"
- LastRangedAttack starts as null (no cooldown)
- JoinedAt set to UtcNow on creation

### Death Handling
- When participant VIT reaches 0, mark IsActive=false, set LeaveReason="Death"
- If only 1 active participant remains, end session with winner
- Keep session record for history/logging (don't delete)

### Claude's Discretion
- Error handling for edge cases (null checks, missing entities)
- Logging verbosity for debugging
- Performance optimization for database queries

</decisions>

<requirements>
## Requirements for This Phase

From REQUIREMENTS.md:

- **ORCH-01**: Attacking an NPC creates CombatSession automatically if none exists
- **ORCH-02**: CombatParticipant entities created for attacker and defender when combat starts
- **ORCH-03**: Combat session ends when participant dies or successfully flees
- **ORCH-04**: Combat state tracked per participant (parry mode, timed penalties, last attack time)

**Success Criteria:**
1. "attack goblin" creates CombatSession if none exists
2. ActiveSpawn entities have CurrentRoomId populated and queryable
3. CombatParticipant records track attacker and defender with combat state
4. Combat session ends automatically when a participant dies or successfully flees
5. Subsequent attacks against same NPC reuse existing combat session

</requirements>

---

*Phase: 02-combat-orchestration*
*Context gathered: 2026-01-30*
