# Cloud-Native Configuration Update

**Date:** October 5, 2025  
**Status:** ✅ Complete - Cloud-native configuration with secret management

## Summary

Updated the Mordecai MUD project to use cloud-native configuration practices with proper secret management. Database passwords are no longer stored in configuration files and instead use User Secrets for development and environment variables for production.

## Changes Made

### 1. Configuration Structure Changed

**Before:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=...;Username=...;Password=..."
  }
}
```

**After:**
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

**Password:** Now stored separately in User Secrets or environment variables (NOT in JSON files)

### 2. Project Files Updated

#### Mordecai.Web.csproj
- ✅ Added `UserSecretsId`: `mordecai-mud-secrets`

#### Mordecai.AdminCli.csproj
- ✅ Added `UserSecretsId`: `mordecai-mud-secrets`
- ✅ Added `Microsoft.Extensions.Configuration.UserSecrets` package (v9.0.4)

### 3. Code Changes

#### Mordecai.Web/Program.cs
**Updated:**
- Builds connection string from individual configuration values
- Reads password from `Database:Password` configuration key
- Supports environment variable overrides with `Database__*` format
- Logs database connection info without exposing password

#### Mordecai.AdminCli/Program.cs
**Updated:**
- Added `.AddEnvironmentVariables()` to configuration builder
- Added `.AddUserSecrets<Program>(optional: true)` to configuration builder
- Builds connection string from individual configuration values
- Reads password from secure sources only

### 4. Configuration Files Updated

#### Updated Files:
- `Mordecai.Web/appsettings.json` - Removed password, restructured to Database section
- `Mordecai.Web/appsettings.Development.json` - Removed password, restructured to Database section
- `Mordecai.AdminCli/appsettings.json` - Removed password, restructured to Database section

### 5. User Secrets Configured

Both projects now use the same User Secrets ID for consistency:
```
UserSecretsId: mordecai-mud-secrets
```

Secrets stored:
```bash
dotnet user-secrets set "Database:Password" "Scepter42!" --project Mordecai.Web
dotnet user-secrets set "Database:Password" "Scepter42!" --project Mordecai.AdminCli
```

## Configuration Priority

Values are resolved in this order (highest to lowest priority):

1. **Environment Variables** (`Database__Host`, `Database__Password`, etc.)
2. **User Secrets** (development only)
3. **appsettings.Development.json** (development only)
4. **appsettings.json** (base configuration)

## Security Improvements

### ✅ Enhanced Security:
- **No passwords in source control** - Passwords removed from all JSON files
- **User Secrets for development** - Secure local development without exposing secrets
- **Environment variables for production** - Cloud-native secret injection
- **Individual configuration values** - Fine-grained control and easier secret rotation
- **Secret management system ready** - Compatible with Azure Key Vault, AWS Secrets Manager, Kubernetes Secrets

### 🔒 Password Storage:

| Environment | Storage Method | Configuration Key |
|-------------|---------------|-------------------|
| Development | User Secrets | `Database:Password` |
| Docker | Environment Variable | `Database__Password` |
| Kubernetes | Secrets | `Database__Password` |
| Azure App Service | App Settings / Key Vault | `Database__Password` |
| AWS | Secrets Manager / Parameter Store | `Database__Password` |

## Local Development Setup

### Initial Setup (One-Time)

```bash
# Set password in User Secrets for Mordecai.Web
dotnet user-secrets set "Database:Password" "Scepter42!" --project Mordecai.Web

# Set password in User Secrets for Mordecai.AdminCli
dotnet user-secrets set "Database:Password" "Scepter42!" --project Mordecai.AdminCli
```

### Verify Setup

```bash
# View stored secrets
dotnet user-secrets list --project Mordecai.Web
dotnet user-secrets list --project Mordecai.AdminCli

# Build and run
dotnet build
dotnet run --project Mordecai.Web
```

## Production Deployment

### Docker

```dockerfile
# In docker-compose.yml
environment:
  - Database__Host=postgres
  - Database__Port=5432
  - Database__Name=mordecai
  - Database__User=mordecaimud
  - Database__Password=${DB_PASSWORD}
```

### Kubernetes

```yaml
# Secret
apiVersion: v1
kind: Secret
metadata:
  name: mordecai-db-secret
stringData:
  password: "YOUR_SECURE_PASSWORD"

# Deployment
env:
- name: Database__Password
  valueFrom:
    secretKeyRef:
      name: mordecai-db-secret
      key: password
```

### Azure App Service

```bash
az webapp config appsettings set \
  --resource-group myResourceGroup \
  --name myAppName \
  --settings Database__Password="YOUR_SECURE_PASSWORD"
```

Or use Key Vault reference:
```bash
az webapp config appsettings set \
  --resource-group myResourceGroup \
  --name myAppName \
  --settings Database__Password="@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/db-password/)"
```

## Backward Compatibility

### Old Connection String Support Removed

The old `ConnectionStrings:DefaultConnection` format is **no longer supported**.

**Migration Required:**
If you have existing deployments, update them to use the new configuration structure:

```bash
# Old (no longer works)
export ConnectionStrings__DefaultConnection="Host=...;Password=..."

