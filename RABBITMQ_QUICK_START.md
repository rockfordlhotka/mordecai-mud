# RabbitMQ Configuration Quick Reference

> **⚠️ SECURITY WARNING:** Replace `<your-secure-password>` with your actual password only in User Secrets or Kubernetes Secrets. Never commit passwords to source control.

## Quick Start - Local Development

### 1. Set User Secrets (One-time setup)

```bash
# Web Application
cd Mordecai.Web
dotnet user-secrets set "RabbitMQ:Password" "<your-secure-password>"

# Admin CLI
cd Mordecai.AdminCli
dotnet user-secrets set "RabbitMQ:Password" "<your-secure-password>"
```

### 2. Run the Application

```bash
cd Mordecai.Web
dotnet run
```

The application will connect to: `default-rabbitmq-amqp:5672` with user `mordecai`

## Quick Start - Kubernetes Deployment

### 1. Create Secret

```bash
kubectl create secret generic rabbitmq-credentials \
  --from-literal=password='<your-secure-password>'
```

### 2. Add Environment Variables to Deployment

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

## Troubleshooting

### Check Configuration
```bash
# List User Secrets
dotnet user-secrets list --project Mordecai.Web

# Check K8s Secret
kubectl get secret rabbitmq-credentials -o yaml
```

### Common Errors

| Error | Solution |
|-------|----------|
| RabbitMQ password not configured | Run: `dotnet user-secrets set "RabbitMQ:Password" "<your-password>"` |
| Connection refused | Verify: `kubectl get svc | grep rabbitmq` |
| Authentication failed | Check password in User Secrets or K8s secret |

## Configuration Priority

1. **RABBITMQ_HOST** env var → RabbitMQ:Host in config
2. **RABBITMQ_USERNAME** env var → RabbitMQ:Username in config
3. **RABBITMQ_PASSWORD** env var → RabbitMQ:Password in User Secrets

## Connection Details

- **Host**: default-rabbitmq-amqp
- **Port**: 5672
- **User**: mordecai
- **Password**: (stored securely in User Secrets/K8s Secret)
- **VHost**: /

## What Changed?

✅ Removed Aspire RabbitMQ integration  
✅ Direct connection to K8s RabbitMQ  
✅ Cloud-native configuration (env vars + secrets)  
✅ Password stored securely (never in source control)  

## Full Documentation

See [RABBITMQ_KUBERNETES_MIGRATION.md](RABBITMQ_KUBERNETES_MIGRATION.md) for complete details.
