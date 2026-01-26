# External Integrations

**Analysis Date:** 2026-01-26

## APIs & External Services

**Not Detected:** This codebase does not currently integrate with third-party APIs (Stripe, AWS, etc.). All game systems are self-contained.

## Data Storage

**Databases:**
- **PostgreSQL 12+** (production)
  - Connection: Built from `Database:Host`, `Database:Port`, `Database:Name`, `Database:User`, `Database:Password` configuration
  - Fallback in dev: `localhost:5432` if not configured
  - Client: Npgsql (via `Npgsql.EntityFrameworkCore.PostgreSQL`)
  - ORM: Entity Framework Core 9.0
  - Context: `ApplicationDbContext` in `S:\src\rdl\mordecai-mud\Mordecai.Web\Data\ApplicationDbContext.cs`

- **SQLite** (development only, legacy)
  - Not explicitly configured; EF Core in-memory used for tests

**File Storage:**
- Local filesystem only - No S3, blob storage, or external file services

**Caching:**
- In-memory caching via .NET DI (singleton/scoped services)
- No Redis or distributed caching layer currently configured
- RabbitMQ acts as event broadcast mechanism (not a cache)

## Authentication & Identity

**Auth Provider:**
- Custom ASP.NET Core Identity with Entity Framework Core
- Implementation: `IdentityDbContext` in `S:\src\rdl\mordecai-mud\Mordecai.Web\Data\ApplicationDbContext.cs`
- No OAuth/OpenID providers (Discord, GitHub, etc.)

**User Management:**
- IdentityUser and IdentityRole models
- Configured in `Mordecai.Web\Program.cs` lines 37-47
- Password requirements relaxed for development:
  - No digit requirement
  - Minimum 6 characters
  - No special characters required
  - No uppercase/lowercase requirements
- Roles: `Admin` role configured; others assignable via admin CLI

**Authorization:**
- Policy-based: `AdminOnly` policy requires `Admin` role
- Configured in `Mordecai.Web\Program.cs` line 114-117

## Message Broker

**Service:**
- **RabbitMQ 3.8+** (message-driven events)
  - Configuration:
    - Env var `RABBITMQ_HOST` or config key `RabbitMQ:Host` (default: localhost)
    - Env var `RABBITMQ_PORT` or config key `RabbitMQ:Port` (default: 5672)
    - Env var `RABBITMQ_USERNAME` or config key `RabbitMQ:Username` (default: guest)
    - Config key `RabbitMQ:Password`
    - Config key `RabbitMQ:VirtualHost` (default: /)
  - Client: RabbitMQ.Client 6.2.1
  - Implementation: `S:\src\rdl\mordecai-mud\Mordecai.Messaging\Services\RabbitMqGameMessagePublisher.cs`
  - Exchange: Topic exchange named `mordecai.game.events`
  - Durable: true, auto-delete: false

**Messages Published:**
- Located in `S:\src\rdl\mordecai-mud\Mordecai.Messaging\Messages\`:
  - `ChatMessages.cs` - Chat and communication events
  - `CombatMessages.cs` - Combat action and result events
  - `EnvironmentMessages.cs` - Environmental effects
  - `ItemMessages.cs` - Item pickup, drop, equip events
  - `MovementMessages.cs` - Character movement and room entry/exit
  - `SkillMessages.cs` - Skill usage and learning events
  - `SpawnerMessages.cs` - NPC spawn/despawn events
  - `SystemMessages.cs` - System-level notifications
  - `GameMessage.cs` - Base message contract

**Message Scopes (routing):**
- `MessageScope.Personal` - Only receiving character
- `MessageScope.Group` - Character's group/party
- `MessageScope.Zone` - Zone-wide broadcast
- `MessageScope.World` - Global broadcast

**Graceful Degradation:**
- If RabbitMQ unavailable at startup, application continues with warning (dev mode)
- Retry logic: Connection attempts with exponential backoff
- Subscriber factory: `RabbitMqGameMessageSubscriberFactory` creates subscribers on-demand

## Monitoring & Observability

**Error Tracking:**
- Not detected - No Sentry, DataDog, or similar service

**Logs:**
- Standard .NET logging to console and file (configured per environment)
- Configuration: `Logging:LogLevel` in appsettings files
- Default: Information level
- ASP.NET Core: Warning level (to reduce noise)

**OpenTelemetry Integration:**
- Exporter: OpenTelemetry Protocol (OTLP)
- Instrumentation:
  - ASP.NET Core (request tracing)
  - HTTP client (outbound request tracing)
  - Runtime (performance metrics)
- Implementation: `Mordecai.ServiceDefaults` project
- Packages:
  - `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.12.0
  - `OpenTelemetry.Extensions.Hosting` 1.12.0
  - `OpenTelemetry.Instrumentation.AspNetCore` 1.12.0
  - `OpenTelemetry.Instrumentation.Http` 1.12.0
  - `OpenTelemetry.Instrumentation.Runtime` 1.12.0

