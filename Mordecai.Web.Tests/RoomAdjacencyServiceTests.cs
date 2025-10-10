using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using Mordecai.Web.Services;
using Xunit;

namespace Mordecai.Web.Tests;

public sealed class RoomAdjacencyServiceTests
{
    [Fact]
    public async Task ClosedDoorStopsPropagationAtOneStep()
    {
        var factory = CreateFactory($"DoorsBlock_{Guid.NewGuid():N}");
        var ids = await SeedWorldAsync(factory, DoorState.Closed);
        var service = new RoomAdjacencyService(factory, NullLogger<RoomAdjacencyService>.Instance);

        var adjacent = await service.GetAdjacentRoomsAsync(ids.SourceRoomId, 1, TestContext.Current.CancellationToken);

        Assert.Empty(adjacent);
    }

    [Fact]
    public async Task ClosedDoorConsumesOneStepWithinRange()
    {
        var factory = CreateFactory($"DoorsConsume_{Guid.NewGuid():N}");
        var ids = await SeedWorldAsync(factory, DoorState.Closed);
        var service = new RoomAdjacencyService(factory, NullLogger<RoomAdjacencyService>.Instance);

        var adjacent = await service.GetAdjacentRoomsAsync(ids.SourceRoomId, 2, TestContext.Current.CancellationToken);

        Assert.Single(adjacent);
        var throughDoor = Assert.Single(adjacent, info => info.RoomId == ids.RoomBeyondDoorId);
        Assert.Equal(1, throughDoor.Distance);
    }

    [Fact]
    public async Task OpenDoorAllowsPropagationNormally()
    {
        var factory = CreateFactory($"DoorsOpen_{Guid.NewGuid():N}");
        var ids = await SeedWorldAsync(factory, DoorState.Open);
        var service = new RoomAdjacencyService(factory, NullLogger<RoomAdjacencyService>.Instance);

        var adjacent = await service.GetAdjacentRoomsAsync(ids.SourceRoomId, 1, TestContext.Current.CancellationToken);

        var throughDoor = Assert.Single(adjacent);
        Assert.Equal(ids.RoomBeyondDoorId, throughDoor.RoomId);
        Assert.Equal(1, throughDoor.Distance);
    }

    private static IDbContextFactory<ApplicationDbContext> CreateFactory(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new TestDbContextFactory(options);
    }

    private static async Task<(int SourceRoomId, int RoomBeyondDoorId, int RemoteRoomId)> SeedWorldAsync(
        IDbContextFactory<ApplicationDbContext> factory,
        DoorState doorState)
    {
        await using var context = await factory.CreateDbContextAsync();

        var zone = new Zone
        {
            Name = "Test Zone",
            Description = "Integration zone",
            CreatedBy = "tests",
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
            WeatherType = "Clear"
        };

        var roomType = new RoomType
        {
            Name = "Generic",
            Description = "Generic room type",
            AllowsCombat = true,
            AllowsLogout = true
        };

        context.Zones.Add(zone);
        context.RoomTypes.Add(roomType);
        await context.SaveChangesAsync();

        var sourceRoom = new Room
        {
            ZoneId = zone.Id,
            Zone = zone,
            RoomTypeId = roomType.Id,
            RoomType = roomType,
            Name = "Source Room",
            Description = "The origin of the sound.",
            CreatedBy = "tests",
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        var doorRoom = new Room
        {
            ZoneId = zone.Id,
            Zone = zone,
            RoomTypeId = roomType.Id,
            RoomType = roomType,
            Name = "Door Room",
            Description = "Room separated by a door.",
            CreatedBy = "tests",
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
            X = 1
        };

        var remoteRoom = new Room
        {
            ZoneId = zone.Id,
            Zone = zone,
            RoomTypeId = roomType.Id,
            RoomType = roomType,
            Name = "Remote Room",
            Description = "A distant room beyond the doorway.",
            CreatedBy = "tests",
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
            X = 2
        };

        context.Rooms.AddRange(sourceRoom, doorRoom, remoteRoom);
        await context.SaveChangesAsync();

        var doorName = doorState == DoorState.None ? null : "oak door";

        var exits = new List<RoomExit>
        {
            new()
            {
                FromRoomId = sourceRoom.Id,
                ToRoomId = doorRoom.Id,
                Direction = "east",
                IsActive = true,
                DoorState = doorState,
                DoorName = doorName,
                ExitDescription = doorName
            },
            new()
            {
                FromRoomId = doorRoom.Id,
                ToRoomId = sourceRoom.Id,
                Direction = "west",
                IsActive = true,
                DoorState = doorState,
                DoorName = doorName,
                ExitDescription = doorName
            },
            new()
            {
                FromRoomId = doorRoom.Id,
                ToRoomId = remoteRoom.Id,
                Direction = "east",
                IsActive = true
            },
            new()
            {
                FromRoomId = remoteRoom.Id,
                ToRoomId = doorRoom.Id,
                Direction = "west",
                IsActive = true
            }
        };

        context.RoomExits.AddRange(exits);
        await context.SaveChangesAsync();

        return (sourceRoom.Id, doorRoom.Id, remoteRoom.Id);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public ApplicationDbContext CreateDbContext() => new(_options);

        public ValueTask<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new ApplicationDbContext(_options));
        }
    }
}
