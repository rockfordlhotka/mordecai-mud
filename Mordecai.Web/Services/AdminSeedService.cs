using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for seeding initial admin users and roles
/// </summary>
public class AdminSeedService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AdminSeedService> _logger;

    public AdminSeedService(
        UserManager<IdentityUser> userManager, 
        RoleManager<IdentityRole> roleManager,
        ILogger<AdminSeedService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with initial admin roles and default admin user
    /// </summary>
    public async Task SeedAdminDataAsync()
    {
        try
        {
            // Create Admin role if it doesn't exist
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                var adminRole = new IdentityRole("Admin");
                await _roleManager.CreateAsync(adminRole);
                _logger.LogInformation("Created Admin role");
            }

            // Create Player role if it doesn't exist
            if (!await _roleManager.RoleExistsAsync("Player"))
            {
                var playerRole = new IdentityRole("Player");
                await _roleManager.CreateAsync(playerRole);
                _logger.LogInformation("Created Player role");
            }

            // Create default admin user if no admin users exist
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            if (!adminUsers.Any())
            {
                const string defaultAdminEmail = "admin@mordecai.mud";
                const string defaultAdminPassword = "AdminPass123!";

                var existingUser = await _userManager.FindByEmailAsync(defaultAdminEmail);
                if (existingUser == null)
                {
                    var adminUser = new IdentityUser
                    {
                        UserName = defaultAdminEmail,
                        Email = defaultAdminEmail,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(adminUser, defaultAdminPassword);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(adminUser, "Admin");
                        _logger.LogWarning("Created default admin user: {Email} with password: {Password}", 
                            defaultAdminEmail, defaultAdminPassword);
                        _logger.LogWarning("SECURITY: Please change the default admin password after first login!");
                    }
                    else
                    {
                        _logger.LogError("Failed to create default admin user: {Errors}", 
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    // User exists but may not be admin
                    var isAdmin = await _userManager.IsInRoleAsync(existingUser, "Admin");
                    if (!isAdmin)
                    {
                        await _userManager.AddToRoleAsync(existingUser, "Admin");
                        _logger.LogInformation("Added existing user {Email} to Admin role", defaultAdminEmail);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding admin data");
        }
    }
}