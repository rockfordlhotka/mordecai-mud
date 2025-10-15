using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mordecai.Web.Data;
using Mordecai.Messaging.Extensions;
using Mordecai.Messaging.Services;
using Mordecai.Web.Services;
using Mordecai.Game.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Build PostgreSQL connection string from environment variables (cloud-native)
// Priority: Environment variables > User Secrets > appsettings.json
var dbHost = builder.Configuration["Database:Host"] ?? "localhost";
var dbPort = builder.Configuration["Database:Port"] ?? "5432";
var dbName = builder.Configuration["Database:Name"] ?? "mordecai";
var dbUser = builder.Configuration["Database:User"] ?? "mordecaimud";
var dbPassword = builder.Configuration["Database:Password"] 
    ?? throw new InvalidOperationException("Database password not configured. Set Database:Password in User Secrets or environment variable.");

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

// Register ONLY the pooled DbContext factory (not AddDbContext separately)
// This provides both IDbContextFactory and direct DbContext injection
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Identity needs DbContext, so we add a scoped resolver that uses the factory
builder.Services.AddScoped<ApplicationDbContext>(sp => 
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

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

// Add game messaging services (connects to RabbitMQ using configuration)
builder.Services.AddGameMessaging();

// Add character creation services
builder.Services.AddScoped<IDiceService, DiceService>();
builder.Services.AddScoped<IDoorInteractionService, DoorInteractionService>();
builder.Services.AddScoped<ICharacterCreationService, CharacterCreationService>();

// Add skill services
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<SkillService>();

// Add admin services
builder.Services.AddScoped<AdminSeedService>();

// Add data migration services
builder.Services.AddScoped<DataMigrationService>();

// Add seed services
builder.Services.AddScoped<SkillSeedService>();
builder.Services.AddScoped<RoomTypeSeedService>();

// Add game services
builder.Services.AddSingleton<IGameTimeService, GameTimeService>();
builder.Services.AddScoped<IGameConfigurationService, GameConfigurationService>();
builder.Services.AddScoped<Mordecai.Game.Services.IRoomService, Mordecai.Game.Services.RoomService>();
builder.Services.AddScoped<Mordecai.Web.Services.IZoneService, Mordecai.Web.Services.ZoneService>();
builder.Services.AddScoped<Mordecai.Web.Services.IRoomService, Mordecai.Web.Services.RoomService>();
builder.Services.AddScoped<IRoomAdjacencyService, RoomAdjacencyService>();
builder.Services.AddScoped<IWorldService, WorldService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<IItemTemplateService, ItemTemplateService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();

// Add room effects service
builder.Services.AddScoped<IRoomEffectService, RoomEffectService>();

// Add background services
builder.Services.AddHostedService<RoomEffectBackgroundService>();
builder.Services.AddHostedService<HealthTickBackgroundService>();

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
    
    logger.LogInformation("Using PostgreSQL database: Host={DbHost}, Database={DbName}, User={DbUser}", dbHost, dbName, dbUser);
    
    // Log RabbitMQ configuration
    var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? builder.Configuration["RABBITMQ_HOST"] ?? "localhost";
    var rabbitMqUser = builder.Configuration["RabbitMQ:Username"] ?? builder.Configuration["RABBITMQ_USERNAME"] ?? "guest";
    logger.LogInformation("Using RabbitMQ: Host={RabbitMqHost}, User={RabbitMqUser}", rabbitMqHost, rabbitMqUser);
    
    context.Database.Migrate();
    
    // Seed admin data first (roles and users)
    var adminSeedService = scope.ServiceProvider.GetRequiredService<AdminSeedService>();
    await adminSeedService.SeedAdminDataAsync();
    
    // Seed room types
    var roomTypeSeedService = scope.ServiceProvider.GetRequiredService<RoomTypeSeedService>();
    await roomTypeSeedService.SeedRoomTypesAsync();
    
    // Seed skill data if needed
    var skillSeedService = scope.ServiceProvider.GetRequiredService<SkillSeedService>();
    await skillSeedService.SeedSkillDataAsync();
    
    // Run any necessary data migrations
    var dataMigrationService = scope.ServiceProvider.GetRequiredService<DataMigrationService>();
    await dataMigrationService.RunAllDataMigrationsAsync();
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
