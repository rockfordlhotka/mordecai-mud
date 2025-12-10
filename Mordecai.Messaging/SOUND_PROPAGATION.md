# Sound Propagation System - Implementation Guide

## Overview

This document describes the sound propagation system that allows players to hear sounds from adjacent rooms, creating a more immersive and tactically interesting game environment.

## Core Concepts

### Sound Levels

Sounds are classified by volume, which determines how far they propagate:

| Level | Name | Distance | Examples |
|-------|------|----------|----------|
| 0 | Silent | 0 rooms | Whispers, thinking, silent actions |
| 1 | Quiet | 1 room (muffled) | Normal conversation, quiet movement |
| 2 | Normal | 1 room (clear) | Regular speech, footsteps, door opening |
| 3 | Loud | 2 rooms | Yelling, shouting, loud combat |
| 4 | Very Loud | 3 rooms | Spells, explosions, fierce combat |
| 5 | Deafening | 4+ rooms | Dragon roars, massive explosions |

### Sound Types

Sounds are categorized to provide appropriate descriptions:

- **Speech**: Talking, yelling, shouting
- **Combat**: Weapon strikes, fighting sounds
- **Magic**: Spell casting, magical effects
- **Movement**: Footsteps, running, objects moving
- **Environmental**: Wind, water, natural sounds
- **Music**: Singing, instruments
- **Animal**: Creature sounds
- **Mechanical**: Doors, mechanisms, traps
- **Destruction**: Breaking, explosions, collapse

## Message Flow

### 1. Source Room - Action Occurs

When a sound-producing action occurs:

```
Player says: "yell Help! Bandits attacking!"
                    ↓
         ChatMessage published to source room
                    ↓
    SoundPropagationService.PropagateSound() called
```

### 2. Sound Propagation Service

The service determines propagation based on sound level:

```csharp
await soundPropagation.PropagateSound(
    sourceRoomId: 42,
    soundLevel: SoundLevel.Loud,    // Yelling
    soundType: SoundType.Speech,
    description: "Help! Bandits attacking!",
    characterName: "Adventurer",
    detailedMessage: "Help! Bandits attacking!"
);
```

### 3. Adjacent Room Discovery

The service queries the room connectivity graph:

```
Source Room (42)
    ↓ north (visible exit)
Adjacent Room (43) - 1 room away
    ↓ northeast (visible exit)
Two Rooms Away (44) - 2 rooms away
```

**Important**: Hidden exits are excluded from sound propagation.

### 4. Message Generation

For each adjacent room within propagation distance:

**Distance 1 (Adjacent):**
```csharp
// Loud sounds include the words
new AdjacentRoomSoundMessage(
    SourceRoomId: 42,
    ListenerRoomId: 43,
    Direction: "south",  // Reverse of "north"
    SoundLevel: SoundLevel.Loud,
    SoundType: SoundType.Speech,
    Description: "Someone yells from the south",
    CharacterName: "Adventurer",
    DetailedMessage: "Help! Bandits attacking!"
);
```

**Distance 2 (One Room Away):**
```csharp
// Loud sounds are muffled
new AdjacentRoomSoundMessage(
    SourceRoomId: 42,
    ListenerRoomId: 44,
    Direction: "southwest",
    SoundLevel: SoundLevel.Loud,
    SoundType: SoundType.Speech,
    Description: "You hear distant shouting to the southwest",
    CharacterName: null,
    DetailedMessage: null
);
```

### 5. Message Delivery

Messages are published to RabbitMQ with appropriate routing:

```
Routing Key: chat.adjacentroomsoundmessage.43
             ↓
All characters in room 43 receive the message
             ↓
UI displays: "Someone yells from the south: 'Help! Bandits attacking!'"
```

## Command Implementations

### Say Command

**Sound Level**: Quiet (1)

```csharp
// In GameActionService or CommandHandler
await messagePublisher.PublishAsync(new ChatMessage(
    CharacterId: character.Id,
    CharacterName: character.Name,
    SourceRoomId: character.CurrentRoomId,
    Message: "Hello everyone",
    ChatType: ChatType.Say,
    SoundLevel: SoundLevel.Quiet
));

// Propagate to adjacent rooms
await soundPropagation.PropagateSound(
    sourceRoomId: character.CurrentRoomId,
    soundLevel: SoundLevel.Quiet,
    soundType: SoundType.Speech,
    description: "conversation"
);
```

**Adjacent Room Sees**: "You hear muffled voices from the east"

### Yell/Shout Command

**Sound Level**: Loud (3)

