# Technology Stack

**Analysis Date:** 2026-01-26

## Languages

**Primary:**
- C# (.NET 9.0) - Used across all projects, with nullable reference types enabled
- JavaScript/TypeScript - Implicit in Blazor components (server-side rendering)

## Runtime

**Environment:**
- .NET 9.0 runtime
- CLR (Common Language Runtime) for all server-side execution

**Package Manager:**
- NuGet (implicit in .csproj files)
- Lockfile: Generated automatically by .NET

## Frameworks

**Core:**
- ASP.NET Core 9.0 - Web framework for server-side rendering
- Blazor Server 9.0 - Server-side UI framework with real-time updates
- Entity Framework Core 9.0 - ORM for data access
- .NET Aspire 9.3.1/9.5.1 - Orchestration and service discovery

**Testing:**
- xUnit 3.0.0 - Test framework in `Mordecai.Web.Tests`
- Microsoft.NET.Test.Sdk 17.14.1 - Test runner and utilities
- Microsoft.EntityFrameworkCore.InMemory 9.0.0 - In-memory database for tests

**Build/Dev:**
- Aspire.AppHost (as SDK via Mordecai.AppHost)
- Aspire.Hosting.PostgreSQL 9.5.1 - PostgreSQL container orchestration
- Aspire.Hosting.RabbitMQ 9.5.1 - RabbitMQ container orchestration
- Microsoft.EntityFrameworkCore.Tools 9.0.0 - Migration CLI tools

## Key Dependencies

**Critical:**
- RabbitMQ.Client 6.2.1 - Message broker client for event-driven architecture, used in `Mordecai.Messaging`
- Npgsql.EntityFrameworkCore.PostgreSQL 9.0.2 - PostgreSQL provider for EF Core
- Microsoft.AspNetCore.Identity.EntityFrameworkCore 9.0.0 - User authentication/authorization
- Microsoft.Extensions.* 9.0.0+ - DI, configuration, logging abstractions

**Infrastructure:**
- OpenTelemetry.Exporter.OpenTelemetryProtocol 1.12.0 - Observability export
- OpenTelemetry.Extensions.Hosting 1.12.0 - Hosting integration
- OpenTelemetry.Instrumentation.AspNetCore 1.12.0 - ASP.NET Core tracing
- OpenTelemetry.Instrumentation.Http 1.12.0 - HTTP client tracing
- OpenTelemetry.Instrumentation.Runtime 1.12.0 - Runtime metrics
- Microsoft.Extensions.Http.Resilience 9.4.0 - Resilience patterns for HTTP
- Microsoft.Extensions.ServiceDiscovery 9.3.1 - Service discovery (Aspire integration)
- AspNetCore.HealthChecks.NpgSql 9.0.0 - PostgreSQL health checks

**Admin/CLI:**
- Spectre.Console 0.49.1 - Rich terminal UI for admin CLI
- Spectre.Console.Cli 0.49.1 - Command-line parsing framework

## Configuration

**Environment:**
- Configuration hierarchy (highest priority first):
  1. Environment variables (RABBITMQ_HOST, RABBITMQ_USERNAME, RABBITMQ_PORT, DATABASE_PASSWORD, etc.)
  2. User Secrets (.NET user secrets for local development, ID: `mordecai-mud-secrets`)
  3. appsettings.Development.json or appsettings.json

**Build:**
- `Mordecai.sln` - Solution file
- `.csproj` files per project with PropertyGroup settings:
  - TargetFramework: net9.0
  - Nullable: enable
  - ImplicitUsings: enable
  - UserSecretsId: mordecai-mud-secrets (Web projects)

**Key Configuration Keys:**
- `Database:Host` - PostgreSQL hostname
- `Database:Port` - PostgreSQL port (default: 5432)
- `Database:Name` - Database name (default: mordecai)
- `Database:User` - Database username (default: mordecaimud)
- `Database:Password` - Database password (REQUIRED, no default)
- `RabbitMQ:Host` - RabbitMQ hostname (or RABBITMQ_HOST)
- `RabbitMQ:Port` - RabbitMQ AMQP port (default: 5672)
- `RabbitMQ:Username` - RabbitMQ user (default: guest)
- `RabbitMQ:Password` - RabbitMQ password
- `RabbitMQ:VirtualHost` - RabbitMQ virtual host (default: /)

## Platform Requirements

