# Mordecai MUD - Game Messaging System

## Overview

The messaging system provides real-time communication between players in the Mordecai MUD. It handles:

- Character-to-character chat (say, whisper, tell, yell)
- Global channels (gossip, newbie, admin)
- Movement notifications (player enters/leaves rooms)
- Combat actions and results
- Skill usage and experience gains
- System messages and admin actions

## Documentation

- **[MESSAGE_SCOPES.md](./MESSAGE_SCOPES.md)** - Comprehensive guide to message scopes (global, zone, room, character) and RabbitMQ routing architecture
- **[SOUND_PROPAGATION.md](./SOUND_PROPAGATION.md)** - Sound propagation system for adjacent room awareness
- **[TARGETED_CHAT.md](./TARGETED_CHAT.md)** - Details on targeted chat implementation

## Message Scope Hierarchy

Messages in Mordecai operate at four distinct hierarchical scopes:

1. **Game-Wide (Global)**: Server announcements, global chat channels, admin broadcasts
   - Routing: `{category}.{messagetype}.global`
   - Example: `system.systemmessage.global`

2. **Zone-Wide** (Future): Environmental effects, zone events, area-wide objectives
   - Routing: `{category}.{messagetype}.zone.{zoneId}`
   - Example: `environment.weatherchange.zone.5`

3. **Room-Wide**: Local chat, character movement, combat visible in room, NPC interactions
   - Routing: `{category}.{messagetype}.{roomId}`
   - Example: `chat.chatmessage.42`

4. **Character-Level**: Private tells, personal notifications, error messages
   - Routing: Same as parent scope, filtered by `TargetCharacterIds`
   - Example: `chat.chatmessage.42` with `TargetCharacterIds = [guid]`

See [MESSAGE_SCOPES.md](./MESSAGE_SCOPES.md) for complete details on implementation, routing patterns, and examples.

## Current Implementation

**Status**: RabbitMQ is the active message broker for real-time communication.

### Available Implementations

| Implementation | Status | Description |
|---|---|---|
| `RabbitMqGameMessagePublisher` | Active | Publishes messages to RabbitMQ exchanges |
| `RabbitMqGameMessageSubscriber` | Active | Subscribes to messages from RabbitMQ queues |

## Architecture

```
???????????????????    ????????????????????    ???????????????????
?   Game Actions  ?????? RabbitMQ Publisher ?????? RabbitMQ Broker ?
?   (Chat, Move)  ?    ?                  ?    ? (exchanges)     ?
???????????????????    ????????????????????    ???????????????????
                                     ?
                                     ?
???????????????????    ????????????????????    ???????????????????
? Blazor Component?????? RabbitMQ Subscriber?????? RabbitMQ Broker ?
?   (Play.razor)  ?    ?                  ?    ? (queues)        ?
???????????????????    ????????????????????    ???????????????????
```

## Message Types

All message types are defined and ready for use:

### Chat Messages
- `ChatMessage` - Room-based communication (say, whisper, yell)
- `GlobalChatMessage` - Server-wide channels
- `EmoteMessage` - Character emotes and actions

### Movement Messages  
- `PlayerMoved` - Character movement between rooms
- `PlayerJoined` - Character enters the game
- `PlayerLeft` - Character leaves a room
- `PlayerDisconnected` - Character disconnects

### Combat Messages
- `CombatStarted` - Combat initiation
- `CombatAction` - Attack actions and results
- `CombatEnded` - Combat conclusion
- `HealthChanged` - Health/damage updates

### Skill Messages
- `SkillUsed` - Skill activation
- `SkillExperienceGained` - Experience points awarded
- `SkillLearned` - New skill acquisition

### System Messages
- `SystemMessage` - Server announcements
- `AdminAction` - Administrative actions
- `ErrorMessage` - Error notifications

## Configuration

The messaging services are registered automatically. When RabbitMQ is used, configuration will support:

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": "5672", 
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

## Usage Example

The messaging API remains the same regardless of implementation:

```csharp
// Publishing a message
await messagePublisher.PublishAsync(new ChatMessage
{
    CharacterId = characterId,
    CharacterName = "Alice",
    Message = "Hello, world!",
    RoomId = currentRoomId,
    ChatType = ChatType.Say
});

// Subscribing to messages
var subscriber = subscriberFactory.CreateSubscriber(characterId, roomId);
subscriber.MessageReceived += async (message) =>
{
    // Handle received message
    await UpdateUIWithMessage(message);
};
await subscriber.StartAsync();
```

## Development Impact

### What Works Now
- Game starts with real-time messaging
- All game mechanics including real-time multiplayer features
- Character creation and management
- Room navigation and descriptions
- Skill systems and combat (multiplayer)
- Admin tools and content management

### What Needs Real Messaging? (This section is now largely obsolete as real messaging is active, but kept for historical context or future considerations)
- Multi-player chat and communication
- Real-time movement notifications
- Live combat between players
- Global channels and announcements
- Cross-character skill demonstrations

## Testing

### Current Testing
```bash
# Start RabbitMQ server first.
# Build and run
dotnet build
dotnet run --project Mordecai.Web

# Open multiple browser tabs
# Create different characters in each tab
# Test chat, movement, and interactions between characters
# Verify real-time message delivery
```

## Troubleshooting

### RabbitMQ Issues
- **Connection refused**: Ensure RabbitMQ server is running and accessible. Check firewall settings.
- **Authentication failed**: Verify username and password in configuration.
- **Missing exchange/queue**: These should be created automatically. If not, check RabbitMQ logs for errors during startup or connection.

---