**Health Checks:**
- PostgreSQL health check: `AspNetCore.HealthChecks.NpgSql` 9.0.0
- Endpoint: Likely `/health` (standard ASP.NET Core Diagnostics pattern)

## CI/CD & Deployment

**Hosting:**
- Not yet deployed; supports containerization
- Designed for cloud-native via Aspire

**Container Orchestration:**
- Aspire AppHost manages local development
- PostgreSQL and RabbitMQ deployed as container services via Aspire

**CI Pipeline:**
- Not detected - No GitHub Actions, Azure Pipelines, or similar workflows in repo

## Environment Configuration

**Required Environment Variables (Production):**
- `Database:Password` - PostgreSQL password (REQUIRED)
- `RabbitMQ:Host` - RabbitMQ hostname (optional: localhost default in dev)
- `RabbitMQ:Username` - RabbitMQ user (optional: guest default)
- `RabbitMQ:Password` - RabbitMQ password (optional)

**Optional Environment Variables:**
- `Database:Host` (default: localhost)
- `Database:Port` (default: 5432)
- `Database:Name` (default: mordecai)
- `Database:User` (default: mordecaimud)
- `ASPNETCORE_ENVIRONMENT` (Development/Production)
- `RABBITMQ_HOST`, `RABBITMQ_PORT`, `RABBITMQ_USERNAME` (env var aliases)

**Secrets Location:**
- Development: .NET user secrets (ID: `mordecai-mud-secrets`)
  - Access: `dotnet user-secrets list --project Mordecai.Web`
  - Set: `dotnet user-secrets set "Database:Password" "value" --project Mordecai.Web`
- Production: Environment variables or secrets management system

**Configuration Files:**
- `Mordecai.Web\appsettings.json` - Production defaults
- `Mordecai.Web\appsettings.Development.json` - Dev-specific overrides
- `Mordecai.AppHost\appsettings.Development.json` - Aspire orchestration config
- `Mordecai.AdminCli\appsettings.json` - Admin tool config
- Priority: Environment variables > User Secrets > appsettings.{Environment}.json > appsettings.json

## Real-Time Communication

**Blazor Server:**
- Server-side rendering with automatic SignalR connection
- Bidirectional real-time updates to connected clients
- Configuration: `app.MapBlazorHub()` in `Mordecai.Web\Program.cs`
- No explicit SignalR hubs detected - Blazor handles auto-connection

## Webhooks & Callbacks

**Incoming:**
- Not detected

**Outgoing:**
- Not detected

## Data Persistence

**Database Initialization:**
- EF Core migrations applied automatically on app startup
- Location: `context.Database.Migrate()` in `Mordecai.Web\Program.cs` lines 136-152
- Seeding pipeline:
  1. Admin roles and users (`AdminSeedService`)
  2. Room types (`RoomTypeSeedService`)
  3. Skill definitions (`SkillSeedService`)
  4. Data migrations (`DataMigrationService`)

**Entities Persisted:**
- Characters, Zones, Rooms, Room Exits
- Skills, Skill Usage Logs
- Items, Item Templates, Equipment
- NPCs, Spawners, Combat Sessions
- Room Effects, Game Configuration
- Full schema: See `S:\src\rdl\mordecai-mud\Mordecai.Web\Data\ApplicationDbContext.cs` (DbSet definitions, lines 16-56)

## Service Discovery

**Development (Aspire):**
- PostgreSQL auto-discovered at connection time
- RabbitMQ auto-discovered at connection time
- Service names: `default-postgres`, `default-rabbitmq-amqp`

**Production:**
- Requires explicit hostname/port configuration via environment variables

---

*Integration audit: 2026-01-26*
