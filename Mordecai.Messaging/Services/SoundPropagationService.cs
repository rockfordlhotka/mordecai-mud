using Microsoft.Extensions.Logging;
using Mordecai.Messaging.Messages;

namespace Mordecai.Messaging.Services;

/// <summary>
/// Service that propagates sounds to adjacent rooms based on sound level and room connectivity
/// </summary>
public sealed class SoundPropagationService : ISoundPropagationService
{
    private readonly IGameMessagePublisher _messagePublisher;
    private readonly IRoomAdjacencyService _adjacencyService;
    private readonly ILogger<SoundPropagationService> _logger;

    public SoundPropagationService(
        IGameMessagePublisher messagePublisher,
        IRoomAdjacencyService adjacencyService,
        ILogger<SoundPropagationService> logger)
    {
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _adjacencyService = adjacencyService ?? throw new ArgumentNullException(nameof(adjacencyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PropagateSound(
        int sourceRoomId,
        SoundLevel soundLevel,
        SoundType soundType,
        string description,
        string? characterName = null,
        string? detailedMessage = null,
        CancellationToken cancellationToken = default)
    {
        if (soundLevel == SoundLevel.Silent)
        {
            return;
        }

        var maxDistance = GetPropagationDistance(soundLevel);
        if (maxDistance <= 0)
        {
            return;
        }

        try
        {
            var adjacentRooms = await _adjacencyService
                .GetAdjacentRoomsAsync(sourceRoomId, maxDistance, cancellationToken)
                .ConfigureAwait(false);

            if (adjacentRooms.Count == 0)
            {
                _logger.LogDebug("No adjacent rooms found for sound propagation from room {RoomId}", sourceRoomId);
                return;
            }

            var messages = new List<AdjacentRoomSoundMessage>(adjacentRooms.Count);

            foreach (var adjacent in adjacentRooms)
            {
                var message = BuildSoundMessage(
                    sourceRoomId,
                    adjacent,
                    soundLevel,
                    soundType,
                    description,
                    characterName,
                    detailedMessage);

                if (message is { } builtMessage)
                {
                    messages.Add(builtMessage);
                }
            }

            if (messages.Count == 0)
            {
                return;
            }

            await _messagePublisher.PublishBatchAsync(messages, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Propagated {MessageCount} sound messages from room {SourceRoomId} with level {SoundLevel}",
                messages.Count,
                sourceRoomId,
                soundLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to propagate sound from room {SourceRoomId}", sourceRoomId);
        }
    }

    private static AdjacentRoomSoundMessage? BuildSoundMessage(
        int sourceRoomId,
        AdjacentRoomInfo adjacent,
        SoundLevel soundLevel,
        SoundType soundType,
        string baseDescription,
        string? characterName,
        string? detailedMessage)
    {
        var directionPhrase = FormatDirectionPhrase(adjacent.DirectionFromListener, adjacent.Distance);

        if (string.IsNullOrEmpty(directionPhrase))
        {
            directionPhrase = adjacent.Distance == 1 ? "nearby" : "somewhere nearby";
        }

        string description;
        string? detailText = null;
        string? resolvedCharacterName = null;

        if (soundType == SoundType.Speech)
        {
            if (adjacent.Distance == 1 && soundLevel >= SoundLevel.Loud && !string.IsNullOrWhiteSpace(detailedMessage))
            {
                resolvedCharacterName = string.IsNullOrWhiteSpace(characterName) ? "Someone" : characterName;
                var verb = GetSpeechVerb(soundLevel);
                var quotedMessage = QuoteIfNeeded(detailedMessage);
                description = $"{resolvedCharacterName} {verb} {directionPhrase}: {quotedMessage}";
                detailText = detailedMessage;
            }
            else
            {
                var speechDescriptor = GetSpeechDescriptor(soundLevel, adjacent.Distance, baseDescription);
                description = BuildGenericMessage(speechDescriptor, directionPhrase);
            }
        }
        else
        {
            var descriptor = GetGenericDescriptor(soundType, adjacent.Distance, baseDescription);
            description = BuildGenericMessage(descriptor, directionPhrase);
        }

        return new AdjacentRoomSoundMessage(
            sourceRoomId,
            adjacent.RoomId,
            adjacent.DirectionFromListener,
            soundLevel,
            soundType,
            description,
            resolvedCharacterName,
            detailText);
    }

    private static string BuildGenericMessage(string descriptor, string directionPhrase)
    {
        if (string.Equals(directionPhrase, "nearby", StringComparison.OrdinalIgnoreCase))
        {
            return $"You hear {descriptor} nearby.";
        }

        return $"You hear {descriptor} {directionPhrase}.";
    }

    private static string GetSpeechDescriptor(SoundLevel soundLevel, int distance, string baseDescription)
    {
        if (!string.IsNullOrWhiteSpace(baseDescription))
        {
            baseDescription = baseDescription.Trim();
        }

        return (soundLevel, distance) switch
        {
            (SoundLevel.Quiet, _) => "muffled voices",
            (SoundLevel.Normal, 1) => string.IsNullOrWhiteSpace(baseDescription) ? "someone speaking" : baseDescription,
            (SoundLevel.Loud, 1) => "someone shouting",
            (SoundLevel.VeryLoud, 1) => "someone shouting loudly",
            (SoundLevel.Deafening, 1) => "someone screaming",
            (_, 2) => "distant shouting",
            _ => "very distant shouting"
        };
    }

    private static string GetGenericDescriptor(SoundType soundType, int distance, string baseDescription)
    {
        var descriptor = string.IsNullOrWhiteSpace(baseDescription)
            ? GetDefaultDescriptor(soundType)
            : baseDescription.Trim();

        return distance switch
        {
            1 => descriptor,
            2 => descriptor.StartsWith("distant", StringComparison.OrdinalIgnoreCase) ? descriptor : $"distant {descriptor}",
            _ => descriptor.StartsWith("very distant", StringComparison.OrdinalIgnoreCase) ? descriptor : $"very distant {descriptor}"
        };
    }

    private static string GetDefaultDescriptor(SoundType soundType) => soundType switch
    {
        SoundType.Combat => "the sounds of combat",
        SoundType.Magic => "magical energies",
        SoundType.Movement => "movement",
        SoundType.Environmental => "ambient sounds",
        SoundType.Music => "music",
        SoundType.Animal => "animal sounds",
        SoundType.Mechanical => "mechanical noises",
        SoundType.Destruction => "a crashing sound",
        _ => "a sound"
    };

    private static string FormatDirectionPhrase(string direction, int distance)
    {
        if (string.IsNullOrWhiteSpace(direction) || direction.Equals("nearby", StringComparison.OrdinalIgnoreCase))
        {
            return distance == 1 ? "nearby" : "somewhere nearby";
        }

        if (direction.Equals("above", StringComparison.OrdinalIgnoreCase))
        {
            return distance == 1 ? "from above" : "somewhere above";
        }

        if (direction.Equals("down", StringComparison.OrdinalIgnoreCase) || direction.Equals("below", StringComparison.OrdinalIgnoreCase))
        {
            return distance == 1 ? "from below" : "somewhere below";
        }

        var preposition = distance == 1 ? "from" : "to";
        return $"{preposition} the {direction}";
    }

    private static string GetSpeechVerb(SoundLevel soundLevel) => soundLevel switch
    {
        >= SoundLevel.Deafening => "bellows",
        >= SoundLevel.VeryLoud => "shouts",
        _ => "yells"
    };

    private static string QuoteIfNeeded(string message)
    {
        var trimmed = message.Trim();
        if (trimmed.StartsWith("\"", StringComparison.Ordinal) && trimmed.EndsWith("\"", StringComparison.Ordinal))
        {
            return trimmed;
        }

        return $"\"{trimmed}\"";
    }

    private static int GetPropagationDistance(SoundLevel soundLevel) => soundLevel switch
    {
        SoundLevel.Silent => 0,
        SoundLevel.Quiet => 1,
        SoundLevel.Normal => 1,
        SoundLevel.Loud => 2,
        SoundLevel.VeryLoud => 3,
        SoundLevel.Deafening => 4,
        _ => 0
    };
}
