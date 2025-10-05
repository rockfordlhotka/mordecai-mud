# Mordecai MUD Configuration Guide

This document explains all configuration options for Mordecai MUD, including environment variables, configuration files, and deployment scenarios.

## Configuration Sources

Mordecai follows standard .NET configuration precedence:

1. **Environment Variables** (highest priority)
2. **appsettings.{Environment}.json**
3. **appsettings.json**
4. **Default Values** (lowest priority)

## Database Configuration

### SQLite Database Path

The database file location can be configured through multiple methods:

#### Option 1: Environment Variable (Recommended for Kubernetes)

```bash
DATABASE_PATH=/data/mordecai/mordecai.db
```

#### Option 2: Configuration File

```json
{
  "DatabasePath": "/data/mordecai/mordecai.db"
}
```

#### Option 3: Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/data/mordecai/mordecai.db"
  }
}
```

### Priority Resolution

The application resolves the database path in this order:

1. `DATABASE_PATH` environment variable
2. `DatabasePath` configuration value
3. Extract path from `ConnectionStrings:DefaultConnection`
4. Default to `mordecai.db` in application directory

### Path Handling

- **Relative paths**: Converted to absolute paths based on application directory
- **Absolute paths**: Used as-is
- **Directory creation**: Parent directories are created automatically if they don't exist
- **Logging**: Database path is logged on startup for verification

### Example Configurations

**Local Development:**
```json
{
  "DatabasePath": "mordecai.db"
}
```

**Docker Container:**
```bash
docker run -e DATABASE_PATH=/data/mordecai.db -v /host/data:/data mordecai-web
```

**Kubernetes:**
```yaml
env:
- name: DATABASE_PATH
  value: "/data/mordecai.db"
volumeMounts:
- name: db-storage
  mountPath: /data
```

## RabbitMQ Configuration

### Connection String Method (Aspire/Local Development)

When using .NET Aspire or local development, use connection strings:

```json
{
  "ConnectionStrings": {
    "messaging": "amqp://username:password@hostname:5672/virtualhost"
  }
}
```

### Individual Parameters Method (Kubernetes/Production)

For production deployments, use individual configuration parameters:

```json
{
  "RabbitMQ": {
    "Host": "rabbitmq-service",
    "Port": 5672,
    "Username": "gameuser",
    "Password": "secretpassword",
    "VirtualHost": "/"
  }
}
```

### Environment Variable Overrides

All RabbitMQ settings can be overridden with environment variables:

| Configuration Key | Environment Variable | Default |
|------------------|---------------------|---------|
| `RabbitMQ:Host` | `RABBITMQ_HOST` | `localhost` |
| `RabbitMQ:Port` | `RABBITMQ_PORT` | `5672` |
| `RabbitMQ:Username` | `RABBITMQ_USERNAME` | `guest` |
| `RabbitMQ:Password` | `RABBITMQ_PASSWORD` | `guest` |
| `RabbitMQ:VirtualHost` | `RABBITMQ_VIRTUALHOST` | `/` |

### Priority Resolution

RabbitMQ connection is established using this priority:

1. **Connection String**: If `ConnectionStrings:messaging` exists, use it
2. **Individual Parameters**: Otherwise, build connection from individual settings
3. **Environment Variables**: Always override configuration file values
4. **Defaults**: Fall back to default values if nothing specified

### Example Configurations

**Local Development (Aspire):**
```json
{
  "ConnectionStrings": {
    "messaging": "amqp://guest:guest@localhost:5672/"
  }
}
```

**Kubernetes with Secrets:**
```yaml
env:
- name: RABBITMQ_HOST
  value: "rabbitmq-service"
- name: RABBITMQ_PORT
  value: "5672"
- name: RABBITMQ_USERNAME
  valueFrom:
    secretKeyRef:
      name: rabbitmq-secret
      key: username
- name: RABBITMQ_PASSWORD
  valueFrom:
    secretKeyRef:
      name: rabbitmq-secret
      key: password
```

**Docker Compose:**
```yaml
environment:
  RABBITMQ_HOST: rabbitmq
  RABBITMQ_PORT: 5672
  RABBITMQ_USERNAME: gameuser
  RABBITMQ_PASSWORD: gamepassword
```

## ASP.NET Core Configuration

### Standard ASP.NET Core Settings

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Mordecai": "Debug"
    }
  },
  "AllowedHosts": "*"
}
```

### Environment-Specific Configuration

Create environment-specific files for different deployments:

