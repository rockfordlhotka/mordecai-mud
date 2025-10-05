# Sync cloud configuration from Web app to AppHost
# This ensures both projects know about your cloud resources

Write-Host "Syncing cloud configuration from Mordecai.Web to Mordecai.AppHost..." -ForegroundColor Cyan

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# PostgreSQL Configuration
Write-Host "`nSetting PostgreSQL configuration..." -ForegroundColor Yellow
Set-Location "$scriptDir\Mordecai.AppHost"
dotnet user-secrets set "Database:Host" "default-postgres.tail920062.ts.net"
dotnet user-secrets set "Database:Port" "5432"
dotnet user-secrets set "Database:Name" "mordecai"
dotnet user-secrets set "Database:User" "mordecaimud"

# RabbitMQ Configuration
Write-Host "`nSetting RabbitMQ configuration..." -ForegroundColor Yellow
dotnet user-secrets set "RabbitMQ:Host" "default-rabbitmq-amqp"
dotnet user-secrets set "RabbitMQ:Port" "5672"
dotnet user-secrets set "RabbitMQ:Username" "mordecai"

Write-Host "`n✅ Configuration synced!" -ForegroundColor Green
Write-Host "`nAppHost will now show cloud resources instead of starting local containers." -ForegroundColor White
Write-Host "To verify, run: cd Mordecai.AppHost; dotnet user-secrets list" -ForegroundColor Gray
Write-Host "`nNow restart your AppHost: dotnet run --project Mordecai.AppHost" -ForegroundColor Cyan

Set-Location $scriptDir
