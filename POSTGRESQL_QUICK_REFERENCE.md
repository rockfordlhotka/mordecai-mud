# PostgreSQL Quick Reference

## Connection Details

```
Server:   default-postgres.tail920062.ts.net
Database: mordecai
User:     mordecaimud
Password: (stored in User Secrets or environment variables)
```

## Configuration Format

### User Secrets (Development)

```bash
dotnet user-secrets set "Database:Password" "Scepter42!" --project Mordecai.Web
```

### Environment Variables (Production)

```bash
export Database__Host="default-postgres.tail920062.ts.net"
export Database__Port="5432"
export Database__Name="mordecai"
export Database__User="mordecaimud"
export Database__Password="Scepter42!"
```

**Note:** Use double underscores (`__`) in environment variables, not colons

## Configuration Files

### Mordecai.Web
- `appsettings.json`
- `appsettings.Development.json`

### Mordecai.AdminCli
- `appsettings.json`

## Common Commands

### EF Core Migrations

```bash
# List migrations
dotnet ef migrations list --project Mordecai.Web

# Add a new migration
dotnet ef migrations add MigrationName --project Mordecai.Web

# Apply migrations
dotnet ef database update --project Mordecai.Web

# Remove last migration (if not applied)
dotnet ef migrations remove --project Mordecai.Web

# Generate SQL script
dotnet ef migrations script --project Mordecai.Web
```

### Database Backup & Restore

```bash
# Backup
pg_dump -h default-postgres.tail920062.ts.net -U mordecaimud -d mordecai > backup.sql

# Restore
psql -h default-postgres.tail920062.ts.net -U mordecaimud -d mordecai < backup.sql

# Backup with timestamp
pg_dump -h default-postgres.tail920062.ts.net -U mordecaimud -d mordecai > "backup_$(date +%Y%m%d_%H%M%S).sql"
```

### Direct Database Access

```bash
# Connect via psql
psql -h default-postgres.tail920062.ts.net -U mordecaimud -d mordecai

# Quick query
psql -h default-postgres.tail920062.ts.net -U mordecaimud -d mordecai -c "SELECT * FROM \"AspNetUsers\""
```

### Common PostgreSQL Queries

```sql
-- List all tables
SELECT tablename FROM pg_tables WHERE schemaname = 'public';

-- Count records in all tables
SELECT schemaname,relname,n_live_tup 
FROM pg_stat_user_tables 
ORDER BY n_live_tup DESC;

-- Show table structure
\d+ "TableName"

-- Show indexes on a table
SELECT * FROM pg_indexes WHERE tablename = 'TableName';

-- View migration history
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

## AdminCli Commands

All commands now work with PostgreSQL:

```bash
# User management
dotnet run --project Mordecai.AdminCli -- list-users
dotnet run --project Mordecai.AdminCli -- make-admin --user admin@example.com
dotnet run --project Mordecai.AdminCli -- set-password --user admin@example.com --generate

# Zone management
dotnet run --project Mordecai.AdminCli -- list-zones
dotnet run --project Mordecai.AdminCli -- create-zone --name "Zone Name" --description "Description"
dotnet run --project Mordecai.AdminCli -- show-zone 1
dotnet run --project Mordecai.AdminCli -- seed-world
```

## Troubleshooting

### Connection Issues

```bash
# Test connection
psql -h default-postgres.tail920062.ts.net -U mordecaimud -d mordecai -c "SELECT version();"

# Check if server is reachable
ping default-postgres.tail920062.ts.net

# Verify connection string in config
cat Mordecai.Web/appsettings.json | grep ConnectionStrings -A 3
```

### Migration Issues

```bash
# Check current migration status
dotnet ef database update --project Mordecai.Web --verbose

# Force re-apply last migration
dotnet ef database update 0 --project Mordecai.Web
dotnet ef database update --project Mordecai.Web
```

### Data Issues

```sql
-- Check if tables exist
SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';

-- Reset sequence (if identity issues)
SELECT setval(pg_get_serial_sequence('"TableName"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "TableName";
```

## Environment Variables (Optional)

Can override connection string using environment variable:

```bash
# Windows (PowerShell)
$env:ConnectionStrings__DefaultConnection = "Host=default-postgres.tail920062.ts.net;Database=mordecai;Username=mordecaimud;Password=Scepter42!"

# Linux/Mac
export ConnectionStrings__DefaultConnection="Host=default-postgres.tail920062.ts.net;Database=mordecai;Username=mordecaimud;Password=Scepter42!"
```

## Important Notes

- ✅ Database uses **case-sensitive table/column names** (quoted identifiers)
- ✅ UUIDs are native PostgreSQL type (not strings)
- ✅ Timestamps include timezone information
- ✅ Decimal precision is enforced (unlike SQLite)
- ⚠️ Connection pooling is active (default 100 connections)
- ⚠️ Password is in plaintext in config files (use User Secrets for dev, Key Vault for prod)

## User Secrets (Recommended for Development)

```bash
# Initialize user secrets
dotnet user-secrets init --project Mordecai.Web

# Set connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=default-postgres.tail920062.ts.net;Database=mordecai;Username=mordecaimud;Password=Scepter42!" --project Mordecai.Web

# List secrets
dotnet user-secrets list --project Mordecai.Web

# Remove a secret
dotnet user-secrets remove "ConnectionStrings:DefaultConnection" --project Mordecai.Web
```

## Performance Tips

```csharp
// Use AsNoTracking for read-only queries
var users = await context.Users.AsNoTracking().ToListAsync();

// Use projections to reduce data transfer
var userNames = await context.Users
    .Select(u => new { u.Id, u.UserName })
    .ToListAsync();

// Use compiled queries for frequently-run queries
// See: https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics
```

## Documentation

- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Migration Details](./docs/POSTGRESQL_MIGRATION.md)

---
For detailed migration information, see [POSTGRESQL_MIGRATION.md](./docs/POSTGRESQL_MIGRATION.md)
