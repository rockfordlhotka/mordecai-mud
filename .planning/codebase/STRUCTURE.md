# Codebase Structure

**Analysis Date:** 2026-01-26

## Directory Layout

```
Mordecai.sln (root solution file)
├── Mordecai.AppHost/              # .NET Aspire orchestration (dev environment)
│   └── AppHost.cs                 # Defines PostgreSQL, RabbitMQ, Web service dependencies
├── Mordecai.Game/                 # Domain layer (entities, core game logic)
│   ├── Entities/                  # Domain model definitions
│   │   ├── WorldEntities.cs       # Zone, Room, RoomType, RoomExit, DoorState
│   │   ├── SkillEntities.cs       # Skill progression system
│   │   ├── ItemEntities.cs        # Items, equipment, inventory
│   │   ├── CombatEntities.cs      # Combat sessions, participants, actions
│   │   ├── SpawnerEntities.cs     # NPC spawning system
│   │   ├── RoomEffectEntities.cs  # Environmental effects
│   │   └── GameConfiguration.cs   # Game-wide settings
│   └── Services/                  # Domain service contracts and implementations
│       ├── RoomService.cs         # Room description, exits, movement validation
│       ├── GameTimeService.cs     # Shared game clock (singleton)
│       ├── ZoneService.cs         # Zone operations
│       ├── ICombatService.cs      # Combat mechanics (interface)
│       └── ISpawnerService.cs     # NPC spawn management (interface)
├── Mordecai.Web/                  # Application layer (Blazor UI, services, database)
│   ├── Program.cs                 # Startup, DI configuration, migrations
│   ├── Pages/                     # Blazor pages (entry points for users)
│   │   ├── Play.razor             # Main game page (largest file ~2400 lines)
│   │   ├── Characters.razor       # Character creation/selection
│   │   ├── CharacterSkills.razor  # Skill management UI
│   │   ├── Index.razor            # Home page
│   │   ├── Admin/                 # Administrative management pages
│   │   │   ├── RoomCreate.razor   # Create rooms
│   │   │   ├── RoomEdit.razor     # Edit room properties
│   │   │   ├── RoomExits.razor    # Configure room connections
│   │   │   ├── Zones.razor        # Zone management
│   │   │   ├── Items.razor        # Item template management
│   │   │   ├── Npcs.razor         # NPC management
│   │   │   ├── Skills.razor       # Skill definition management
│   │   │   ├── Settings.razor     # Game configuration
│   │   │   └── (13 admin pages total)
│   │   └── _Host.cshtml           # Root HTML host
│   ├── Components/                # Reusable Blazor components
│   │   ├── ConfirmationModal.razor
│   │   └── ConfirmDeleteDialog.razor
│   ├── Services/                  # Application business logic (25+ services)
│   │   ├── CharacterService.cs    # Character state management
│   │   ├── CharacterCreationService.cs
│   │   ├── WorldService.cs        # World navigation and queries
│   │   ├── CombatService.cs       # Combat mechanics implementation
│   │   ├── EquipmentService.cs    # Equipment and bonuses
│   │   ├── ItemTemplateService.cs # Item templates and creation
│   │   ├── SkillService.cs        # Skill progression logic
│   │   ├── GameActionService.cs   # Unified action dispatcher
│   │   ├── RoomEffectService.cs   # Room effect logic
│   │   ├── DiceService.cs         # 4dF dice rolling (Fudge/Fate mechanics)
│   │   ├── CurrencyService.cs     # Gold, silver, copper, platinum
│   │   ├── SpawnerService.cs      # NPC spawner management
│   │   ├── GameTimeService.cs     # Game clock (shared with Game layer)
│   │   ├── RoomAdjacencyService.cs
│   │   ├── CharacterMessageBroadcastService.cs
│   │   ├── CombatMessageBroadcastService.cs
│   │   ├── TargetResolutionService.cs
│   │   ├── AdminSeedService.cs    # Admin user initialization
│   │   ├── RoomTypeSeedService.cs # Room type data seeding
│   │   ├── SkillSeedService.cs    # Skill data initialization
│   │   ├── DataMigrationService.cs
│   │   └── (additional utility services)
│   ├── Data/                      # Database context and migrations
│   │   ├── ApplicationDbContext.cs# EF Core DbContext with all entity sets
│   │   └── SkillEntities.cs       # Web-specific skill entities (separate from Game.Entities)
│   ├── Migrations/                # EF Core database migration history
│   ├── Models/                    # Web-layer models
│   │   └── CharacterAttributes.cs # Character attribute calculations
│   ├── Shared/                    # Layout components
│   ├── Areas/                     # ASP.NET Identity areas
│   │   └── Identity/              # User login/registration
│   ├── wwwroot/                   # Static assets (CSS, JS, images)
│   │   ├── css/
│   │   └── js/
│   └── appsettings.json           # Configuration defaults
├── Mordecai.Web.Tests/            # Test project (xUnit)
│   └── (test files matching services)
├── Mordecai.Data/                 # Placeholder for future data-only library
│   └── Class1.cs                  # Currently empty stub
├── Mordecai.Messaging/            # Message contracts and RabbitMQ integration
│   ├── Messages/                  # Immutable message record definitions
│   │   ├── GameMessage.cs         # Base record (abstract)
│   │   ├── ChatMessages.cs        # ChatMessage, GlobalChatMessage, EmoteMessage
│   │   ├── MovementMessages.cs    # CharacterMovedMessage, etc.
│   │   ├── CombatMessages.cs      # CombatStartedMessage, DamageCalculatedMessage, etc.
│   │   ├── SkillMessages.cs       # SkillUsedMessage, SkillProgressedMessage, etc.
│   │   ├── ItemMessages.cs        # ItemObtainedMessage, ItemEquippedMessage, etc.
│   │   ├── EnvironmentMessages.cs # RoomEffectAppliedMessage, WeatherChangedMessage, etc.
│   │   ├── SpawnerMessages.cs     # NpcSpawnedMessage, NpcDespawnedMessage, etc.
│   │   └── SystemMessages.cs      # SystemAnnouncementMessage, ServerMaintenanceMessage, etc.
│   ├── Services/                  # Message pub/sub implementation
│   │   ├── RabbitMqGameMessagePublisher.cs # Publishes messages to RabbitMQ topic exchange
│   │   ├── RabbitMqGameMessageSubscriber.cs
│   │   ├── RabbitMqGameMessageSubscriberFactory.cs
│   │   ├── IGameMessageServices.cs # Publisher/subscriber interfaces
│   │   ├── SoundPropagationService.cs # Message scope routing
│   │   └── ISoundPropagationService.cs
│   └── Extensions/                # DI setup
│       └── ServiceCollectionExtensions.cs # AddGameMessaging() extension
├── Mordecai.BackgroundServices/   # Long-running background tasks
│   ├── SpawnerTickService.cs      # Periodically spawns NPCs
│   └── (HostedService implementations referenced in Program.cs)
├── Mordecai.ServiceDefaults/      # Shared infrastructure configuration
│   └── (OpenTelemetry, observability setup)
├── Mordecai.AdminCli/             # Command-line admin tools
│   ├── Commands/                  # Admin commands
│   └── Mordecai.AdminCli.csproj
├── docs/                          # Design documentation
│   ├── MORDECAI_SPECIFICATION.md  # Complete game design
│   ├── QUICK_REFERENCE.md         # Summary and key decisions
│   ├── DATABASE_DESIGN.md         # Schema and relationships
│   └── (implementation status docs)
└── tools/                         # Development utilities
    ├── ListAssemblyTypes/         # Utility for analyzing assemblies
    └── rabbitmq-smoke/            # RabbitMQ connectivity test
```

