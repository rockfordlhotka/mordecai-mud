# Health Checks Configuration for Cloud Resources

This document explains how to configure Aspire to monitor the health of your cloud PostgreSQL and RabbitMQ instances in the Aspire dashboard.

## Overview

The Aspire dashboard will now show real-time health status for:
- **PostgreSQL** - Your cloud database instance
- **RabbitMQ** - Your cloud messaging broker

Health checks run automatically and report:
- ✅ **Healthy** - Resource is accessible and functioning
- ⚠️ **Degraded** - Resource has connectivity issues
- ❌ **Unhealthy** - Resource is down or unreachable

## Configuration

Health checks are automatically configured based on your existing connection settings. The same configuration you use for database and messaging connections is used for health checks.

### PostgreSQL Health Check

Configured via these settings (already in use):
- `Database:Host` - PostgreSQL server hostname
- `Database:Port` - PostgreSQL port (default: 5432)
- `Database:Name` - Database name
- `Database:User` - Database username
- `Database:Password` - Database password

### RabbitMQ Health Check

Configured via these settings (already in use):
- `RabbitMQ:Host` or `RABBITMQ_HOST` - RabbitMQ server hostname
- `RabbitMQ:Port` or `RABBITMQ_PORT` - RabbitMQ port (default: 5672)
- `RabbitMQ:Username` or `RABBITMQ_USERNAME` - RabbitMQ username
- `RabbitMQ:Password` or `RABBITMQ_PASSWORD` - RabbitMQ password
- `RabbitMQ:VirtualHost` or `RABBITMQ_VHOST` - Virtual host (default: /)

## User Secrets Configuration

For local development with cloud resources, set your user secrets:

```bash
# Navigate to the Web project
cd Mordecai.Web

# Set PostgreSQL cloud instance
dotnet user-secrets set "Database:Host" "your-cloud-postgres-host.com"
dotnet user-secrets set "Database:Port" "5432"
dotnet user-secrets set "Database:Name" "mordecai"
dotnet user-secrets set "Database:User" "mordecai_user"
dotnet user-secrets set "Database:Password" "your-secure-password"

# Set RabbitMQ cloud instance
dotnet user-secrets set "RabbitMQ:Host" "your-cloud-rabbitmq-host.com"
dotnet user-secrets set "RabbitMQ:Port" "5672"
dotnet user-secrets set "RabbitMQ:Username" "mordecai_user"
dotnet user-secrets set "RabbitMQ:Password" "your-secure-password"
dotnet user-secrets set "RabbitMQ:VirtualHost" "/"
```

## Environment Variables (Production)

For Kubernetes/production deployments, use environment variables:

```yaml
# PostgreSQL
- name: Database__Host
  value: "your-cloud-postgres-host.com"
- name: Database__Port
  value: "5432"
- name: Database__Name
  value: "mordecai"
- name: Database__User
  valueFrom:
    secretKeyRef:
      name: database-credentials
      key: username
- name: Database__Password
  valueFrom:
    secretKeyRef:
      name: database-credentials
      key: password

# RabbitMQ
- name: RABBITMQ_HOST
  value: "your-cloud-rabbitmq-host.com"
- name: RABBITMQ_PORT
  value: "5672"
- name: RABBITMQ_USERNAME
  valueFrom:
    secretKeyRef:
      name: rabbitmq-credentials
      key: username
- name: RABBITMQ_PASSWORD
  valueFrom:
    secretKeyRef:
      name: rabbitmq-credentials
      key: password
- name: RABBITMQ_VHOST
  value: "/"
```

## Viewing Health Status

### In Aspire Dashboard

1. Start your application with Aspire:
   ```bash
   dotnet run --project Mordecai.AppHost
   ```

2. Open the Aspire Dashboard (URL shown in console output)

3. Navigate to the **Resources** view

4. You'll see health status for:
   - `webfrontend` - Your web application
   - `postgresql` - Your cloud PostgreSQL instance
   - `rabbitmq` - Your cloud RabbitMQ instance

### Health Check Endpoints

In development, health check endpoints are available at:

- **Ready check** (all checks): `https://localhost:<port>/health`
  - Includes: self, postgresql, rabbitmq
  
- **Liveness check** (minimal): `https://localhost:<port>/alive`
  - Includes: self only

Example response:
```json
{
  "status": "Healthy",
  "results": {
    "self": {
      "status": "Healthy"
    },
    "postgresql": {
      "status": "Healthy",
      "tags": ["ready", "db", "postgresql"]
    },
    "rabbitmq": {
      "status": "Healthy",
      "tags": ["ready", "messaging", "rabbitmq"]
    }
  }
}
```

## Health Check Behavior

### PostgreSQL Health Check
- Tests database connectivity
- Executes a simple query to verify database is responsive
- Tagged as: `ready`, `db`, `postgresql`
- Failure status: `Degraded` (app can continue without immediate DB access)

### RabbitMQ Health Check
- Tests broker connectivity
- Verifies connection can be established
- Tagged as: `ready`, `messaging`, `rabbitmq`
- Failure status: `Degraded` (app can continue without immediate messaging)

### Self Health Check
- Always healthy (confirms app is running)
- Tagged as: `live`
- Used for Kubernetes liveness probes

## Troubleshooting

### Health Checks Not Appearing

If health checks don't appear in the dashboard:

1. Verify configuration is set (check logs on startup)
2. Ensure network connectivity to cloud resources
3. Check firewall rules allow connections from your dev machine
4. Verify credentials are correct

### Health Checks Failing

If health checks show as Degraded or Unhealthy:

1. **PostgreSQL issues:**
   - Verify hostname/port are correct
   - Check credentials
   - Ensure your IP is whitelisted on cloud provider
   - Test connection: `psql -h hostname -U username -d database`

2. **RabbitMQ issues:**
   - Verify hostname/port are correct
   - Check credentials
   - Ensure your IP is whitelisted on cloud provider
   - Verify virtual host exists
   - Test connection using RabbitMQ management UI

### Disabling Health Checks

If you need to temporarily disable health checks for a specific resource, remove or comment out the relevant configuration values. The health check will be automatically skipped if configuration is missing.

## Production Considerations

1. **Security**: Health check endpoints are only exposed in Development environment by default
2. **Performance**: Health checks run periodically (default: every 30 seconds)
3. **Timeouts**: Default timeout is 30 seconds per check
4. **Dependencies**: Failed health checks don't stop the application, status is `Degraded`

## Integration with Kubernetes

Health checks integrate seamlessly with Kubernetes probes:

```yaml
livenessProbe:
  httpGet:
    path: /alive
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 30

readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
```

The `/health` endpoint includes all checks (PostgreSQL, RabbitMQ), ensuring pods are only marked ready when all dependencies are accessible.

## Related Documentation

- [Aspire Health Checks](https://aka.ms/dotnet/aspire/healthchecks)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
- [PostgreSQL Configuration](./DATABASE_CONFIGURATION.md)
- [RabbitMQ Configuration](./RABBITMQ_QUICK_START.md)
