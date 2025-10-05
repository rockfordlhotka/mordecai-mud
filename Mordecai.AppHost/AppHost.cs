using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Check if we're using cloud resources
var useCloudPostgres = !string.IsNullOrEmpty(builder.Configuration["Database:Host"]);
var useCloudRabbitMq = !string.IsNullOrEmpty(builder.Configuration["RabbitMQ:Host"] ?? builder.Configuration["RABBITMQ_HOST"]);

// PostgreSQL configuration
IResourceBuilder<IResourceWithConnectionString> postgres;
if (useCloudPostgres)
{
    // Cloud PostgreSQL - inject connection string into configuration
    var dbHost = builder.Configuration["Database:Host"]!;
    var dbPort = builder.Configuration["Database:Port"] ?? "5432";
    var dbName = builder.Configuration["Database:Name"] ?? "mordecai";
    var dbUser = builder.Configuration["Database:User"] ?? "postgres";
    var dbPassword = builder.Configuration["Database:Password"] ?? "";
    
    var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
    
    // Inject into configuration so AddConnectionString can find it
    builder.Configuration["ConnectionStrings:postgresql"] = connectionString;
    
    // Add connection string resource (will read from configuration)
    postgres = builder.AddConnectionString("postgresql");
}
else
{
    // Local PostgreSQL container for development
    postgres = builder.AddPostgres("postgres")
        .WithPgAdmin()
        .AddDatabase("mordecai");
}

// RabbitMQ configuration
IResourceBuilder<IResourceWithConnectionString> rabbitmq;
if (useCloudRabbitMq)
{
    // Cloud RabbitMQ - inject connection string into configuration
    var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? builder.Configuration["RABBITMQ_HOST"]!;
    var rabbitPort = builder.Configuration["RabbitMQ:Port"] ?? builder.Configuration["RABBITMQ_PORT"] ?? "5672";
    var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? builder.Configuration["RABBITMQ_USERNAME"] ?? "guest";
    var rabbitPassword = builder.Configuration["RabbitMQ:Password"] ?? builder.Configuration["RABBITMQ_PASSWORD"] ?? "guest";
    var rabbitVHost = builder.Configuration["RabbitMQ:VirtualHost"] ?? builder.Configuration["RABBITMQ_VHOST"] ?? "/";
    
    var connectionString = $"amqp://{rabbitUser}:{rabbitPassword}@{rabbitHost}:{rabbitPort}{rabbitVHost}";
    
    // Inject into configuration so AddConnectionString can find it
    builder.Configuration["ConnectionStrings:messaging"] = connectionString;
    
    // Add connection string resource (will read from configuration)
    rabbitmq = builder.AddConnectionString("messaging");
}
else
{
    // Local RabbitMQ container for development
    rabbitmq = builder.AddRabbitMQ("messaging");
}

// Add the web application with references
builder.AddProject<Projects.Mordecai_Web>("webfrontend")
    .WithReference(postgres)
    .WithReference(rabbitmq);

builder.Build().Run();