## Directory Purposes

**Mordecai.Game:**
- Purpose: Domain layer with game entity definitions and core service contracts
- Contains: Entity models (no EF configuration here), domain service interfaces
- Key files: `Entities/WorldEntities.cs`, `Entities/ItemEntities.cs`, `Services/GameTimeService.cs`

**Mordecai.Web:**
- Purpose: Complete Blazor Server application with database context, services, and UI
- Contains: All application logic, database persistence, Razor components, admin pages
- Key files: `Program.cs` (startup), `Pages/Play.razor` (main game), `Data/ApplicationDbContext.cs` (persistence)

**Mordecai.Web/Services:**
- Purpose: Business logic orchestration layer between UI and persistence
- Contains: 25+ scoped/transient services implementing game rules and state management
- Key files: `CharacterService.cs`, `WorldService.cs`, `CombatService.cs`, `GameActionService.cs`

**Mordecai.Messaging:**
- Purpose: Message contracts and RabbitMQ integration for real-time broadcasts
- Contains: Sealed record definitions for all event types, pub/sub implementation
- Key files: `Messages/GameMessage.cs` (base), `Services/RabbitMqGameMessagePublisher.cs`

**Mordecai.BackgroundServices:**
- Purpose: Hosted services that run periodically or continuously
- Contains: Background service implementations injected in `Program.cs`
- Key files: `SpawnerTickService.cs` (NPC spawning)

**Mordecai.Messaging/Messages:**
- Purpose: Centralized immutable event contract definitions
- Contains: Sealed records for game events (chat, movement, combat, skills, items, environment, spawner, system)
- Pattern: All inherit from abstract `GameMessage` base record

## Key File Locations

**Entry Points:**
- `Mordecai.Web/Program.cs`: Service registration, DI configuration, startup pipeline
- `Mordecai.Web/Pages/Play.razor`: Main game interface (character.Id route parameter)
- `Mordecai.AppHost/AppHost.cs`: Development environment orchestration

