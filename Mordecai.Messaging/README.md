# Mordecai MUD - Game Messaging System

> **⚠️ CURRENT STATUS**: Using stub implementations due to RabbitMQ.Client/.NET 9 compatibility issues.  
> See [MESSAGING_STATUS.md](./MESSAGING_STATUS.md) for details.

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

**Status**: Using stub/mock implementations that log message operations but don't provide real-time functionality.

### Available Implementations

| Implementation | Status | Description |
|---|---|---|
| `StubGameMessagePublisher` | ? Active | Logs message publishing attempts |
| `StubGameMessageSubscriber` | ? Active | Logs subscription attempts |
| RabbitMQ Implementation | ?? Disabled | Backed up due to .NET 9 compatibility issues |

## Architecture

```
???????????????????    ????????????????????    ???????????????????
?   Game Actions  ?????? Message Publisher ?????? Stub Logger     ?
?   (Chat, Move)  ?    ?                  ?    ? (Development)   ?
???????????????????    ????????????????????    ???????????????????

???????????????????    ????????????????????    ???????????????????
? Blazor Component?????? Message Subscriber?????? No Messages     ?
?   (Play.razor)  ?    ?                  ?    ? (Stub Mode)     ?
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

### Current (Stub Mode)
No configuration required. Services are registered automatically:

```csharp
// In Program.cs - already configured
builder.Services.AddGameMessaging(); // Uses stubs automatically
```

### Future (RabbitMQ Mode)
When RabbitMQ is restored, configuration will support:

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
// Publishing a message (currently logs only)
await messagePublisher.PublishAsync(new ChatMessage
{
    CharacterId = characterId,
    CharacterName = "Alice",
    Message = "Hello, world!",
    RoomId = currentRoomId,
    ChatType = ChatType.Say
});

// Subscribing to messages (currently no-op)
var subscriber = subscriberFactory.CreateSubscriber(characterId, roomId);
subscriber.MessageReceived += async (message) =>
{
    // Handle received message (not called in stub mode)
    await UpdateUIWithMessage(message);
};
await subscriber.StartAsync();
```

## Development Impact

### What Works Now ?
- Game starts without messaging errors
- All game mechanics except real-time multiplayer
- Character creation and management
- Room navigation and descriptions
- Skill systems and combat (single-player)
- Admin tools and content management

### What Needs Real Messaging ?
- Multi-player chat and communication  
- Real-time movement notifications
- Live combat between players
- Global channels and announcements
- Cross-character skill demonstrations

## Future Restoration Options

### Option 1: Fix RabbitMQ Compatibility
- Research RabbitMQ.Client versions compatible with .NET 9
- Update package and restore backed-up implementations
- Benefit: Keeps existing architecture

### Option 2: Switch to SignalR
- Use built-in ASP.NET Core SignalR hubs
- Create SignalR-based publisher/subscriber implementations
- Benefit: Native Blazor integration, no external dependencies

### Option 3: Use Redis Pub/Sub  
- Implement Redis-based messaging
- Better performance at scale than RabbitMQ
- Benefit: Simpler setup, proven reliability

### Option 4: Custom WebSocket Solution
- Direct WebSocket communication between clients
- Custom message routing in the web application
- Benefit: Full control, minimal dependencies

## Testing

### Current Testing
```bash
# Build and run - should work without errors
dotnet build
dotnet run --project Mordecai.Web

# Check logs for stub message operations
# Look for "Stub: Would publish message..." entries
```

### Future Testing (With Real Messaging)
1. Start RabbitMQ server
2. Open multiple browser tabs
3. Create different characters in each tab
4. Test chat, movement, and interactions between characters
5. Verify real-time message delivery

## Troubleshooting

### Current Issues
- **No real-time messaging**: Expected behavior with stub implementations
- **Messages only logged**: Check application logs to see message operations

### When Restoring RabbitMQ
- **Connection refused**: Ensure RabbitMQ server is running
- **Authentication failed**: Check username/password configuration  
- **Missing exchange**: Will be created automatically on first connection

---

**Next Steps**: Choose and implement one of the restoration options above to enable full multiplayer functionality.