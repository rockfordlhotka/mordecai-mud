# Architecture

**Analysis Date:** 2026-01-26

## Pattern Overview

**Overall:** Event-driven, Domain-Driven Design with clear separation of concerns

**Key Characteristics:**
- Event-driven message broadcasting via RabbitMQ for all world updates
- Layered service architecture separating domain logic, presentation, and data access
- Message-based pub/sub pattern for real-time multiplayer state synchronization
- Scoped dependency injection for stateful services and transient for stateless operations
- Entity Framework Core with pooled connection management for database access
- Blazor Server for real-time bidirectional client-server communication

## Layers

**Presentation Layer (Web/UI):**
- Purpose: Server-rendered Blazor components for real-time game interaction
- Location: `Mordecai.Web/Pages/`, `Mordecai.Web/Components/`, `Mordecai.Web/Shared/`
- Contains: Razor pages (`.razor`), UI components, page models
- Depends on: Game services, Character service, World service, Message publisher
- Used by: End users via web browsers

**Domain Layer (Game Models & Logic):**
- Purpose: Core game entities, domain rules, and game mechanics
- Location: `Mordecai.Game/Entities/`, `Mordecai.Game/Services/`
- Contains: Entity definitions (`WorldEntities.cs`, `SkillEntities.cs`, `ItemEntities.cs`, `CombatEntities.cs`), service interfaces
- Depends on: Game configuration, no external dependencies beyond .NET
- Used by: All other layers

**Service/Application Layer:**
- Purpose: Business logic orchestration, game rules enforcement, state management
- Location: `Mordecai.Web/Services/`
- Contains: Character management, combat, equipment, skills, world state, spawning
- Depends on: Domain entities, data access, message publishing, game services
- Used by: Presentation layer, background services

**Messaging Layer:**
- Purpose: Event contracts and RabbitMQ integration for distributed messaging
- Location: `Mordecai.Messaging/Messages/`, `Mordecai.Messaging/Services/`
- Contains: Message definitions (chat, movement, combat, skills, environment), RabbitMQ publisher/subscriber
- Depends on: RabbitMQ, domain entities for type references
- Used by: All services for broadcasting state changes

**Data Access Layer:**
- Purpose: Database context, Entity Framework configuration, migrations
- Location: `Mordecai.Web/Data/ApplicationDbContext.cs`, `Mordecai.Web/Migrations/`
- Contains: DbContext configuration, model mapping, migration history
- Depends on: EF Core, PostgreSQL, domain entities
- Used by: Service layer for querying and persisting entities

**Background/Infrastructure Layer:**
- Purpose: Long-running processes, periodic tasks, system maintenance
- Location: `Mordecai.BackgroundServices/`
- Contains: Room effect ticks, health regeneration, spawner management, message broadcasting
- Depends on: Domain services, message publishing
- Used by: ASP.NET Core runtime

## Data Flow

**Character Action → World Update:**

1. User triggers action in Blazor component (e.g., movement, chat, skill use)
2. `Play.razor` page injects appropriate service (`GameActionService`, `CharacterService`, etc.)
3. Service validates action and applies game rules
4. Entity state is persisted via `ApplicationDbContext` to PostgreSQL
5. Service publishes game message via `IGameMessagePublisher` to RabbitMQ
6. `RabbitMqGameMessageSubscriber` receives message and broadcasts to affected clients
7. Blazor clients receive update via SignalR and re-render affected UI components

**Message Scoping (Efficiency Pattern):**
- Personal: Single character (e.g., individual health update)
- Room: All characters in a room (e.g., movement message, combat action)
- Zone: All characters in a zone (e.g., environmental effect)
- World: All connected players (e.g., global announcement)

**State Management:**
- **Persistent State:** Characters, rooms, items, skills, combat sessions stored in PostgreSQL
- **Real-time State:** Game messages and entity updates flow through RabbitMQ
- **Computed State:** Room descriptions, derived attributes calculated on-demand via services
- **Game Time:** Singleton `GameTimeService` maintains shared game clock across all instances