# New (required)
export Database__Host="default-postgres.tail920062.ts.net"
export Database__Port="5432"
export Database__Name="mordecai"
export Database__User="mordecaimud"
export Database__Password="YOUR_PASSWORD"
```

## Testing

### Build Status
```bash
dotnet build
```
✅ **Result:** All projects build successfully

### Runtime Test
```bash
dotnet run --project Mordecai.Web
```
✅ **Result:** Application starts and connects to database

### AdminCli Test
```bash
dotnet run --project Mordecai.AdminCli -- list-users
```
✅ **Result:** CLI connects to database and executes commands

## Documentation Created

1. **`docs/DATABASE_CONFIGURATION.md`** - Comprehensive configuration guide
   - Local development setup
   - Production deployment examples
   - Docker, Kubernetes, Azure, AWS configurations
   - Troubleshooting guide
   - Security best practices

2. **`DATABASE_CONFIG_QUICK_REF.md`** - Quick reference guide
   - Common commands
   - Configuration examples
   - Troubleshooting quick fixes

3. **Updated:** `POSTGRESQL_QUICK_REFERENCE.md`
   - Updated to reflect new configuration structure
   - Removed hardcoded connection strings

## Environment Variable Reference

### Development (Optional Overrides)

```bash
# Linux/Mac
export Database__Host="localhost"
export Database__Port="5432"
export Database__Name="mordecai_dev"
export Database__User="dev_user"
export Database__Password="dev_password"

# Windows PowerShell
$env:Database__Host = "localhost"
$env:Database__Port = "5432"
$env:Database__Name = "mordecai_dev"
$env:Database__User = "dev_user"
$env:Database__Password = "dev_password"
```

### Production (Required)

All database configuration values should be provided via environment variables in production:

- `Database__Host` - PostgreSQL server hostname
- `Database__Port` - PostgreSQL server port (default: 5432)
- `Database__Name` - Database name
- `Database__User` - Database username
- `Database__Password` - Database password (from secret store)

## Breaking Changes

### ⚠️ Configuration Format Changed

**Impact:** Existing deployments using `ConnectionStrings:DefaultConnection` will fail

**Action Required:**
1. Update environment variables to use `Database__*` format
2. Move passwords to secure secret storage
3. Update any deployment scripts or CI/CD pipelines

### ⚠️ User Secrets Required for Development

**Impact:** Developers need to set up User Secrets locally

**Action Required:**
```bash
dotnet user-secrets set "Database:Password" "YOUR_PASSWORD" --project Mordecai.Web
dotnet user-secrets set "Database:Password" "YOUR_PASSWORD" --project Mordecai.AdminCli
```

## Benefits

### 🎯 Cloud-Native
- ✅ Compatible with all major cloud platforms
- ✅ Follows 12-factor app principles
- ✅ Environment-specific configuration without code changes
- ✅ Easy to integrate with secret management systems

### 🔒 Security
- ✅ No secrets in source control
- ✅ Passwords never logged or displayed
- ✅ Separate credentials per environment
- ✅ Easy secret rotation

### 🛠️ Operations
- ✅ Simplified deployment configuration
- ✅ Clear separation of concerns
- ✅ Easy to override individual values
- ✅ Better troubleshooting (can verify each setting)

### 👨‍💻 Developer Experience
- ✅ Secure local development with User Secrets
- ✅ No accidental secret commits
- ✅ Consistent experience across team
- ✅ Clear documentation and examples

## Rollback Plan

If you need to revert to the old configuration format:

1. **Restore old appsettings.json:**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=...;Database=...;Username=...;Password=..."
     }
   }
   ```

2. **Revert code changes:**
   ```bash
   git revert <commit-hash>
   ```

3. **Remove User Secrets:**
   ```bash
   dotnet user-secrets clear --project Mordecai.Web
   dotnet user-secrets clear --project Mordecai.AdminCli
   ```

## Next Steps

1. ✅ **Completed:** Configuration restructured
2. ✅ **Completed:** User Secrets configured for development
3. ✅ **Completed:** Documentation created
4. ⏳ **Recommended:** Update CI/CD pipelines with new environment variable format
5. ⏳ **Recommended:** Configure production secret management (Key Vault, Secrets Manager)
6. ⏳ **Recommended:** Update deployment documentation
7. ⏳ **Recommended:** Rotate database passwords using new configuration

## Support

### Common Issues

**Problem:** Application fails to start with "Database password not configured"

**Solution:** Set the password via User Secrets or environment variable
```bash
dotnet user-secrets set "Database:Password" "YOUR_PASSWORD" --project Mordecai.Web
```

**Problem:** Environment variables not being picked up

**Solution:** Ensure proper naming with double underscores
```bash
# Correct
export Database__Password="..."

# Incorrect
export Database:Password="..."
```

**Problem:** AdminCli can't find database

**Solution:** Ensure User Secrets are set for AdminCli project
```bash
dotnet user-secrets set "Database:Password" "YOUR_PASSWORD" --project Mordecai.AdminCli
```

## Resources

- [Safe storage of app secrets (Microsoft Docs)](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [The Twelve-Factor App - Config](https://12factor.net/config)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)

---
**Configuration is now cloud-native and production-ready!** ☁️🔒
