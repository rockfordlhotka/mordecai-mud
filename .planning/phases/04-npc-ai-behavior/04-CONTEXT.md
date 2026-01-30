# Phase 04: NPC AI Behavior - Context

## Requirements

From REQUIREMENTS.md:
- **AI-01**: NPC counterattacks automatically when attacked by player
- **AI-02**: NPC selects defense mode (dodge/parry/shield) based on available FAT and equipment
- **AI-03**: NPC attempts flee when VIT drops below threshold (25-30% modified by personality)
- **AI-04**: NPC flee attempt uses skill check, can be intercepted/pursued
- **TICK-05**: Each tick executes NPC AI decisions (attack/defend/flee) for all active NPCs in combat

## Existing Infrastructure

### CombatService (Mordecai.Web/Services/CombatService.cs)
- `PerformMeleeAttackAsync()` - Resolves attacks using 4dF dice
- `SetParryModeAsync()` - Toggle parry defense mode
- `FleeFromCombatAsync()` - Attempt to flee (skill check already implemented)
- `GetActiveCombatSessionAsync()` - Find combat session for participant
- `GetCombatParticipantsAsync()` - Get all participants in session

### HealthTickBackgroundService
- Runs every 3 seconds
- Already processes NPC pending damage
- Can be extended for NPC AI decisions (TICK-05)

### NpcTemplate Fields
- `Strength`, `Endurance`, `Willpower` - for derived stats
- `IsHostile` - whether NPC attacks on sight
- `BehaviorConfig` (JSON) - AI configuration placeholder

### CombatParticipant Fields
- `IsInParryMode` - current defense stance
- `ActiveSpawnId` - links to NPC spawn
- `CharacterId` - null for NPCs

## Design Decisions

### AI Decision Priority
1. Flee check first (survival instinct)
2. Defense mode selection second
3. Counterattack last

### Flee Threshold
- Base: 25% VIT remaining
- Adjustable via NpcTemplate.BehaviorConfig personality

### Defense Selection
- Dodge (default): Costs 1 FAT, uses Dodge skill
- Parry: No FAT cost, uses weapon skill, requires weapon
- Shield: No FAT cost, uses Shield skill (future - not in v1)

### Counterattack Logic
- Only counterattack if NPC was attacked this tick
- Use melee attack against random attacker in session
- Respect attack cooldown (prevent spam)

## Implementation Approach

Extend HealthTickBackgroundService with NPC AI processing:
1. Query all active NPCs in combat sessions
2. For each NPC, run AI decision loop
3. Use CombatService methods for actions

New service: INpcAiService
- Encapsulate AI logic separately from tick service
- Easier to test and configure
