
# Mordecai MUD - Quick Reference

## Technology Stack Summary

| Component    | Technology           | Justification                                 |
|--------------|----------------------|-----------------------------------------------|
| **Frontend** | Blazor Web App (Server) | Real-time UI via built-in SignalR, modern .NET |
| **Backend**  | .NET 8+ Blazor Server | All game logic/state managed server-side      |
| **App Host (dev)** | .NET Aspire     | Orchestrates all services, local RabbitMQ, OpenTelemetry |
| **Async Messaging** | RabbitMQ        | Decoupled, scalable event-driven architecture |
| **Observability** | OpenTelemetry    | Distributed tracing, logging, and metrics     |
| **Database** | SQLite → PostgreSQL  | Simple start, scalable migration path         |
| **ORM**      | Entity Framework Core| Code-first, strong typing, migrations         |
| **UI Style** | Terminal/Console     | Classic MUD aesthetic, accessibility          |

## Core Game Systems

### Character System
- **Species**: Human, Elf, Dwarf, Halfling, Orc
- **Attributes**: STR (Physicality), DEX (Dodge), END (Drive), INT (Reasoning), ITT (Awareness), WIL (Focus), PHY (Bearing)
- **Skill System**:
	- All characters start with 7 core attribute skills
	- Weapon skills (Swords, Axes, Bows, etc.)
	- Individual spell skills (each spell is a skill)
	- Mana recovery skills (per magic school)
	- Crafting skills (Blacksmithing, Alchemy, etc.)
- **Progression**: Practice-based advancement (skills improve through use)

### World Design
- **Room-based world**: Interconnected rooms, multiple zones, dynamic weather/time
- **Persistence**: Player actions affect world state, items persist, room changes persist


### Combat Mechanics
- **Style**: Real-time with cooldowns (no turn-based rounds)
- **Resolution**: All actions resolved via skill checks
- **PvE/PvP**: Monsters, bosses, optional PvP, dueling, guild wars

### Magic & Spells
- **Individual spell skills**: Each spell is a skill, organized by school
- **Mana system**: Separate mana pools and recovery skills per school
- **Magic items**: Enchanted gear, consumables, artifacts


### Social & Admin Features
- **Communication**: Global/local chat, private messaging, guild chat (all via RabbitMQ async messaging)
- **Guilds**: Player organizations, halls, ranks, competition
- **Admin tools**: Web-based content creation (zones, rooms, NPCs, quests, items)

## Development Priorities


### Phase 1: World Foundation (Weeks 1-4)
- Project setup and architecture
- Database design and Entity Framework setup
- Basic Blazor app structure and authentication
- **Priority 1: Room system, movement, descriptions, look commands, in-world talking**

### Phase 2: Character Systems (Weeks 5-8)
- **Priority 2: Character creation, skill-based progression, attribute system**
- Character persistence, save/load, basic inventory

### Phase 3: Combat Foundation (Weeks 9-12)
- **Priority 3: Real-time combat (melee, ranged, spells, cooldowns)**
- Basic monster encounters, combat balance

### Phase 4: Social & Admin Tools (Weeks 13-16)
- Real-time chat, multiplayer, guilds
- **Admin content tools: zone/room/NPC/quest/item creation**

### Phase 5: Content & Polish (Weeks 17-20)
- Advanced quest system, magic schools, economy, performance, beta

### Phase 6: Extended Features (Future)
- Advanced crafting, complex quests, player housing, advanced PvP, mobile, community

## Key Technical Decisions


### Why Blazor Web App?
- Single technology stack (C# frontend/backend)
- Real-time UI via Blazor Server’s built-in SignalR
- Modern web standards, PWA potential
- Accessibility (screen reader support)

### Why SQLite First?
- Fast development, no external DB needed
- Easy deployment, simple backup
- Seamless migration to PostgreSQL



### Why RabbitMQ for Async Messaging?
- Decouples game logic and user/NPC/system events
- Enables scalable, event-driven architecture
- All cross-user, NPC, and system messaging flows through RabbitMQ
- Supports distributed processing and future microservices


### Why OpenTelemetry for Observability?
- Standardized distributed tracing, logging, and metrics
- Works seamlessly with .NET and Blazor Server
- Exportable to Azure Monitor, Jaeger, Zipkin, and more
- Enables deep insight into app performance and issues

### Why .NET Aspire for App Hosting (dev)?
- Orchestrates all services (web, RabbitMQ, database, etc.)
- Simplifies local development and service discovery
- Built-in OpenTelemetry and distributed tracing
- Easily run RabbitMQ and other dependencies as containers

## Success Metrics

### Technical
- <100ms command response
- 99.9% uptime
- 50+ concurrent users (target 10-50)
- Zero critical security vulnerabilities

### Gameplay
- 70% player retention (7 days)
- Avg. session > 30 min
- 80% reach skill milestone (level 10 equivalent)
- 60% join guilds/groups

## Next Steps

1. Review and approve specification
2. Set up .NET solution structure
3. Design Entity Framework models
4. Establish project architecture
5. Begin Phase 1: authentication and basic rooms

## Questions for Consideration

1. Skill system: Any additional skills or categories?
2. Progression: Any special rules for skill advancement?
3. Admin interface: Web-based only, or in-game commands too?
4. World size: Initial number of rooms/zones?
5. Community: Out-of-game features (forums, wikis)?

---

*This quick reference complements the full specification document and provides a rapid overview of key decisions and priorities for the Mordecai MUD project.*