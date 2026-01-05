using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Mordecai.AdminCli.Commands;

public class ListNpcTemplatesCommand : AsyncCommand
{
    private readonly ApplicationDbContext _dbContext;

    public ListNpcTemplatesCommand(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {
            var npcTemplates = await _dbContext.NpcTemplates
                .OrderBy(nt => nt.Level)
                .ThenBy(nt => nt.Name)
                .ToListAsync();

            if (npcTemplates.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No NPC templates found.[/]");
                return 0;
            }

            var table = new Table();
            table.AddColumn("ID");
            table.AddColumn("Name");
            table.AddColumn("Level");
            table.AddColumn("Hostile");
            table.AddColumn("Attributes");
            table.AddColumn("Active");

            foreach (var npc in npcTemplates)
            {
                var attributes = $"S:{npc.Strength} E:{npc.Endurance} C:{npc.Coordination} Q:{npc.Quickness} I:{npc.Intelligence} W:{npc.Willpower} Ch:{npc.Charisma}";

                table.AddRow(
                    npc.Id.ToString(),
                    npc.Name,
                    npc.Level.ToString(),
                    npc.IsHostile ? "[red]Yes[/]" : "[green]No[/]",
                    attributes,
                    npc.IsActive ? "[green]Yes[/]" : "[red]No[/]"
                );
            }

            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error listing NPC templates: {ex.Message}[/]");
            return 1;
        }
    }
}
