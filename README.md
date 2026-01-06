# Mordecai MUD

A modern, skill-based text MUD built with **Microsoft .NET 9**, **Blazor Server**, and **RabbitMQ**. Designed for a community of 10-50 concurrent players exploring a persistent fantasy world with rich combat, magic, and character progression systems.

## 🎮 About Mordecai

Mordecai combines the classic feel of traditional multi-user dungeons with contemporary web technologies and real-time game mechanics. Players create characters, explore interconnected zones, engage in dynamic combat using Fudge dice (4dF), and develop skills through practice-based progression.

### Key Features

- **Real-time multiplayer** — Concurrent player interactions via SignalR
- **Fudge dice mechanics** — 4dF system for all skill checks and combat
- **Practice-based progression** — Skills improve through use with anti-abuse safeguards
- **Rich combat system** — Melee, ranged, spell casting with skill-based resolution
- **Dynamic world** — NPCs, spawners, world ticks, environmental effects
- **Event-driven architecture** — RabbitMQ powers all asynchronous game events
- **Persistent world** — EF Core with SQLite (dev) / PostgreSQL (production)

## 🚀 Quick Start

### Prerequisites

- **.NET 9 SDK** ([download](https://dotnet.microsoft.com/en-us/download/dotnet/9.0))
- **Docker** (for RabbitMQ container in Aspire orchestration)
- **PowerShell 7+** or **Git Bash** (for running commands)

### Running Locally

The recommended way to run Mordecai locally is with **.NET Aspire**, which automatically orchestrates RabbitMQ, the database, and all services:

```bash
dotnet run --project Mordecai.AppHost
```

Then open your browser to the Aspire dashboard (typically `http://localhost:15000`) to view running services and start the web app.

### Standalone Web App (Manual Setup)

If you prefer to run without Aspire, you'll need to manually set up RabbitMQ and a database:

```bash
# Option 1: Run with SQLite (default)
dotnet run --project Mordecai.Web

# Option 2: Configure PostgreSQL in appsettings.json, then run
dotnet run --project Mordecai.Web
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Mordecai.Web.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Admin CLI Tools

Manage zones, NPCs, spawners, and users:

```bash
dotnet run --project Mordecai.AdminCli -- [command] [args]
```

Common commands: `SeedWorld`, `CreateZone`, `CreateNpcTemplate`, `MakeAdmin`, `SetPassword`

See [Mordecai.AdminCli/README.md](Mordecai.AdminCli/README.md) for detailed command reference.

## 📚 Documentation

All design documentation, specifications, and implementation guides are in the **[docs/](docs/)** folder.

### Start Here

| Document | Purpose |
|----------|---------|
| [QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md) | 📋 **Rapid overview** — tech stack, game systems, development phases |
| [MORDECAI_SPECIFICATION.md](docs/MORDECAI_SPECIFICATION.md) | 🎲 **Complete game design** — mechanics, attributes, skills, combat, magic, formulas |
| [DATABASE_DESIGN.md](docs/DATABASE_DESIGN.md) | 🗄️ **Schema reference** — entities, relationships, EF Core design |

### Deep Dives

| Document | Purpose |
|----------|---------|
| [ITEM_SYSTEM_OVERVIEW.md](docs/ITEM_SYSTEM_OVERVIEW.md) | 🎒 Items, equipment, containers, inventory |
| [CURRENCY_SYSTEM_IMPLEMENTATION.md](docs/CURRENCY_SYSTEM_IMPLEMENTATION.md) | 💰 Currency, prices, wallets |
| [SKILL_PROGRESSION_ANTI_ABUSE.md](docs/SKILL_PROGRESSION_ANTI_ABUSE.md) | 📈 Anti-exploit mechanics, cooldowns |
| [KUBERNETES_DEPLOYMENT.md](docs/KUBERNETES_DEPLOYMENT.md) | ☸️ Production deployment guide |

See [docs/README.md](docs/README.md) for the complete documentation index.

## 🏗️ Project Structure

```
Mordecai.sln
├── Mordecai.AppHost/              ★ Aspire orchestration (local dev entry point)
├── Mordecai.Game/                 Core domain models, entities, game services
├── Mordecai.Messaging/            RabbitMQ message contracts and event definitions
├── Mordecai.BackgroundServices/   Hosted services (world ticks, NPC AI, cleanup)
├── Mordecai.Web/                  Blazor Server UI, components, API controllers
├── Mordecai.ServiceDefaults/      Cross-cutting concerns (OpenTelemetry, config)
├── Mordecai.AdminCli/             Command-line admin tools
├── Mordecai.Web.Tests/            Unit and integration tests (xUnit)
├── docs/                          📚 Design specs, implementation guides
└── tools/                         Development utilities
```

## 🎯 Development Priorities

Mordecai is actively under development with clear phase prioritization:

1. **World Foundation** ✅ — Rooms, movement, descriptions, look/examine, chat
2. **Character System** ✅ — Attributes, 7 core skills, practice-based progression
3. **Item & Equipment Systems** 🔄 — Inventory, equipment slots, bonuses
4. **Combat Foundation** 🔄 — Melee, ranged, spells, skill-based resolution
5. **Admin Content Tools** 📋 — Builder interface for world creation

For current status on specific systems, see [docs/ITEM_IMPLEMENTATION_STATUS.md](docs/ITEM_IMPLEMENTATION_STATUS.md).

## 💻 Technology Stack

| Layer | Technology |
|-------|-----------|
| **Frontend** | Blazor Web App (Server rendering) + SignalR |
| **Backend** | ASP.NET Core 9, EF Core |
| **Database** | SQLite (dev) → PostgreSQL (prod) |
| **Messaging** | RabbitMQ (all async game events) |
| **Orchestration** | .NET Aspire (local dev) |
| **Observability** | OpenTelemetry (tracing, metrics, logs) |
| **Testing** | xUnit |

## 🎲 Game Mechanics Overview

### Fudge Dice (4dF)

All skill checks and combat use Fudge dice for consistent, predictable outcomes:

- **Roll Range**: -4 to +4 (bell-curve distribution heavily weighted toward 0)
- **Combat Formula**: `Attack Skill + 4dF+ vs Defense Skill + 4dF+`
- **Skill Check**: `Character Skill + 4dF+ vs Target Number`

### Attributes & Derived Stats

| Attribute | Purpose |
|-----------|---------|
| **Physicality (STR)** | Physical strength; base for melee skills |
| **Dodge (DEX)** | Agility and evasion |
| **Drive (END)** | Endurance and stamina |
| **Reasoning (INT)** | Intelligence and logic |
| **Awareness (ITT)** | Intuition and perception |
| **Focus (WIL)** | Willpower and concentration |
| **Bearing (PHY)** | Social presence and charisma |

### Health System

- **Vitality (VIT)** = `(STR × 2) - 5` — Physical health pool
- **Fatigue (FAT)** = `(END + WIL) - 5` — Stamina and mental resilience

See [MORDECAI_SPECIFICATION.md](docs/MORDECAI_SPECIFICATION.md) for complete mechanics.

## 🔧 Development Guide

### Architecture Principles

- **Event-driven**: All world changes trigger RabbitMQ messages
- **Domain-driven**: Clear separation of concerns with services in `Mordecai.Game`
- **Message-scoped**: Use appropriate message scopes (Personal/Group/Zone/World) for efficiency
- **Testable**: Pure functions where feasible; side effects isolated and testable
- **Async-first**: All I/O operations are async/await

### Code Style

- **C# 12 / .NET 9 features** — Pattern matching, records, expressions
- **Nullable reference types** — Enabled project-wide
- **Async/await** — Never use `.Result` or `.Wait()`
- **Structured logging** — `ILogger<T>` with contextual information

See [CLAUDE.md](CLAUDE.md) for detailed coding conventions and patterns.

### Adding a Feature

Prefer **vertical slices** when adding new features:

1. **Data Model** — Add or modify entities in `Mordecai.Game/Entities/`
2. **Migration** — Create EF Core migration with descriptive name
3. **Service Logic** — Add business logic in `Mordecai.Game/Services/`
4. **Message Contracts** — Define event types in `Mordecai.Messaging/Messages/`
5. **Event Handling** — Subscribe to messages in services/background services
6. **UI** — Add Blazor components or pages in `Mordecai.Web/`

### Database Migrations

```bash
# Create migration
dotnet ef migrations add DescriptiveName --project Mordecai.Web

# Apply pending migrations
dotnet ef database update --project Mordecai.Web

# Generate SQL script (useful for review/deployment)
dotnet ef migrations script --project Mordecai.Web
```

### Testing

Write unit tests for game logic using xUnit:

```bash
# Run single test class
dotnet test --filter ClassName

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

## 🚢 Deployment

### Local Development
Use **Aspire** to orchestrate all services:
```bash
dotnet run --project Mordecai.AppHost
```

### Production
Deploy to **Kubernetes** using provided manifests:
- See [docs/KUBERNETES_DEPLOYMENT.md](docs/KUBERNETES_DEPLOYMENT.md) for complete guide
- Requires PostgreSQL, RabbitMQ, and Mordecai.Web services

## 🤝 Contributing

When contributing to Mordecai:

1. **Read the specification** — [docs/MORDECAI_SPECIFICATION.md](docs/MORDECAI_SPECIFICATION.md)
2. **Check project conventions** — [CLAUDE.md](CLAUDE.md) for coding standards
3. **Follow development priorities** — Reinforce earlier phases before adding new features
4. **Write tests** — Unit tests for game logic, integration tests for services
5. **Update documentation** — Link to existing docs; avoid duplicating content

## ⚠️ Important Notes

### Game Mechanics

- **Formulas are critical**: Verify against specification before implementing
- **Message scoping**: Always use appropriate scope (Personal/Group/Zone/World)
- **Skill progression**: Must include anti-abuse mechanics (diminishing returns, cooldowns)
- **No raw attribute comparisons**: All resolution must be skill-based

### Common Pitfalls

- ❌ Hardcoding game mechanics (use constants instead)
- ❌ Skipping RabbitMQ for world events (everything must be message-driven)
- ❌ Blocking async operations (no `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`)
- ❌ Ignoring nullable reference type warnings

## 📖 Key References

| Resource | Location |
|----------|----------|
| **Game Specification** | [docs/MORDECAI_SPECIFICATION.md](docs/MORDECAI_SPECIFICATION.md) |
| **Quick Reference** | [docs/QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md) |
| **Architecture Notes** | [CLAUDE.md](CLAUDE.md) |
| **Coding Conventions** | [CLAUDE.md](CLAUDE.md) |
| **Documentation Index** | [docs/README.md](docs/README.md) |
| **Admin CLI Commands** | [Mordecai.AdminCli/README.md](Mordecai.AdminCli/README.md) |

## 📞 Getting Help

- **Game mechanics questions?** → Check [docs/QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md) and [docs/MORDECAI_SPECIFICATION.md](docs/MORDECAI_SPECIFICATION.md)
- **Implementation questions?** → See [CLAUDE.md](CLAUDE.md) and relevant docs in [docs/](docs/)
- **Database schema?** → Refer to [docs/DATABASE_DESIGN.md](docs/DATABASE_DESIGN.md)
- **How do I...?** → Start with [docs/README.md](docs/README.md) for navigation

## 📋 License

[Add your license information here]

---

**Current Status**: Early Development Phase (World Foundation + Character System complete; Item/Combat systems in progress)

**Last Updated**: January 2026
