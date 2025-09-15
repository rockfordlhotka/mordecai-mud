using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Mordecai.AdminCli.Commands;

public class MakeAdminCommand : AsyncCommand<MakeAdminCommand.Settings>
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public MakeAdminCommand(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public class Settings : CommandSettings
    {
        [Description("Email or username of the user")]
        [CommandOption("--user|-u")]
        public required string UserEmail { get; set; }

        [Description("Revoke admin role instead of granting it")]
        [CommandOption("--revoke|-r")]
        public bool Revoke { get; set; }

        [Description("Force operation without confirmation")]
        [CommandOption("--force|-f")]
        public bool Force { get; set; }
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

            // Find user by email or username
            var user = await _userManager.FindByEmailAsync(settings.UserEmail) 
                       ?? await _userManager.FindByNameAsync(settings.UserEmail);

            if (user == null)
            {
                AnsiConsole.MarkupLine($"[red]User '{settings.UserEmail}' not found.[/]");
                AnsiConsole.MarkupLine("[dim]Use 'mordecai-admin list-users' to see available users.[/]");
                return 1;
            }

            var isCurrentlyAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var userDisplay = user.Email ?? user.UserName ?? user.Id;

            if (settings.Revoke)
            {
                if (!isCurrentlyAdmin)
                {
                    AnsiConsole.MarkupLine($"[yellow]User '{userDisplay}' is not currently an admin.[/]");
                    return 0;
                }

                if (!settings.Force)
                {
                    var confirm = AnsiConsole.Confirm($"Remove admin role from user '{userDisplay}'?");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                        return 0;
                    }
                }

                var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
                if (result.Succeeded)
                {
                    AnsiConsole.MarkupLine($"[green]Successfully removed admin role from '{userDisplay}'[/]");
                    return 0;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Failed to remove admin role from '{userDisplay}':[/]");
                    foreach (var error in result.Errors)
                    {
                        AnsiConsole.MarkupLine($"[red]  - {error.Description}[/]");
                    }
                    return 1;
                }
            }
            else
            {
                if (isCurrentlyAdmin)
                {
                    AnsiConsole.MarkupLine($"[yellow]User '{userDisplay}' is already an admin.[/]");
                    return 0;
                }

                if (!settings.Force)
                {
                    var confirm = AnsiConsole.Confirm($"Grant admin role to user '{userDisplay}'?");
                    if (!confirm)
                    {
                        AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                        return 0;
                    }
                }

                var result = await _userManager.AddToRoleAsync(user, "Admin");
                if (result.Succeeded)
                {
                    AnsiConsole.MarkupLine($"[green]Successfully granted admin role to '{userDisplay}'[/]");
                    
                    // Show helpful information
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[dim]The user can now access admin features at /admin[/]");
                    return 0;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Failed to grant admin role to '{userDisplay}':[/]");
                    foreach (var error in result.Errors)
                    {
                        AnsiConsole.MarkupLine($"[red]  - {error.Description}[/]");
                    }
                    return 1;
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}