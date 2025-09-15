using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Mordecai.AdminCli.Commands;

public class ListZonesCommand : AsyncCommand<ListZonesCommand.Settings>
{
    private readonly ApplicationDbContext _dbContext;

    public ListZonesCommand(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public class Settings : CommandSettings
    {
        [Description("Show only active zones")]
        [CommandOption("--active")]
        public bool ActiveOnly { get; set; }

        [Description("Show detailed information including room counts")]
        [CommandOption("--detailed")]
        public bool Detailed { get; set; }

        [Description("Filter zones by difficulty level")]
        [CommandOption("--difficulty")]
        public int? DifficultyLevel { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Ensure database exists
            await _dbContext.Database.EnsureCreatedAsync();

            var query = _dbContext.Zones.AsQueryable();

            // Apply filters
            if (settings.ActiveOnly)
            {
                query = query.Where(z => z.IsActive);
            }

            if (settings.DifficultyLevel.HasValue)
            {
                query = query.Where(z => z.DifficultyLevel == settings.DifficultyLevel.Value);
            }

            var zones = await query
                .OrderBy(z => z.DifficultyLevel)
                .ThenBy(z => z.Name)
                .ToListAsync();

            if (!zones.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No zones found matching the criteria.[/]");
                return 0;
            }

            var table = new Table();
            table.AddColumn("ID");
            table.AddColumn("Name");
            table.AddColumn("Difficulty");
            table.AddColumn("Environment");
            table.AddColumn("Weather");
            table.AddColumn("Status");
            
            if (settings.Detailed)
            {
                table.AddColumn("Rooms");
                table.AddColumn("Created By");
                table.AddColumn("Created");
            }

            foreach (var zone in zones)
            {
                var environment = zone.IsOutdoor ? "[green]Outdoor[/]" : "[blue]Indoor[/]";
                var status = zone.IsActive ? "[green]Active[/]" : "[red]Inactive[/]";
                var difficulty = GetDifficultyDisplay(zone.DifficultyLevel);

                if (settings.Detailed)
                {
                    var roomCount = await _dbContext.Rooms
                        .CountAsync(r => r.ZoneId == zone.Id && r.IsActive);
                    
                    table.AddRow(
                        zone.Id.ToString(),
                        zone.Name,
                        difficulty,
                        environment,
                        zone.WeatherType,
                        status,
                        roomCount.ToString(),
                        zone.CreatedBy,
                        zone.CreatedAt.ToString("yyyy-MM-dd")
                    );
                }
                else
                {
                    table.AddRow(
                        zone.Id.ToString(),
                        zone.Name,
                        difficulty,
                        environment,
                        zone.WeatherType,
                        status
                    );
                }
            }

            var title = settings.ActiveOnly ? "Active Zones" : "All Zones";
            if (settings.DifficultyLevel.HasValue)
            {
                title += $" (Difficulty {settings.DifficultyLevel})";
            }

            AnsiConsole.Write(new Panel(table).Header($"[bold]{title}[/]"));

            // Summary information
            AnsiConsole.WriteLine();
            var totalZones = zones.Count;
            var activeZones = zones.Count(z => z.IsActive);
            var totalRooms = settings.Detailed ? 
                await _dbContext.Rooms.CountAsync(r => zones.Select(z => z.Id).Contains(r.ZoneId) && r.IsActive) : 0;

            AnsiConsole.MarkupLine($"[dim]Total zones: {totalZones} | Active: {activeZones}[/]");
            if (settings.Detailed)
            {
                AnsiConsole.MarkupLine($"[dim]Total rooms across all zones: {totalRooms}[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
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