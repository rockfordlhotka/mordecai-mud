# Environment Variables Quick Reference

Quick reference for all Mordecai MUD environment variables for Kubernetes and Docker deployments.

## Database Configuration

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `DATABASE_PATH` | Absolute path to SQLite database file | `/data/mordecai.db` | No (defaults to `mordecai.db`) |

## RabbitMQ Configuration

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `RABBITMQ_HOST` | RabbitMQ server hostname | `rabbitmq-service` | Yes (if no connection string) |
| `RABBITMQ_PORT` | RabbitMQ AMQP port | `5672` | No (default: 5672) |
| `RABBITMQ_USERNAME` | RabbitMQ username | `gameuser` | No (default: guest) |
| `RABBITMQ_PASSWORD` | RabbitMQ password | `secretpassword` | No (default: guest) |
| `RABBITMQ_VIRTUALHOST` | RabbitMQ virtual host | `/` | No (default: /) |

## ASP.NET Core Configuration

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | Application environment | `Production` | No (default: Production) |
| `ASPNETCORE_URLS` | URLs to listen on | `http://+:8080` | No (default: http://+:8080) |

## OpenTelemetry Configuration (Optional)

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OpenTelemetry collector endpoint | `http://otel-collector:4317` | No |
| `OTEL_SERVICE_NAME` | Service name for tracing | `mordecai-web` | No |

## Kubernetes Deployment Example

```yaml
env:
# Database
- name: DATABASE_PATH
  value: "/data/mordecai.db"

# RabbitMQ (using secrets)
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

# OpenTelemetry (optional)
- name: OTEL_EXPORTER_OTLP_ENDPOINT
  value: "http://otel-collector:4317"
- name: OTEL_SERVICE_NAME
  value: "mordecai-web"
```

## Docker Compose Example

```yaml
version: '3.8'
services:
  mordecai-web:
    image: mordecai-web:latest
    environment:
      DATABASE_PATH: /data/mordecai.db
      RABBITMQ_HOST: rabbitmq
      RABBITMQ_PORT: 5672
      RABBITMQ_USERNAME: gameuser
      RABBITMQ_PASSWORD: gamepassword
      RABBITMQ_VIRTUALHOST: /
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
    volumes:
      - ./data:/data
    ports:
      - "8080:8080"
```

## Docker Run Example

```bash
docker run -d \
  --name mordecai-web \
  -e DATABASE_PATH=/data/mordecai.db \
  -e RABBITMQ_HOST=rabbitmq-service \
  -e RABBITMQ_PORT=5672 \
  -e RABBITMQ_USERNAME=gameuser \
  -e RABBITMQ_PASSWORD=secretpassword \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v /host/data:/data \
  -p 8080:8080 \
  mordecai-web:latest
```

## Local Development (Aspire)

**No environment variables needed** - Aspire handles everything automatically.

```bash
dotnet run --project Mordecai.AppHost
```

## Testing Environment Variables

### PowerShell (Windows)
```powershell
$env:DATABASE_PATH = "C:\data\mordecai.db"
$env:RABBITMQ_HOST = "localhost"
dotnet run --project Mordecai.Web
```

### Bash (Linux/macOS)
```bash
export DATABASE_PATH=/data/mordecai.db
export RABBITMQ_HOST=localhost
dotnet run --project Mordecai.Web
```

### Inline (Any platform)
```bash
DATABASE_PATH=/data/test.db RABBITMQ_HOST=testhost dotnet run --project Mordecai.Web
```

## Configuration Precedence

1. **Environment Variables** ? Highest priority
2. **appsettings.{Environment}.json**
3. **appsettings.json**
4. **Default Values** ? Lowest priority

## Validation

### Check Current Values

```bash
# Inside container/pod
env | grep DATABASE
env | grep RABBITMQ
env | grep ASPNETCORE
```

### Kubernetes
```bash
# View environment variables in deployment
kubectl describe deployment mordecai-web -n mordecai-mud

# Exec into pod and check
kubectl exec -it -n mordecai-mud <pod-name> -- env | grep DATABASE
```

## Common Patterns

### Local Development
```bash
# No environment variables needed with Aspire
dotnet run --project Mordecai.AppHost
```

### Docker Local Testing
```bash
DATABASE_PATH=./data/mordecai.db \
RABBITMQ_HOST=localhost \
docker-compose up
```

### Kubernetes Development
```yaml
env:
- name: DATABASE_PATH
  value: "/data/mordecai.db"
- name: RABBITMQ_HOST
  value: "rabbitmq-service"
- name: RABBITMQ_USERNAME
  value: "guest"  # Use secrets in production!
- name: RABBITMQ_PASSWORD
  value: "guest"  # Use secrets in production!
```

### Kubernetes Production
```yaml
env:
- name: DATABASE_PATH
  value: "/data/mordecai.db"
- name: RABBITMQ_HOST
  value: "rabbitmq-cluster.rabbitmq-system.svc.cluster.local"
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
```

## Troubleshooting

### Database Not Found
```bash
# Check path
echo $DATABASE_PATH

# Check directory exists
ls -la /data/

# Check permissions
ls -la /data/mordecai.db
```

### RabbitMQ Connection Failed
```bash
# Check host reachable
ping $RABBITMQ_HOST

# Check port open
telnet $RABBITMQ_HOST $RABBITMQ_PORT

# Verify credentials (don't echo password!)
echo $RABBITMQ_USERNAME
```

### View Application Logs
```bash
# Kubernetes
kubectl logs -n mordecai-mud -l app=mordecai-web --tail=50

# Docker
docker logs mordecai-web --tail=50
```

## Security Notes

?? **Never commit secrets to version control**

? **Use Kubernetes secrets:**
```yaml
- name: RABBITMQ_PASSWORD
  valueFrom:
    secretKeyRef:
      name: mordecai-secrets
      key: rabbitmq-password
```

? **Use external secret management:**
- Azure Key Vault
- AWS Secrets Manager
- HashiCorp Vault
- External Secrets Operator

## Related Documentation

- **Full Configuration Guide:** `docs/CONFIGURATION.md`
- **Kubernetes Deployment:** `docs/KUBERNETES_DEPLOYMENT.md`
- **Implementation Summary:** `docs/KUBERNETES_READINESS_SUMMARY.md`

---

**Last Updated:** 2025-01-23  
**Version:** 1.0
