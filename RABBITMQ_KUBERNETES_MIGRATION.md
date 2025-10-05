# RabbitMQ Kubernetes Migration Summary

> **⚠️ SECURITY WARNING:** This document contains placeholder passwords only. Never commit actual passwords to source control. Use User Secrets for local development and Kubernetes Secrets for production deployment.

## Overview

Successfully migrated the Mordecai MUD application from using Aspire-managed local RabbitMQ container to a RabbitMQ instance running in a Kubernetes cluster.

## Changes Made

### 1. RabbitMQ Configuration Updates

#### appsettings.json Changes
- **Mordecai.Web/appsettings.json**: Updated to point to `default-rabbitmq-amqp` with user `mordecai`
- **Mordecai.AdminCli/appsettings.json**: Added RabbitMQ configuration section
- Removed password from configuration files (moved to User Secrets)

#### Messaging Service Changes
- **RabbitMqGameMessagePublisher.cs**: Updated to use cloud-native configuration
- **RabbitMqGameMessageSubscriber.cs**: Updated to use cloud-native configuration
- Removed Aspire connection string fallback logic
- Added proper error handling for missing configuration
- Environment variables now take priority over appsettings.json

### 2. Web Application Changes

#### Program.cs
- Removed `builder.AddRabbitMQClient("messaging")` (Aspire integration)
- Added logging for RabbitMQ configuration on startup
- Direct configuration is now used instead of Aspire service discovery

#### Project File (Mordecai.Web.csproj)
- Removed `Aspire.RabbitMQ.Client` package reference
- Application now uses direct `RabbitMQ.Client` (already referenced via Mordecai.Messaging)

### 3. User Secrets Configuration

Configured User Secrets for local development:
```bash
# Web application
dotnet user-secrets set "RabbitMQ:Password" "<your-secure-password>" --project Mordecai.Web

# AdminCli
dotnet user-secrets set "RabbitMQ:Password" "<your-secure-password>" --project Mordecai.AdminCli
```

## Configuration Priority

The application now follows cloud-native configuration practices:

1. **Environment Variables** (highest priority)
   - `RABBITMQ_HOST`
   - `RABBITMQ_PORT`
   - `RABBITMQ_USERNAME`
   - `RABBITMQ_PASSWORD`
   - `RABBITMQ_VIRTUALHOST`

2. **User Secrets** (for passwords in local development)
   - `RabbitMQ:Password`

3. **appsettings.json** (for non-sensitive defaults)
   - `RabbitMQ:Host`
   - `RabbitMQ:Port`
   - `RabbitMQ:Username`
   - `RabbitMQ:VirtualHost`

## Connection Details

- **Host**: `default-rabbitmq-amqp`
- **Port**: `5672`
- **Username**: `mordecai`
- **Password**: (stored in User Secrets/K8s Secrets - not in documentation)
- **Virtual Host**: `/`

## Aspire Status

### What Remains
- **Mordecai.ServiceDefaults**: Still provides OpenTelemetry, health checks, and resilience patterns
- **Mordecai.AppHost**: Still exists but is now optional for local development

### What Was Removed
- Aspire RabbitMQ integration (`Aspire.RabbitMQ.Client` package)
- Connection string-based RabbitMQ configuration
- Dependency on Aspire service discovery for RabbitMQ

### Can Aspire Be Fully Removed?

**Short Answer**: Yes, but you'll lose some useful features.

**What You Would Lose**:
1. **OpenTelemetry Integration**: Automatic tracing, metrics, and structured logging
2. **Health Checks**: Built-in health check endpoints (`/health`, `/alive`)
3. **Resilience Patterns**: Automatic retry/circuit breaker for HTTP clients
4. **Service Discovery**: Automatic endpoint resolution (not currently used for RabbitMQ)

**What You Would Keep**:
- All core game functionality
- Direct RabbitMQ connection
- PostgreSQL connection
- Identity/authentication

**Recommendation**: Keep Mordecai.ServiceDefaults for now because:
- OpenTelemetry is valuable for debugging and monitoring
- Health checks are useful for Kubernetes readiness/liveness probes
- Minimal overhead and follows cloud-native best practices
- Can be easily removed later if needed

