# CLAUDE.MD - Mordecai MUD Project

## Project Overview

**Mordecai** is a skill-based, real-time text-based Multi-User Dungeon (MUD) built with modern .NET technologies. This is a comprehensive fantasy game featuring Fudge dice mechanics (4dF), practice-based skill progression, and rich multiplayer interactions.

### Key Characteristics
- **Genre**: Text-based MUD with classic feel and modern technology
- **Scale**: Designed for 10-50 concurrent players (intimate community)
- **Game Mechanics**: 4dF (Fudge/Fate dice) for all skill checks and combat
- **Persistence**: Real-time world simulation with practice-based skill advancement

## Technology Stack

### Core Technologies
- **.NET Version**: .NET 9
- **Frontend**: Blazor Web App (Server rendering mode)
- **Backend**: ASP.NET Core, EF Core
- **Database**: SQLite (development) → PostgreSQL (production)
- **Messaging**: RabbitMQ for all asynchronous events and world updates
- **Orchestration**: .NET Aspire for development environment
- **Observability**: OpenTelemetry integration
- **Testing**: xUnit (Mordecai.Web.Tests)

### Architecture Pattern
- **Event-driven architecture** using RabbitMQ for message broadcasting
- **Domain-driven design** with clear separation of concerns
- **Service layer** in Mordecai.Game for business logic
- **Message contracts** in Mordecai.Messaging for event definitions

## Project Structure

```
Mordecai.sln
├── Mordecai.AppHost/              # Aspire orchestration and service discovery
├── Mordecai.Game/                 # Core domain models, entities, game services
├── Mordecai.Data/                 # Database context, migrations, repositories
├── Mordecai.Messaging/            # RabbitMQ message contracts and integration
├── Mordecai.BackgroundServices/   # Hosted services (world ticks, AI, cleanup)
├── Mordecai.Web/                  # Blazor Server UI, SignalR hubs, API controllers
├── Mordecai.ServiceDefaults/      # Cross-cutting concerns (telemetry, configs)
├── Mordecai.AdminCli/             # Command-line admin tools
├── Mordecai.Web.Tests/            # Unit and integration tests
├── docs/                          # Comprehensive design documentation
└── tools/                         # Development utilities
```

## Coding Preferences

### General C# Guidelines
- **Use modern C# features**: Pattern matching, switch expressions, records where appropriate
- **Async/await**: All I/O operations must be async (database, messaging, file access)
- **Nullable reference types**: Enabled project-wide; be explicit about nullability
- **Expression-bodied members**: Prefer for simple property getters and single-line methods
- **var keyword**: Use for obvious types; be explicit for clarity when type isn't clear

