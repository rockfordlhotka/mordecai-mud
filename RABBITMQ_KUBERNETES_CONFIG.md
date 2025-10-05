# RabbitMQ Kubernetes Configuration

> **⚠️ SECURITY WARNING:** Never commit actual passwords to source control or include them in documentation. All password examples in this document use placeholder values. Replace `<your-secure-password>` with your actual password only in User Secrets or Kubernetes Secrets.

This document describes the cloud-native configuration for connecting to RabbitMQ running in a Kubernetes cluster.

## Configuration Overview

The application uses a cloud-native configuration approach with the following priority:

1. **Environment Variables** (highest priority - for Kubernetes deployments)
2. **User Secrets** (for passwords in local development)
3. **appsettings.json** (for non-sensitive defaults)

## Kubernetes Cluster Connection

The application connects to a RabbitMQ instance running in Kubernetes:
- **Host**: `default-rabbitmq-amqp`
- **Port**: `5672` (AMQP protocol)
- **Username**: `mordecai`
- **VirtualHost**: `/`

## Configuration Methods

### For Local Development (User Secrets)

Set the RabbitMQ password using User Secrets to avoid storing it in source control:

```bash
# For the Web application
cd Mordecai.Web
dotnet user-secrets set "RabbitMQ:Password" "<your-secure-password>"

# For the AdminCli
cd Mordecai.AdminCli
dotnet user-secrets set "RabbitMQ:Password" "<your-secure-password>"
```

The host and username are already configured in `appsettings.json` and don't need to be in User Secrets.

### For Kubernetes Deployment (Environment Variables)

Set environment variables in your Kubernetes deployment manifests:

```yaml
env:
  - name: RABBITMQ_HOST
    value: "default-rabbitmq-amqp"
  - name: RABBITMQ_PORT
    value: "5672"
  - name: RABBITMQ_USERNAME
    value: "mordecai"
  - name: RABBITMQ_PASSWORD
    valueFrom:
      secretKeyRef:
        name: rabbitmq-credentials
        key: password
  - name: RABBITMQ_VIRTUALHOST
    value: "/"
```

Create the Kubernetes secret:

```bash
kubectl create secret generic rabbitmq-credentials \
  --from-literal=password='<your-secure-password>'
```

### For Container/Docker Deployment

Set environment variables when running the container:

```bash
docker run -e RABBITMQ_HOST=default-rabbitmq-amqp \
           -e RABBITMQ_PORT=5672 \
           -e RABBITMQ_USERNAME=mordecai \
           -e RABBITMQ_PASSWORD=<your-secure-password> \
           -e RABBITMQ_VIRTUALHOST=/ \
           mordecai-web:latest
```

## Configuration File Reference

### appsettings.json (Non-sensitive defaults)

```json
{
  "RabbitMQ": {
    "Host": "default-rabbitmq-amqp",
    "Port": 5672,
    "Username": "mordecai",
    "VirtualHost": "/"
  }
}
```

**Note**: The `Password` field is intentionally omitted from `appsettings.json` and must be provided via User Secrets or environment variables.

## Environment Variable Reference

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `RABBITMQ_HOST` | RabbitMQ server hostname | `default-rabbitmq-amqp` | Yes |
| `RABBITMQ_PORT` | AMQP port number | `5672` | No (defaults to 5672) |
| `RABBITMQ_USERNAME` | RabbitMQ username | `mordecai` | Yes |
| `RABBITMQ_PASSWORD` | RabbitMQ password | `<your-secure-password>` | Yes |
| `RABBITMQ_VIRTUALHOST` | RabbitMQ virtual host | `/` | No (defaults to `/`) |

## Verification

To verify the configuration is working:

1. Check application startup logs for RabbitMQ connection confirmation:
   ```
   Using RabbitMQ: Host=default-rabbitmq-amqp, User=mordecai
   RabbitMQ Game Message Publisher initialized successfully
   ```

2. Test message publishing by sending a chat message in the game

3. Check RabbitMQ management console to see exchanges and queues being created

## Troubleshooting

### Connection Refused

- Verify the Kubernetes service name is correct: `default-rabbitmq-amqp`
- Check if the RabbitMQ service is running in the cluster
- Ensure network policies allow traffic to RabbitMQ

### Authentication Failed

- Verify username and password are correct
- Check User Secrets or environment variables are properly set
- Confirm the RabbitMQ user has proper permissions

### Missing Configuration

If you see errors about missing configuration:
- `RabbitMQ host not configured`: Set `RABBITMQ_HOST` or `RabbitMQ:Host`
- `RabbitMQ username not configured`: Set `RABBITMQ_USERNAME` or `RabbitMQ:Username`
- `RabbitMQ password not configured`: Set `RABBITMQ_PASSWORD` or store in User Secrets

## Migration from Aspire

The application has been migrated from using Aspire's local RabbitMQ container to connecting directly to a Kubernetes-hosted RabbitMQ instance. Key changes:

1. Removed `builder.AddRabbitMQClient("messaging")` from `Program.cs`
2. Removed dependency on Aspire RabbitMQ connection strings
3. Updated messaging services to use direct configuration
4. Added cloud-native environment variable support
5. Made password configuration required (no default values)

## Security Best Practices

✅ **Do**:
- Use User Secrets for local development passwords
- Use Kubernetes Secrets for deployment passwords
- Use environment variables for all configuration in production
- Restrict access to RabbitMQ management interface

❌ **Don't**:
- Commit passwords to source control
- Store passwords in `appsettings.json`
- Use default credentials (`guest`/`guest`) in production
- Share User Secrets files

## Related Documentation

- [Database Configuration](DATABASE_CONFIGURATION.md)
- [Environment Variables](docs/ENVIRONMENT_VARIABLES.md)
- [Kubernetes Deployment](docs/KUBERNETES_DEPLOYMENT.md)
- [Cloud Native Configuration Update](docs/CLOUD_NATIVE_CONFIG_UPDATE.md)
