using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Mordecai.AdminCli.Commands;

public class SetPasswordCommand : AsyncCommand<SetPasswordCommand.Settings>
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public SetPasswordCommand(UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public class Settings : CommandSettings
    {
        [Description("Email or username of the user")]
        [CommandOption("--user|-u")]
        public required string UserEmail { get; set; }

        [Description("New password for the user")]
        [CommandOption("--password|-p")]
        public string? Password { get; set; }

        [Description("Generate a random secure password")]
        [CommandOption("--generate|-g")]
        public bool GeneratePassword { get; set; }

        [Description("Force operation without confirmation")]
        [CommandOption("--force|-f")]
        public bool Force { get; set; }

        [Description("Show the password in output (use with caution)")]
        [CommandOption("--show")]
        public bool ShowPassword { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Ensure database exists
            await _dbContext.Database.EnsureCreatedAsync();

            // Find user by email or username
            var user = await _userManager.FindByEmailAsync(settings.UserEmail) 
                       ?? await _userManager.FindByNameAsync(settings.UserEmail);

            if (user == null)
            {
                AnsiConsole.MarkupLine($"[red]User '{settings.UserEmail}' not found.[/]");
                AnsiConsole.MarkupLine("[dim]Use 'list-users' to see available users.[/]");
                return 1;
            }

            var userDisplay = user.Email ?? user.UserName ?? user.Id;

            // Determine password to set
            string passwordToSet;
            bool isGenerated = false;

            if (settings.GeneratePassword)
            {
                passwordToSet = GenerateSecurePassword();
                isGenerated = true;
            }
            else if (!string.IsNullOrEmpty(settings.Password))
            {
                passwordToSet = settings.Password;
            }
            else
            {
                // Prompt for password interactively
                passwordToSet = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter new password:")
                        .PromptStyle("green")
                        .Secret());

                if (string.IsNullOrWhiteSpace(passwordToSet))
                {
                    AnsiConsole.MarkupLine("[red]Password cannot be empty.[/]");
                    return 1;
                }

                var confirmPassword = AnsiConsole.Prompt(
                    new TextPrompt<string>("Confirm password:")
                        .PromptStyle("green")
                        .Secret());

                if (passwordToSet != confirmPassword)
                {
                    AnsiConsole.MarkupLine("[red]Passwords do not match.[/]");
                    return 1;
                }
            }

            // Validate password against policy
            var passwordValidationResult = await ValidatePasswordAsync(passwordToSet, user);
            if (!passwordValidationResult.Succeeded)
            {
                AnsiConsole.MarkupLine("[red]Password does not meet requirements:[/]");
                foreach (var error in passwordValidationResult.Errors)
                {
                    AnsiConsole.MarkupLine($"[red]  - {error.Description}[/]");
                }
                return 1;
            }

            // Show confirmation unless forced
            if (!settings.Force)
            {
                var actionText = isGenerated ? "set a generated password for" : "change the password for";
                var confirm = AnsiConsole.Confirm($"Are you sure you want to {actionText} user '{userDisplay}'?");
                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                    return 0;
                }
            }

            // Set the password
            IdentityResult result;
            var hasPassword = await _userManager.HasPasswordAsync(user);
            
            if (hasPassword)
            {
                // Remove existing password and add new one
                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded)
                {
                    AnsiConsole.MarkupLine("[red]Failed to remove existing password:[/]");
                    foreach (var error in removeResult.Errors)
                    {
                        AnsiConsole.MarkupLine($"[red]  - {error.Description}[/]");
                    }
                    return 1;
                }
            }

            result = await _userManager.AddPasswordAsync(user, passwordToSet);

            if (result.Succeeded)
            {
                AnsiConsole.MarkupLine($"[green]Successfully set password for '{userDisplay}'[/]");
                
                if (isGenerated)
                {
                    AnsiConsole.WriteLine();
                    if (settings.ShowPassword)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Generated password: {passwordToSet}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]Generated password has been set.[/]");
                        AnsiConsole.MarkupLine("[dim]Use --show option to display the password in output.[/]");
                    }
                    AnsiConsole.MarkupLine("[dim]Make sure to securely communicate this password to the user.[/]");
                }

                // Show helpful information
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]The user can now log in with their new password.[/]");
                
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to set password for '{userDisplay}':[/]");
                foreach (var error in result.Errors)
                {
                    AnsiConsole.MarkupLine($"[red]  - {error.Description}[/]");
                }
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private async Task<IdentityResult> ValidatePasswordAsync(string password, IdentityUser user)
    {
        var validators = _userManager.PasswordValidators;
        var errors = new List<IdentityError>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(_userManager, user, password);
            if (!result.Succeeded)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
    }

    private static string GenerateSecurePassword()
    {
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string numberChars = "0123456789";
        const string symbolChars = "!@#$%^&*";
        
        var random = new Random();
        var password = new List<char>();

        // Ensure at least one character from each category
        password.Add(upperChars[random.Next(upperChars.Length)]);
        password.Add(lowerChars[random.Next(lowerChars.Length)]);
        password.Add(numberChars[random.Next(numberChars.Length)]);
        password.Add(symbolChars[random.Next(symbolChars.Length)]);

        // Fill the rest randomly
        const string allChars = upperChars + lowerChars + numberChars + symbolChars;
        for (int i = 4; i < 12; i++) // Total length of 12 characters
        {
            password.Add(allChars[random.Next(allChars.Length)]);
        }

        // Shuffle the password
        for (int i = password.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password.ToArray());
    }
}