**Configuration:**
- `Mordecai.Web/appsettings.json`: Default configuration (database, RabbitMQ defaults)
- `Mordecai.Web/Program.cs`: Cloud-native config (environment variables override appsettings)

**Core Logic:**
- `Mordecai.Web/Services/CharacterService.cs`: Character access and health management
- `Mordecai.Web/Services/WorldService.cs`: Room/zone navigation
- `Mordecai.Web/Services/CombatService.cs`: Combat system implementation
- `Mordecai.Web/Services/GameActionService.cs`: Unified action dispatcher (movement, chat, skills)
- `Mordecai.Game/Services/GameTimeService.cs`: Shared game clock (singleton)

**Database:**
- `Mordecai.Web/Data/ApplicationDbContext.cs`: EF Core DbContext with all entity sets
- `Mordecai.Web/Migrations/`: Migration history (run via `dotnet ef database update`)

**Testing:**
- `Mordecai.Web.Tests/`: xUnit test project (integration tests with in-memory DB)

## Naming Conventions

**Files:**
- Entity files: `{PluralEntityType}Entities.cs` (e.g., `SkillEntities.cs`, `CombatEntities.cs`)
- Service files: `{ServiceName}Service.cs` (e.g., `CharacterService.cs`, `CombatService.cs`)
- Interface files: `I{ServiceName}.cs` (e.g., `ICharacterService.cs`, `ICombatService.cs`)
- Blazor pages: `{FeatureName}.razor` (e.g., `Play.razor`, `Characters.razor`)
- Admin pages: `Admin/{ResourceName}.razor` (e.g., `Admin/Rooms.razor`, `Admin/Items.razor`)
- Messages: `{EventType}Messages.cs` or `{EventType}Message.cs` (e.g., `ChatMessages.cs`, `CombatMessages.cs`)

**Directories:**
- Feature/domain folders: Plural names (`Entities/`, `Services/`, `Messages/`, `Pages/`)
- Admin subfolder: `Admin/` under `Pages/` for admin-only features
- Aspire host: `AppHost/` (special project type)

## Where to Add New Code

**New Feature (e.g., Quest System):**
- Domain entities: `Mordecai.Game/Entities/QuestEntities.cs` (quest definitions, player progress)
- Service interface: `Mordecai.Game/Services/IQuestService.cs`
- Service implementation: `Mordecai.Web/Services/QuestService.cs`
- Messages: `Mordecai.Messaging/Messages/QuestMessages.cs` (QuestCompletedMessage, etc.)
- UI pages: `Mordecai.Web/Pages/Quests.razor` (player-facing), `Admin/Quests.razor` (admin)
- Tests: `Mordecai.Web.Tests/QuestServiceTests.cs`
- Database: Add DbSet to `ApplicationDbContext.cs`, create migration

**New Component/Module (e.g., Trade System):**
- Implementation: `Mordecai.Web/Services/TradeService.cs` (scoped service)
- Entities: Add to `Mordecai.Game/Entities/` (new file if 100+ lines)
- Messages: `Mordecai.Messaging/Messages/TradeMessages.cs`
- UI integration: Inject into `Play.razor` or create component in `Components/`
- Register in: `Program.cs` as `builder.Services.AddScoped<ITradeService, TradeService>()`

**Utilities/Helpers:**
- Shared helpers: `Mordecai.Web/Services/{HelperName}Service.cs` (if stateful) or `Mordecai.Game/Services/` (if pure logic)
- Extension methods: As static classes in `Extensions/` (e.g., `Mordecai.Messaging/Extensions/ServiceCollectionExtensions.cs`)

## Special Directories

**Mordecai.Web/Migrations/:**
- Purpose: EF Core auto-generated migration files
- Generated: Yes (by `dotnet ef migrations add MigrationName`)
- Committed: Yes (stored in version control for reproducibility)
- Manual modification: Not recommended; create new migration instead

**Mordecai.Web/wwwroot/:**
- Purpose: Static web assets (CSS, JavaScript, images)
- Generated: No (manually maintained or built from source)
- Committed: Yes
- Structure: `css/` for stylesheets, `js/` for scripts

**Mordecai.AppHost/**
- Purpose: Development-only orchestration (not deployed to production)
- Generated: No (manually maintained)
- Committed: Yes (important for dev environment consistency)
- Structure: Single `AppHost.cs` file defining services

**Mordecai.Web/Areas/Identity/**
- Purpose: ASP.NET Core Identity UI (user login, registration)
- Generated: Yes (scaffolded from Identity packages)
- Committed: Yes (customizations checked in)
- Customization: Modify to match game theme if needed

---

*Structure analysis: 2026-01-26*