```csharp
await messagePublisher.PublishAsync(new ChatMessage(
    CharacterId: character.Id,
    CharacterName: character.Name,
    SourceRoomId: character.CurrentRoomId,
    Message: "Help! Bandits!",
    ChatType: ChatType.Yell,
    SoundLevel: SoundLevel.Loud
));

await soundPropagation.PropagateSound(
    sourceRoomId: character.CurrentRoomId,
    soundLevel: SoundLevel.Loud,
    soundType: SoundType.Speech,
    description: "someone yelling",
    characterName: character.Name,
    detailedMessage: "Help! Bandits!"  // Heard at distance 1
);
```

**Adjacent Room (1 away)**: "Adventurer yells from the south: 'Help! Bandits!'"  
**Two Rooms Away**: "You hear distant shouting to the northeast"

### Whisper Command

**Sound Level**: Silent (0)

```csharp
await messagePublisher.PublishAsync(new ChatMessage(
    CharacterId: character.Id,
    CharacterName: character.Name,
    SourceRoomId: character.CurrentRoomId,
    Message: "Meet me at midnight",
    ChatType: ChatType.Whisper,
    TargetId: targetCharacter.Id,
    TargetName: targetCharacter.Name,
    SoundLevel: SoundLevel.Silent
));

// No sound propagation - whispers are silent
```

**Adjacent Rooms**: Nothing (no sound travels)

## Combat Sound Propagation

### Combat Start

```csharp
await messagePublisher.PublishAsync(new CombatStarted(
    InitiatorId: attacker.Id,
    InitiatorName: attacker.Name,
    TargetId: defender.Id,
    TargetName: defender.Name,
    LocationRoomId: roomId,
    SoundLevel: SoundLevel.Loud
));

await soundPropagation.PropagateSound(
    sourceRoomId: roomId,
    soundLevel: SoundLevel.Loud,
    soundType: SoundType.Combat,
    description: "combat",
    detailedMessage: $"{attacker.Name} attacking {defender.Name}"
);
```

**Adjacent Room**: "You hear sounds of combat to the west"  
**Two Rooms Away**: "You hear distant fighting to the northwest"

### Combat Actions

```csharp
await messagePublisher.PublishAsync(new CombatAction(
    AttackerId: attacker.Id,
    AttackerName: attacker.Name,
    DefenderId: defender.Id,
    DefenderName: defender.Name,
    LocationRoomId: roomId,
    ActionDescription: "slashes with sword",
    Damage: 15,
    IsHit: true,
    SkillUsed: "Melee Combat",
    SoundLevel: SoundLevel.Normal
));

await soundPropagation.PropagateSound(
    sourceRoomId: roomId,
    soundLevel: SoundLevel.Normal,
    soundType: SoundType.Combat,
    description: "weapon strikes"
);
```

**Adjacent Room**: "You hear the clash of weapons from the north"

## Spell Casting

### Minor Spell (Magic Missile)

**Sound Level**: Normal (2)

```csharp
await soundPropagation.PropagateSound(
    sourceRoomId: caster.CurrentRoomId,
    soundLevel: SoundLevel.Normal,
    soundType: SoundType.Magic,
    description: "magical energy",
    characterName: caster.Name
);
```

**Adjacent Room**: "You hear magical sounds from the east"

### Major Spell (Fireball)

**Sound Level**: VeryLoud (4)

```csharp
await soundPropagation.PropagateSound(
    sourceRoomId: caster.CurrentRoomId,
    soundLevel: SoundLevel.VeryLoud,
    soundType: SoundType.Magic,
    description: "explosive magical energy",
    characterName: caster.Name,
    detailedMessage: "a powerful fireball detonating"
);
```

**1 Room Away**: "You hear a powerful fireball detonating to the south"  
**2 Rooms Away**: "You hear explosive magical energy from the southwest"  
**3 Rooms Away**: "You sense distant magical energies"

### Massive Spell (Meteor Swarm)

**Sound Level**: Deafening (5)

```csharp
await soundPropagation.PropagateSound(
    sourceRoomId: caster.CurrentRoomId,
    soundLevel: SoundLevel.Deafening,
    soundType: SoundType.Destruction,
    description: "massive explosions",
    detailedMessage: "meteors crashing from the sky"
);
```

**1-2 Rooms**: "You hear meteors crashing from the sky to the north!"  
**3 Rooms**: "You hear massive explosions in the distance"  
**4 Rooms**: "You hear a distant thunderous roar"

## Room Service Integration

The `SoundPropagationService` needs to integrate with `IRoomService` to discover adjacent rooms:

```csharp
public interface IRoomService
{
    /// <summary>
    /// Gets all rooms connected to the source room within specified distance,
    /// excluding hidden exits
    /// </summary>
    Task<IReadOnlyList<AdjacentRoomInfo>> GetAdjacentRoomsAsync(
        int sourceRoomId,
        int maxDistance,
        bool includeHidden = false,
        CancellationToken cancellationToken = default);
}

public record AdjacentRoomInfo(
    int RoomId,
    int Distance,
    string DirectionFromSource,
    string DirectionToSource
);
```