## Key Abstractions

**IGameMessagePublisher/IGameMessageSubscriber:**
- Purpose: Abstract message delivery mechanism
- Examples: `RabbitMqGameMessagePublisher` (`Mordecai.Messaging/Services/RabbitMqGameMessagePublisher.cs`)
- Pattern: Factory pattern with `RabbitMqGameMessageSubscriberFactory` for creating message-type-specific subscribers

**GameMessage (Sealed Record Hierarchy):**
- Purpose: Immutable event contract for all world changes
- Examples: `ChatMessage`, `MovementMessages`, `CombatMessages`, `SkillMessages` (all in `Mordecai.Messaging/Messages/`)
- Pattern: Sealed records with `RoomId`, `ZoneId`, `TargetCharacterIds` for message routing

**ICharacterService:**
- Purpose: Central character state management and access control
- Location: `Mordecai.Web/Services/CharacterService.cs`
- Pattern: Scoped service using `IDbContextFactory` for connection pooling

**IWorldService:**
- Purpose: Room and zone navigation, world state queries
- Location: `Mordecai.Web/Services/WorldService.cs`
- Pattern: Scoped service for transactional world queries

**IRoomService (Game Layer):**
- Purpose: Room description formatting with time-of-day variations
- Location: `Mordecai.Game/Services/RoomService.cs`
- Pattern: Returns immutable `RoomExitInfo` records for exit information

## Entry Points

**Web Application:**
- Location: `Mordecai.Web/Program.cs`
- Triggers: ASP.NET Core startup
- Responsibilities: Service registration, database migrations, seed data initialization, Blazor setup

**Play Page:**
- Location: `Mordecai.Web/Pages/Play.razor`
- Triggers: User navigates to `/play/{characterId:guid}`
- Responsibilities: Game loop, message subscription, command input, UI rendering
- Injects: CharacterService, GameActionService, MessageBroadcastService, multiple domain services

**Background Services (Hosted):**
- `RoomEffectBackgroundService`: Applies room effects on timer tick
- `HealthTickBackgroundService`: Regenerates character health
- `SpawnerTickService` (`Mordecai.BackgroundServices/SpawnerTickService.cs`): Spawns NPCs at intervals
- `CombatMessageBroadcastService`: Broadcasts combat updates to participants

**Aspire Orchestration:**
- Location: `Mordecai.AppHost/AppHost.cs`
- Triggers: `dotnet run --project Mordecai.AppHost`
- Responsibilities: Service discovery, container orchestration (PostgreSQL, RabbitMQ), connection string injection

## Error Handling

**Strategy:** Graceful degradation with logging

**Patterns:**
- RabbitMQ publisher operates in "offline mode" if broker unavailable (logs warning, continues)
- Database context creation wrapped in try-catch with null returns on failure
- Service methods return `null` or `false` on error, logged to ILogger
- UI pages check for null results and display appropriate fallback messages

## Cross-Cutting Concerns

**Logging:** ILogger injected throughout; async operations log entry/exit at Information level

**Validation:**
- Data annotations on entities (e.g., `[Required]`, `[StringLength]`)
- Business rule validation in service methods (e.g., `CanMoveToRoomAsync`)
- Game rule enforcement (e.g., fatigue cost checks, skill level requirements)

**Authentication:**
- ASP.NET Core Identity for user accounts
- `[Authorize]` attribute on Play.razor page
- `UserId` property on Character entity for ownership verification
- Admin role for special operations (`[Authorize(Roles = "Admin")]`)

**Async/Await:**
- All I/O operations (database, messaging) async via `async Task` pattern
- `CancellationToken` parameters for graceful shutdown
- Scoped DbContext factory for connection pooling

---

*Architecture analysis: 2026-01-26*
