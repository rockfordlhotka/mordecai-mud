# Message Scopes and Routing in Mordecai MUD

## Overview

This document describes the hierarchical message scoping system used in Mordecai MUD, how messages are routed through RabbitMQ, and how to implement each scope correctly.

## Message Scope Hierarchy

Messages in Mordecai operate at four distinct scopes, from broadest to most specific:

### 1. Game-Wide (Global) Scope

**Definition**: Messages delivered to all connected players regardless of location.

**When to Use**:
- Server-wide announcements (maintenance, updates, events)
- Global chat channels (OOC, Gossip, Newbie, Trade)
- Admin broadcasts
- System-wide events (world bosses, server competitions)

**Implementation**:
```csharp
// Global system message
var message = new SystemMessage(
    Message: "Server restart in 5 minutes",
    Priority: MessagePriority.Critical,
    Category: MessageCategory.System
)
{
    RoomId = null  // Null RoomId indicates global scope
};

await messagePublisher.PublishAsync(message);
```

**Routing Key Pattern**: `{category}.{messagetype}.global`

**Examples**:
- `system.systemmessage.global`
- `chat.globalchatmessage.global`
- `system.adminaction.global`

**Subscription**:
```csharp
// Subscribers automatically bind to:
_channel.QueueBind(_queueName, _exchangeName, "system.*.global");
_channel.QueueBind(_queueName, _exchangeName, "chat.globalchatmessage.*");
```

---

### 2. Zone-Wide Scope

**Definition**: Messages delivered to all players within a specific geographic zone.

**When to Use**:
- Zone environmental effects (weather, earthquakes, day/night)
- Zone-specific events (zone bosses, festivals)
- Zone status changes (PvP flag changes, zone effects)
- Area-wide quest objectives

**Implementation** (Future):
```csharp
// Zone-wide weather change
var message = new ZoneEnvironmentMessage(
    ZoneId: 5,
    EffectType: "weather",
    Description: "Dark storm clouds gather overhead",
    Duration: TimeSpan.FromMinutes(10)
)
{
    RoomId = null,  // Zone scope doesn't use RoomId
    ZoneId = 5      // Future property on GameMessage
};

await messagePublisher.PublishAsync(message);
```

**Routing Key Pattern**: `{category}.{messagetype}.zone.{zoneId}`

**Examples**:
- `environment.weatherchange.zone.5`
- `event.zoneboss.zone.12`
- `system.zonestatus.zone.8`

**Status**: Not yet implemented - requires Zone entity and ZoneId property on GameMessage base class.

**Future Subscription**:
```csharp
// When entering a zone:
_channel.QueueBind(_queueName, _exchangeName, $"environment.*.zone.{zoneId}");
_channel.QueueBind(_queueName, _exchangeName, $"event.*.zone.{zoneId}");

// When leaving a zone:
_channel.QueueUnbind(_queueName, _exchangeName, $"environment.*.zone.{oldZoneId}");
```

---

### 3. Room-Wide Scope

**Definition**: Messages delivered to all players in the same room.

**When to Use**:
- Local communication (say, emote)
- Character movement (enters, leaves)
- Combat actions visible to observers
- Skill demonstrations
- Room environmental effects
- NPC actions and dialogue

**Implementation**:
```csharp
// Room-based chat
var message = new ChatMessage(
    CharacterId: characterId,
    CharacterName: "Adventurer",
    SourceRoomId: 42,
    Message: "Hello everyone!",
    ChatType: ChatType.Say
)
{
    RoomId = 42  // Specific room scope
};

await messagePublisher.PublishAsync(message);
```

**Routing Key Pattern**: `{category}.{messagetype}.{roomId}`

**Examples**:
- `chat.chatmessage.42`
- `movement.playermoved.42`
- `combat.combataction.42`
- `skill.skillused.42`

**Subscription**:
```csharp
// When entering a room:
var roomRoutingKeys = new[]
{
    $"movement.*.{roomId}",
    $"chat.*.{roomId}",
    $"combat.*.{roomId}",
    $"skill.*.{roomId}"
};

foreach (var routingKey in roomRoutingKeys)
{
    _channel.QueueBind(_queueName, _exchangeName, routingKey);
}

// When leaving a room:
foreach (var routingKey in roomRoutingKeys)
{
    _channel.QueueUnbind(_queueName, _exchangeName, routingKey);
}
```

