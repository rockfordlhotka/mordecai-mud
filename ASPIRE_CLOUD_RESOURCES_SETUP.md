# Aspire Cloud Resources Setup Guide

## Overview

Your Aspire AppHost now intelligently switches between **local development containers** and **cloud resources** based on configuration. The dashboard will show the appropriate resources depending on what's configured.

## How It Works

### Automatic Resource Detection

The AppHost checks for cloud configuration at startup:

1. **PostgreSQL**: 
   - If `Database:Host` is configured → Uses cloud PostgreSQL (shown as "postgresql" in dashboard)
   - If not configured → Starts local PostgreSQL container

2. **RabbitMQ**:
   - If `RabbitMQ:Host` or `RABBITMQ_HOST` is configured → Uses cloud RabbitMQ (shown as "messaging" in dashboard)
   - If not configured → Starts local RabbitMQ container

### Health Checks

Health checks run in the web application and monitor:
- ✅ Cloud PostgreSQL connection
- ✅ Cloud RabbitMQ connection
- ✅ Application health

Access health status at:
- `https://localhost:<port>/health` - All checks (ready probe)
- `https://localhost:<port>/alive` - Liveness only

## Configuration Options

### Option 1: User Secrets (Recommended for Development)

Configure cloud resources for the **AppHost** using user secrets:

```bash
cd Mordecai.AppHost

# PostgreSQL Cloud Configuration
dotnet user-secrets set "Database:Host" "your-cloud-postgres.com"
dotnet user-secrets set "Database:Port" "5432"
dotnet user-secrets set "Database:Name" "mordecai"
dotnet user-secrets set "Database:User" "your-username"
dotnet user-secrets set "Database:Password" "your-secure-password"

# RabbitMQ Cloud Configuration
dotnet user-secrets set "RabbitMQ:Host" "your-cloud-rabbitmq.com"
dotnet user-secrets set "RabbitMQ:Port" "5672"
dotnet user-secrets set "RabbitMQ:Username" "your-username"
dotnet user-secrets set "RabbitMQ:Password" "your-secure-password"
dotnet user-secrets set "RabbitMQ:VirtualHost" "/"
```

### Option 2: Environment Variables

Set environment variables before starting AppHost:

```bash
# PostgreSQL
export Database__Host="your-cloud-postgres.com"
export Database__Port="5432"
export Database__Name="mordecai"
export Database__User="your-username"
export Database__Password="your-secure-password"

# RabbitMQ
export RabbitMQ__Host="your-cloud-rabbitmq.com"
export RabbitMQ__Port="5672"
export RabbitMQ__Username="your-username"
export RabbitMQ__Password="your-secure-password"
export RabbitMQ__VirtualHost="/"
```

### Option 3: appsettings.Development.json (Not Recommended - Secrets in Source)

Only use for non-sensitive local testing:

```json
{
  "Database": {
    "Host": "your-cloud-postgres.com",
    "Port": "5432",
    "Name": "mordecai",
    "User": "your-username",
    "Password": "your-password"
  },
  "RabbitMQ": {
    "Host": "your-cloud-rabbitmq.com",
    "Port": "5672",
    "Username": "your-username",
    "Password": "your-password",
    "VirtualHost": "/"
  }
}
```

## Usage Scenarios

### Scenario 1: Full Local Development (Default)

**Don't configure anything** - AppHost will start local containers:

```bash
dotnet run --project Mordecai.AppHost
```

Dashboard shows:
- 🐳 `postgres` - Local PostgreSQL container
- 🐳 `messaging` - Local RabbitMQ container
- 🌐 `webfrontend` - Your web application

### Scenario 2: Cloud PostgreSQL + Local RabbitMQ

Configure only PostgreSQL in user secrets:

```bash
cd Mordecai.AppHost
dotnet user-secrets set "Database:Host" "your-cloud-db.com"
dotnet user-secrets set "Database:User" "username"
dotnet user-secrets set "Database:Password" "password"
```

