# Phase 06: Combat UI - Context

## Design Decisions (from user)

### Health Bars (Option A - Extend)
```
FAT: [██████████████░░░░░░] 10/15 + 4 pending damage
      └─blue─────┘└─red──┘└empty┘

VIT: [████████░░░░░░░░░░░░] 6/20 + 3 pending heal  
      └blue──┘└green┘└─empty──┘
```
- **Blue** = Current actual value
- **Red** = Pending damage (extends right, shows future drain)
- **Green** = Pending healing (extends right, shows future recovery)
- **Gray/empty** = Remaining to max

### Combat Panel Layout
- Side panel + inline messages in main feed
- 15-20 lines history in side panel
- Collapses when not in combat

### Color Coding
- Green = Your hits (damage dealt)
- Red = Damage to you
- Gray = Misses
- Yellow = Flee attempts

### Status Indicators
- Parry mode (active/inactive toggle)
- Fleeing status
- Exhausted (FAT=0)
- Wounded (low VIT)
- Pending damage indicator (shown in bars)

### NPC Health Descriptions
- 100-75%: "uninjured" / "barely scratched"
- 75-50%: "lightly wounded"
- 50-25%: "badly wounded"
- 25-10%: "near death"
- <10%: "on the verge of collapse"

## Existing Infrastructure

### Play.razor (Main Game Page)
- Already has health bars (FAT/VIT) with basic progress display
- Has `pendingFat` and `pendingVit` variables
- Has `activeEffects` list for status indicators
- Uses `CharacterMessageBroadcastService` for messages
- `OnMessageReceived` handles incoming game messages
- `gameMessages` list stores display messages

### CharacterMessageBroadcastService
- Already formats CombatStarted, CombatAction, CombatEnded messages
- Returns plain text strings (no color/styling)
- Line 209-214 has combat message formatting

### Message Types Available
- `CombatStarted` - attacker, target, room
- `CombatAction` - attacker, defender, damage, isHit, skillUsed
- `CombatEnded` - endReason, winner

### CSS/Styling
- Bootstrap 5 used throughout
- Custom CSS in `wwwroot/css/site.css`
- Scoped CSS supported (`.razor.css` files)

## Implementation Approach

1. **Enhance health bars** in Play.razor with multi-segment display
2. **Add combat panel component** as collapsible side panel
3. **Enhance message formatting** with HTML/color classes
4. **Add combat state tracking** (isInCombat, currentTarget, etc.)
5. **Add status indicator component** for combat states
6. **Add NPC health description helper** for combat messages
