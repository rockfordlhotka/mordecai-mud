# Mordecai MUD - Game Design Specification

## Project Overview

**Project Name:** Mordecai  
**Type:** Text-based Multi-User Dungeon (MUD)  
**Platform:** Web-based application  
**Technology Stack:** .NET 9, Blazor Web App, SQLite  
**Target Scale:** 10-50 concurrent players (intimate community focus)  
**Target Audience:** Classic MUD enthusiasts and new players interested in text-based RPG experiences  
**Vision Scope:** Comprehensive MUD with extensive features (this document covers core features with room for expansion)

## Core Vision

Mordecai is a modern, web-accessible text-based MUD that combines the classic feel of traditional MUDs with contemporary web technologies. Players will explore a persistent fantasy world, interact with other players, combat monsters, complete quests, and develop their characters through a rich text-based interface.

## Core Dice Mechanics

Mordecai uses a **4dF (Fudge/Fate Dice)** system as the foundation for all skill checks, combat resolution, and random events. This provides consistent, predictable probability curves that favor average results while still allowing for dramatic successes and failures.

### Fudge Dice (dF) Properties
- **6-sided dice** with special faces:
  - **2 "+" sides**: Positive result (+1 each)
  - **2 "-" sides**: Negative result (-1 each)  
  - **2 blank sides**: Neutral result (0 each)
- **4dF roll range**: -4 to +4 (bell curve distribution)
- **Average result**: 0 (most common outcome)
- **Probability distribution**: Heavily weighted toward 0, with decreasing probability for extreme results

### Standard 4dF Resolution
- **Skill Check Formula**: `Character Skill + 4dF vs. Target Number or Opposing Skill + 4dF`
- **Combat Resolution**: `Attack Skill + 4dF vs. Defense Skill + 4dF`
- **Difficulty Modifiers**: Applied as bonuses/penalties to the target number
- **Margin of Success**: Difference between final results determines outcome quality

### Dice Roll Notation
- **4dF**: Standard fudge dice roll (no exploding mechanics)
- **4dF+**: Exploding fudge dice roll (uses exploding mechanics described below)

### Exploding Dice Mechanics
For dramatic moments and critical situations, some rolls use **exploding dice** rules (denoted as **4dF+**):

- **+4 Result (Maximum Success)**: 
  - Roll 4dF again, but **only count the "+" results**
  - Add these additional "+" results to the total
  - Continue rolling if another +4 is achieved (rare but possible)
  
- **-4 Result (Critical Failure)**:
  - Roll 4dF again, but **only count the "-" results** 
  - Add these additional "-" results to the total (making failure worse)
  - Continue rolling if another -4 is achieved

### When Exploding Dice Apply
- **Combat**: Critical hits and fumbles use **4dF+** exploding mechanics
- **High-Stakes Skill Checks**: Important story moments, dangerous actions use **4dF+**
- **Spell Casting**: Particularly powerful or risky magic uses **4dF+**
- **Crafting**: When attempting masterwork items or using rare materials uses **4dF+**
- **Social Encounters**: Dramatic persuasion, intimidation, or deception attempts use **4dF+**
- **Standard Actions**: Most routine actions use standard **4dF** without exploding

### Examples
- **Standard Combat**: Sword skill 12 + **4dF** (+1) = 13 vs. Dodge skill 10 + **4dF** (-1) = 9. Attack succeeds by 4.
- **Exploding Success**: Fire Bolt skill 8 + **4dF+** (+4) = 12, then exploding roll 4dF yields 2 more "+" = 14 total. Devastating magical attack!
- **Exploding Failure**: Lockpicking skill 6 + **4dF+** (-4) = 2, then exploding roll 4dF yields 2 more "-" = 0 total. Lock mechanism jams permanently.

This dice system provides consistent mechanics across all game systems while maintaining excitement through the possibility of extraordinary results during critical moments.

## Attribute System and Species Modifiers

