# Phase 3: Messaging & Round Tick - Context

**Gathered:** 2026-01-30
**Status:** Ready for planning

<domain>
## Phase Boundary

Broadcast combat actions to the room and implement continuous health pool drainage via a combat round tick service. Players in the room see combat narration, participants receive detailed damage breakdowns.

**What's in scope:**
- Broadcasting combat actions (attacks, hits, misses, flees) to room
- Personal messages with damage breakdown to affected participants
- Combat round tick service (3-second interval)
- Pending FAT/VIT pool drainage each tick
- Expired timed penalty removal

**What's out of scope (other phases):**
- NPC AI decisions (Phase 4)
- Timed penalty APPLICATION from failed attacks (Phase 5)
- Multi-combatant tracking beyond what Phase 2 delivered (Phase 5)

</domain>

<existing_infrastructure>
## What Already Exists

### Messaging System

**IGameMessagePublisher** (`Mordecai.Messaging/Services/IGameMessageServices.cs`):
- `PublishAsync<T>()` - Publish single message
- `PublishBatchAsync<T>()` - Publish multiple messages
- Implementation uses RabbitMQ topic exchange

**Combat Messages** (`Mordecai.Messaging/Messages/CombatMessages.cs`):
- `CombatStarted` - Already published when combat initiates
- `CombatAction` - For attacks (AttackerId, DefenderName, Damage, IsHit, SkillUsed, etc.)
- `CombatEnded` - Already published when combat ends
- `HealthChanged` - For health updates

**Message Scoping** (property-based, not enum):
- `RoomId` property → room-scoped delivery
- `ZoneId` property → zone-wide delivery
- `TargetCharacterIds` → personal delivery

### Background Service Pattern

**HealthTickBackgroundService** (`Mordecai.Web/Services/`):
- 3-second tick interval (same as our target!)
- Already handles pending FAT/VIT drainage for Characters
- Uses `IServiceProvider.CreateScope()` for DI per tick
- Pattern: while loop + try/catch + Task.Delay

### Sound Propagation

**SoundPropagationService** (`Mordecai.Messaging/Services/`):
- Broadcasts combat sounds to adjacent rooms
- Used by CombatMessageBroadcastService for CombatStarted events

### Gap Analysis

**What already works:**
- ✅ CombatStarted message published (InitiateCombatAsync)
- ✅ CombatEnded message published (EndCombatAsync)
- ✅ Health tick for Characters (HealthTickBackgroundService)
- ✅ Sound propagation infrastructure

**What needs work:**
- ❌ CombatAction messages NOT published during attacks
- ❌ Personal damage breakdown messages NOT sent
- ❌ NPC ActiveSpawns NOT included in health tick
- ❌ Timed penalty expiration NOT processed

</existing_infrastructure>

<decisions>
## Implementation Decisions

### Combat Action Broadcasting
- Publish `CombatAction` message after each attack in `PerformMeleeAttackAsync`
- Include attack description, damage, hit/miss, skill used
- Room-scoped via `RoomId` property

### Personal Damage Messages
- Create new `CombatDamageReceived` message type for detailed breakdown
- Include: armor absorption, FAT damage, VIT damage, wounds inflicted
- Target via `TargetCharacterIds` property

### Combat Tick Service
- Extend existing HealthTickBackgroundService OR create separate CombatRoundTickService
- Decision: **Extend existing HealthTickBackgroundService** to include:
  - ActiveSpawn pending damage (NPCs)
  - Timed penalty expiration for CombatParticipants
- Keeps 3-second interval aligned with existing health tick

### FAT Recovery in Combat
- Per TICK-02: "1 FAT for participants not taking damage this round"
- Need to track "last damage received" timestamp on CombatParticipant
- Recover if no damage in last 3 seconds

### Claude's Discretion
- Exact message text/descriptions for combat narration
- Error handling and logging verbosity
- Performance optimization for bulk queries

</decisions>

<requirements>
## Requirements for This Phase

From REQUIREMENTS.md:

**Combat Messaging:**
- **MSG-01**: Global room messages broadcast all combat actions (attacks, hits, misses, flees)
- **MSG-02**: Personal messages send detailed damage breakdown to affected participant
- **MSG-03**: Messages use RabbitMQ scoped messaging

**Combat Round Tick:**
- **TICK-01**: CombatRoundTickService runs as BackgroundService with 3-second tick interval
- **TICK-02**: Each tick recovers 1 FAT for participants not taking damage this round
- **TICK-03**: Each tick drains half of pending FAT/VIT pools to actual health values
- **TICK-04**: Each tick removes expired timed AV penalties from participants

Note: TICK-05 (NPC AI decisions) is Phase 4.

**Success Criteria:**
1. All players in room see combat narration messages
2. Affected participants receive personal damage breakdown
3. NPC health pools (ActiveSpawns) drain same as Characters
4. Expired timed penalties removed each tick

</requirements>

---

*Phase: 03-messaging-round-tick*
*Context gathered: 2026-01-30*
