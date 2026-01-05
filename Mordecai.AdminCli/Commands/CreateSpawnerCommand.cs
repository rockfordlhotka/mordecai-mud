using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Mordecai.AdminCli.Commands;

public class CreateSpawnerCommand : AsyncCommand<CreateSpawnerCommand.Settings>
{
    private readonly ApplicationDbContext _dbContext;

    public CreateSpawnerCommand(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public class Settings : CommandSettings
    {
        [Description("Name of the spawner template")]
        [CommandOption("--name|-n")]
        public required string Name { get; set; }

        [Description("Description of the spawner")]
        [CommandOption("--description|-d")]
        public string? Description { get; set; }

        [Description("NPC template IDs to spawn (comma-separated)")]
        [CommandOption("--npcs")]
        public required string NpcTemplateIds { get; set; }

        [Description("Spawn behavior: Fixed, Random, Weighted (default: Fixed)")]
        [CommandOption("--behavior|-b")]
        public string Behavior { get; set; } = "Fixed";

        [Description("Min spawn interval in seconds (default: 300)")]
        [CommandOption("--min-interval")]
        public int SpawnIntervalMin { get; set; } = 300;

        [Description("Max spawn interval in seconds (default: 600)")]
        [CommandOption("--max-interval")]
        public int SpawnIntervalMax { get; set; } = 600;

        [Description("Maximum active creatures (default: 1)")]
        [CommandOption("--max-active")]
        public int MaxActiveCreatures { get; set; } = 1;

        [Description("Respawn on death (default: true)")]
        [CommandOption("--no-respawn")]
        public bool NoRespawn { get; set; }

        [Description("Room ID to place spawner in")]
        [CommandOption("--room|-r")]
        public int? RoomId { get; set; }

        [Description("Block spawn if players present")]
        [CommandOption("--block-players")]
        public bool BlockIfPlayersPresent { get; set; }

        [Description("Block spawn if creatures present")]
        [CommandOption("--block-creatures")]
        public bool BlockIfCreaturesPresent { get; set; }

        [Description("Spawn chance (0.0-1.0, default: 1.0)")]
        [CommandOption("--spawn-chance")]
        public float SpawnChance { get; set; } = 1.0f;

        [Description("Creator name (default: CLI)")]
        [CommandOption("--creator|-c")]
        public string CreatedBy { get; set; } = "CLI";
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            await _dbContext.Database.EnsureCreatedAsync();

            // Parse spawn behavior
            if (!Enum.TryParse<SpawnBehavior>(settings.Behavior, true, out var spawnBehavior))
            {
                AnsiConsole.MarkupLine($"[red]Invalid spawn behavior: {settings.Behavior}. Use Fixed, Random, or Weighted.[/]");
                return 1;
            }

            // Parse NPC template IDs
            var npcIds = settings.NpcTemplateIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

            if (npcIds.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No valid NPC template IDs provided.[/]");
                return 1;
            }

            // Verify NPC templates exist
            var npcTemplates = await _dbContext.NpcTemplates
                .Where(nt => npcIds.Contains(nt.Id))
                .ToListAsync();

            if (npcTemplates.Count != npcIds.Count)
            {
                var missing = npcIds.Except(npcTemplates.Select(nt => nt.Id)).ToList();
                AnsiConsole.MarkupLine($"[red]NPC template(s) not found: {string.Join(", ", missing)}[/]");
                return 1;
            }

            // Check if room exists (if specified)
            if (settings.RoomId.HasValue)
            {
                var room = await _dbContext.Rooms.FindAsync(settings.RoomId.Value);
                if (room == null)
                {
                    AnsiConsole.MarkupLine($"[red]Room {settings.RoomId} not found.[/]");
                    return 1;
                }
            }

            // Create spawner template
            var spawnerTemplate = new SpawnerTemplate
            {
                Name = settings.Name,
                Description = settings.Description,
                SpawnBehavior = spawnBehavior,
                SpawnIntervalMin = settings.SpawnIntervalMin,
                SpawnIntervalMax = settings.SpawnIntervalMax,
                MaxActiveCreatures = settings.MaxActiveCreatures,
                RespawnOnDeath = !settings.NoRespawn,
                CreatedBy = settings.CreatedBy,
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            // Create spawn conditions if any specified
            if (settings.BlockIfPlayersPresent || settings.BlockIfCreaturesPresent || settings.SpawnChance < 1.0f)
            {
                var conditions = new SpawnConditions
                {
                    BlockIfPlayersPresent = settings.BlockIfPlayersPresent,
                    BlockIfCreaturesPresent = settings.BlockIfCreaturesPresent,
                    SpawnChance = settings.SpawnChance
                };
                spawnerTemplate.ConditionsJson = JsonSerializer.Serialize(conditions);
            }

            _dbContext.SpawnerTemplates.Add(spawnerTemplate);
            await _dbContext.SaveChangesAsync();

            // Create spawn table entries
            foreach (var npcTemplate in npcTemplates)
            {
                var entry = new SpawnerNpcEntry
                {
                    SpawnerTemplateId = spawnerTemplate.Id,
                    NpcTemplateId = npcTemplate.Id,
                    Weight = 1
                };
                _dbContext.SpawnerNpcEntries.Add(entry);
            }

            await _dbContext.SaveChangesAsync();

            // Create spawner instance if room specified
            if (settings.RoomId.HasValue)
            {
                var instance = new SpawnerInstance
                {
                    SpawnerTemplateId = spawnerTemplate.Id,
                    Type = SpawnerType.RoomBound,
                    RoomId = settings.RoomId.Value,
                    IsEnabled = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.SpawnerInstances.Add(instance);
                await _dbContext.SaveChangesAsync();

                AnsiConsole.MarkupLine($"[green]Created spawner '{spawnerTemplate.Name}' (ID: {spawnerTemplate.Id}) and placed in room {settings.RoomId}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]Created spawner template '{spawnerTemplate.Name}' (ID: {spawnerTemplate.Id})[/]");
                AnsiConsole.MarkupLine("[yellow]Note: Use place-spawner command to place this spawner in rooms[/]");
            }

            AnsiConsole.MarkupLine($"  NPCs: {string.Join(", ", npcTemplates.Select(nt => $"{nt.Name} ({nt.Id})"))}");
            AnsiConsole.MarkupLine($"  Behavior: {spawnBehavior}, Interval: {settings.SpawnIntervalMin}-{settings.SpawnIntervalMax}s, Max Active: {settings.MaxActiveCreatures}");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error creating spawner: {ex.Message}[/]");
            return 1;
        }
    }
}