**appsettings.Development.json:**
```json
{
  "DatabasePath": "mordecai.db",
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

**appsettings.Production.json:**
```json
{
  "DatabasePath": "/data/mordecai.db",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Identity Configuration

Password requirements and user settings:

```json
{
  "Identity": {
    "Password": {
      "RequireDigit": false,
      "RequiredLength": 6,
      "RequireNonAlphanumeric": false,
      "RequireUppercase": false,
      "RequireLowercase": false
    },
    "SignIn": {
      "RequireConfirmedAccount": false
    }
  }
}
```

**Note:** These are currently configured in `Program.cs`. Consider moving to configuration file for easier customization.

## OpenTelemetry Configuration

### OTLP Exporter

Enable OpenTelemetry export to collectors:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_SERVICE_NAME=mordecai-web
```

### Azure Monitor (Optional)

For Azure deployments:

```bash
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=...
```

Uncomment Azure Monitor integration in `ServiceDefaults/Extensions.cs`.

## Complete Configuration Example

### appsettings.json (Full)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=mordecai.db"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  },
  "DatabasePath": "mordecai.db",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Mordecai": "Debug"
    }
  },
  "AllowedHosts": "*"
}
```

### Environment Variables (Kubernetes)

```yaml
env:
# Database
- name: DATABASE_PATH
  value: "/data/mordecai.db"

# RabbitMQ
- name: RABBITMQ_HOST
  value: "rabbitmq-service"
- name: RABBITMQ_PORT
  value: "5672"
- name: RABBITMQ_USERNAME
  valueFrom:
    secretKeyRef:
      name: mordecai-secrets
      key: rabbitmq-username
- name: RABBITMQ_PASSWORD
  valueFrom:
    secretKeyRef:
      name: mordecai-secrets
      key: rabbitmq-password
- name: RABBITMQ_VIRTUALHOST
  value: "/"

# ASP.NET Core
- name: ASPNETCORE_ENVIRONMENT
  value: "Production"
- name: ASPNETCORE_URLS
  value: "http://+:8080"

# OpenTelemetry (Optional)
- name: OTEL_EXPORTER_OTLP_ENDPOINT
  value: "http://otel-collector:4317"
- name: OTEL_SERVICE_NAME
  value: "mordecai-web"
```

## Configuration Validation

### Startup Validation

The application validates configuration on startup and logs:

- Database path being used
- RabbitMQ connection details (host/port, not credentials)
- Environment name
- Active configuration sources

### Common Validation Errors

**Missing RabbitMQ Connection:**
```
Failed to initialize RabbitMQ Game Message Publisher
```
**Solution:** Verify RabbitMQ configuration and service availability

**Database Path Issues:**
```
SqliteException: Error Code: 14 - Unable to open database file
```
**Solution:** Check DATABASE_PATH environment variable and directory permissions

## Configuration Best Practices

### Development
- Use default configuration files
- Rely on Aspire for service orchestration
- Keep database in project directory
- Use default RabbitMQ credentials

### Staging/Production
- Use environment variables for all sensitive data
- Store credentials in Kubernetes secrets
- Use absolute paths for database files
- Enable persistent volumes for database
- Configure OpenTelemetry export
- Set appropriate log levels

### Security
- **Never commit secrets to version control**
- Use Kubernetes secrets for sensitive data
- Rotate credentials regularly
- Use strong passwords for RabbitMQ
- Enable TLS for RabbitMQ in production
- Consider using managed secret services (Azure Key Vault, AWS Secrets Manager)

## Debugging Configuration

### View Current Configuration

Add debug logging in `Program.cs`:

```csharp
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Database Path: {Path}", databasePath);
logger.LogInformation("RabbitMQ Host: {Host}", 
    builder.Configuration["RABBITMQ_HOST"] ?? builder.Configuration["RabbitMQ:Host"]);
```

### Test Configuration

```bash
# Test database access
dotnet run --project Mordecai.Web

# Test with environment variables
DATABASE_PATH=/tmp/test.db dotnet run --project Mordecai.Web

# Test RabbitMQ connection
RABBITMQ_HOST=testhost dotnet run --project Mordecai.Web
```

## Migration from SQLite to PostgreSQL

When ready to scale, migrate to PostgreSQL:

### 1. Update NuGet Packages

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet remove package Microsoft.EntityFrameworkCore.Sqlite
```

### 2. Update Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres-service;Database=mordecai;Username=dbuser;Password=dbpass"
  }
}
```

Or use environment variable:

```bash
DATABASE_CONNECTION_STRING="Host=postgres-service;Database=mordecai;Username=dbuser;Password=dbpass"
```

### 3. Update Program.cs

Replace:
```csharp
options.UseSqlite(connectionString)
```

With:
```csharp
options.UseNpgsql(connectionString)
```

### 4. Regenerate Migrations

```bash
dotnet ef migrations add InitialPostgreSQL --project Mordecai.Web
dotnet ef database update --project Mordecai.Web
```

## Troubleshooting

### Configuration Not Loading

1. Check `ASPNETCORE_ENVIRONMENT` value
2. Verify configuration file names match environment
3. Check file is set to "Copy to Output Directory"
4. Verify JSON syntax is valid

### Environment Variables Not Working

1. Confirm environment variable naming (use colons or double underscores)
   - Valid: `RabbitMQ__Host` or `RabbitMQ:Host`
2. Check variable is set in correct scope (system, user, process)
3. Restart application after setting variables
4. Use `dotnet run` environment variable syntax: `dotnet run -e VAR=value`

### Database Permissions

```bash
# Check file permissions
ls -la /data/mordecai.db

# Fix permissions (Linux/macOS)
chmod 660 /data/mordecai.db
chown app:app /data/mordecai.db

# Check directory permissions
ls -la /data/
```

## Additional Resources

- [ASP.NET Core Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
- [.NET Aspire Configuration](https://learn.microsoft.com/dotnet/aspire/fundamentals/configuration)
- [Kubernetes ConfigMaps](https://kubernetes.io/docs/concepts/configuration/configmap/)
- [Kubernetes Secrets](https://kubernetes.io/docs/concepts/configuration/secret/)

---

**Last Updated:** 2025-01-23  
**Version:** 1.0