### Base Attribute Calculation

All characters start with seven core attributes that serve as the foundation for all derived skills and secondary statistics. These attributes are calculated using the 4dF system combined with species-specific modifiers.

#### Human Baseline (Default Species)
For Humans, all seven attributes are calculated as:
**Attribute Value = 4dF + 10**

This gives each Human attribute an average value of **10**, with the following probability distribution:
- **Range**: 6-14 (possible values)
- **Average**: 10 (most common result)
- **Distribution**: Bell curve weighted toward 10, with decreasing probability for extreme values

#### Core Attributes
All characters possess these seven core attributes:
1. **Physicality (STR)** - Physical strength and power
2. **Dodge (DEX)** - Agility and evasion ability  
3. **Drive (END)** - Endurance and stamina
4. **Reasoning (INT)** - Intelligence and logical thinking
5. **Awareness (ITT)** - Intuition and perception
6. **Focus (WIL)** - Willpower and mental concentration
7. **Bearing (PHY)** - Physical beauty and social presence

### Species Attribute Modifiers

Each non-Human species applies specific modifiers to the base Human attribute calculation, creating distinct racial advantages and disadvantages:

#### Available Species and Modifiers

- **Human**: No modifiers (baseline)
  - All attributes: 4dF + 10

- **Elf**: Intellectual and agile, but physically delicate
  - **Reasoning (INT)**: 4dF + 11 (+1 modifier)
  - **Physicality (STR)**: 4dF + 9 (-1 modifier)
  - All other attributes: 4dF + 10

- **Dwarf**: Strong and resilient, but less agile
  - **Physicality (STR)**: 4dF + 11 (+1 modifier)
  - **Dodge (DEX)**: 4dF + 9 (-1 modifier)
  - All other attributes: 4dF + 10

- **Halfling**: Quick and perceptive, but physically weak
  - **Dodge (DEX)**: 4dF + 11 (+1 modifier)
  - **Awareness (ITT)**: 4dF + 11 (+1 modifier)
  - **Physicality (STR)**: 4dF + 8 (-2 modifier)
  - All other attributes: 4dF + 10

- **Orc**: Physically powerful and enduring, but less intelligent and social
  - **Physicality (STR)**: 4dF + 12 (+2 modifier)
  - **Drive (END)**: 4dF + 11 (+1 modifier)
  - **Reasoning (INT)**: 4dF + 9 (-1 modifier)
  - **Bearing (PHY)**: 4dF + 9 (-1 modifier)
  - All other attributes: 4dF + 10

### Attribute Usage and Relationships

#### Direct Attribute Effects
While all action resolution uses skills rather than raw attributes, attributes serve several critical functions:

1. **Skill Starting Values**: Many skills begin with a base value derived from related attributes
2. **Health Calculations**: Primary and secondary health pools are calculated from attributes
3. **Skill Advancement Modifiers**: Higher attributes may provide learning bonuses for related skills
4. **Equipment Requirements**: Some items may require minimum attribute thresholds
5. **Skill Caps**: Attributes may influence the maximum potential of related skills

#### Health Pool Calculations
- **Fatigue (FAT)**: (Drive × 2) - 5
  - Represents stamina, exhaustion, and non-lethal damage capacity
- **Vitality (VIT)**: (Physicality + Drive) - 5  
  - Represents life force and lethal damage capacity

#### Skill Relationship Examples
- **Weapon Skills**: May use Physicality for damage bonuses and weapon requirements
- **Dodge Skill**: Starts with base value influenced by Dodge attribute
- **Spell Skills**: Different spells may have attribute relationships (Fire Bolt with Focus, etc.)
- **Social Skills**: Bearing influences persuasion, intimidation, and leadership abilities
- **Crafting Skills**: Reasoning affects complex crafting and blueprint understanding

### Character Creation Impact