Dashboard shows:
- ☁️ `postgresql` - Cloud PostgreSQL connection
- 🐳 `messaging` - Local RabbitMQ container
- 🌐 `webfrontend` - Your web application

### Scenario 3: Full Cloud Resources

Configure both PostgreSQL and RabbitMQ in user secrets.

Dashboard shows:
- ☁️ `postgresql` - Cloud PostgreSQL connection
- ☁️ `messaging` - Cloud RabbitMQ connection
- 🌐 `webfrontend` - Your web application

## Viewing Cloud Resource Status

### In Aspire Dashboard

1. Start your application:
   ```bash
   dotnet run --project Mordecai.AppHost
   ```

2. Open the dashboard URL (shown in console output)

3. Navigate to **Resources** tab

4. You'll see your configured resources with their connection status

### Health Check Details

Click on the `webfrontend` resource in the dashboard to see:
- Detailed health check results
- PostgreSQL connectivity status
- RabbitMQ connectivity status
- Response times and error details

## Troubleshooting

### "Connection string parameter resource could not be used"

**Problem**: AppHost tried to use a connection string but it wasn't configured.

**Solution**: Either:
1. Configure the cloud resource (see Configuration Options above)
2. Remove the configuration to use local containers

### Local Containers Still Starting

**Problem**: You configured cloud resources but local containers are starting.

**Solution**: Verify configuration is set in the **AppHost** project:
```bash
cd Mordecai.AppHost
dotnet user-secrets list
```

### Can't Connect to Cloud Resources

**Problem**: Health checks show degraded status for cloud resources.

**Solution**: 
1. Verify network connectivity: `ping your-cloud-host.com`
2. Check firewall/IP whitelist on cloud provider
3. Verify credentials are correct
4. Check port accessibility: `telnet your-cloud-host.com 5432`

### Mixed Configuration (AppHost vs Web)

The Web app and AppHost need the same configuration for cloud resources:

- **AppHost**: Needs configuration to show resources in dashboard
- **Web App**: Needs configuration to actually connect to resources

**Best Practice**: Set configuration in both places using the same method:

```bash
# For AppHost (dashboard visibility)
cd Mordecai.AppHost
dotnet user-secrets set "Database:Host" "cloud-host.com"
dotnet user-secrets set "Database:Password" "password"

# For Web App (actual connections)
cd Mordecai.Web
dotnet user-secrets set "Database:Host" "cloud-host.com"
dotnet user-secrets set "Database:Password" "password"
```

Or use environment variables that both will read.

## Production Deployment

For Kubernetes/production, use environment variables:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: mordecai-config
data:
  Database__Host: "your-cloud-postgres.com"
  Database__Port: "5432"
  Database__Name: "mordecai"
  RabbitMQ__Host: "your-cloud-rabbitmq.com"
  RabbitMQ__Port: "5672"
  RabbitMQ__VirtualHost: "/"

---
apiVersion: v1
kind: Secret
metadata:
  name: mordecai-secrets
type: Opaque
stringData:
  Database__User: "your-username"
  Database__Password: "your-secure-password"
  RabbitMQ__Username: "your-username"
  RabbitMQ__Password: "your-secure-password"
```

## Configuration Priority

Configuration is read in this order (highest to lowest):

1. Environment Variables
2. User Secrets (Development only)
3. appsettings.Development.json (Development only)
4. appsettings.json
5. Default values (local containers)

## Benefits

✅ **Seamless Switching**: Change between local and cloud resources without code changes
✅ **Dashboard Visibility**: See all resources (local or cloud) in one place
✅ **Health Monitoring**: Real-time health checks for cloud resources
✅ **Team Friendly**: Each developer can configure their own environment
✅ **Production Ready**: Same code works in dev and production

## Related Documentation

- [Health Checks Configuration](./docs/HEALTH_CHECKS_CONFIGURATION.md)
- [Database Configuration](./docs/DATABASE_CONFIGURATION.md)
- [RabbitMQ Quick Start](./RABBITMQ_QUICK_START.md)
- [Aspire Cloud Health Checks](./ASPIRE_CLOUD_HEALTH_CHECKS.md)
