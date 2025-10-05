# FIXED: AppHost Now Configured for Cloud Resources

## The Problem
Your **Web app** had cloud configuration in `appsettings.json`, but the **AppHost** didn't have the same configuration in its user secrets, so it was starting local containers instead.

## What I Did

Synced your cloud configuration from the Web app to the AppHost user secrets:

### PostgreSQL
- Host: `default-postgres.tail920062.ts.net`
- Port: `5432`
- Database: `mordecai`
- User: `mordecaimud`

### RabbitMQ
- Host: `default-rabbitmq-amqp`
- Port: `5672`
- Username: `mordecai`

## Next Steps

**Restart your AppHost** - it will now detect the cloud configuration and show external resources:

```bash
dotnet run --project Mordecai.AppHost
```

You should now see in the dashboard:
- ☁️ **postgresql** - External resource pointing to `default-postgres.tail920062.ts.net`
- ☁️ **messaging** - External resource pointing to `default-rabbitmq-amqp`
- 🌐 **webfrontend** - Your web app (with health checks monitoring cloud resources)

**No more local containers** for postgres, pgadmin, or rabbitmq!

## For Future Reference

I created two helper scripts to sync configuration:

### Bash
```bash
./sync-cloud-config.sh
```

### PowerShell
```powershell
.\sync-cloud-config.ps1
```

These scripts copy your cloud configuration from the Web app to the AppHost.

## Why Both Need Configuration?

- **AppHost** configuration → Controls what appears in the dashboard (local containers vs external resources)
- **Web app** configuration → Actual connection details for your application

They need to match so the dashboard shows the resources your app is actually using.

## Verify Configuration

Check AppHost has cloud config:
```bash
cd Mordecai.AppHost
dotnet user-secrets list | grep -E "(Database|RabbitMQ)"
```

Should show:
- `Database:Host = default-postgres.tail920062.ts.net`
- `Database:Port = 5432`
- `Database:Name = mordecai`
- `Database:User = mordecaimud`
- `RabbitMQ:Host = default-rabbitmq-amqp`
- `RabbitMQ:Port = 5672`
- `RabbitMQ:Username = mordecai`

## Switching Back to Local

To use local containers instead, remove the cloud configuration:

```bash
cd Mordecai.AppHost
dotnet user-secrets remove "Database:Host"
dotnet user-secrets remove "RabbitMQ:Host"
```

Then restart the AppHost - it will start local containers.
