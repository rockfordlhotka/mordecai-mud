# Mordecai MUD Messaging System

This document explains the publish-subscribe messaging system implemented for the Mordecai MUD using RabbitMQ.

## Overview

The messaging system enables real-time communication between players, NPCs, and game systems through a centralized publish-subscribe model. All game events (chat, movement, combat, skill usage, etc.) flow through RabbitMQ, allowing for scalable and decoupled communication.

## Architecture

### Components

1. **Message Contracts** (`Mordecai.Messaging.Messages`)
   - Strongly-typed immutable records representing game events
   - Categories: Movement, Chat, Combat, Skills, System, Admin

2. **Publisher Service** (`IGameMessagePublisher`)
   - Publishes messages to RabbitMQ exchange
   - Uses topic-based routing for efficient message delivery

3. **Subscriber Service** (`IGameMessageSubscriber`)
   - Character-specific message subscription
   - Automatic filtering based on room location and character targeting

4. **Broadcast Service** (`CharacterMessageBroadcastService`)
   - Manages Blazor component subscriptions
   - Routes messages from RabbitMQ to UI components

## Message Types

### Movement Messages
- `PlayerMoved` - Character arrives in a new room
- `PlayerLeft` - Character leaves a room
- `PlayerJoined` - Character logs into the game
- `PlayerDisconnected` - Character logs out

### Chat Messages
- `ChatMessage` - Room-based communication (say, whisper, yell)
- `GlobalChatMessage` - Global channels (OOC, etc.)
- `EmoteMessage` - Character emotes and actions

### Combat Messages
- `CombatStarted` - Combat begins between entities
- `CombatAction` - Individual attacks/actions
- `CombatEnded` - Combat resolution
- `HealthChanged` - Significant health changes

### Skill Messages
- `SkillExperienceGained` - Character gains skill XP
- `SkillUsed` - Skill usage (visible to others)
- `SkillLearned` - Character learns new skill

### System Messages
- `SystemMessage` - Server announcements
- `AdminAction` - Administrative actions
- `ErrorMessage` - Error notifications

## Routing Strategy

Messages use topic-based routing with the pattern: `{category}.{type}.{scope}`

Examples:
- `movement.playermoved.1` - Player movement in room 1
- `chat.chatmessage.5` - Chat message in room 5
- `system.systemmessage.global` - Global system message

### Message Filtering

Characters automatically receive:
- Messages from their current room
- Global messages (system announcements, OOC chat)
- Messages specifically targeted to them (tells, errors, skill gains)

## Usage Examples

### Publishing a Message

```csharp
// Inject the publisher service
private readonly IGameMessagePublisher _publisher;

// Publish a chat message
var message = new ChatMessage(
    characterId: player.Id,
    characterName: player.Name,
    sourceRoomId: player.CurrentRoomId,
    message: "Hello, world!",
    chatType: ChatType.Say
);

await _publisher.PublishAsync(message);
```

### Subscribing to Messages (Blazor Component)

```csharp
@inject CharacterMessageBroadcastService MessageBroadcastService
@implements IDisposable

protected override async Task OnInitializedAsync()
{
    // Register for messages
    MessageBroadcastService.MessageReceived += OnMessageReceived;
    await MessageBroadcastService.RegisterCharacterListenerAsync(characterId, currentRoomId);
}

private void OnMessageReceived(Guid targetCharacterId, string message)
{
    if (targetCharacterId == characterId)
    {
        InvokeAsync(() =>
        {
            // Update UI with new message
            AddMessage(message);
            StateHasChanged();
        });
    }
}

public void Dispose()
{
    MessageBroadcastService.MessageReceived -= OnMessageReceived;
    _ = MessageBroadcastService.UnregisterCharacterListenerAsync(characterId);
}
```

### Using GameActionService

```csharp
// Inject the game action service
private readonly GameActionService _gameActionService;

// Handle character movement
await _gameActionService.HandleCharacterMovementAsync(
    characterId: player.Id,
    characterName: player.Name,
    fromRoomId: oldRoom.Id,
    toRoomId: newRoom.Id,
    direction: "north"
);

// Handle chat
await _gameActionService.HandleChatMessageAsync(
    characterId: player.Id,
    characterName: player.Name,
    roomId: player.CurrentRoomId,
    message: "Hello everyone!",
    chatType: ChatType.Say
);
```

## Configuration

The system is configured in `Program.cs`:

```csharp
// Add RabbitMQ client (Aspire integration)
builder.AddRabbitMQClient("messaging");

// Add game messaging services
builder.Services.AddGameMessaging();

// Add character message broadcast service
builder.Services.AddSingleton<CharacterMessageBroadcastService>();

// Add game action service
builder.Services.AddScoped<GameActionService>();
```

## Integration with Aspire

The messaging system integrates with .NET Aspire for development orchestration:

1. **AppHost Configuration** - RabbitMQ is configured in `Mordecai.AppHost`
2. **Service Discovery** - Connection strings are automatically provided
3. **Health Checks** - RabbitMQ health is monitored
4. **Observability** - Message publishing/consumption is traced

## Performance Considerations

### Scalability
- Topic-based routing ensures messages only go to interested parties
- Character-specific queues auto-delete when characters disconnect
- Publisher uses connection pooling for high throughput

### Memory Management
- Messages are immutable records with minimal memory footprint
- Character subscriptions use reference counting
- Message history is limited to prevent memory leaks

### Error Handling
- Failed messages are logged and optionally requeued
- Dead letter queues can be configured for undeliverable messages
- Circuit breaker patterns protect against RabbitMQ outages

## Development Testing

### Local Setup
1. Start the Aspire AppHost project
2. RabbitMQ will be automatically started in a container
3. Multiple browser windows can simulate different characters
4. Messages will flow between characters in real-time

### Message Flow Testing
1. Create multiple test characters
2. Join the game with different characters in different browser tabs
3. Test chat, movement, and other interactions
4. Messages should appear in real-time across all connected clients

## Future Enhancements

### Planned Features
- Message persistence for offline delivery
- Rate limiting and spam protection
- Message encryption for sensitive data
- Analytics and metrics collection

### Scaling Considerations
- Cluster RabbitMQ for high availability
- Implement message sharding for large player counts
- Consider Redis Streams for ultra-high throughput scenarios

## Troubleshooting

### Common Issues
1. **Messages not received** - Check character room subscription
2. **Connection errors** - Verify RabbitMQ is running
3. **Performance issues** - Monitor queue depths and connection counts

### Debugging
- Enable debug logging for message flow
- Use RabbitMQ Management UI to inspect queues
- Monitor Aspire dashboard for service health

This messaging system provides a robust foundation for real-time multiplayer interaction in the Mordecai MUD, with room for growth as the game evolves.