---

### 4. Character-Level (Targeted) Scope

**Definition**: Messages delivered only to specific character(s).

**When to Use**:
- Private tells/whispers between players
- Error messages for specific characters
- Personal notifications (skill advancement, quest updates)
- Direct NPC dialogue responses
- Targeted skill or combat feedback
- Personal loot notifications

**Implementation**:
```csharp
// Private tell to specific character
var message = new ChatMessage(
    CharacterId: senderCharacterId,
    CharacterName: "Adventurer",
    SourceRoomId: 42,
    Message: "Meet me at the inn",
    ChatType: ChatType.Tell,
    TargetId: recipientCharacterId,
    TargetName: "Friend"
)
{
    RoomId = 42,
    TargetCharacterIds = new[] { recipientCharacterId }
};

await messagePublisher.PublishAsync(message);
```

**Routing Key Pattern**: Same as underlying scope (room/global), filtered by `TargetCharacterIds`

**Examples**:
- `chat.chatmessage.42` (with TargetCharacterIds)
- `system.errormessage.global` (with TargetCharacterIds)
- `skill.skillexperiencegained.42` (with TargetCharacterIds)

**Filtering**:
```csharp
private bool ShouldProcessMessage(GameMessage message)
{
    // Check if message is targeted
    if (message.TargetCharacterIds?.Any() == true)
    {
        return message.TargetCharacterIds.Contains(CharacterId);
    }

    // Check if message is room-specific
    if (message.RoomId.HasValue)
    {
        return CurrentRoomId == message.RoomId.Value;
    }

    // Global message - process it
    return true;
}
```

---

## RabbitMQ Architecture

### Topic Exchange Configuration

**Exchange Name**: `mordecai.game.events`  
**Exchange Type**: Topic  
**Durable**: Yes  
**Auto-Delete**: No

### Visual Architecture

```text
                    ┌─────────────────────────────────────┐
                    │   mordecai.game.events Exchange     │
                    │         (Topic Exchange)            │
                    └─────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
          ┌─────────▼─────────┐    │    ┌─────────▼─────────┐
          │ system.*.global   │    │    │ chat.*.global     │
          │ (Global Messages) │    │    │ (Global Chat)     │
          └─────────┬─────────┘    │    └─────────┬─────────┘
                    │               │               │
          ┌─────────▼─────────┐    │    ┌─────────▼─────────┐
          │ movement.*.42     │    │    │ chat.*.42         │
          │ (Room 42)         │    │    │ (Room 42)         │
          └─────────┬─────────┘    │    └─────────┬─────────┘
                    │               │               │
          ┌─────────▼─────────┐    │    ┌─────────▼─────────┐
          │ combat.*.42       │    │    │ skill.*.42        │
          │ (Room 42)         │    │    │ (Room 42)         │
          └─────────┬─────────┘    │    └─────────┬─────────┘
                    │               │               │
                    └───────────────┼───────────────┘
                                    │
                    ┌───────────────▼────────────────┐
                    │ Character Queue (temp, auto)   │
                    │   - Binds to relevant keys     │
                    │   - Filters by TargetCharIds   │
                    │   - Delivers to UI             │
                    └────────────────────────────────┘
```

### Message Flow Example

```text
Player says "Hello!" in Room 42
           │
           ▼
  ┌─────────────────┐
  │ ChatMessage     │  RoomId = 42
  │ Message: Hello! │  TargetCharacterIds = null
  └────────┬────────┘
           │
           ▼
  ┌──────────────────────┐
  │ Publisher generates  │  Routing key: "chat.chatmessage.42"
  │ routing key          │
  └────────┬─────────────┘
           │
           ▼
  ┌───────────────────────┐
  │ RabbitMQ Topic        │  Routes to all queues bound to "chat.*.42"
  │ Exchange routes       │
  └────────┬──────────────┘
           │
      ┌────┴────┬────────┬────────┐
      ▼         ▼        ▼        ▼
  ┌──────┐  ┌──────┐ ┌──────┐ ┌──────┐
  │Queue1│  │Queue2│ │Queue3│ │Queue4│  (All characters in Room 42)
  └──┬───┘  └──┬───┘ └──┬───┘ └──┬───┘
     │         │        │        │
     ▼         ▼        ▼        ▼
  Char A    Char B   Char C   Char D
  sees it   sees it  sees it  sees it
```