### Naming Conventions
- **Classes/Interfaces**: PascalCase (e.g., `CharacterEntity`, `IMessageBroker`)
- **Methods/Properties**: PascalCase (e.g., `SendMessage`, `CurrentHealth`)
- **Private fields**: `_camelCase` with underscore prefix (e.g., `_dbContext`, `_logger`)
- **Parameters/locals**: camelCase (e.g., `characterId`, `skillLevel`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE (e.g., `MaxPlayers` or `MAX_PLAYERS`)
- **Message types**: Suffix with purpose (e.g., `CharacterMovedEvent`, `DamageCalculatedMessage`)

### Code Organization
- **One class per file** (unless nested types are tightly coupled)
- **Namespace matches folder structure**
- **Group using statements**: System namespaces first, then third-party, then project namespaces
- **Organize class members**: Constants → Fields → Constructors → Properties → Methods (public before private)

### Documentation
- **XML comments** for public APIs, especially service interfaces
- **Inline comments** only when business logic or game mechanics are non-obvious
- **Refer to docs/** for game mechanics instead of duplicating in code comments

## Domain-Specific Patterns

### Game Mechanics Constants
All game mechanics formulas and constants should reference the specification:
- **Vitality (VIT)**: `(STR × 2) - 5` - Physical health pool
- **Fatigue (FAT)**: `(END + WIL) - 5` - Mental/stamina pool
- **Skill Checks**: `4dF + Skill vs Target` - Fudge dice (-4 to +4 range)
- **See**: `docs/MORDECAI_SPECIFICATION.md` for complete mechanics

### Entity Design
- **Database entities** live in `Mordecai.Game/Entities/`
- **Use EF Core conventions** for relationships and navigation properties
- **Value objects** should be immutable records or structs where appropriate
- **Entity validation** via data annotations and/or FluentValidation

### Message-Driven Updates
- **All world changes** should publish events via RabbitMQ
- **Message contracts** in `Mordecai.Messaging/Messages/`
- **Use scoped messaging** for efficiency: Personal → Group → Zone → World
- **Example scopes**: `MessageScope.Personal`, `MessageScope.Zone`, etc.

### Service Layer
- **Services** in `Mordecai.Game/Services/` handle business logic
- **Inject dependencies** via constructor (prefer `IServiceProvider` for optional/dynamic services)
- **Services should be scoped or transient** (avoid singleton for stateful services)
- **Use repository pattern** for data access abstraction

### Background Processing
- **World ticks**: Implement as `BackgroundService` in Mordecai.BackgroundServices
- **Use timers carefully**: Consider performance impact on small-scale deployment
- **Graceful shutdown**: Respect cancellation tokens

## Testing Approach

### Test Organization
- **Unit tests**: Test services and domain logic in isolation
- **Integration tests**: Test full flows with in-memory database
- **Use xUnit** with standard naming: `MethodName_Scenario_ExpectedResult`
- **Arrange-Act-Assert** pattern consistently

### Mocking
- **Prefer test doubles** over heavy mocking frameworks when simple
- **Mock external dependencies**: Database, RabbitMQ, HTTP clients
- **Don't mock domain entities**: Use real objects or builders

## Common Tasks

### Running the Application
```bash
# Start all services via Aspire (recommended for development)
dotnet run --project Mordecai.AppHost

# Run web app standalone (requires manual RabbitMQ + DB setup)
dotnet run --project Mordecai.Web
```

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project Mordecai.Data

# Update database
dotnet ef database update --project Mordecai.Data

# Generate SQL script
dotnet ef migrations script --project Mordecai.Data
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Mordecai.Web.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Admin CLI
```bash
# Use admin tools for world building
dotnet run --project Mordecai.AdminCli -- [command] [args]
```

## Important Contexts

### Game Design Philosophy
- **Practice-based progression**: Skills improve through use, not XP allocation
- **Anti-abuse mechanics**: Diminishing returns, cooldowns, context validation (see `SKILL_PROGRESSION_ANTI_ABUSE.md`)
- **Balance**: Target 10-50 concurrent players; optimize for community, not massive scale
- **Text immersion**: Rich descriptions, atmospheric writing, strong narrative

### Current Development Phase
Check recent commits and `docs/ITEM_IMPLEMENTATION_STATUS.md` for current focus areas:
1. ✅ World Foundation (rooms, movement, chat)
2. ✅ Character System (attributes, skills, progression)
3. 🔄 Item & Equipment Systems (ongoing)
4. 🔄 Combat Foundation (in progress)
5. 📋 Admin Tools (planned)

### Known Patterns
- **Zone messaging**: Recent implementation allows zone-wide broadcasts (see commit history)
- **VIT/FAT calculation**: Fixed in recent commits; ensure formulas match specification
- **Equipment bonuses**: Cascading effects on derived stats (see `ITEM_BONUSES_AND_CASCADING_EFFECTS.md`)

## Documentation

### Primary References
| Document | Purpose |
|----------|---------|
| `docs/MORDECAI_SPECIFICATION.md` | **Complete game design** - mechanics, formulas, systems |
| `docs/QUICK_REFERENCE.md` | Rapid overview and key decisions |
| `docs/DATABASE_DESIGN.md` | Entity schema and relationships |
| `docs/README.md` | Documentation index and navigation |

### Before Making Changes
1. **Read relevant docs** in `docs/` folder first
2. **Understand game mechanics** before implementing features
3. **Check implementation status** docs for current progress
4. **Verify formulas** against specification (common error source!)

## Notes for Claude

### When Working on This Project
- ✅ **DO**: Read specification docs before implementing game features
- ✅ **DO**: Use message-driven architecture for world updates
- ✅ **DO**: Test game mechanics formulas against specification
- ✅ **DO**: Consider 10-50 player scale (not millions)
- ✅ **DO**: Maintain immersive text-based experience
- ❌ **DON'T**: Hardcode game mechanics; use constants or configuration
- ❌ **DON'T**: Skip RabbitMQ for world events; everything is message-driven
- ❌ **DON'T**: Assume REST patterns; this is real-time via SignalR + RabbitMQ
- ❌ **DON'T**: Over-optimize prematurely; clarity over micro-optimizations

### Common Pitfalls
- **VIT/FAT formulas**: Double-check against `MORDECAI_SPECIFICATION.md` (recently fixed)
- **Message scoping**: Use appropriate scope (Personal/Group/Zone/World) for efficiency
- **Dice mechanics**: 4dF is -4 to +4, not 1d20; understand probability curves
- **Skill progression**: Must include anti-abuse mechanics (see `SKILL_PROGRESSION_ANTI_ABUSE.md`)

### Architecture Decisions
- **Why Blazor Server**: Real-time updates, reduced client complexity
- **Why RabbitMQ**: Decoupled event broadcasting, scales better than in-memory
- **Why SQLite → PostgreSQL**: Easy dev setup, production-ready migration path
- **Why Aspire**: Simplified local orchestration, observability out of the box

---

*This file helps Claude Code understand the Mordecai MUD project. Keep it updated as architecture or conventions evolve.*

*Last updated: 2026-01-04*
