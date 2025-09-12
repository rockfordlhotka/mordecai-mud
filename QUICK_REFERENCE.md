# Mordecai MUD - Quick Reference

## Technology Stack Summary

| Component | Technology | Justification |
|-----------|------------|---------------|
| **Frontend** | Blazor Web App | Modern .NET web framework, real-time capabilities |
| **Backend** | .NET 8+ Web API | High performance, robust ecosystem |
| **Database** | SQLite → PostgreSQL | Simple start, scalable migration path |
| **Real-time** | SignalR | Native .NET real-time communication |
| **ORM** | Entity Framework Core | Code-first, strong typing, migrations |
| **UI Style** | Terminal/Console Theme | Classic MUD aesthetic, accessibility |

## Core Game Systems

### Character System
- **Races**: Human, Elf, Dwarf, Halfling, Orc
- **Classes**: Warrior, Mage, Rogue, Cleric, Ranger  
- **Levels**: 1-50 progression
- **Stats**: STR, DEX, INT, WIS, CON, CHA

### World Design
- **Structure**: Room-based navigation
- **Scale**: Multiple zones with themes
- **Persistence**: Player actions affect world state
- **Time**: Dynamic day/night and weather

### Combat Mechanics
- **Style**: Turn-based tactical combat
- **Types**: PvE (monsters) and optional PvP
- **Features**: Weapons, spells, status effects
- **Balance**: Level-appropriate encounters

## Development Priorities

### Phase 1: World Foundation (Weeks 1-4)
1. Project setup and .NET solution structure
2. Database schema design with Entity Framework
3. Basic Blazor application with authentication
4. **Core room system and character movement (PRIORITY 1)**
5. **Room descriptions, "look" commands, and neighboring room visibility**
6. **In-world talking and basic communication**

### Phase 2: Character Systems (Weeks 5-8)
1. **Character creation system (PRIORITY 2)**
2. **Skill-based progression and advancement**
3. **Attribute system implementation**
4. Character persistence and save/load
5. Basic inventory framework

### Phase 3: Combat Foundation (Weeks 9-12)
1. **Basic combat mechanics (PRIORITY 3)**
2. **Melee combat system**
3. **Ranged combat (bows, throwing weapons)**
4. **Spell casting system**
5. Basic monster encounters and balance

### Phase 4: Admin Tools & Social (Weeks 13-16)
1. **Administrative content creation tools**
2. **Zone and room creation interface**
3. **NPC and quest creation tools**
4. Real-time chat system (SignalR)
5. Multi-player interactions and guilds

## Key Technical Decisions

### Why Blazor Web App?
- **Single Technology Stack**: Use C# for both frontend and backend
- **Real-time Capabilities**: Native SignalR integration
- **Modern Web Standards**: Progressive Web App potential
- **Accessibility**: Better screen reader support than pure SPA

### Why SQLite First?
- **Development Speed**: No external database setup required
- **Deployment Simplicity**: Single file database, easy backup
- **Migration Path**: EF Core makes PostgreSQL transition seamless
- **Cost Effective**: No database hosting costs for small deployments

### Why SignalR for Real-time?
- **Native Integration**: Built into .NET ecosystem
- **Automatic Fallbacks**: WebSockets → Server-Sent Events → Long Polling
- **Scaling Options**: Redis backplane for multi-server deployments
- **Type Safety**: Strongly typed hubs with C# client

## Success Metrics to Track

### Technical Performance
- Response time < 100ms for game commands
- Support 50+ concurrent users (targeting 10-50)
- 99.9% uptime target
- Zero critical security vulnerabilities

### Player Engagement
- 70% player retention within 7 days
- Average session length > 30 minutes
- 80% of players reach level 10
- 60% of players join social groups

## Next Steps

1. **Review and Approve Specification**: Ensure all features align with vision
2. **Set Up Development Environment**: Create .NET solution structure
3. **Design Database Schema**: Plan Entity Framework models
4. **Create Project Architecture**: Establish folder structure and dependencies
5. **Begin Phase 1 Implementation**: Start with authentication and basic rooms

## Questions for Consideration

1. **Skill System Details**: What specific skills do you envision? (swords, archery, magic schools, etc.)
2. **Character Attributes**: Should we stick with the 7 attributes mentioned (STR, DEX, END, INT, INT, WIL, PB)?
3. **Combat Mechanics**: Turn-based rounds, or real-time with cooldowns?
4. **Admin Interface**: Should content creation tools be web-based or in-game commands?
5. **Progression Rate**: How quickly should players advance skills and gain experience?
6. **World Size**: How many rooms/zones should we plan for initially?

---

*This quick reference complements the full specification document and provides a rapid overview of key decisions and priorities for the Mordecai MUD project.*