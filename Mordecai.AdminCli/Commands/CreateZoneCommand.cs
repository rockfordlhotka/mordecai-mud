using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Mordecai.AdminCli.Commands;

public class CreateZoneCommand : AsyncCommand<CreateZoneCommand.Settings>
{
    private readonly ApplicationDbContext _dbContext;

    public CreateZoneCommand(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public class Settings : CommandSettings
    {
        [Description("Name of the zone")]
        [CommandOption("--name|-n")]
        public required string Name { get; set; }

        [Description("Description of the zone")]
        [CommandOption("--description|-d")]
        public required string Description { get; set; }

        [Description("Difficulty level (1-10, default: 1)")]
        [CommandOption("--difficulty")]
        public int DifficultyLevel { get; set; } = 1;

        [Description("Weather type (Clear, Cloudy, Rainy, etc.)")]
        [CommandOption("--weather|-w")]
        public string WeatherType { get; set; } = "Clear";

        [Description("Zone creator name (default: CLI)")]
        [CommandOption("--creator|-c")]
        public string CreatedBy { get; set; } = "CLI";

        [Description("Mark zone as indoor (default: outdoor)")]
        [CommandOption("--indoor")]
        public bool IsIndoor { get; set; }

        [Description("Mark zone as inactive (default: active)")]
        [CommandOption("--inactive")]
        public bool IsInactive { get; set; }

        [Description("Create zone without confirmation")]
        [CommandOption("--force|-f")]
        public bool Force { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Ensure database exists
            await _dbContext.Database.EnsureCreatedAsync();

            // Validate inputs
            if (settings.DifficultyLevel < 1 || settings.DifficultyLevel > 10)
            {
                AnsiConsole.MarkupLine("[red]Difficulty level must be between 1 and 10.[/]");
                return 1;
            }

            // Check if zone name already exists
            var existingZone = await _dbContext.Zones
                .FirstOrDefaultAsync(z => z.Name.ToLower() == settings.Name.ToLower());

            if (existingZone != null)
            {
                AnsiConsole.MarkupLine($"[red]A zone with the name '{settings.Name}' already exists (ID: {existingZone.Id}).[/]");
                return 1;
            }

            // Validate weather type
            var validWeatherTypes = new[] { "Clear", "Cloudy", "Rainy", "Stormy", "Snowy", "Foggy", "Windy", "Hot", "Cold", "Humid", "Dry" };
            if (!validWeatherTypes.Contains(settings.WeatherType, StringComparer.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: '{settings.WeatherType}' is not a standard weather type.[/]");
                AnsiConsole.MarkupLine($"[dim]Valid types: {string.Join(", ", validWeatherTypes)}[/]");
            }

            // Show preview
            var table = new Table();
            table.AddColumn("Property");
            table.AddColumn("Value");

            table.AddRow("Name", settings.Name);
            table.AddRow("Description", settings.Description);
            table.AddRow("Difficulty", settings.DifficultyLevel.ToString());
            table.AddRow("Environment", settings.IsIndoor ? "Indoor" : "Outdoor");
            table.AddRow("Weather", settings.WeatherType);
            table.AddRow("Status", settings.IsInactive ? "Inactive" : "Active");
            table.AddRow("Created By", settings.CreatedBy);

            AnsiConsole.Write(new Panel(table).Header("[bold]Zone to Create[/]"));

            // Confirm creation
            if (!settings.Force)
            {
                if (!AnsiConsole.Confirm($"Create zone '{settings.Name}'?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Zone creation cancelled.[/]");
                    return 0;
                }
            }

            // Create the zone
            var zone = new Zone
            {
                Name = settings.Name,
                Description = settings.Description,
                DifficultyLevel = settings.DifficultyLevel,
                IsOutdoor = !settings.IsIndoor,
                WeatherType = settings.WeatherType,
                CreatedBy = settings.CreatedBy,
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = !settings.IsInactive
            };

            _dbContext.Zones.Add(zone);
            await _dbContext.SaveChangesAsync();

            AnsiConsole.MarkupLine($"[green]Successfully created zone '{zone.Name}' with ID {zone.Id}[/]");
            
            // Show next steps
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Next steps:[/]");
            AnsiConsole.MarkupLine($"[dim]- Use the web admin interface at /admin/zones/edit/{zone.Id} to manage this zone[/]");
            AnsiConsole.MarkupLine($"[dim]- Create rooms for this zone using the web admin interface[/]");
            AnsiConsole.MarkupLine($"[dim]- Use 'mordecai-admin list-zones --detailed' to see zone details[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}