# Message Scope Implementation Summary

## Changes Made

This implementation adds comprehensive documentation and structure for hierarchical message scoping in Mordecai MUD.

## Documentation Added

### 1. Specification Update (docs/MORDECAI_SPECIFICATION.md)

Added a new "Message Scopes and Architecture" section under "Technical Features" that documents:

- The four message scope levels (Game-Wide, Zone-Wide, Room-Wide, Character-Level)
- RabbitMQ topic exchange routing patterns
- Subscription model and dynamic re-subscription
- Message filtering at routing and application levels
- Message priority and delivery guarantees

### 2. Comprehensive Message Scopes Guide (Mordecai.Messaging/MESSAGE_SCOPES.md)

Created detailed documentation covering:

- **Message Scope Hierarchy**: Complete explanation of all four scope levels
- **RabbitMQ Architecture**: Topic exchange configuration, routing key structure
- **Implementation Guidelines**: How to publish and subscribe to messages at each scope
- **Message Flow Examples**: Real-world scenarios with code and flow diagrams
- **Future Enhancements**: Zone messaging, priorities, persistence, compression
- **Troubleshooting**: Common issues and solutions
- **Best Practices**: Guidelines for proper messaging usage

### 3. Updated Messaging README (Mordecai.Messaging/README.md)

Added:
- Quick reference to message scope hierarchy
- Links to comprehensive MESSAGE_SCOPES.md documentation
- Brief overview of the four scope levels with routing examples

## Code Changes

### 1. New Environment Messages (Mordecai.Messaging/Messages/EnvironmentMessages.cs)

Added new message types for future zone and room environmental effects:

- `ZoneEnvironmentMessage`: Zone-wide environmental effects (weather, etc.)
- `RoomEnvironmentMessage`: Room-specific environmental effects
- `ZoneEventMessage`: Zone-wide events (bosses, festivals, invasions)
- `EnvironmentEffectType` enum: Types of environmental effects
- `ZoneEventType` enum: Types of zone events

**Note**: Zone messages are placeholders for future implementation when Zone entity and ZoneId property are added to GameMessage base class.

### 2. Updated Message Category Enum (Mordecai.Messaging/Messages/GameMessage.cs)

Added `Environment` to the `MessageCategory` enum to support environmental message categorization.

### 3. Updated RabbitMQ Publisher (Mordecai.Messaging/Services/RabbitMqGameMessagePublisher.cs)

Updated `GetMessageCategory` method to handle new environment message types:
- `ZoneEnvironmentMessage` → "environment" category
- `RoomEnvironmentMessage` → "environment" category
- `ZoneEventMessage` → "environment" category

### 4. Updated RabbitMQ Subscriber (Mordecai.Messaging/Services/RabbitMqGameMessageSubscriber.cs)

Updated `DeserializeMessage` method to deserialize new message types:
- `ZoneEnvironmentMessage`
- `RoomEnvironmentMessage`
- `ZoneEventMessage`

## Message Scope Summary

### 1. Game-Wide (Global) Scope

**Current Implementation**: ✅ Fully Implemented

- RoomId = null
- Routing: `{category}.{messagetype}.global`
- Examples: System announcements, global chat, admin broadcasts

### 2. Zone-Wide Scope

**Current Implementation**: 📋 Placeholder (awaiting Zone entity)

- Future ZoneId property on GameMessage
- Routing: `{category}.{messagetype}.zone.{zoneId}`
- Examples: Weather changes, zone bosses, zone events
- Message types created and ready to use when Zone implementation is complete

### 3. Room-Wide Scope

**Current Implementation**: ✅ Fully Implemented

- RoomId = specific room number
- Routing: `{category}.{messagetype}.{roomId}`
- Examples: Say/emote, character movement, combat, skill usage
- Dynamic subscription updates when characters move between rooms

### 4. Character-Level (Targeted) Scope

**Current Implementation**: ✅ Fully Implemented