**Development:**
- .NET 9.0 SDK
- PostgreSQL 12+ (or use Aspire hosting)
- RabbitMQ 3.8+ (or use Aspire hosting)
- Visual Studio 2022+ (recommended) or Visual Studio Code with C# extension

**Production:**
- .NET 9.0 runtime
- PostgreSQL 12+ (production instance)
- RabbitMQ 3.8+ (production instance or cluster)
- Docker container support (via Aspire/standard container)

## Project Structure and Dependencies

**Mordecai.sln:**
```
Mordecai.AppHost
├── Aspire.Hosting.AppHost (9.3.1)
├── Aspire.Hosting.PostgreSQL (9.5.1)
├── Aspire.Hosting.RabbitMQ (9.5.1)
└── References Mordecai.Web

Mordecai.Web (main web project)
├── Mordecai.Game
├── Mordecai.Data
├── Mordecai.Messaging
├── Mordecai.BackgroundServices
├── Mordecai.ServiceDefaults
├── Microsoft.AspNetCore.Identity.* (9.0.0)
├── Npgsql.EntityFrameworkCore.PostgreSQL (9.0.2)
├── Microsoft.EntityFrameworkCore.Tools (9.0.0)
└── Microsoft.VisualStudio.Web.CodeGeneration.Design (9.0.0)

Mordecai.Game (domain models and services)
├── Microsoft.EntityFrameworkCore (9.0.0)
├── Microsoft.Extensions.Configuration.* (9.0.0)
├── Microsoft.Extensions.Logging.Abstractions (9.0.0)
├── Microsoft.Extensions.DependencyInjection.Abstractions (9.0.0)
└── System.ComponentModel.Annotations (5.0.0)

Mordecai.Data (persistence layer)
└── (No direct NuGet deps, uses EF Core via Game project)

Mordecai.Messaging (event publishing/subscribing)
├── Microsoft.Extensions.* (9.0.0)
├── System.Text.Json (9.0.0)
├── RabbitMQ.Client (6.2.1)
└── References Mordecai.Game

Mordecai.BackgroundServices (hosted background services)
├── Microsoft.Extensions.Hosting.Abstractions (9.0.0)
└── References Mordecai.Game

Mordecai.ServiceDefaults (cross-cutting setup)
├── AspNetCore.HealthChecks.NpgSql (9.0.0)
├── Microsoft.Extensions.Http.Resilience (9.4.0)
├── Microsoft.Extensions.ServiceDiscovery (9.3.1)
├── OpenTelemetry.* (1.12.0)
└── RabbitMQ.Client (6.2.1)

Mordecai.AdminCli (command-line admin tool)
├── Microsoft.AspNetCore.Identity.EntityFrameworkCore (9.0.0)
├── Npgsql.EntityFrameworkCore.PostgreSQL (9.0.2)
├── Microsoft.Extensions.Configuration.* (9.0.4)
├── Microsoft.Extensions.Hosting (9.0.4)
├── Microsoft.Extensions.Logging (9.0.4)
├── Spectre.Console (0.49.1)
├── Spectre.Console.Cli (0.49.1)
└── References Mordecai.Game, Mordecai.Web

Mordecai.Web.Tests (test project)
├── Microsoft.NET.Test.Sdk (17.14.1)
├── xunit.v3 (3.0.0)
├── xunit.runner.visualstudio (3.1.3)
├── Microsoft.EntityFrameworkCore.InMemory (9.0.0)
└── References Mordecai.Web
```

## Notable Implementation Details

**Dependency Injection:**
- Service defaults configured via `builder.AddServiceDefaults()` in `Mordecai.Web\Program.cs`
- Pooled DbContext factory for connection efficiency: `AddPooledDbContextFactory<ApplicationDbContext>`
- Scoped resolver pattern for Identity services that need DbContext

**Messaging Integration:**
- RabbitMQ connection configured via IConfiguration
- Topic exchange named `mordecai.game.events`
- Graceful fallback if RabbitMQ unavailable in development
- JSON serialization with camelCase naming

**OpenTelemetry:**
- Spans exported via OTLP (OpenTelemetry Protocol)
- Includes ASP.NET Core, HTTP, and runtime instrumentation
- Integrated via ServiceDefaults project

**Aspire Orchestration:**
- Development environment uses Aspire to spin up PostgreSQL and RabbitMQ containers
- Service discovery enabled via `Microsoft.Extensions.ServiceDiscovery`
- Health checks configured for PostgreSQL

---

*Stack analysis: 2026-01-26*