### Routing Key Structure

```text
{category}.{messagetype}.{scope}
```

**Components**:

- **category**: Message domain (movement, chat, combat, skill, system, environment)
- **messagetype**: Specific message class name in lowercase (e.g., `chatmessage`, `playermoved`)
- **scope**: Target scope identifier
  - `global` for game-wide messages
  - `{roomId}` for room-specific messages (e.g., `42`)
  - `zone.{zoneId}` for zone-specific messages (future)

### Message Categories

```csharp
public enum MessageCategory
{
    Movement,     // Character/NPC movement
    Chat,         // Communication
    Combat,       // Combat actions and results
    Skill,        // Skill usage and advancement
    System,       // System messages and errors
    Admin,        // Administrative actions
    Emote,        // Character emotes
    Look,         // Observation and examination
    Inventory,    // Inventory changes
    Trade,        // Trading and economy
    Environment   // Environmental effects (future)
}
```

### Queue Management

**Per-Character Queues**:
- Each connected character has a temporary, auto-delete queue
- Queue name: Server-generated unique name
- Queue is bound to multiple routing keys based on character state
- Queue is deleted when character disconnects

**Queue Bindings**:
```csharp
// Global bindings (always active)
system.*.global
chat.globalchatmessage.*

// Room bindings (dynamic)
movement.*.{currentRoomId}
chat.*.{currentRoomId}
combat.*.{currentRoomId}
skill.*.{currentRoomId}

// Future: Zone bindings
environment.*.zone.{currentZoneId}
event.*.zone.{currentZoneId}
```

---

## Implementation Guidelines

### Publishing Messages

**Step 1**: Create the appropriate message type with correct scope properties:

```csharp
// Room-scoped message
var message = new ChatMessage(/*...*/)
{
    RoomId = 42  // Set specific room
};

// Global message
var message = new SystemMessage(/*...*/)
{
    RoomId = null  // Null for global
};

// Targeted message
var message = new ErrorMessage(/*...*/)
{
    RoomId = null,
    TargetCharacterIds = new[] { characterId }
};
```

**Step 2**: Publish through the message publisher:

```csharp
await messagePublisher.PublishAsync(message);
```

The publisher automatically determines the routing key based on message properties.

### Subscribing to Messages

**Step 1**: Create subscriber for a character:

```csharp
var subscriber = subscriberFactory.CreateSubscriber(characterId, currentRoomId);
```

**Step 2**: Register message handler:

```csharp
subscriber.MessageReceived += async (message) =>
{
    await HandleGameMessage(message);
};
```

**Step 3**: Start subscription:

```csharp
await subscriber.StartAsync();
```

**Step 4**: Update room when character moves:

```csharp
await subscriber.UpdateRoomAsync(newRoomId);
```

**Step 5**: Clean up when character disconnects:

```csharp
await subscriber.StopAsync();
subscriber.Dispose();
```

### Message Filtering

The subscriber automatically filters messages:

1. **Routing-level filtering**: RabbitMQ only delivers messages matching bound routing keys
2. **Application-level filtering**: Subscriber checks `TargetCharacterIds` for targeted messages

**No additional filtering needed** in most cases - the system handles it automatically.

---

## Message Flow Examples

### Example 1: Character Says Something in a Room

```csharp
// 1. Player types: say Hello everyone!

// 2. Command handler creates and publishes message
var message = new ChatMessage(
    CharacterId: currentCharacter.Id,
    CharacterName: currentCharacter.Name,
    SourceRoomId: currentRoomId,
    Message: "Hello everyone!",
    ChatType: ChatType.Say
);

await messagePublisher.PublishAsync(message);

// 3. Publisher generates routing key: "chat.chatmessage.42"

// 4. RabbitMQ routes to all queues bound to "chat.*.42"

// 5. All characters in room 42 receive the message

// 6. Each subscriber's MessageReceived event fires

// 7. UI updates with formatted message
```

