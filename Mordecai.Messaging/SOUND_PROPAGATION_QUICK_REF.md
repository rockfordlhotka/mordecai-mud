# Adjacent Room Sound Propagation - Quick Reference

## Sound Level Chart

| Command/Action | Sound Level | Distance 1 (Adjacent) | Distance 2 | Distance 3+ |
|----------------|-------------|----------------------|------------|-------------|
| **Whisper** | Silent (0) | Nothing | Nothing | Nothing |
| **Say** | Quiet (1) | "Muffled voices" | Nothing | Nothing |
| **Say (normal)** | Normal (2) | "Someone speaking" | Nothing | Nothing |
| **Yell/Shout** | Loud (3) | Full message + direction | "Distant shouting" | Nothing |
| **Combat** | Normal-Loud (2-3) | "Sounds of combat" | "Distant fighting" | Nothing |
| **Fireball** | VeryLoud (4) | Full description | Description | "Distant sound" |
| **Explosion** | Deafening (5) | Full description | Full description | "Distant sound" |

## Message Examples

### Say Command (Quiet)

**Source Room (42):**
```
You say: Hello everyone
```

**Adjacent Room (43, to the north):**
```
[From the south] You hear muffled voices
```

### Yell Command (Loud)

**Source Room (42):**
```
You yell: Help! Bandits!
```

**Adjacent Room (43, 1 room away):**
```
[From the south] Adventurer yells: "Help! Bandits!"
```

**Two Rooms Away (44, 2 rooms away):**
```
[To the southwest] You hear distant shouting
```

### Combat (Loud)

**Source Room (42):**
```
Adventurer attacks Guard!
Adventurer slashes Guard with sword for 15 damage.
```

**Adjacent Room (43):**
```
[From the west] You hear sounds of combat
```

### Fireball Spell (Very Loud)

**Source Room (42):**
```
Wizard casts Fireball!
A massive ball of fire explodes in the room!
```

**Adjacent Room (43, 1 away):**
```
[From the north] You hear a powerful fireball detonating
```

**Two Rooms Away (44):**
```
[From the northeast] You hear explosive magical energy
```

**Three Rooms Away (45):**
```
[To the east] You sense distant magical energies
```

## Implementation Checklist

### For Chat Commands

- [ ] Add `SoundLevel` parameter to `ChatMessage`
- [ ] Call `ISoundPropagationService.PropagateSound()` after publishing chat message
- [ ] Set appropriate sound level:
  - Whisper: `SoundLevel.Silent`
  - Say: `SoundLevel.Quiet` or `SoundLevel.Normal`
  - Yell: `SoundLevel.Loud`

### For Combat

- [ ] Add `SoundLevel` to `CombatStarted` (default: `Loud`)
- [ ] Add `SoundLevel` to `CombatAction` (default: `Normal`)
- [ ] Call sound propagation for combat start
- [ ] Optionally call for significant combat actions

### For Spells

- [ ] Determine sound level based on spell power
- [ ] Call sound propagation with appropriate `SoundType.Magic`
- [ ] Provide detailed description for loud spells

### For Room Service

- [ ] Implement `GetAdjacentRoomsAsync(sourceRoomId, maxDistance, includeHidden)`
- [ ] Return list of `AdjacentRoomInfo` with room ID, distance, and directions
- [ ] Use breadth-first search for room graph traversal
- [ ] Cache results for performance

### For UI

- [ ] Handle `AdjacentRoomSoundMessage` in message subscriber
- [ ] Format with direction indicator: `[From the north]`
- [ ] Style differently from direct messages (italic, gray)
- [ ] Show loud sounds prominently (bold, red)

## Direction Mapping

When sound comes from an exit, listeners hear from the reverse direction:

| Exit Direction | Sound From Direction |
|----------------|---------------------|
| north | south |
| south | north |
| east | west |
| west | east |
| northeast | southwest |
| northwest | southeast |
| southeast | northwest |
| southwest | northeast |
| up | below |
| down | above |

## Hidden Exits

**Rule**: Sounds do NOT travel through hidden exits.

**Rationale**: Maintains stealth and secret passage concealment.

**Implementation**: `GetAdjacentRoomsAsync` filters out `RoomExit` entries where `IsHidden = true` (unless `includeHidden` parameter is explicitly set).

## Configuration Values

### Current Propagation Distances

```csharp
SoundLevel.Silent => 0 rooms
SoundLevel.Quiet => 1 room (muffled)
SoundLevel.Normal => 1 room (clear)
SoundLevel.Loud => 2 rooms (clear at 1, muffled at 2)
SoundLevel.VeryLoud => 3 rooms (clear at 1-2, distant at 3)
SoundLevel.Deafening => 4+ rooms (clear at 1-3, distant at 4+)
```

## Common Pitfalls

1. **Forgetting to exclude hidden exits** - Always filter `IsHidden = true`
2. **Not setting sound level** - Defaults may not match intent
3. **Publishing sound to source room** - Adjacent messages only go to OTHER rooms
4. **Wrong direction** - Must reverse the exit direction for listener's perspective
5. **Performance** - Cache adjacent room queries, use batch publishing for many rooms

## Testing Commands

### Test Suite

```bash
# Test 1: Say in room, verify adjacent hears muffled
> go room A
> say Hello
> [check room B hears "muffled voices from south"]

# Test 2: Yell, verify 1 room hears message, 2 rooms hears distant
> yell Help!
> [check room B hears full message]
> [check room C hears "distant shouting"]

# Test 3: Hidden exit does not propagate
> yell Testing
> [check room D (via hidden exit) hears nothing]

# Test 4: Combat sounds
> attack guard
> [check adjacent rooms hear "sounds of combat"]
```

---

**See Also:**
- [SOUND_PROPAGATION.md](./SOUND_PROPAGATION.md) - Full implementation guide
- [MESSAGE_SCOPES.md](./MESSAGE_SCOPES.md) - Message routing architecture
- [docs/MORDECAI_SPECIFICATION.md](../docs/MORDECAI_SPECIFICATION.md) - Game design spec

Last updated: 2025-01-23
