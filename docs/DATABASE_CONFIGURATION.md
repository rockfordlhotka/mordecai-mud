# Database Configuration - Cloud-Native Setup

**Updated:** October 5, 2025  
**Status:** ✅ Cloud-native configuration with secret management

## Overview

The Mordecai MUD project uses a cloud-native database configuration approach that separates sensitive data (passwords) from application configuration and supports environment variable overrides for containerized deployments.

## Configuration Hierarchy

Configuration values are loaded in this priority order (highest to lowest):

1. **Environment Variables** (highest priority - for production/containers)
2. **User Secrets** (for local development)
3. **appsettings.json** (for non-sensitive defaults)

## Configuration Structure

### Database Settings

All database connection parameters are configured individually:

```json
{
  "Database": {
    "Host": "default-postgres.tail920062.ts.net",
    "Port": "5432",
    "Name": "mordecai",
    "User": "mordecaimud"
  }
}
```

**Note:** The password is **NOT** stored in `appsettings.json` for security reasons.

## Local Development Setup

### Option 1: User Secrets (Recommended)

User Secrets store sensitive data outside of your project directory and are never committed to source control.

#### Initialize User Secrets (if not already done)

```bash
# For Mordecai.Web
dotnet user-secrets init --project Mordecai.Web

# For Mordecai.AdminCli
dotnet user-secrets init --project Mordecai.AdminCli
```

Both projects share the same `UserSecretsId`: `mordecai-mud-secrets`

#### Set the Database Password

```bash
# For Mordecai.Web
dotnet user-secrets set "Database:Password" "Scepter42!" --project Mordecai.Web

# For Mordecai.AdminCli
dotnet user-secrets set "Database:Password" "Scepter42!" --project Mordecai.AdminCli
```

#### View Current Secrets

```bash
# For Mordecai.Web
dotnet user-secrets list --project Mordecai.Web

# For Mordecai.AdminCli
dotnet user-secrets list --project Mordecai.AdminCli
```

#### Remove a Secret

```bash
dotnet user-secrets remove "Database:Password" --project Mordecai.Web
```

#### Clear All Secrets

```bash
dotnet user-secrets clear --project Mordecai.Web
```

### Option 2: Environment Variables (Alternative)

You can also use environment variables for local development:

**Windows (PowerShell):**
```powershell
$env:Database__Password = "Scepter42!"
```

**Windows (Command Prompt):**
```cmd
set Database__Password=Scepter42!
```

**Linux/Mac:**
```bash
export Database__Password="Scepter42!"
```

**Note:** Use double underscores (`__`) in environment variables to represent nested configuration (`:` in JSON).

## Production/Container Setup

### Environment Variables

For production deployments (Docker, Kubernetes, Azure, AWS, etc.), use environment variables:

```bash
Database__Host=default-postgres.tail920062.ts.net
Database__Port=5432
Database__Name=mordecai
Database__User=mordecaimud
Database__Password=<secret-password>
```

### Docker

#### Docker Compose Example

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
      - Database__Password=${DB_PASSWORD}  # From .env file or system env
    depends_on:
      - postgres
  
  postgres:
    image: postgres:16
    environment:
      - POSTGRES_DB=mordecai
      - POSTGRES_USER=mordecaimud
      - POSTGRES_PASSWORD=${DB_PASSWORD}
```

#### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Mordecai.Web/Mordecai.Web.csproj", "Mordecai.Web/"]
RUN dotnet restore "Mordecai.Web/Mordecai.Web.csproj"
COPY . .
WORKDIR "/src/Mordecai.Web"
RUN dotnet build "Mordecai.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Mordecai.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mordecai.Web.dll"]
```

### Kubernetes

#### Using Secrets

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: mordecai-db-secret
type: Opaque
stringData:
  password: "Scepter42!"  # Replace with actual password
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: mordecai-db-config
data:
  Database__Host: "default-postgres.tail920062.ts.net"
  Database__Port: "5432"
  Database__Name: "mordecai"
  Database__User: "mordecaimud"
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mordecai-web
spec:
  replicas: 3
  selector:
    matchLabels:
      app: mordecai-web
  template:
    metadata:
      labels:
        app: mordecai-web
    spec:
      containers:
      - name: web
        image: mordecai-web:latest
        ports:
        - containerPort: 80
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

#### Using External Secrets Operator

For production environments, consider using External Secrets Operator with Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault:

```yaml
apiVersion: external-secrets.io/v1beta1
kind: SecretStore
metadata:
  name: azure-keyvault
spec:
  provider:
    azurekv:
      tenantId: "<tenant-id>"
      vaultUrl: "https://<keyvault-name>.vault.azure.net"
      authSecretRef:
        clientId:
          name: azure-secret
          key: client-id
        clientSecret:
          name: azure-secret
          key: client-secret
---
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: mordecai-db-password
spec:
  refreshInterval: 1h
  secretStoreRef:
    name: azure-keyvault
    kind: SecretStore
  target:
    name: mordecai-db-secret
  data:
  - secretKey: password
    remoteRef:
      key: mordecai-db-password
```

### Azure App Service

Azure App Service provides built-in configuration support:

