using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Mordecai.AdminCli.Commands;

public class CreateNpcTemplateCommand : AsyncCommand<CreateNpcTemplateCommand.Settings>
{
    private readonly ApplicationDbContext _dbContext;

    public CreateNpcTemplateCommand(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public class Settings : CommandSettings
    {
        [Description("Name of the NPC")]
        [CommandOption("--name|-n")]
        public required string Name { get; set; }

        [Description("Description of the NPC")]
        [CommandOption("--description|-d")]
        public required string Description { get; set; }

        [Description("Short description for room listings")]
        [CommandOption("--short")]
        public string? ShortDescription { get; set; }

        [Description("Level of the NPC (default: 1)")]
        [CommandOption("--level|-l")]
        public int Level { get; set; } = 1;

        [Description("Strength attribute (default: 0)")]
        [CommandOption("--str")]
        public int Strength { get; set; } = 0;

        [Description("Endurance attribute (default: 0)")]
        [CommandOption("--end")]
        public int Endurance { get; set; } = 0;

        [Description("Coordination attribute (default: 0)")]
        [CommandOption("--coo")]
        public int Coordination { get; set; } = 0;

        [Description("Quickness attribute (default: 0)")]
        [CommandOption("--qui")]
        public int Quickness { get; set; } = 0;

        [Description("Intelligence attribute (default: 0)")]
        [CommandOption("--int")]
        public int Intelligence { get; set; } = 0;

        [Description("Willpower attribute (default: 0)")]
        [CommandOption("--wil")]
        public int Willpower { get; set; } = 0;

        [Description("Charisma attribute (default: 0)")]
        [CommandOption("--cha")]
        public int Charisma { get; set; } = 0;

        [Description("Make NPC hostile (default: false)")]
        [CommandOption("--hostile")]
        public bool IsHostile { get; set; }

        [Description("Enable group assist behavior (default: false)")]
        [CommandOption("--group-assist")]
        public bool IsGroupAssist { get; set; }

        [Description("Allow NPC to wander (default: false)")]
        [CommandOption("--wander")]
        public bool CanWander { get; set; }

        [Description("Creator name (default: CLI)")]
        [CommandOption("--creator|-c")]
        public string CreatedBy { get; set; } = "CLI";
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            await _dbContext.Database.EnsureCreatedAsync();

            // Check if NPC template name already exists
            var existing = await _dbContext.NpcTemplates
                .FirstOrDefaultAsync(nt => nt.Name.ToLower() == settings.Name.ToLower());

            if (existing != null)
            {
                AnsiConsole.MarkupLine($"[red]An NPC template with the name '{settings.Name}' already exists (ID: {existing.Id}).[/]");
                return 1;
            }

            var npcTemplate = new NpcTemplate
            {
                Name = settings.Name,
                Description = settings.Description,
                ShortDescription = settings.ShortDescription ?? settings.Name,
                Level = settings.Level,
                Strength = settings.Strength,
                Endurance = settings.Endurance,
                Coordination = settings.Coordination,
                Quickness = settings.Quickness,
                Intelligence = settings.Intelligence,
                Willpower = settings.Willpower,
                Charisma = settings.Charisma,
                IsHostile = settings.IsHostile,
                IsGroupAssist = settings.IsGroupAssist,
                CanWander = settings.CanWander,
                CreatedBy = settings.CreatedBy,
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            _dbContext.NpcTemplates.Add(npcTemplate);
            await _dbContext.SaveChangesAsync();

            AnsiConsole.MarkupLine($"[green]Successfully created NPC template '{npcTemplate.Name}' (ID: {npcTemplate.Id})[/]");
            AnsiConsole.MarkupLine($"  Level: {npcTemplate.Level}, Hostile: {npcTemplate.IsHostile}");
            AnsiConsole.MarkupLine($"  STR:{npcTemplate.Strength} END:{npcTemplate.Endurance} COO:{npcTemplate.Coordination} QUI:{npcTemplate.Quickness}");
            AnsiConsole.MarkupLine($"  INT:{npcTemplate.Intelligence} WIL:{npcTemplate.Willpower} CHA:{npcTemplate.Charisma}");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error creating NPC template: {ex.Message}[/]");
            return 1;
        }
    }
}