### Example Query

```csharp
// Get rooms up to 2 away, excluding hidden exits
var adjacentRooms = await roomService.GetAdjacentRoomsAsync(
    sourceRoomId: 42,
    maxDistance: 2,
    includeHidden: false
);

// Result:
// [
//   { RoomId: 43, Distance: 1, DirectionFromSource: "north", DirectionToSource: "south" },
//   { RoomId: 41, Distance: 1, DirectionFromSource: "south", DirectionToSource: "north" },
//   { RoomId: 44, Distance: 2, DirectionFromSource: "north->east", DirectionToSource: "west" }
// ]
```

## UI Display Formatting

### In Source Room

```
> yell Help! Bandits attacking!
You yell: Help! Bandits attacking!
```

### In Adjacent Room (Distance 1, Loud Sound)

```
[From the south] Adventurer yells: "Help! Bandits attacking!"
```

or

```
Someone yells from the south: "Help! Bandits attacking!"
```

### In Adjacent Room (Distance 1, Normal Sound)

```
[From the east] You hear someone speaking
```

### In Adjacent Room (Distance 1, Quiet Sound)

```
[From the north] You hear muffled voices
```

### Two Rooms Away (Loud Sound)

```
[To the southwest] You hear distant shouting
```

### CSS Styling

```css
.adjacent-sound {
    color: #888;
    font-style: italic;
    margin-left: 1em;
}

.adjacent-sound-direction {
    color: #666;
    font-weight: bold;
}

.adjacent-sound-loud {
    color: #c66;
    font-weight: bold;
}

.adjacent-sound-very-loud {
    color: #f33;
    font-weight: bold;
    font-size: 1.1em;
}
```

## Performance Considerations

### Caching Room Connectivity

```csharp
// Cache adjacent room lists per room
private readonly IMemoryCache _adjacentRoomCache;

var cacheKey = $"adjacent_rooms_{sourceRoomId}_{maxDistance}";
var adjacentRooms = await _adjacentRoomCache.GetOrCreateAsync(
    cacheKey,
    async entry => 
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        return await roomService.GetAdjacentRoomsAsync(sourceRoomId, maxDistance);
    }
);
```

### Message Batching

For very loud sounds affecting many rooms, batch message publishing:

```csharp
var messages = adjacentRooms.Select(room => 
    new AdjacentRoomSoundMessage(/*...*/)).ToList();

await messagePublisher.PublishBatchAsync(messages);
```

### Filtering at Subscriber

Let RabbitMQ route messages to room-based queues. The subscriber doesn't need additional filtering.

## Configuration Options

Future configuration for game tuning:

```json
{
  "SoundPropagation": {
    "EnablePropagation": true,
    "IncludeHiddenExits": false,
    "PropagationDistances": {
      "Quiet": 1,
      "Normal": 1,
      "Loud": 2,
      "VeryLoud": 3,
      "Deafening": 4
    },
    "MinimumSoundLevel": "Quiet"
  }
}
```

## Testing Scenarios

### Test 1: Basic Say Command

```
Room A: Player says "hello"
Room B (north of A): Should see "You hear muffled voices from the south"
Room C (north of B): Should see nothing
```

### Test 2: Yell Command

```
Room A: Player yells "Help!"
Room B (adjacent): Should see full message with direction
Room C (2 away): Should see "distant shouting" notification
Room D (3 away): Should see nothing
```

### Test 3: Hidden Exit

```
Room A: Player yells "Hello"
Room B (via normal exit): Should hear
Room C (via hidden exit): Should NOT hear
```

### Test 4: Combat Sounds

```
Room A: Combat starts
Room B (adjacent): Should hear "sounds of combat from [direction]"
Room C (2 away): Should hear "distant fighting"
```

### Test 5: Loud Spell

```
Room A: Wizard casts fireball (VeryLoud)
Rooms B & C (1-2 away): Should hear detailed description
Room D (3 away): Should hear distant magical energy
Room E (4 away): Should hear nothing
```

## Future Enhancements

1. **Awareness Skill Bonus**: Higher Awareness allows hearing sounds from farther away
2. **Environmental Dampening**: Weather or room type affects sound propagation
3. **Directional Hearing**: More precise direction indication based on room layout
4. **Sound Accumulation**: Multiple sounds in different directions reported separately
5. **Echo Effects**: Caverns and special rooms create echo effects
6. **Magical Silence**: Spells that prevent sound propagation
7. **Deaf Status**: Characters with hearing impairment don't receive sound messages

---

Last updated: 2025-12-09