1. **Navigate to:** Azure Portal → Your App Service → Configuration
2. **Add Application Settings:**
   - `Database__Host` = `default-postgres.tail920062.ts.net`
   - `Database__Port` = `5432`
   - `Database__Name` = `mordecai`
   - `Database__User` = `mordecaimud`
   - `Database__Password` = `<password>` (click "Key Vault Reference" for secure storage)

#### Using Azure Key Vault with App Service

```bash
# Create Key Vault secret
az keyvault secret set --vault-name <vault-name> \
  --name mordecai-db-password \
  --value "Scepter42!"

# Get the secret URI
az keyvault secret show --vault-name <vault-name> \
  --name mordecai-db-password \
  --query id -o tsv

# Set in App Service (use the URI from above)
az webapp config appsettings set \
  --resource-group <resource-group> \
  --name <app-name> \
  --settings Database__Password="@Microsoft.KeyVault(SecretUri=<secret-uri>)"
```

### AWS Elastic Beanstalk

Set environment variables via the AWS Console or CLI:

```bash
aws elasticbeanstalk update-environment \
  --environment-name mordecai-env \
  --option-settings \
    Namespace=aws:elasticbeanstalk:application:environment,OptionName=Database__Host,Value=default-postgres.tail920062.ts.net \
    Namespace=aws:elasticbeanstalk:application:environment,OptionName=Database__Port,Value=5432 \
    Namespace=aws:elasticbeanstalk:application:environment,OptionName=Database__Name,Value=mordecai \
    Namespace=aws:elasticbeanstalk:application:environment,OptionName=Database__User,Value=mordecaimud \
    Namespace=aws:elasticbeanstalk:application:environment,OptionName=Database__Password,Value=<password>
```

## Configuration Override Examples

### Override Just the Password

```bash
export Database__Password="NewPassword123!"
# All other values come from appsettings.json
```

### Override Everything

```bash
export Database__Host="production-db.example.com"
export Database__Port="5432"
export Database__Name="mordecai_prod"
export Database__User="prod_user"
export Database__Password="SecurePassword456!"
```

### Mixed Configuration

```bash
# Use appsettings.json for Host, Port, Name, User
# Override only the password
export Database__Password="DevPassword789!"
```

## Troubleshooting

### Password Not Found Error

If you see:
```
Database password not configured. Set Database:Password in User Secrets or environment variable.
```

**Solution:**
1. Set the password via User Secrets (recommended for dev):
   ```bash
   dotnet user-secrets set "Database:Password" "YourPassword" --project Mordecai.Web
   ```

2. Or set via environment variable:
   ```bash
   export Database__Password="YourPassword"  # Linux/Mac
   $env:Database__Password = "YourPassword"   # PowerShell
   ```

### Connection Failed

If the database connection fails:

1. **Verify all configuration values:**
   ```bash
   # Check User Secrets
   dotnet user-secrets list --project Mordecai.Web
   
   # Check environment variables (Linux/Mac)
   env | grep Database__
   
   # Check environment variables (PowerShell)
   Get-ChildItem Env: | Where-Object { $_.Name -like "Database*" }
   ```

2. **Test the connection manually:**
   ```bash
   psql -h default-postgres.tail920062.ts.net -U mordecaimud -d mordecai
   ```

3. **Check the logs:**
   Look for the startup message showing which database is being used (password is NOT logged).

## Security Best Practices

### ✅ DO:
- Use User Secrets for local development
- Use environment variables in containers
- Use secret management systems (Key Vault, Secrets Manager) in production
- Use separate passwords for dev/staging/production
- Rotate passwords regularly
- Use managed identities when available (Azure, AWS)

### ❌ DON'T:
- Store passwords in `appsettings.json`
- Commit `appsettings.json` with passwords to source control
- Share secrets via chat/email
- Use the same password across environments
- Log connection strings with passwords

## Migration from Previous Setup

If you're migrating from the old connection string format:

### Old Format (ConnectionStrings)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=...;Username=...;Password=..."
  }
}
```

### New Format (Individual Settings)
```json
{
  "Database": {
    "Host": "...",
    "Port": "5432",
    "Name": "...",
    "User": "..."
  }
}
```

**Password:** Set via User Secrets or environment variable (NOT in JSON)

## Configuration File Locations

### User Secrets Location

**Windows:**
```
%APPDATA%\Microsoft\UserSecrets\mordecai-mud-secrets\secrets.json
```

**Linux/Mac:**
```
~/.microsoft/usersecrets/mordecai-mud-secrets/secrets.json
```

### appsettings.json Locations

- `Mordecai.Web/appsettings.json` - Web application configuration
- `Mordecai.Web/appsettings.Development.json` - Development overrides
- `Mordecai.AdminCli/appsettings.json` - CLI tool configuration

## Additional Resources

- [Safe storage of app secrets in development in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Use Azure Key Vault secrets in Azure Pipelines](https://learn.microsoft.com/en-us/azure/devops/pipelines/release/key-vault-in-own-project)
- [Kubernetes Secrets](https://kubernetes.io/docs/concepts/configuration/secret/)
- [External Secrets Operator](https://external-secrets.io/)

---
**Configuration is secure and ready for cloud deployment!** 🔒
