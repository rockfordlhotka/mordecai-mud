# Cloud-Native Configuration - Quick Reference

## Setting Database Password

### Development (User Secrets - Recommended)

```bash
# Mordecai.Web
dotnet user-secrets set "Database:Password" "YOUR_PASSWORD" --project Mordecai.Web

# Mordecai.AdminCli
dotnet user-secrets set "Database:Password" "YOUR_PASSWORD" --project Mordecai.AdminCli

# View secrets
dotnet user-secrets list --project Mordecai.Web
```

### Production (Environment Variables)

```bash
# Linux/Mac
export Database__Host="your-db-host.com"
export Database__Port="5432"
export Database__Name="mordecai"
export Database__User="mordecaimud"
export Database__Password="YOUR_PASSWORD"

# Windows PowerShell
$env:Database__Host = "your-db-host.com"
$env:Database__Port = "5432"
$env:Database__Name = "mordecai"
$env:Database__User = "mordecaimud"
$env:Database__Password = "YOUR_PASSWORD"
```

**Note:** Use double underscores (`__`) in environment variables instead of colons (`:`)

## Docker Compose

```yaml
version: '3.8'
services:
  web:
    image: mordecai-web:latest
    environment:
      - Database__Host=postgres
      - Database__Port=5432
      - Database__Name=mordecai
      - Database__User=mordecaimud
      - Database__Password=${DB_PASSWORD}
```

## Kubernetes

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: mordecai-db-secret
stringData:
  password: "YOUR_PASSWORD"
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: mordecai-db-config
data:
  Database__Host: "your-db-host.com"
  Database__Port: "5432"
  Database__Name: "mordecai"
  Database__User: "mordecaimud"
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mordecai-web
spec:
  template:
    spec:
      containers:
      - name: web
        envFrom:
        - configMapRef:
            name: mordecai-db-config
        env:
        - name: Database__Password
          valueFrom:
            secretKeyRef:
              name: mordecai-db-secret
              key: password
```

## Azure App Service

**Via Portal:**
1. Go to Configuration → Application Settings
2. Add:
   - `Database__Host`
   - `Database__Port`
   - `Database__Name`
   - `Database__User`
   - `Database__Password`

**Via CLI:**
```bash
az webapp config appsettings set \
  --resource-group <rg> \
  --name <app-name> \
  --settings \
    Database__Host=your-db.postgres.database.azure.com \
    Database__Port=5432 \
    Database__Name=mordecai \
    Database__User=mordecaimud \
    Database__Password=YOUR_PASSWORD
```

## Configuration Priority

1. **Environment Variables** (highest)
2. **User Secrets** (development)
3. **appsettings.json** (lowest)

## Troubleshooting

**Error: "Database password not configured"**

Solution:
```bash
# Set via User Secrets (dev)
dotnet user-secrets set "Database:Password" "YOUR_PASSWORD" --project Mordecai.Web

# OR set via environment variable
export Database__Password="YOUR_PASSWORD"
```

**Verify configuration:**
```bash
# Check User Secrets
dotnet user-secrets list --project Mordecai.Web

# Check environment variables (Linux/Mac)
env | grep Database__

# Check environment variables (Windows PowerShell)
Get-ChildItem Env: | Where-Object { $_.Name -like "Database*" }
```

## Security Best Practices

✅ **DO:**
- Use User Secrets for local development
- Use environment variables in containers
- Use Key Vault/Secrets Manager in production
- Rotate passwords regularly

❌ **DON'T:**
- Store passwords in appsettings.json
- Commit secrets to source control
- Share secrets via chat/email
- Use same password across environments

---
For detailed information, see [DATABASE_CONFIGURATION.md](./docs/DATABASE_CONFIGURATION.md)
