# Mordecai MUD - Game Design Specification

## Project Overview

**Project Name:** Mordecai  
**Type:** Text-based Multi-User Dungeon (MUD)  
**Platform:** Web-based application  
**Technology Stack:** .NET 8+, Blazor Web App, SQLite  
**Target Scale:** 10-50 concurrent players (intimate community focus)  
**Target Audience:** Classic MUD enthusiasts and new players interested in text-based RPG experiences  
**Vision Scope:** Comprehensive MUD with extensive features (this document covers core features with room for expansion)

## Core Vision

Mordecai is a modern, web-accessible text-based MUD that combines the classic feel of traditional MUDs with contemporary web technologies. Players will explore a persistent fantasy world, interact with other players, combat monsters, complete quests, and develop their characters through a rich text-based interface.

## Technical Architecture

### Frontend
- **Blazor Web App** with Server-Side Rendering (SSR) and Interactive WebAssembly components
- Real-time communication using SignalR for instant game updates
- Responsive web design accessible on desktop and mobile devices
- Clean, terminal-style UI with configurable themes

### Backend
- **.NET 8+ Web API** handling game logic and player actions
- **Entity Framework Core** with SQLite for data persistence
- **SignalR Hubs** for real-time multiplayer communication
- **Background Services** for game world simulation (NPCs, spawning, etc.)

### Database
- **SQLite** database for local development and deployment simplicity
- Designed for easy migration to PostgreSQL/SQL Server for production scaling

## Core Game Features

### 1. Player System
- **Character Creation**
  - Choose from multiple species (Human, Elf, Dwarf, Halfling, Orc)
  - Customize appearance and background
  - All characters start with the 7 core attribute skills
  - Initial skill point allocation for starting specialization

- **Skill System Architecture**
  - **Core Attribute Skills** (all characters start with these):
    - Physicality (STR) - physical strength and power
    - Dodge (DEX) - agility and evasion
    - Drive (END) - endurance and stamina
    - Reasoning (INT) - intelligence and logic
    - Awareness (ITT) - intuition and perception
    - Focus (WIL) - willpower and concentration
    - Bearing (PHY) - physical beauty and presence
  
  - **Weapon Skills** (learned through practice):
    - Swords, Axes, Maces, Polearms, Bows, Crossbows, Throwing, etc.
  
  - **Individual Spell Skills** (each spell is its own skill):
    - Fire Bolt, Heal, Lightning Strike, Invisibility, etc.
    - Organized by magic schools for mana management
  
  - **Mana Recovery Skills** (per magic school):
    - Fire Mana Recovery, Healing Mana Recovery, etc.
    - Determines how quickly mana regenerates for that school
  
  - **Crafting Skills**:
    - Blacksmithing, Alchemy, Carpentry, Cooking, etc.

- **Practice-Based Progression**
  - Skills improve through actual use (practice-based advancement)
  - No traditional "experience points" or "levels"
  - Skill advancement rates vary by skill type and difficulty
  - Actions are resolved through skill checks, never direct attribute use
  - Higher skills unlock advanced techniques and abilities

- **Player Profile**
  - Individual skill ratings and progression history
  - Achievement system based on skill milestones
  - Play time tracking and character statistics
  - Social features (friends, guilds)

### 2. World System
- **Room-Based World**
  - Interconnected rooms with descriptions
  - Multiple zones with different themes and difficulty levels
  - Dynamic weather and time-of-day effects
  - Hidden areas and secret passages

- **World Persistence**
  - Player actions affect the world state
  - Items can be dropped and picked up by other players
  - Room modifications persist between sessions

### 3. Combat System
- **Real-Time Combat with Cooldowns**
  - Continuous real-time combat to accommodate multiplayer and AFK players
  - Action cooldowns based on weapon type, spell complexity, and skill level
  - Higher skill levels reduce cooldown times and improve effectiveness
  - Combat actions resolved through skill checks vs. target's defensive skills

- **PvE Combat**
  - Diverse monster types with unique abilities
  - Boss encounters in special areas
  - Loot drops and experience rewards

- **PvP Combat**
  - Optional player vs player in designated areas
  - Dueling system for consensual combat
  - Guild wars and factional conflicts

### 4. Items and Equipment
- **Equipment System**
  - Weapons, armor, and accessories
  - Equipment stats and magical properties
  - Durability and repair mechanics
  - Set bonuses for matched equipment

- **Inventory Management**
  - Limited carrying capacity
  - Item containers and storage
  - Trading between players
  - Auction house system

### 5. Magic and Spells
- **Individual Spell Skills System**
  - Each spell is a separate skill that must be learned and practiced
  - Spells organized into schools (Fire, Healing, Illusion, etc.)
  - Spell effectiveness increases with individual spell skill level
  - Area of effect, targeted, and self-buff spell types

- **Mana System**
  - Separate mana pools for each magic school
  - Mana recovery rate determined by school-specific recovery skills
  - Casting spells consumes mana from the appropriate school
  - Mana regeneration continues in real-time, even when offline

- **Magic Items**
  - Enchanted weapons and armor
  - Consumable magical items
  - Artifacts with unique properties
  - Items may provide bonuses to specific spell skills or mana recovery

### 6. Social Features
- **Communication**
  - Global chat channels
  - Local area communication
  - Private messaging
  - Guild/party chat

- **Guilds**
  - Player-created organizations
  - Guild halls and shared resources
  - Rank and permission systems
  - Guild vs guild competition

