# How External Resources Work in Aspire Dashboard

## The Key Concept

Aspire distinguishes between two types of resources:

1. **Managed Resources** - Containers/services that Aspire starts and stops (e.g., local PostgreSQL, local RabbitMQ)
2. **External Resources** - Cloud services you manage separately (e.g., Azure Database for PostgreSQL, CloudAMQP)

## What We Built

### Custom External Resource Types

Created `ExternalResourceExtensions.cs` that defines:
- `ExternalPostgresResource` - Represents a cloud PostgreSQL instance
- `ExternalRabbitMQResource` - Represents a cloud RabbitMQ instance

These resources:
- ✅ **Appear in the Aspire dashboard**
- ✅ **Show host, port, and connection info**
- ❌ **Don't start containers** (marked with `.ExcludeFromManifest()`)
- ❌ **Don't get deployed** (they're external - you manage them)

### Smart Resource Selection

The AppHost now:
1. Checks for cloud configuration (`Database:Host`, `RabbitMQ:Host`)
2. If configured → Shows external resource in dashboard
3. If not configured → Starts local container

## Configuration Requirements

For external resources to appear in the dashboard, configure **BOTH**:

### 1. AppHost Configuration (Dashboard Visibility)
```bash
cd Mordecai.AppHost
dotnet user-secrets set "Database:Host" "your-cloud-host.com"
dotnet user-secrets set "Database:User" "username"
dotnet user-secrets set "Database:Password" "password"
```

### 2. Web App Configuration (Actual Connections)
```bash
cd Mordecai.Web
dotnet user-secrets set "Database:Host" "your-cloud-host.com"
dotnet user-secrets set "Database:User" "username"
dotnet user-secrets set "Database:Password" "password"
```

## Why Both?

- **AppHost config** → Creates the visual representation in the dashboard
- **Web app config** → Makes the actual database/messaging connections
- **Health checks** (in Web app) → Monitor the cloud resources and show status

## Dashboard View

### External Resources Show:
- Name (e.g., "postgresql", "messaging")
- Type (ExternalPostgresResource, ExternalRabbitMQResource)
- Host and Port information
- Status from health checks (via webfrontend)

### Local Resources Show:
- Name (e.g., "postgres", "messaging")
- Container ID
- Logs
- Environment variables
- Start/Stop controls

## Health Check Integration

The health checks we configured earlier in `ServiceDefaults/Extensions.cs`:
- Run inside the web application
- Monitor cloud resources
- Report status back to the dashboard
- Show up under the `webfrontend` resource health

## Benefits

✅ **Single Dashboard** - See both local containers and cloud resources
✅ **Smart Switching** - Automatic detection based on configuration
✅ **Team Friendly** - Each developer chooses local or cloud
✅ **Production Ready** - Deploy to cloud without code changes
✅ **Health Visibility** - Monitor cloud resource connectivity

## Limitations

External resources in the dashboard:
- Don't show real-time metrics (CPU, memory) - they're not containers
- Don't have logs - they're external services
- Don't have start/stop controls - you manage them separately
- Show connection info and health status only

For detailed monitoring of cloud resources, use:
- Cloud provider's monitoring (Azure Monitor, AWS CloudWatch, etc.)
- The health check endpoints (`/health`) in your web app
- The Aspire dashboard's health check view

## Files Modified

1. **`Mordecai.AppHost/ExternalResourceExtensions.cs`** (NEW)
   - Custom external resource types
   - Extension methods for adding external resources

2. **`Mordecai.AppHost/AppHost.cs`** (UPDATED)
   - Smart resource selection logic
   - Adds external resources when cloud is configured
   - Adds local containers when cloud is not configured

3. **`Mordecai.ServiceDefaults/Extensions.cs`** (PREVIOUSLY UPDATED)
   - Health checks for PostgreSQL and RabbitMQ
   - Runs in web app, monitors cloud resources

## Next Steps

1. Configure your cloud resources in AppHost user secrets
2. Configure your cloud resources in Web app user secrets (same values)
3. Start the AppHost: `dotnet run --project Mordecai.AppHost`
4. Open the dashboard and see your external resources!

## Troubleshooting

**External resources don't appear?**
- Check AppHost has configuration: `cd Mordecai.AppHost && dotnet user-secrets list`
- Verify `Database:Host` or `RabbitMQ:Host` is set
- Restart the AppHost after changing configuration

**Can't connect to cloud resources?**
- Check Web app has configuration: `cd Mordecai.Web && dotnet user-secrets list`
- Verify health checks in ServiceDefaults are enabled
- Check firewall/IP whitelist on cloud provider
- View health endpoint: `https://localhost:<port>/health`
