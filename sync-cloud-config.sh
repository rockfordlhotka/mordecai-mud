#!/bin/bash
# Sync cloud configuration from Web app to AppHost
# This ensures both projects know about your cloud resources

echo "Syncing cloud configuration from Mordecai.Web to Mordecai.AppHost..."

# Get current directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# PostgreSQL Configuration
echo "Setting PostgreSQL configuration..."
cd "$SCRIPT_DIR/Mordecai.AppHost"
dotnet user-secrets set "Database:Host" "default-postgres.tail920062.ts.net"
dotnet user-secrets set "Database:Port" "5432"
dotnet user-secrets set "Database:Name" "mordecai"
dotnet user-secrets set "Database:User" "mordecaimud"

# RabbitMQ Configuration
echo "Setting RabbitMQ configuration..."
dotnet user-secrets set "RabbitMQ:Host" "default-rabbitmq-amqp"
dotnet user-secrets set "RabbitMQ:Port" "5672"
dotnet user-secrets set "RabbitMQ:Username" "mordecai"

echo ""
echo "✅ Configuration synced!"
echo ""
echo "AppHost will now show cloud resources instead of starting local containers."
echo "To verify, run: cd Mordecai.AppHost && dotnet user-secrets list"
echo ""
echo "Now restart your AppHost: dotnet run --project Mordecai.AppHost"
