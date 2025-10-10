using Mordecai.Messaging.Messages;

namespace Mordecai.Messaging.Services;

/// <summary>
/// Service responsible for propagating sounds to adjacent rooms
/// </summary>
public interface ISoundPropagationService
{
    /// <summary>
    /// Propagates a sound from a source room to adjacent rooms based on sound level
    /// </summary>
    /// <param name="sourceRoomId">The room where the sound originates</param>
    /// <param name="soundLevel">The loudness of the sound</param>
    /// <param name="soundType">The type of sound</param>
    /// <param name="description">Description of what is heard in adjacent rooms</param>
    /// <param name="characterName">Optional name of character making the sound</param>
    /// <param name="detailedMessage">Optional detailed message for louder sounds (heard 1 room away)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PropagateSound(
        int sourceRoomId,
        SoundLevel soundLevel,
        SoundType soundType,
        string description,
        string? characterName = null,
        string? detailedMessage = null,
        CancellationToken cancellationToken = default);
}
