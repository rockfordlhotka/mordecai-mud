using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mordecai.AdminCli.Commands;
using Mordecai.Web.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Mordecai.AdminCli;

// Custom type registrar for Spectre.Console.Cli
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _builder;

    public TypeRegistrar(IServiceCollection builder)
    {
        _builder = builder;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_builder.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        _builder.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _builder.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _builder.AddSingleton(service, _ => factory());
    }
}

// Custom type resolver for Spectre.Console.Cli
public sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return _provider.GetService(type);
    }

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Get the directory where the executable is located
        var baseDirectory = AppContext.BaseDirectory;
        
        // Build configuration (includes environment variables and user secrets)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(baseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>(optional: true)
            .Build();

        // Build PostgreSQL connection string from environment variables (cloud-native)
        // Priority: Environment variables > User Secrets > appsettings.json
        var dbHost = configuration["Database:Host"] ?? "localhost";
        var dbPort = configuration["Database:Port"] ?? "5432";
        var dbName = configuration["Database:Name"] ?? "mordecai";
        var dbUser = configuration["Database:User"] ?? "mordecaimud";
        var dbPassword = configuration["Database:Password"] 
            ?? throw new InvalidOperationException("Database password not configured. Set Database:Password in User Secrets or environment variable.");

        var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

        // Build service collection
        var services = new ServiceCollection();
        
        // Add configuration
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Add Entity Framework with PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Add Identity services
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            // Configure identity options if needed
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Create type registrar with our services
        var registrar = new TypeRegistrar(services);

        // Create command app with proper DI
        var app = new CommandApp(registrar);
        app.Configure(config =>
        {
            config.SetApplicationName("mordecai-admin");
            config.SetApplicationVersion("1.0.0");
            
            // User Management Commands
            config.AddCommand<ListUsersCommand>("list-users")
                .WithDescription("List all users in the system")
                .WithExample(["list-users"])
                .WithExample(["list-users", "--admins"])
                .WithExample(["list-users", "--detailed"]);
                
            config.AddCommand<MakeAdminCommand>("make-admin")
                .WithDescription("Grant or revoke admin role for a user")
                .WithExample(["make-admin", "--user", "john@example.com"])
                .WithExample(["make-admin", "--user", "john@example.com", "--revoke"])
                .WithExample(["make-admin", "-u", "admin@example.com", "--force"]);

            config.AddCommand<SetPasswordCommand>("set-password")
                .WithDescription("Set or reset a user's password")
                .WithExample(["set-password", "--user", "john@example.com"])
                .WithExample(["set-password", "--user", "john@example.com", "--generate"])
                .WithExample(["set-password", "-u", "admin@example.com", "--password", "NewPassword123!", "--force"])
                .WithExample(["set-password", "-u", "user@example.com", "--generate", "--show"]);

            // Zone Management Commands
            config.AddCommand<ListZonesCommand>("list-zones")
                .WithDescription("List all zones in the game world")
                .WithExample(["list-zones"])
                .WithExample(["list-zones", "--active"])
                .WithExample(["list-zones", "--detailed"])
                .WithExample(["list-zones", "--difficulty", "2"]);

            config.AddCommand<CreateZoneCommand>("create-zone")
                .WithDescription("Create a new zone in the game world")
                .WithExample(["create-zone", "--name", "Whispering Woods", "--description", "A mysterious forest full of ancient secrets"])
                .WithExample(["create-zone", "-n", "Dragon's Lair", "-d", "The fearsome lair of an ancient dragon", "--difficulty", "5", "--indoor"])
                .WithExample(["create-zone", "-n", "Peaceful Meadow", "-d", "A serene grassland", "--weather", "Clear", "--creator", "WorldBuilder"]);

            config.AddCommand<ShowZoneCommand>("show-zone")
                .WithDescription("Show detailed information about a specific zone")
                .WithExample(["show-zone", "1"])
                .WithExample(["show-zone", "Whispering Woods"])
                .WithExample(["show-zone", "Dragon's Lair", "--rooms"]);

            config.AddCommand<SeedWorldCommand>("seed-world")
                .WithDescription("Create basic world structure with starting zone and rooms")
                .WithExample(["seed-world"])
                .WithExample(["seed-world", "--force"]);

            // Legacy aliases for backwards compatibility
            config.AddCommand<ListUsersCommand>("list")
                .WithDescription("List all users in the system (alias for list-users)")
                .IsHidden();
                
            config.AddCommand<MakeAdminCommand>("admin")
                .WithDescription("Grant or revoke admin role for a user (alias for make-admin)")
                .IsHidden();
        });

        try
        {
            return await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}