### Example 2: Global Announcement

```csharp
// 1. Admin sends global announcement

var message = new SystemMessage(
    Message: "New quest available in the town square!",
    Priority: MessagePriority.High,
    Category: MessageCategory.System
);

await messagePublisher.PublishAsync(message);

// 2. Publisher generates routing key: "system.systemmessage.global"

// 3. RabbitMQ routes to all queues bound to "system.*.global"

// 4. All connected characters receive the message

// 5. UI shows notification
```

### Example 3: Private Tell Between Characters

```csharp
// 1. Player types: tell Friend Meet me at the inn

var message = new ChatMessage(
    CharacterId: senderId,
    CharacterName: "Adventurer",
    SourceRoomId: currentRoomId,
    Message: "Meet me at the inn",
    ChatType: ChatType.Tell,
    TargetId: recipientId,
    TargetName: "Friend"
)
{
    TargetCharacterIds = new[] { recipientId }
};

await messagePublisher.PublishAsync(message);

// 2. Publisher generates routing key based on current room or global

// 3. RabbitMQ delivers to subscribers

// 4. Only recipient's subscriber processes (TargetCharacterIds check)

// 5. Only recipient sees the tell
```

### Example 4: Character Movement

```csharp
// 1. Player types: north

// 2. Movement validated and executed

// 3. "Leaving" message published to old room
var leftMessage = new PlayerLeft(
    CharacterId: characterId,
    CharacterName: characterName,
    RoomId: oldRoomId,
    Direction: "north"
);

await messagePublisher.PublishAsync(leftMessage);

// 4. Character's subscriber updated
await subscriber.UpdateRoomAsync(newRoomId);

// 5. "Entered" message published to new room
var enteredMessage = new PlayerMoved(
    CharacterId: characterId,
    CharacterName: characterName,
    FromRoomId: oldRoomId,
    ToRoomId: newRoomId,
    Direction: "north"
);

await messagePublisher.PublishAsync(enteredMessage);

// 6. Both rooms' occupants receive appropriate messages
```

---

## Future Enhancements

### Zone Messaging (Planned)

- Add `ZoneId` property to `GameMessage` base class
- Implement zone-based routing: `{category}.{messagetype}.zone.{zoneId}`
- Create `ZoneEnvironmentMessage` and related message types
- Update subscriber to bind/unbind zone routing keys on zone transitions

### Message Priorities

- Implement priority queues for critical messages
- High-priority messages (combat, health) processed first
- Low-priority messages (ambient effects) queued

### Message Persistence

- Store important messages for offline delivery
- Character login shows missed tells/notifications
- Configurable message retention periods

### Message Compression

- Compress large message batches for efficiency
- Reduce bandwidth for mobile clients

---

## Troubleshooting

### Messages Not Being Received

**Check**:
1. Is subscriber started? (`await subscriber.StartAsync()`)
2. Is character in correct room? (`await subscriber.UpdateRoomAsync(roomId)`)
3. Are routing keys bound correctly? (check logs)
4. Is RabbitMQ connection active? (check connection status)

### Receiving Wrong Messages

**Check**:
1. Is `RoomId` set correctly on messages?
2. Are `TargetCharacterIds` populated for targeted messages?
3. Is subscriber filtering working? (check `ShouldProcessMessage`)

### Performance Issues

**Check**:
1. Are queues accumulating messages? (check RabbitMQ management UI)
2. Is message handler async? (avoid blocking operations)
3. Are old room bindings cleaned up? (check unbind on movement)

---

## Best Practices

1. **Always set RoomId correctly**: `null` for global, specific value for room-scoped
2. **Use TargetCharacterIds for private messages**: Don't rely on routing alone
3. **Clean up subscriptions**: Always dispose subscribers on disconnect
4. **Update room bindings promptly**: Call `UpdateRoomAsync` immediately on movement
5. **Log important events**: Use structured logging for message operations
6. **Handle errors gracefully**: Don't throw in message handlers - log and continue
7. **Test with multiple characters**: Verify scoping with concurrent connections
8. **Monitor queue depths**: Watch for message accumulation indicating problems

---

Last updated: 2025-12-09
