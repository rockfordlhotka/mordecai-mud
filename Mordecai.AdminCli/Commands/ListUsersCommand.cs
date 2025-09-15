using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Mordecai.AdminCli.Commands;

public class ListUsersCommand : AsyncCommand<ListUsersCommand.Settings>
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public ListUsersCommand(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public class Settings : CommandSettings
    {
        [Description("Show only admin users")]
        [CommandOption("--admins")]
        public bool AdminsOnly { get; set; }

        [Description("Show detailed information")]
        [CommandOption("--detailed")]
        public bool Detailed { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Ensure database exists
            await _dbContext.Database.EnsureCreatedAsync();

            // Create Admin role if it doesn't exist
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
                AnsiConsole.MarkupLine("[yellow]Created Admin role[/]");
            }

            var users = await _userManager.Users.ToListAsync();
            
            if (!users.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No users found in the system.[/]");
                return 0;
            }

            var table = new Table();
            table.AddColumn("Email/Username");
            table.AddColumn("Email Confirmed");
            table.AddColumn("Is Admin");
            
            if (settings.Detailed)
            {
                table.AddColumn("User ID");
                table.AddColumn("Created");
                table.AddColumn("Last Login");
            }

            foreach (var user in users)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                
                // Skip non-admin users if filtering for admins only
                if (settings.AdminsOnly && !isAdmin)
                    continue;

                var email = user.Email ?? user.UserName ?? "Unknown";
                var emailConfirmed = user.EmailConfirmed ? "[green]Yes[/]" : "[red]No[/]";
                var adminStatus = isAdmin ? "[green]Yes[/]" : "[dim]No[/]";

                if (settings.Detailed)
                {
                    var created = user.LockoutEnd?.ToString("yyyy-MM-dd HH:mm") ?? "Unknown";
                    var lastLogin = "Unknown"; // IdentityUser doesn't track last login by default
                    
                    table.AddRow(email, emailConfirmed, adminStatus, user.Id, created, lastLogin);
                }
                else
                {
                    table.AddRow(email, emailConfirmed, adminStatus);
                }
            }

            var title = settings.AdminsOnly ? "Admin Users" : "All Users";
            AnsiConsole.Write(new Panel(table).Header($"[bold]{title}[/]"));

            var totalCount = users.Count;
            var adminCount = 0;
            
            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    adminCount++;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[dim]Total users: {totalCount} | Admins: {adminCount}[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}