During character creation:
1. **Species Selection**: Player chooses species, which determines attribute modifiers
2. **Attribute Rolling**: System rolls 4dF for each attribute and applies species modifiers
3. **Health Calculation**: Fatigue and Vitality are automatically calculated from final attributes
4. **Starting Skills**: Core attribute skills are set to their calculated attribute values
5. **Skill Point Allocation**: Player receives bonus skill points to distribute among learned skills

### Balancing Considerations

- **No "Dump Stats"**: All attributes provide meaningful benefits to avoid useless attributes
- **Species Trade-offs**: Each species has both advantages and disadvantages
- **Skill-Based Resolution**: Attributes influence but never directly resolve actions
- **Long-term Growth**: Skills can advance far beyond starting attribute limitations through practice
- **Meaningful Choices**: Species selection creates distinct playstyle opportunities without hard restrictions

This attribute system provides character diversity at creation while maintaining the skill-based progression focus throughout gameplay.

## Technical Architecture


### Frontend
- **Blazor Web App** (Server mode) with SSR and interactive pages
- Real-time client-server communication via Blazor Server’s built-in SignalR
- Responsive web design for desktop and mobile
- Terminal-style UI with configurable themes




### Backend
- **.NET 9 Blazor Server**: All game logic and state managed server-side
- **Entity Framework Core** with SQLite for data persistence
- **RabbitMQ** for async messaging between users, NPCs, and system events
- **OpenTelemetry** for distributed tracing, logging, and metrics
- **.NET Aspire** as the app host for development:
  - Orchestrates all services (web, RabbitMQ, database, etc.)
  - Simplifies local development and service discovery
  - Built-in OpenTelemetry and distributed tracing
  - Easily run RabbitMQ and other dependencies as containers
- **Background Services** for world simulation, NPCs, and event processing


### Database
- **SQLite** for local development and deployment simplicity
- Designed for easy migration to PostgreSQL/SQL Server for production scaling

## Core Game Features

### 1. Player System
- **Account & Characters**
  - Each player account may own multiple characters (initial soft cap 3; configurable)
  - Upon login, players are taken to a Character Selection screen listing their existing characters (name, species, last played)
  - From this screen players can: Create New Character, Delete (soft-delete future), or Enter World with a selected character
  - Future: Recently played character shortcut on landing page
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
  
  - **Health System**:
    - **Fatigue (FAT)**: Calculated as (END × 2) - 5, represents stamina and exhaustion
    - **Vitality (VIT)**: Calculated as (STR + END) - 5, represents physical health and life force
    - Both FAT and VIT track current values that can be reduced by damage and restored by rest/healing
    - Death occurs when VIT reaches 0; unconsciousness when FAT reaches 0
    - **Pending Damage/Healing System**:
      - Each character and NPC has pending FAT and VIT pools that accumulate damage/healing
      - Positive pending values represent damage to be applied over time
      - Negative pending values represent healing to be applied over time
      - Every 10 seconds, half the pending pool value is applied to current health (rounded to ensure pools reach zero)
      - This creates a gradual damage/healing effect rather than instant application
  
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
  - Current health status (Fatigue and Vitality levels)
  - Pending damage and healing pools (visible to show incoming effects)
  - Achievement system based on skill milestones
  - Play time tracking and character statistics
  - Social features (friends, guilds)

### 2. World System
- **Zone-Based World Organization**
  - World is organized into distinct zones, each containing multiple interconnected rooms
  - Zones represent thematic areas (towns, dungeons, wilderness, etc.) with consistent themes and difficulty levels
  - Each zone has configurable properties: name, description, level range, PvP flags, and special rules
  - Zone boundaries may have travel restrictions or require specific conditions to cross

- **Room-Based Navigation**
  - Individual rooms within zones contain detailed descriptions and interactive elements
  - Rooms connect to other rooms via exits (north, south, east, west, up, down, and custom directions)
  - Room connections can span zone boundaries for world continuity
  - Hidden areas and secret passages discoverable through exploration or specific conditions
  - Dynamic weather and time-of-day effects affect room descriptions and gameplay

