# Aspire Cloud Health Checks - Quick Start

## What Changed

Your Aspire dashboard can now monitor the health of your **cloud PostgreSQL** and **cloud RabbitMQ** instances in real-time!

## Changes Made

### 1. Added NuGet Packages
- `AspNetCore.HealthChecks.Npgsql` - PostgreSQL health checks
- `AspNetCore.HealthChecks.RabbitMQ` - RabbitMQ health checks
- `RabbitMQ.Client` - RabbitMQ connection support

### 2. Updated ServiceDefaults (`Mordecai.ServiceDefaults/Extensions.cs`)
Added automatic health check registration that:
- Detects PostgreSQL configuration and adds database health monitoring
- Detects RabbitMQ configuration and adds messaging health monitoring
- Uses your existing connection settings (no duplicate configuration needed)

### 3. Updated AppHost (`Mordecai.AppHost/AppHost.cs`)
Configured to work with external cloud resources:
- RabbitMQ is managed by Aspire (can be local or cloud via connection string)
- PostgreSQL is configured directly in the web app (via appsettings/user secrets)
- Health checks automatically monitor both resources from within the web app

## How to Use

### Start Your App
```bash
dotnet run --project Mordecai.AppHost
```

### View Health Status
1. Open the Aspire Dashboard (URL shown in console)
2. Go to the **Resources** tab
3. See real-time health status for:
   - ✅ **webfrontend** - Your application
   - ✅ **postgresql** - Your cloud database
   - ✅ **rabbitmq** - Your cloud message broker

### Health Check Endpoints (Development Only)
- **All checks**: `https://localhost:<port>/health`
- **Liveness only**: `https://localhost:<port>/alive`

## Configuration

**No additional configuration needed!** Health checks automatically use your existing connection settings:

### PostgreSQL
Uses these settings you already have:
- `Database:Host`
- `Database:Port`
- `Database:Name`
- `Database:User`
- `Database:Password`

### RabbitMQ
Uses these settings you already have:
- `RabbitMQ:Host` or `RABBITMQ_HOST`
- `RabbitMQ:Port` or `RABBITMQ_PORT`
- `RabbitMQ:Username` or `RABBITMQ_USERNAME`
- `RabbitMQ:Password` or `RABBITMQ_PASSWORD`
- `RabbitMQ:VirtualHost` or `RABBITMQ_VHOST`

## What You'll See

### Healthy Status
When everything is working:
```
✅ postgresql - Healthy
✅ rabbitmq - Healthy
✅ webfrontend - Healthy
```

### Degraded Status
If a cloud resource is unreachable:
```
⚠️ postgresql - Degraded
✅ rabbitmq - Healthy
✅ webfrontend - Healthy (still running)
```

**Important**: Your app continues running even if cloud resources are degraded. This allows for graceful degradation during network issues.

## Benefits

1. **Real-time Monitoring**: See cloud resource status at a glance
2. **Early Detection**: Catch connectivity issues before they impact users
3. **Kubernetes Ready**: Health checks integrate with K8s liveness/readiness probes
4. **Zero Extra Config**: Uses your existing connection settings
5. **Development & Production**: Works in both environments

## Troubleshooting

### Health Checks Not Showing
- Verify your configuration is set (check logs on startup)
- Ensure network connectivity to cloud resources
- Check firewall rules allow connections

### Health Checks Failing
- **PostgreSQL**: Verify hostname, port, credentials, IP whitelist
- **RabbitMQ**: Verify hostname, port, credentials, virtual host exists

### Test Connections Manually
```bash
# Test PostgreSQL
psql -h your-host -U your-user -d your-database

# Test RabbitMQ
# Use RabbitMQ Management UI: http://your-host:15672
```

## Next Steps

See [HEALTH_CHECKS_CONFIGURATION.md](./docs/HEALTH_CHECKS_CONFIGURATION.md) for:
- Detailed configuration options
- Kubernetes integration
- Production deployment considerations
- Advanced troubleshooting

## Related Documentation
- [Aspire Health Checks](https://aka.ms/dotnet/aspire/healthchecks)
- [Database Configuration](./docs/DATABASE_CONFIGURATION.md)
- [RabbitMQ Configuration](./RABBITMQ_QUICK_START.md)