### 7. Quests and NPCs
- **Quest System**
  - Story-driven main quests
  - Side quests and daily tasks
  - Dynamic quest generation
  - Quest rewards and progression

- **NPCs**
  - Interactive non-player characters
  - Merchant and trainer NPCs
  - Quest givers and story characters
  - AI-driven NPC behaviors

### 8. Economy
- **Currency System**
  - Primary currency (gold pieces)
  - Alternative currencies for special areas
  - Bank and storage systems

- **Player Economy**
  - Player-run shops
  - Resource gathering and crafting
  - Market price fluctuations
  - Economic events and challenges

### 9. Administrative Tools
- **Content Creation System**
  - Web-based admin interface for authorized users
  - Zone and room creation/editing tools
  - NPC creation and behavior scripting
  - Quest designer with branching logic
  - Item and equipment creator
  - Monster/creature design tools

- **Game Management**
  - Player administration (bans, warnings, etc.)
  - Real-time game monitoring
  - Economy management and oversight
  - Content approval workflow
  - Backup and restore functionality

## Technical Features

### 1. Real-Time Communication
- **SignalR Integration**
  - Instant message delivery
  - Real-time game state updates
  - Connection management and reconnection
  - Scalable group management

### 2. Data Management
- **Entity Framework Core**
  - Code-first database design
  - Automatic migrations
  - Optimized queries for game performance
  - Audit logging for player actions

### 3. Performance Optimization
- **Caching Strategy**
  - Redis-compatible caching for game state
  - Session state management
  - Static content optimization

- **Scalability Design**
  - Horizontal scaling preparation
  - Load balancing considerations
  - Database optimization for concurrent users

### 4. Security Features
- **Authentication & Authorization**
  - ASP.NET Core Identity integration
  - Role-based access control
  - Account security measures
  - Anti-cheating mechanisms

## User Interface Design

### 1. Game Interface
- **Terminal-Style Display**
  - Scrolling text output
  - Command input field
  - Configurable fonts and colors
  - Screen reader compatibility

- **Information Panels**
  - Character status display
  - Mini-map (optional)
  - Chat windows
  - Inventory quick-view

### 2. Web Interface
- **Responsive Design**
  - Mobile-friendly layout
  - Progressive Web App (PWA) capabilities
  - Offline mode for character viewing
  - Touch-friendly controls

### 3. Accessibility
- **WCAG Compliance**
  - Screen reader support
  - Keyboard navigation
  - High contrast themes
  - Font size adjustment

## Development Phases

### Phase 1: World Foundation (Weeks 1-4)
- Project setup and architecture
- Database design and Entity Framework setup
- Basic Blazor application structure
- User authentication system
- **Priority 1: Basic room system and character movement**
- **Room descriptions and "look" commands**
- **In-world talking and basic communication**
- **Looking into neighboring rooms/areas**

### Phase 2: Character Systems (Weeks 5-8)
- **Priority 2: Character creation system**
- **Skill-based progression and advancement**
- **Attribute system implementation**
- Character persistence and save/load
- Basic inventory framework

### Phase 3: Combat Foundation (Weeks 9-12)
- **Priority 3: Basic combat mechanics**
- **Melee combat system**
- **Ranged combat (bows, throwing)**
- **Spell casting system**
- Basic monster encounters
- Combat balance and testing

### Phase 4: Social & Admin Tools (Weeks 13-16)
- Real-time chat system (SignalR)
- Multi-player interactions and guilds
- **Administrative content creation tools**
- **Zone and room creation interface**
- **NPC and quest creation tools**

### Phase 5: Content & Polish (Weeks 17-20)
- Advanced quest system
- Magic schools and spell variety
- Economic systems
- Performance optimization
- Beta testing and refinement

### Phase 6: Extended Features (Future)
- Advanced crafting systems
- Complex quest chains
- Player housing
- Advanced PvP mechanics
- Mobile optimization
- Community features

## Success Metrics

### Technical Metrics
- **Performance**: <100ms response time for game commands
- **Reliability**: 99.9% uptime target
- **Scalability**: Support for 50+ concurrent users (10-50 target range)
- **Security**: Zero critical security vulnerabilities

### Gameplay Metrics
- **Player Retention**: 70% of players return within 7 days
- **Session Length**: Average session > 30 minutes
- **Player Progression**: 80% of players reach level 10
- **Social Engagement**: 60% of players join guilds or groups

## Risk Assessment

### Technical Risks
- **Real-time Performance**: SignalR scaling challenges
- **Database Performance**: SQLite limitations with high concurrency
- **Browser Compatibility**: WebAssembly support variations

### Mitigation Strategies
- Implement comprehensive caching
- Design for database migration path
- Progressive enhancement for browser features
- Extensive testing across platforms

## Future Enhancements

### Post-Launch Features
- **Mobile Native Apps**: iOS and Android clients
- **Advanced AI**: Machine learning for dynamic content
- **User-Generated Content**: Player-created areas and quests
- **Analytics Dashboard**: Real-time game metrics
- **API Integration**: Third-party tools and bots

### Technology Upgrades
- **Database Migration**: Move to PostgreSQL for production
- **Microservices**: Split into dedicated services for scaling
- **Cloud Deployment**: Azure/AWS hosting with auto-scaling
- **CDN Integration**: Global content delivery optimization

---

*This specification serves as the foundation for the Mordecai MUD development project. It will be updated and refined throughout the development process based on feedback and technical discoveries.*