- **World Persistence**
  - Player actions affect the world state within rooms and zones
  - Items can be dropped and picked up by other players in specific rooms
  - Room modifications and zone events persist between sessions
  - Zone-wide events and changes can affect all rooms within that zone
  - Resurrection system for player characters in specific zones

### 3. Combat System
- **Real-Time Combat with Cooldowns**
  - Continuous real-time combat to accommodate multiplayer and AFK players
  - Action cooldowns based on weapon type, spell complexity, and skill level
  - Higher skill levels reduce cooldown times and improve effectiveness
  - Combat actions resolved through skill checks vs. target's defensive skills

- **Health and Damage System**
  - **Fatigue (FAT)** damage represents exhaustion, stunning, and non-lethal harm
  - **Vitality (VIT)** damage represents serious injury and life-threatening wounds
  - Combat actions can target either or both health pools depending on attack type
  - **Pending Damage Pools**: Damage is not applied instantly but goes into pending pools
    - Damage accumulates in pending FAT and VIT pools (positive values)
    - Every 10 seconds, half the pending damage is applied to current health values
    - Multiple attacks can accumulate in pending pools before being applied
    - This creates realistic "bleeding out" or gradual injury effects
  - **Healing System**: Works through the same pending pool mechanism
    - Healing adds negative values to pending pools (representing recovery)
    - Negative pending values gradually restore current health over time
    - Allows for "healing over time" effects and prevents instant full healing
  - Damage calculations based on weapon type, skill levels, and armor protection
  - Recovery systems for both health types (rest, healing, medical treatment)

- **PvE Combat**
  - Diverse monster types with unique abilities
  - Boss encounters in special areas
  - Loot drops and skill advancement opportunities

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
  - **Health tracking** with same Fatigue and Vitality system as player characters
  - **Pending damage/healing pools** identical to player system for consistent combat mechanics
  - NPCs use attribute skills for health calculations and combat resolution

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
  - Web-based admin interface integrated into main application
  - Role-based access control for authorized users
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


### 1. Real-Time & Async Communication
- **Blazor Server** provides real-time updates between browser and server
- **RabbitMQ** handles all async, cross-user, and NPC/system messaging
  - Decouples game logic, enables scalable event-driven architecture
  - All chat, combat, and world events flow through RabbitMQ
- **Background Services** for timed game mechanics
  - Health application timer (10-second intervals for pending pool processing)
  - World simulation and NPC behavior processing
  - Scheduled events and maintenance tasks

### 2. Data Management
- **Entity Framework Core**
  - Code-first database design
  - Automatic migrations
  - Optimized queries for game performance
  - Audit logging for player actions

### 3. Performance & Observability
- **Caching Strategy**
  - Redis-compatible caching for game state
  - Session state management
  - Static content optimization
- **Scalability Design**
  - RabbitMQ enables distributed, event-driven scaling
  - Load balancing and horizontal scaling ready
  - Database optimization for concurrent users
- **Observability**
  - OpenTelemetry for tracing, logging, and metrics
  - Export to Azure Monitor, Jaeger, Zipkin, or other backends

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
  - Multi-character per account selection UI (list + create)
- **Health tracking implementation**
  - Fatigue and Vitality calculation and persistence
  - Pending damage and healing pool system
  - Health application timer (10-second intervals)
  - Current health status display in character interface
- **Skill-based progression and advancement**
- **Attribute system implementation**
- Character persistence and save/load
- Basic inventory framework

### Phase 3: Combat Foundation (Weeks 9-12)
- **Priority 3: Basic combat mechanics**
- **Health and damage system implementation**
  - Pending pool damage accumulation and application
  - Gradual damage and healing effects over time
  - Combat damage routing to pending pools
  - Healing spell and rest mechanics using pending pools
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