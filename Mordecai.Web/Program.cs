using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;
using Mordecai.Messaging.Extensions;
using Mordecai.Web.Services;
using Mordecai.Game.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=mordecai.db"));

builder.Services.AddDefaultIdentity<IdentityUser>(options => 
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add RabbitMQ client (Aspire integration)
builder.AddRabbitMQClient("messaging");

// Add game messaging services
builder.Services.AddGameMessaging();

// Add character creation services
builder.Services.AddScoped<IDiceService, DiceService>();
builder.Services.AddScoped<ICharacterCreationService, CharacterCreationService>();

// Add skill services
builder.Services.AddScoped<ISkillService, SkillService>();

// Add game services
builder.Services.AddSingleton<IGameTimeService, GameTimeService>();
builder.Services.AddScoped<Mordecai.Game.Services.IRoomService, Mordecai.Game.Services.RoomService>();
builder.Services.AddScoped<Mordecai.Web.Services.IZoneService, Mordecai.Web.Services.ZoneService>();
builder.Services.AddScoped<Mordecai.Web.Services.IRoomService, Mordecai.Web.Services.RoomService>();
builder.Services.AddScoped<IWorldService, WorldService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();

// Add character message broadcast service as singleton
builder.Services.AddSingleton<CharacterMessageBroadcastService>();

// Add target resolution service as scoped
builder.Services.AddScoped<TargetResolutionService>();

// Add game action service as scoped
builder.Services.AddScoped<GameActionService>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Try to migrate first
        logger.LogInformation("Applying database migrations...");
        context.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning"))
    {
        // If there are pending model changes, recreate the database
        logger.LogWarning("Pending model changes detected. Recreating database...");
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        logger.LogInformation("Database recreated successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database migration. Attempting to recreate database...");
        try
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            logger.LogInformation("Database recreated successfully after migration error.");
        }
        catch (Exception recreateEx)
        {
            logger.LogError(recreateEx, "Failed to recreate database. Application may not function correctly.");
            throw;
        }
    }
    
    // Initialize base attribute skills
    try
    {
        logger.LogInformation("Initializing base attribute skills...");
        var skillService = scope.ServiceProvider.GetRequiredService<ISkillService>();
        await skillService.InitializeBaseAttributeSkillsAsync();
        logger.LogInformation("Base attribute skills initialized successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing base attribute skills.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