- TargetCharacterIds array on message
- Routing: Same as parent scope, filtered at subscriber
- Examples: Private tells, error messages, personal notifications
- Application-level filtering ensures only targeted characters receive messages

## RabbitMQ Routing Architecture

### Exchange Configuration

- **Name**: `mordecai.game.events`
- **Type**: Topic
- **Durable**: Yes
- **Pattern**: `{category}.{messagetype}.{scope}`

### Current Categories

- `movement` - Character and NPC movement
- `chat` - All communication
- `combat` - Combat actions and results
- `skill` - Skill usage and advancement
- `system` - System messages and admin actions
- `environment` - Environmental effects and events (new)

### Subscription Bindings

Each character queue binds to:

**Global** (always):
```text
system.*.global
chat.globalchatmessage.*
```

**Room** (dynamic):
```text
movement.*.{currentRoomId}
chat.*.{currentRoomId}
combat.*.{currentRoomId}
skill.*.{currentRoomId}
environment.*.{currentRoomId}
```

**Zone** (future):
```text
environment.*.zone.{currentZoneId}
event.*.zone.{currentZoneId}
```

## Integration Points

### For Game Developers

When implementing new features that need messaging:

1. **Determine scope**: Which level (global, zone, room, character)?
2. **Choose message type**: Use existing or create new message class
3. **Set properties correctly**:
   - Set `RoomId` (null for global, number for room)
   - Set `TargetCharacterIds` for targeted messages
   - Set `Priority` for important messages
4. **Publish**: `await messagePublisher.PublishAsync(message);`

The routing happens automatically based on message properties.

### For Subscribers

Characters automatically receive messages based on:

- Their current room (room-scoped messages)
- Their inclusion in TargetCharacterIds (targeted messages)
- Global scope (all messages with RoomId = null)

No manual filtering needed - the system handles it.

## Future Work

### Zone Messaging Implementation

When ready to implement zone-wide messaging:

1. Add `ZoneId` property to `GameMessage` base class
2. Update `GetRoutingKey` in publisher to handle zone routing
3. Update subscriber to bind/unbind zone routing keys
4. Implement zone-based filtering in `ShouldProcessMessage`
5. Update `UpdateRoomAsync` to also update zone subscriptions
6. Test zone boundaries and transitions

### Message Priority Queues

Consider implementing priority-based routing:

- High priority: Combat, health, critical errors
- Normal priority: Chat, movement, skill usage
- Low priority: Ambient effects, background events

### Message Persistence

For offline delivery:

- Store targeted messages when recipient is offline
- Deliver on next login
- Configurable retention periods

## Testing Recommendations

### Unit Tests

- Message routing key generation for each scope
- Subscriber filtering logic
- Dynamic subscription updates on room changes

### Integration Tests

- Multiple characters in same room receive room messages
- Character movement updates subscriptions correctly
- Targeted messages only reach intended recipients
- Global messages reach all connected characters

### Performance Tests

- Message throughput under load
- Queue depth monitoring
- Subscription update latency
- Memory usage with many subscriptions

## Documentation Cross-References

- **Main Spec**: See "Message Scopes and Architecture" in `docs/MORDECAI_SPECIFICATION.md`
- **Detailed Guide**: See `Mordecai.Messaging/MESSAGE_SCOPES.md`
- **Targeted Chat**: See `Mordecai.Messaging/TARGETED_CHAT.md`
- **Messaging Status**: See `Mordecai.Messaging/MESSAGING_STATUS.md` (if exists)

---

## Quick Reference

| Scope | RoomId | ZoneId | TargetCharacterIds | Routing Pattern |
|-------|--------|--------|-------------------|-----------------|
| Game-Wide | null | N/A | null | `{category}.{type}.global` |
| Zone-Wide | null | number | null | `{category}.{type}.zone.{zoneId}` (future) |
| Room-Wide | number | N/A | null | `{category}.{type}.{roomId}` |
| Character-Level | varies | N/A | array | Same as parent scope + filtering |

---

Last updated: 2025-01-23
