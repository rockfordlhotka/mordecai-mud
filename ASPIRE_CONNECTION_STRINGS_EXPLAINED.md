# External Resources in Aspire Dashboard - Using Connection Strings

## What Changed

Switched from custom external resource types to **Aspire's built-in `AddConnectionString()` method**. This is the recommended approach for external resources.

## The Correct Pattern

For external cloud resources, use `AddConnectionString()`:

```csharp
// Build the connection string from configuration
var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";

// Add as connection string resource - Aspire recognizes this as external
var postgres = builder.AddConnectionString("postgresql", connectionString);

// Reference it in your projects
builder.AddProject<Projects.Mordecai_Web>("webfrontend")
    .WithReference(postgres);
```

## What You'll See in the Dashboard

### Connection String Resources (External)
- ✅ **Name**: "postgresql" or "messaging"
- ✅ **Type**: Parameter or ConnectionString resource
- ✅ **State**: Shown as available (not "Running" like containers)
- ✅ **Connection Info**: The connection string is available to referenced projects
- ⚠️ **URLs**: Not shown (it's just a connection string, not an endpoint)

### Health Status
Health checks run in the **webfrontend** resource and monitor:
- PostgreSQL connectivity
- RabbitMQ connectivity
- Overall application health

Click on `webfrontend` → Health checks to see the status of your cloud resources.

## Why This Approach?

1. **Aspire's Standard Pattern**: `AddConnectionString()` is how Aspire handles external resources
2. **No Custom Code**: Uses built-in Aspire features
3. **Proper Semantics**: Connection strings represent external resources you don't manage
4. **Health Checks Work**: Your ServiceDefaults health checks monitor the actual connectivity

## Current Configuration

Your AppHost now:
1. Checks if `Database:Host` or `RabbitMQ:Host` are configured
2. If yes → Creates connection string resources (external)
3. If no → Starts local containers (managed)
4. References them in the webfrontend project

## Viewing Resource Details

### In the Dashboard

**Connection String Resources** (postgresql, messaging):
- Listed in the Resources table
- Show name and type
- No "State" (they're not running processes)
- No URLs (they're connection strings)
- Connection string is available to projects that reference them

**Web Frontend** (webfrontend):
- Shows "Running" state
- Has URLs (http://localhost:xxx)
- Has health checks showing cloud resource connectivity
- Click "View Details" → "Health" to see PostgreSQL and RabbitMQ status

### Checking Health

Go to webfrontend resource → Health checks:
- `self` - Application is running
- `postgresql` - Cloud database connectivity
- `rabbitmq` - Cloud message broker connectivity

All should show ✅ Healthy if your cloud resources are accessible.

## This is Normal!

Connection string resources are **supposed to** show minimal information in the Resources table. They're configuration, not running processes.

The actual health and connectivity status is shown in the **webfrontend health checks**, which is where you want to look for cloud resource status.

## Alternative: If You Want URLs/State

If you want to see more details like URLs and state, you would need to:

1. Run your PostgreSQL/RabbitMQ as Docker containers Aspire manages
2. Or use health check monitoring services

But for **truly external** cloud resources (Azure Database, CloudAMQP, etc.), connection strings are the correct approach.

## Summary

✅ **Resources show correctly** - postgresql and messaging appear
✅ **Configuration works** - Web app can connect using the connection strings
✅ **Health checks work** - Monitored in webfrontend health checks
⚠️ **Limited dashboard info** - By design for external resources

This is the **correct and recommended** way to handle external cloud resources in Aspire!
