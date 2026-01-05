using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Mordecai.AdminCli.Commands;

public class ListSpawnersCommand : AsyncCommand
{
    private readonly ApplicationDbContext _dbContext;

    public ListSpawnersCommand(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {
            var spawnerTemplates = await _dbContext.SpawnerTemplates
                .Include(st => st.SpawnTable)
                    .ThenInclude(sne => sne.NpcTemplate)
                .Include(st => st.Instances)
                    .ThenInclude(si => si.Room)
                .OrderBy(st => st.Name)
                .ToListAsync();

            if (spawnerTemplates.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No spawner templates found.[/]");
                return 0;
            }

            var table = new Table();
            table.AddColumn("ID");
            table.AddColumn("Name");
            table.AddColumn("NPCs");
            table.AddColumn("Behavior");
            table.AddColumn("Instances");
            table.AddColumn("Active");

            foreach (var spawner in spawnerTemplates)
            {
                var npcNames = string.Join(", ", spawner.SpawnTable.Select(sne => sne.NpcTemplate.Name));
                var instanceCount = spawner.Instances.Count;
                var activeInstances = spawner.Instances.Count(i => i.IsEnabled);

                table.AddRow(
                    spawner.Id.ToString(),
                    spawner.Name,
                    npcNames.Length > 40 ? npcNames[..37] + "..." : npcNames,
                    spawner.SpawnBehavior.ToString(),
                    instanceCount.ToString(),
                    spawner.IsActive ? "[green]Yes[/]" : "[red]No[/]"
                );
            }

            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error listing spawners: {ex.Message}[/]");
            return 1;
        }
    }
}
