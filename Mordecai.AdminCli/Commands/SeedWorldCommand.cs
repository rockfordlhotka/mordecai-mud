using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Mordecai.AdminCli.Commands;

public class SeedWorldCommand : AsyncCommand<SeedWorldCommand.Settings>
{
    private readonly ApplicationDbContext _dbContext;

    public SeedWorldCommand(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public class Settings : CommandSettings
    {
        [Description("Force seeding even if world data already exists")]
        [CommandOption("-f|--force")]
        public bool Force { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Ensure database exists
            await _dbContext.Database.EnsureCreatedAsync();

            // Check if world data already exists
            var existingZones = await _dbContext.Zones.CountAsync();
            var existingRooms = await _dbContext.Rooms.CountAsync();

            if ((existingZones > 0 || existingRooms > 0) && !settings.Force)
            {
                AnsiConsole.MarkupLine("[yellow]World data already exists![/]");
                AnsiConsole.MarkupLine($"[dim]Found {existingZones} zones and {existingRooms} rooms.[/]");
                AnsiConsole.MarkupLine("[dim]Use --force to recreate world data.[/]");
                return 0;
            }

            if (settings.Force && (existingZones > 0 || existingRooms > 0))
            {
                AnsiConsole.MarkupLine("[yellow]Clearing existing world data...[/]");
                
                // Clear existing data in correct order (respecting foreign keys)
                _dbContext.RoomExits.RemoveRange(_dbContext.RoomExits);
                _dbContext.Rooms.RemoveRange(_dbContext.Rooms);
                _dbContext.Zones.RemoveRange(_dbContext.Zones);
                await _dbContext.SaveChangesAsync();
                
                AnsiConsole.MarkupLine("[green]Existing world data cleared.[/]");
            }

            AnsiConsole.MarkupLine("[cyan]Creating basic world structure...[/]");

            // Create Tutorial Zone
            var tutorialZone = new Zone
            {
                Name = "Tutorial Zone",
                Description = "A peaceful training area for new adventurers to learn the basics of the world.",
                DifficultyLevel = 1,
                IsOutdoor = true,
                WeatherType = "Clear",
                CreatedBy = "System",
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            _dbContext.Zones.Add(tutorialZone);
            await _dbContext.SaveChangesAsync();

            // Get the Normal room type
            var normalRoomType = await _dbContext.RoomTypes.FirstOrDefaultAsync(rt => rt.Name == "Normal");
            if (normalRoomType == null)
            {
                AnsiConsole.MarkupLine("[red]Error: Normal room type not found! Run migrations first.[/]");
                return 1;
            }

            // Create starting room at 0,0,0
            var startingRoom = new Room
            {
                ZoneId = tutorialZone.Id,
                RoomTypeId = normalRoomType.Id,
                Name = "Tutorial Starting Area",
                Description = "You find yourself in a peaceful meadow surrounded by gentle hills. This is the starting area where new adventurers begin their journey. The grass is soft beneath your feet, and a gentle breeze carries the scent of wildflowers. To the north, you can see a small path winding toward what appears to be a training ground.",
                NightDescription = "The meadow is bathed in soft moonlight, creating a serene and mystical atmosphere. Fireflies dance among the wildflowers, and the gentle sound of crickets fills the air. The path to the north is still visible in the moonlight.",
                X = 0,
                Y = 0,
                Z = 0,
                CreatedBy = "System",
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            _dbContext.Rooms.Add(startingRoom);
            await _dbContext.SaveChangesAsync();

            // Create a few more rooms to make the world interesting
            var rooms = new List<Room>
            {
                new Room
                {
                    ZoneId = tutorialZone.Id,
                    RoomTypeId = normalRoomType.Id,
                    Name = "Training Ground",
                    Description = "A well-maintained training ground with wooden practice dummies and weapon racks. This is where novice adventurers can safely practice their combat skills. The ground is packed earth, worn smooth by countless hours of training.",
                    X = 0,
                    Y = 1,
                    Z = 0,
                    CreatedBy = "System",
                    CreatedAt = DateTimeOffset.UtcNow,
                    IsActive = true
                },
                new Room
                {
                    ZoneId = tutorialZone.Id,
                    RoomTypeId = normalRoomType.Id,
                    Name = "Quiet Grove",
                    Description = "A small grove of ancient oak trees provides shade and tranquility. This peaceful spot is perfect for reflection and rest. Sunlight filters through the leaves, creating dappled patterns on the forest floor.",
                    NightDescription = "The grove is mysterious in the darkness, with moonbeams creating silver patterns through the canopy. The ancient oaks seem to whisper secrets in the night breeze.",
                    X = -1,
                    Y = 0,
                    Z = 0,
                    CreatedBy = "System",
                    CreatedAt = DateTimeOffset.UtcNow,
                    IsActive = true
                },
                new Room
                {
                    ZoneId = tutorialZone.Id,
                    RoomTypeId = normalRoomType.Id,
                    Name = "Crystal Spring",
                    Description = "A crystal-clear spring bubbles up from the earth, creating a small pool of perfectly pure water. The sound of flowing water is soothing, and the area radiates a sense of renewal and healing.",
                    X = 1,
                    Y = 0,
                    Z = 0,
                    CreatedBy = "System",
                    CreatedAt = DateTimeOffset.UtcNow,
                    IsActive = true
                }
            };

            _dbContext.Rooms.AddRange(rooms);
            await _dbContext.SaveChangesAsync();

            // Create exits to connect the rooms
            var exits = new List<RoomExit>
            {
                // Starting Area to Training Ground (north)
                new RoomExit
                {
                    FromRoomId = startingRoom.Id,
                    ToRoomId = rooms[0].Id, // Training Ground
                    Direction = "north",
                    ExitDescription = "a well-worn path",
                    IsActive = true
                },
                // Training Ground to Starting Area (south)
                new RoomExit
                {
                    FromRoomId = rooms[0].Id, // Training Ground
                    ToRoomId = startingRoom.Id,
                    Direction = "south",
                    ExitDescription = "a path back to the meadow",
                    IsActive = true
                },
                // Starting Area to Quiet Grove (west)
                new RoomExit
                {
                    FromRoomId = startingRoom.Id,
                    ToRoomId = rooms[1].Id, // Quiet Grove
                    Direction = "west",
                    ExitDescription = "a shaded path between trees",
                    IsActive = true
                },
                // Quiet Grove to Starting Area (east)
                new RoomExit
                {
                    FromRoomId = rooms[1].Id, // Quiet Grove
                    ToRoomId = startingRoom.Id,
                    Direction = "east",
                    ExitDescription = "a path back to the meadow",
                    IsActive = true
                },
                // Starting Area to Crystal Spring (east)
                new RoomExit
                {
                    FromRoomId = startingRoom.Id,
                    ToRoomId = rooms[2].Id, // Crystal Spring
                    Direction = "east",
                    ExitDescription = "a gentle path toward the sound of water",
                    IsActive = true
                },
                // Crystal Spring to Starting Area (west)
                new RoomExit
                {
                    FromRoomId = rooms[2].Id, // Crystal Spring
                    ToRoomId = startingRoom.Id,
                    Direction = "west",
                    ExitDescription = "a path back to the meadow",
                    IsActive = true
                }
            };

            _dbContext.RoomExits.AddRange(exits);
            await _dbContext.SaveChangesAsync();

            AnsiConsole.MarkupLine("[green]Basic world structure created successfully![/]");
            AnsiConsole.WriteLine();
            
            // Show summary
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Zone[/]")
                .AddColumn("[bold]Rooms[/]")
                .AddColumn("[bold]Starting Room[/]");

            table.AddRow(
                tutorialZone.Name,
                "4",
                $"{startingRoom.Name} (0,0,0)"
            );

            AnsiConsole.Write(table);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]The world is now ready for players to explore![/]");
            AnsiConsole.MarkupLine("[dim]Players will start in the Tutorial Starting Area at coordinates (0,0,0).[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}