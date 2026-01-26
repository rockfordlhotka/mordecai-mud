# Phase 1: Target Resolution - Context

**Gathered:** 2026-01-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Enable players to target NPCs in their room for combat commands. This phase makes NPCs discoverable and addressable through natural language targeting (e.g., "attack goblin"). ActiveSpawn entities become queryable by CurrentRoomId, and TargetResolutionService finds real NPCs instead of simulated data.

**What's in scope:**
- Making ActiveSpawn entities queryable by room location
- Resolving player-provided target strings to actual NPC entities
- Handling ambiguous targeting (multiple matching NPCs)

**What's out of scope (other phases):**
- Combat session creation and management (Phase 2)
- Combat mechanics and damage calculation (later phases)
- NPC AI behavior (Phase 4)

</domain>

<decisions>
## Implementation Decisions

### Target Matching Strategy
- **Case-insensitive matching**: 'attack Goblin' and 'attack goblin' both work
- **Prefix matching only**: 'gob' matches 'goblin', but 'blin' does not
- **Ambiguity handling**: When multiple NPCs match, require disambiguation rather than auto-targeting first match
- **Disambiguation syntax**: Use numeric suffix with space separator: 'goblin 2' (not dot notation 'goblin.2')

### Multi-word Names
- **Storage approach**: Store as full display name (e.g., 'goblin warrior' as complete string)
- **Matching behavior**: Match full name prefix only — 'goblin w' matches 'goblin warrior', but 'warrior' does not
- **Disambiguation specificity**: Full first word required when ambiguous — 'goblin' triggers disambiguation between 'goblin warrior' and 'goblin shaman'
- **Disambiguation display**: Show full names with numbers — '1. goblin warrior' and '2. goblin shaman'

### Claude's Discretion
- Error message wording and tone (maintain MUD immersion)
- Performance optimization for room-based NPC queries
- Exact implementation of disambiguation prompt UI
- Handling edge cases (empty rooms, invalid input, special characters)

</decisions>

<specifics>
## Specific Ideas

**Natural language flow:**
- Player types 'attack goblin' naturally without worrying about exact capitalization
- Partial typing ('gob') works like tab-completion in terminals
- When ambiguous, system guides with clear numbered choices

**Disambiguation experience:**
- Should feel helpful, not like an error
- Show player what they matched and what their options are
- Use consistent numbering (1-based, not 0-based) for player-facing indices

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope (target resolution only).

</deferred>

---

*Phase: 01-target-resolution*
*Context gathered: 2026-01-26*
