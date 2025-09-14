# Targeted Say Command Implementation

## Overview

The "say" command has been enhanced to support targeting specific characters, NPCs, or mobs in the same room. This allows for more immersive role-playing interactions where characters can direct their speech to specific entities.

## How It Works

### Basic Usage

1. **General say**: `say Hello everyone!`
   - Broadcasts to all characters in the room
   - Display: "PlayerName says, 'Hello everyone!'"

2. **Targeted say**: `say guard Hello there!` 
   - Directs speech to a specific target (the guard)
   - Display: "PlayerName says to the guard, 'Hello there!'"

### Smart Command Parsing

The system intelligently parses commands to distinguish between targeted and general speech:

- `say hello` ? General message: "hello"
- `say guard hello` ? If "guard" is a valid target, targeted message to guard: "hello"
- `say guard hello there` ? If "guard" is a valid target, targeted message: "hello there"
- `say nonexistent hello` ? General message: "nonexistent hello" (since "nonexistent" isn't a valid target)

### Target Discovery

Use the `targets` command to see all available targets in your current room:

```
> targets
Available targets in this room:
  village guard (NPC)
  merchant (NPC) 
  stray cat (mob)
  OtherPlayer (character)
```

## Technical Implementation

### Message Structure

The `ChatMessage` record has been enhanced with targeting information:

```csharp
public sealed record ChatMessage(
    Guid CharacterId,
    string CharacterName,
    int SourceRoomId,
    string Message,
    ChatType ChatType = ChatType.Say,
    Guid? TargetId = null,
    string? TargetName = null,
    TargetType? TargetType = null
) : GameMessage
```

### Target Resolution Service

The `TargetResolutionService` handles finding targets by name within rooms:

- **Characters**: Searches all characters (currently global, will be room-specific when rooms are implemented)
- **NPCs**: Simulated NPCs based on room ID (placeholder for future NPC system)
- **Mobs**: Simulated mobs based on room ID (placeholder for future mob system)

### Message Formatting

Targeted messages are formatted differently based on target type:

- **Character**: "PlayerName says to OtherPlayer, 'message'"
- **NPC**: "PlayerName says to the village guard, 'message'"
- **Mob**: "PlayerName says to the stray cat, 'message'"

## Room-Based Targets (Simulated)

Since the full room and NPC systems aren't implemented yet, the system includes simulated targets for demonstration:

### Room 1 (Tutorial Area)
- village guard (NPC)
- merchant (NPC)
- stray cat (mob)

### Room 2 (Inn)
- innkeeper (NPC)
- tavern wench (NPC)
- drunk patron (NPC)

### Room 3 (Forest)
- forest sprite (mob)
- ancient oak (NPC)
- woodland fox (mob)

### Other Rooms
- mysterious figure (NPC)

## Examples

```
> say Hello everyone!
You say: Hello everyone!
[Others see: "PlayerName says, 'Hello everyone!'"]

> say guard Good morning!
You say to guard: Good morning!
[Others see: "PlayerName says to the village guard, 'Good morning!'"]

> say cat Here kitty kitty!
You say to cat: Here kitty kitty!
[Others see: "PlayerName says to the stray cat, 'Here kitty kitty!'"]

> say nonexistent Hello there!
You say: nonexistent Hello there!
[General message since "nonexistent" isn't a valid target]

> targets
Available targets in this room:
  village guard (NPC)
  merchant (NPC)
  stray cat (mob)
```

## Integration with Messaging System

Targeted messages flow through the same RabbitMQ publish-subscribe system:

1. **Command Processing**: Parse user input to detect targeting
2. **Target Resolution**: Validate and resolve target names
3. **Message Creation**: Create ChatMessage with targeting information
4. **Message Publishing**: Publish via RabbitMQ to all room subscribers
5. **Message Formatting**: Format differently for speaker vs. observers

## Future Enhancements

### Short-term
- Auto-completion for target names
- Partial name matching (e.g., "guard" matches "village guard")
- Case-insensitive matching

### Long-term
- Integration with actual Room/NPC/Mob entities
- Whisper and tell commands for private communication
- Target highlighting in the UI
- Distance-based communication (room adjacency)

## Error Handling

- **Invalid target**: "There is no 'targetname' here."
- **Empty message**: "Say what?"
- **Invalid target name**: "Invalid target name." (for names with invalid characters)

## Performance Considerations

- Target resolution uses async database queries
- Simulated targets are generated on-demand (no database storage)
- Message filtering happens at the subscriber level
- Target validation occurs before message publishing

This implementation provides a solid foundation for targeted communication that will integrate seamlessly with future NPC and mob systems while maintaining the real-time, immersive experience of the MUD.