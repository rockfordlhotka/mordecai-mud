using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Mordecai.AdminCli.Commands;

public class ShowZoneCommand : AsyncCommand<ShowZoneCommand.Settings>
{
    private readonly ApplicationDbContext _dbContext;

    public ShowZoneCommand(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public class Settings : CommandSettings
    {
        [Description("Zone ID or name to show details for")]
        [CommandArgument(0, "<zone>")]
        public required string Zone { get; set; }

        [Description("Show rooms in the zone")]
        [CommandOption("--rooms")]
        public bool ShowRooms { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Ensure database exists
            await _dbContext.Database.EnsureCreatedAsync();

            // Try to find zone by ID first, then by name
            var zone = await FindZoneAsync(_dbContext, settings.Zone);

            if (zone == null)
            {
                AnsiConsole.MarkupLine($"[red]Zone '{settings.Zone}' not found.[/]");
                AnsiConsole.MarkupLine("[dim]Use 'mordecai-admin list-zones' to see available zones.[/]");
                return 1;
            }

            // Get room count
            var roomCount = await _dbContext.Rooms
                .CountAsync(r => r.ZoneId == zone.Id && r.IsActive);

            // Show zone details
            var panel = new Panel(CreateZoneDetailsTable(zone, roomCount))
                .Header($"[bold]Zone Details: {zone.Name}[/]")
                .Border(BoxBorder.Rounded);

            AnsiConsole.Write(panel);

            // Show rooms if requested
            if (settings.ShowRooms)
            {
                await ShowRoomsInZone(_dbContext, zone.Id);
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static async Task<Game.Entities.Zone?> FindZoneAsync(ApplicationDbContext dbContext, string zoneIdentifier)
    {
        // Try parsing as ID first
        if (int.TryParse(zoneIdentifier, out var zoneId))
        {
            var zoneById = await dbContext.Zones.FirstOrDefaultAsync(z => z.Id == zoneId);
            if (zoneById != null)
                return zoneById;
        }

        // Try finding by name (case insensitive)
        return await dbContext.Zones
            .FirstOrDefaultAsync(z => z.Name.ToLower() == zoneIdentifier.ToLower());
    }

    private static Table CreateZoneDetailsTable(Game.Entities.Zone zone, int roomCount)
    {
        var table = new Table();
        table.AddColumn("Property");
        table.AddColumn("Value");
        table.Border = TableBorder.None;

        table.AddRow("ID", zone.Id.ToString());
        table.AddRow("Name", zone.Name);
        table.AddRow("Description", zone.Description);
        table.AddRow("Difficulty Level", GetDifficultyDisplay(zone.DifficultyLevel));
        table.AddRow("Environment", zone.IsOutdoor ? "[green]Outdoor[/]" : "[blue]Indoor[/]");
        table.AddRow("Weather Type", zone.WeatherType);
        table.AddRow("Status", zone.IsActive ? "[green]Active[/]" : "[red]Inactive[/]");
        table.AddRow("Room Count", roomCount.ToString());
        table.AddRow("Created By", zone.CreatedBy);
        table.AddRow("Created At", zone.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"));

        return table;
    }

    private static async Task ShowRoomsInZone(ApplicationDbContext dbContext, int zoneId)
    {
        var rooms = await dbContext.Rooms
            .Include(r => r.RoomType)
            .Where(r => r.ZoneId == zoneId)
            .OrderBy(r => r.Id)
            .ToListAsync();

        if (!rooms.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]No rooms found in this zone.[/]");
            return;
        }

        AnsiConsole.WriteLine();
        var roomsTable = new Table();
        roomsTable.AddColumn("ID");
        roomsTable.AddColumn("Name");
        roomsTable.AddColumn("Type");
        roomsTable.AddColumn("Coordinates");
        roomsTable.AddColumn("Status");
        roomsTable.AddColumn("Created By");

        foreach (var room in rooms)
        {
            var coordinates = $"({room.X}, {room.Y}, {room.Z})";
            var status = room.IsActive ? "[green]Active[/]" : "[red]Inactive[/]";
            var roomType = room.RoomType?.Name ?? "Unknown";

            roomsTable.AddRow(
                room.Id.ToString(),
                room.Name,
                roomType,
                coordinates,
                status,
                room.CreatedBy
            );
        }

        var roomsPanel = new Panel(roomsTable)
            .Header($"[bold]Rooms in Zone ({rooms.Count})[/]")
            .Border(BoxBorder.Rounded);

        AnsiConsole.Write(roomsPanel);
    }

    private static string GetDifficultyDisplay(int difficulty)
    {
        return difficulty switch
        {
            1 => "[green]1 (Easy)[/]",
            2 => "[yellow]2 (Medium)[/]",
            3 => "[orange1]3 (Hard)[/]",
            >= 4 => "[red]" + difficulty + " (Extreme)[/]",
            _ => "[dim]" + difficulty + "[/]"
        };
    }
}