using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mordecai.Game.Entities;
using Mordecai.Messaging.Services;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Resolves room adjacency information for sound propagation and other area-awareness features.
/// </summary>
public sealed class RoomAdjacencyService : IRoomAdjacencyService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<RoomAdjacencyService> _logger;

    public RoomAdjacencyService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ILogger<RoomAdjacencyService> logger)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<AdjacentRoomInfo>> GetAdjacentRoomsAsync(
        int sourceRoomId,
        int maxDistance,
        CancellationToken cancellationToken = default)
    {
        if (maxDistance <= 0)
        {
            return Array.Empty<AdjacentRoomInfo>();
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var results = new List<AdjacentRoomInfo>();
        var resultMap = new Dictionary<int, AdjacentRoomInfo>();
        var bestRemaining = new Dictionary<int, int> { [sourceRoomId] = maxDistance };
        var currentLevel = new List<QueueItem> { new(sourceRoomId, EmptyPath, maxDistance) };

        while (currentLevel.Count > 0)
        {
            var expandable = currentLevel
                .Where(item => item.RemainingSteps > 0)
                .ToList();

            if (expandable.Count == 0)
            {
                break;
            }

            var roomIds = expandable
                .Select(item => item.RoomId)
                .Distinct()
                .ToList();

            var exits = await context.RoomExits
                .Where(exit => roomIds.Contains(exit.FromRoomId)
                               && exit.IsActive
                               && !exit.IsHidden)
                .Select(exit => new ExitData(exit.FromRoomId, exit.ToRoomId, exit.Direction, exit.DoorState))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (exits.Count == 0)
            {
                break;
            }

            var exitsByRoom = exits
                .GroupBy(exit => exit.FromRoomId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var nextLevel = new List<QueueItem>();

            foreach (var item in expandable)
            {
                if (!exitsByRoom.TryGetValue(item.RoomId, out var roomExits))
                {
                    continue;
                }

                foreach (var exit in roomExits)
                {
                    var penalty = exit.DoorState == DoorState.Closed ? 1 : 0;
                    var remainingAfterStep = item.RemainingSteps - 1 - penalty;

                    if (remainingAfterStep < 0)
                    {
                        continue;
                    }

                    if (bestRemaining.TryGetValue(exit.ToRoomId, out var existingRemaining)
                        && remainingAfterStep <= existingRemaining)
                    {
                        continue;
                    }

                    bestRemaining[exit.ToRoomId] = remainingAfterStep;

                    var normalizedDirection = NormalizeDirection(exit.Direction);
                    var newPath = AppendDirection(item.PathDirections, normalizedDirection);
                    var directionInfo = CalculateDirectionInfo(newPath);
                    var distanceFromSource = newPath.Count;

                    if (resultMap.TryGetValue(exit.ToRoomId, out var existingInfo))
                    {
                        if (existingInfo.Distance > distanceFromSource)
                        {
                            var updatedInfo = new AdjacentRoomInfo(
                                exit.ToRoomId,
                                distanceFromSource,
                                directionInfo.FromSource,
                                directionInfo.FromListener,
                                newPath);

                            resultMap[exit.ToRoomId] = updatedInfo;
                            var index = results.FindIndex(info => info.RoomId == exit.ToRoomId);
                            if (index >= 0)
                            {
                                results[index] = updatedInfo;
                            }
                        }
                    }
                    else
                    {
                        var info = new AdjacentRoomInfo(
                            exit.ToRoomId,
                            distanceFromSource,
                            directionInfo.FromSource,
                            directionInfo.FromListener,
                            newPath);

                        resultMap.Add(exit.ToRoomId, info);
                        results.Add(info);
                    }

                    if (remainingAfterStep > 0)
                    {
                        nextLevel.Add(new QueueItem(exit.ToRoomId, newPath, remainingAfterStep));
                    }
                }
            }

            currentLevel = nextLevel;
        }

        if (results.Count == 0)
        {
            _logger.LogDebug("No reachable adjacent rooms from room {RoomId} within distance {Distance}", sourceRoomId, maxDistance);
        }

        return results;
    }

    private static readonly ReadOnlyCollection<string> EmptyPath = Array.AsReadOnly(Array.Empty<string>());

    private static ReadOnlyCollection<string> AppendDirection(IReadOnlyList<string> path, string direction)
    {
        var newPath = new List<string>(path.Count + 1);
        newPath.AddRange(path);
        newPath.Add(direction);
        return newPath.AsReadOnly();
    }

    private static (string FromSource, string FromListener) CalculateDirectionInfo(IReadOnlyList<string> pathDirections)
    {
        var vector = (X: 0, Y: 0, Z: 0);

        foreach (var direction in pathDirections)
        {
            if (DirectionVectors.TryGetValue(direction, out var delta))
            {
                vector = (vector.X + delta.X, vector.Y + delta.Y, vector.Z + delta.Z);
            }
        }

        var fromSource = DescribeVector(vector);
        var fromListener = DescribeVector((-vector.X, -vector.Y, -vector.Z));

        return (fromSource, fromListener);
    }

    private static string DescribeVector((int X, int Y, int Z) vector)
    {
        var x = Math.Sign(vector.X);
        var y = Math.Sign(vector.Y);
        var z = Math.Sign(vector.Z);

        var horizontal = BuildHorizontalDirection(x, y);

        if (string.IsNullOrEmpty(horizontal))
        {
            if (z > 0)
            {
                return "up";
            }

            if (z < 0)
            {
                return "down";
            }

            return "nearby";
        }

        return z switch
        {
            > 0 => $"{horizontal} and above",
            < 0 => $"{horizontal} and below",
            _ => horizontal
        };
    }

    private static string BuildHorizontalDirection(int x, int y)
    {
        var northSouth = y switch
        {
            > 0 => "north",
            < 0 => "south",
            _ => string.Empty
        };

        var eastWest = x switch
        {
            > 0 => "east",
            < 0 => "west",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(northSouth) && string.IsNullOrEmpty(eastWest))
        {
            return string.Empty;
        }

        return string.Concat(northSouth, eastWest);
    }

    private static string NormalizeDirection(string? direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
        {
            return "unknown";
        }

        var trimmed = direction.Trim().ToLowerInvariant();

        return trimmed switch
        {
            "n" => "north",
            "s" => "south",
            "e" => "east",
            "w" => "west",
            "ne" => "northeast",
            "nw" => "northwest",
            "se" => "southeast",
            "sw" => "southwest",
            "u" => "up",
            "d" => "down",
            _ => trimmed
        };
    }

    private static readonly IReadOnlyDictionary<string, (int X, int Y, int Z)> DirectionVectors =
        new Dictionary<string, (int X, int Y, int Z)>(StringComparer.OrdinalIgnoreCase)
        {
            ["north"] = (0, 1, 0),
            ["south"] = (0, -1, 0),
            ["east"] = (1, 0, 0),
            ["west"] = (-1, 0, 0),
            ["northeast"] = (1, 1, 0),
            ["northwest"] = (-1, 1, 0),
            ["southeast"] = (1, -1, 0),
            ["southwest"] = (-1, -1, 0),
            ["up"] = (0, 0, 1),
            ["down"] = (0, 0, -1),
            ["unknown"] = (0, 0, 0),
            ["in"] = (0, 0, 0),
            ["out"] = (0, 0, 0)
        };

    private sealed record ExitData(int FromRoomId, int ToRoomId, string? Direction, DoorState DoorState);
    private sealed record QueueItem(int RoomId, IReadOnlyList<string> PathDirections, int RemainingSteps);
}
