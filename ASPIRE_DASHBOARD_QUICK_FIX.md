# Quick Reference: Show Cloud Resources in Aspire Dashboard

## The Problem
Aspire dashboard shows local RabbitMQ container, but not your cloud PostgreSQL or cloud RabbitMQ instances.

## The Solution
Configure cloud resource connection details in **BOTH** the AppHost project (for dashboard visibility) and the Web project (for actual connections).

## Quick Setup

### Step 1: Set Cloud Configuration in AppHost (Dashboard Visibility)

```bash
cd Mordecai.AppHost

# Cloud PostgreSQL
dotnet user-secrets set "Database:Host" "your-postgres-host.com"
dotnet user-secrets set "Database:Port" "5432"
dotnet user-secrets set "Database:Name" "mordecai"
dotnet user-secrets set "Database:User" "username"
dotnet user-secrets set "Database:Password" "password"

# Cloud RabbitMQ (optional - omit to use local container)
dotnet user-secrets set "RabbitMQ:Host" "your-rabbitmq-host.com"
dotnet user-secrets set "RabbitMQ:Port" "5672"
dotnet user-secrets set "RabbitMQ:Username" "username"
dotnet user-secrets set "RabbitMQ:Password" "password"
```

### Step 2: Set Cloud Configuration in Web App (Actual Connections)

```bash
cd Mordecai.Web

# Cloud PostgreSQL (same values as AppHost)
dotnet user-secrets set "Database:Host" "your-postgres-host.com"
dotnet user-secrets set "Database:Port" "5432"
dotnet user-secrets set "Database:Name" "mordecai"
dotnet user-secrets set "Database:User" "username"
dotnet user-secrets set "Database:Password" "password"

# Cloud RabbitMQ (same values as AppHost)
dotnet user-secrets set "RabbitMQ:Host" "your-rabbitmq-host.com"
dotnet user-secrets set "RabbitMQ:Port" "5672"
dotnet user-secrets set "RabbitMQ:Username" "username"
dotnet user-secrets set "RabbitMQ:Password" "password"
```

### Step 3: Start AppHost

```bash
dotnet run --project Mordecai.AppHost
```

### Step 4: Open Dashboard

The console will show the dashboard URL. Open it and go to the **Resources** tab.

## What You'll See

### With Cloud Config:
- ☁️ **postgresql** - Your cloud database (external resource)
- ☁️ **messaging** - Your cloud RabbitMQ (external resource)
- 🌐 **webfrontend** - Your web application (with health checks for both)

### Without Cloud Config (Default):
- 🐳 **postgres** - Local PostgreSQL container (managed by Aspire)
- 🐳 **messaging** - Local RabbitMQ container (managed by Aspire)
- 🌐 **webfrontend** - Your web application

## Health Status Indicators

- ✅ **Healthy** - Resource is accessible and responding
- ⚠️ **Degraded** - Resource has connectivity issues
- ❌ **Unhealthy** - Resource is down

## Important Notes

1. **AppHost Config** = Dashboard visibility
2. **Web App Config** = Actual connections
3. **Both need the same settings** for cloud resources

## Quick Test

Verify AppHost has configuration:

```bash
cd Mordecai.AppHost
dotnet user-secrets list
```

Should show your Database:* and RabbitMQ:* settings.

## More Details

See [ASPIRE_CLOUD_RESOURCES_SETUP.md](./ASPIRE_CLOUD_RESOURCES_SETUP.md) for complete documentation.