**You can safely remove**: Mordecai.AppHost project if you don't plan to use Aspire orchestration for local development.

## Testing the Migration

### Local Development
1. Ensure User Secrets are configured (see above)
2. Run the application: `dotnet run --project Mordecai.Web`
3. Check startup logs for:
   ```
   Using RabbitMQ: Host=default-rabbitmq-amqp, User=mordecai
   RabbitMQ Game Message Publisher initialized successfully
   ```

### Kubernetes Deployment
1. Create Kubernetes secret:
   ```bash
   kubectl create secret generic rabbitmq-credentials \
     --from-literal=password='<your-secure-password>'
   ```

2. Update deployment manifest to include environment variables:
   ```yaml
   env:
     - name: RABBITMQ_HOST
       value: "default-rabbitmq-amqp"
     - name: RABBITMQ_USERNAME
       value: "mordecai"
     - name: RABBITMQ_PASSWORD
       valueFrom:
         secretKeyRef:
           name: rabbitmq-credentials
           key: password
   ```

3. Deploy and verify logs

## Troubleshooting

### Common Issues

**"RabbitMQ password not configured"**
- Solution: Set User Secrets locally or environment variable in K8s

**"RabbitMQ host not configured"**
- Solution: Check appsettings.json or set `RABBITMQ_HOST` environment variable

**Connection Timeout**
- Verify the Kubernetes service name: `kubectl get svc | grep rabbitmq`
- Ensure network policies allow traffic
- Check if RabbitMQ is running: `kubectl get pods | grep rabbitmq`

**Authentication Failed**
- Verify credentials are correct
- Check User Secrets: `dotnet user-secrets list --project Mordecai.Web`
- Verify K8s secret exists: `kubectl get secret rabbitmq-credentials`

## Next Steps

1. **Update Kubernetes Manifests**: Add RabbitMQ configuration to your deployment YAML
2. **Remove AppHost** (optional): If not using Aspire orchestration locally
3. **Configure CI/CD**: Ensure environment variables/secrets are set in deployment pipeline
4. **Update Documentation**: Document the RabbitMQ connection for your team
5. **Monitor**: Watch RabbitMQ logs and application telemetry after deployment

## Related Documentation

- [RabbitMQ Kubernetes Configuration](RABBITMQ_KUBERNETES_CONFIG.md)
- [Database Configuration](DATABASE_CONFIGURATION.md)
- [Kubernetes Deployment](docs/KUBERNETES_DEPLOYMENT.md)
- [Environment Variables](docs/ENVIRONMENT_VARIABLES.md)

## Security Notes

✅ **Implemented**:
- Password stored in User Secrets (local) and K8s Secrets (deployment)
- No credentials in source control
- Environment variables for cloud-native configuration

⚠️ **Recommendations**:
- Rotate RabbitMQ password periodically
- Use Kubernetes RBAC to restrict secret access
- Consider using external secret management (e.g., Azure Key Vault, HashiCorp Vault)
- Enable TLS/SSL for RabbitMQ connections in production (port 5671)

## Migration Checklist

- [x] Update appsettings.json files with K8s RabbitMQ endpoint
- [x] Remove Aspire.RabbitMQ.Client package from Web project
- [x] Update messaging services to use cloud-native configuration
- [x] Configure User Secrets for local development
- [x] Remove Aspire integration code from Program.cs
- [x] Add startup logging for RabbitMQ configuration
- [x] Update priority order for configuration sources
- [x] Add proper error messages for missing configuration
- [ ] Update Kubernetes deployment manifests (your task)
- [ ] Create RabbitMQ credentials secret in K8s (your task)
- [ ] Test deployment in K8s cluster (your task)
- [ ] Update CI/CD pipelines (your task)

## Rollback Plan

If you need to rollback to local Aspire-managed RabbitMQ:

1. Restore `Aspire.RabbitMQ.Client` package to Mordecai.Web.csproj
2. Add back `builder.AddRabbitMQClient("messaging")` to Program.cs
3. Update appsettings.json to use `localhost`
4. Run Aspire AppHost for local RabbitMQ container

The messaging services still support both approaches, so no code changes needed there.
