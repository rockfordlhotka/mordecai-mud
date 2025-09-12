using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add RabbitMQ service
var rabbitmq = builder.AddRabbitMQ("messaging");

// Add the web application with reference to RabbitMQ
builder.AddProject<Projects.Mordecai_Web>("webfrontend")
    .WithReference(rabbitmq);

builder.Build().Run();
