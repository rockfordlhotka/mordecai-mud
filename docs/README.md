# Mordecai MUD Documentation

This folder contains the design documentation, implementation guides, and reference materials for the Mordecai MUD project—a skill-based, real-time text MUD built with .NET 9, Blazor Server, and Aspire orchestration.

## Quick Start

| If you want to... | Start here |
|-------------------|------------|
| Understand the game design | [MORDECAI_SPECIFICATION.md](MORDECAI_SPECIFICATION.md) |
| Get a rapid overview | [QUICK_REFERENCE.md](QUICK_REFERENCE.md) |
| Understand the database schema | [DATABASE_DESIGN.md](DATABASE_DESIGN.md) |
| Deploy to Kubernetes | [KUBERNETES_DEPLOYMENT.md](KUBERNETES_DEPLOYMENT.md) |

---

## Documentation Index

### Core Design

| Document | Description |
|----------|-------------|
| [MORDECAI_SPECIFICATION.md](MORDECAI_SPECIFICATION.md) | **Main specification** — Fudge dice mechanics (4dF), attributes, skills, health system (VIT/FAT), combat, magic, progression, world design |
| [QUICK_REFERENCE.md](QUICK_REFERENCE.md) | Concise summary of technology stack, game systems, development phases, and key decisions |
| [DATABASE_DESIGN.md](DATABASE_DESIGN.md) | Entity definitions, relationships, and EF Core schema design |

### Item System

The item system is documented across several focused files:

| Document | Description |
|----------|-------------|
| [ITEM_SYSTEM_OVERVIEW.md](ITEM_SYSTEM_OVERVIEW.md) | Visual architecture diagrams — entity relationships, container hierarchy, service responsibilities, integration points |
| [ITEM_LIST.md](ITEM_LIST.md) | Catalog of items with prices and categories |
| [ITEM_IMPLEMENTATION_STATUS.md](ITEM_IMPLEMENTATION_STATUS.md) | Implementation progress tracking and phase status |
| [ITEM_BONUSES_AND_CASCADING_EFFECTS.md](ITEM_BONUSES_AND_CASCADING_EFFECTS.md) | How equipment bonuses affect ability scores and derived stats |
| [EQUIPMENT_SYSTEM_IMPLEMENTATION.md](EQUIPMENT_SYSTEM_IMPLEMENTATION.md) | Equipment service implementation details and slot mechanics |
| [CURRENCY_SYSTEM_IMPLEMENTATION.md](CURRENCY_SYSTEM_IMPLEMENTATION.md) | Currency denominations (cp/sp/gp/pp), conversion rates, wallet implementation |
| [CARRYING_CAPACITY_ANALYSIS.md](CARRYING_CAPACITY_ANALYSIS.md) | Exponential capacity scaling based on Physicality |

### Skill System

| Document | Description |
|----------|-------------|
| [SKILL_PROGRESSION_ANTI_ABUSE.md](SKILL_PROGRESSION_ANTI_ABUSE.md) | Anti-exploit mechanics — diminishing returns, cooldowns, context validation |

### Deployment

| Document | Description |
|----------|-------------|
| [KUBERNETES_DEPLOYMENT.md](KUBERNETES_DEPLOYMENT.md) | Production deployment guide with RabbitMQ, PostgreSQL, and Kubernetes manifests |

---

## Key Formulas Reference

| Stat | Formula | Notes |
|------|---------|-------|
| **Vitality (VIT)** | `(STR × 2) - 5` | Physical health; 0 = incapacitated |
| **Fatigue (FAT)** | `(END + WIL) - 5` | Mental/physical stamina; 0 = exhausted |
| **Skill Check** | `4dF + Skill` vs Target | Fudge dice: -4 to +4 range |

---

## Technology Stack

- **Frontend**: Blazor Web App (Server mode)
- **Backend**: .NET 9, EF Core
- **Database**: SQLite (dev) → PostgreSQL (prod)
- **Messaging**: RabbitMQ for all async events
- **Orchestration**: .NET Aspire (dev)
- **Observability**: OpenTelemetry

---

## Development Priorities

1. **World Foundation** — Rooms, movement, descriptions, look/examine, local chat
2. **Character System** — Creation, 7 core skills, practice-based progression
3. **Combat Foundation** — Melee, ranged, spells, cooldowns, skill checks
4. **Admin Tools** — Builder interface for zones, rooms, NPCs, items, quests

---

## Project Structure

```
Mordecai.sln
├── Mordecai.AppHost/        # Aspire orchestration
├── Mordecai.Game/           # Core domain models, entities, services
├── Mordecai.Messaging/      # Message contracts, RabbitMQ integration
├── Mordecai.BackgroundServices/  # Hosted services (ticks, world sim)
├── Mordecai.Web/            # Blazor Server UI, controllers, pages
├── Mordecai.ServiceDefaults/    # Cross-cutting (OpenTelemetry, etc.)
├── Mordecai.AdminCli/       # CLI admin tools
├── Mordecai.Web.Tests/      # Unit tests
├── docs/                    # This folder
└── tools/                   # Development utilities
```

---

## Contributing

When adding documentation:

1. Keep documents focused on a single topic
2. Use Markdown tables and diagrams for clarity
3. Update this README when adding new docs
4. Prefer linking to existing docs over duplicating content

---

*Last updated: 2025-01-15*
