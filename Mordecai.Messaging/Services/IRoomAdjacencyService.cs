using System.Collections.Generic;

namespace Mordecai.Messaging.Services;

/// <summary>
/// Information about a room adjacent to a sound source.
/// </summary>
/// <param name="RoomId">The room that will receive the propagated sound.</param>
/// <param name="Distance">Number of steps away from the source room (1 = directly connected).</param>
/// <param name="DirectionFromSource">Human readable direction from the source room toward the adjacent room.</param>
/// <param name="DirectionFromListener">Human readable direction from the adjacent room back to the source.</param>
/// <param name="PathDirections">The ordered list of directions traversed from the source room to the adjacent room.</param>
public sealed record AdjacentRoomInfo(
    int RoomId,
    int Distance,
    string DirectionFromSource,
    string DirectionFromListener,
    IReadOnlyList<string> PathDirections
);

/// <summary>
/// Service abstraction for resolving room adjacency/graph information.
/// </summary>
public interface IRoomAdjacencyService
{
    /// <summary>
    /// Returns rooms that are reachable from the supplied source room up to the requested distance.
    /// Hidden exits should be ignored.
    /// </summary>
    /// <param name="sourceRoomId">Room the sound originates from.</param>
    /// <param name="maxDistance">Maximum number of steps to travel from the source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<AdjacentRoomInfo>> GetAdjacentRoomsAsync(
        int sourceRoomId,
        int maxDistance,
        CancellationToken cancellationToken = default);
}
