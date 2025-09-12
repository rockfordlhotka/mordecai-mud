# Mordecai MUD - Database Design

## Overview
This document outlines the database schema design for Mordecai MUD, focusing on the skill-based character progression system and real-time multiplayer architecture.

## Core Entities

### Users & Authentication
```sql
-- User accounts and authentication
Users
- Id (Primary Key)
- Username (Unique)
- Email 
- PasswordHash
- Created
- LastLogin
- IsActive
- Roles (Admin, Builder, Player)

-- Character profiles
Characters
- Id (Primary Key)
- UserId (Foreign Key -> Users.Id)
- Name (Unique)
- Species (Human, Elf, Dwarf, Halfling, Orc)
- Description
- CurrentRoomId (Foreign Key -> Rooms.Id)
- Created
- LastActive
- IsOnline
```

### Skills System

```sql
-- Master list of all possible skills
SkillDefinitions
- Id (Primary Key)
- Name (e.g., "Physicality", "Fire Bolt", "Swords")
- SkillType (AttributeSkill, WeaponSkill, SpellSkill, ManaRecoverySkill, CraftingSkill)
- Category (e.g., "Fire Magic", "Melee Weapons")
- Description
- BaseSkillRequired (Foreign Key -> SkillDefinitions.Id, nullable)
- MinimumStartingLevel
- MaximumLevel
- IsStartingSkill (true for the 7 attribute skills)

-- Individual character's skill levels
CharacterSkills
- Id (Primary Key)
- CharacterId (Foreign Key -> Characters.Id)
- SkillDefinitionId (Foreign Key -> SkillDefinitions.Id)
- CurrentLevel (decimal for fractional progression)
- ExperiencePoints (accumulated practice points)
- LastUsed
- UNIQUE(CharacterId, SkillDefinitionId)

-- Track skill usage for practice-based progression
SkillUsageLog
- Id (Primary Key)
- CharacterId (Foreign Key -> Characters.Id)
- SkillDefinitionId (Foreign Key -> SkillDefinitions.Id)
- UsageType (Practice, Combat, Crafting, etc.)
- ExperienceGained
- Timestamp

-- Effect definitions (buffs, debuffs, conditions)
EffectDefinitions
- Id (Primary Key)
- Name (e.g., "Poison", "Strength Boost", "Paralysis")
- Description
- EffectType (Buff, Debuff, Condition, Disease)
- Duration (seconds, 0 = permanent until removed)
- IsStackable (can multiple instances exist on same character)
- MaxStacks (if stackable, maximum number)
- TickInterval (seconds between effect applications, 0 = no ticking)
- IconName (for UI display)
- EffectColor (for UI theming)
- RemovalMethods (JSON: ["spell", "item", "time", "death", "zone"])
- CreatedBy (Foreign Key -> Users.Id)
- IsActive

-- Effect impacts on character attributes and skills
EffectImpacts
- Id (Primary Key)
- EffectDefinitionId (Foreign Key -> EffectDefinitions.Id)
- ImpactType (SkillBonus, SkillPenalty, AttributeBonus, AttributePenalty, HealthRegen, ManaRegen, DamageOverTime, etc.)
- TargetSkillId (Foreign Key -> SkillDefinitions.Id, nullable)
- TargetAttribute (nullable: Health, Mana, MovementSpeed, etc.)
- ImpactValue (numeric value of the effect)
- ImpactFormula (nullable: formula for complex calculations)
- IsPercentage (true for percentage-based, false for flat values)

-- Active effects on characters
CharacterEffects
- Id (Primary Key)
- CharacterId (Foreign Key -> Characters.Id)
- EffectDefinitionId (Foreign Key -> EffectDefinitions.Id)
- SourceType (Spell, Item, NPC, Environmental, System)
- SourceId (ID of the source: spell, item, NPC, etc.)
- SourceName (display name of source)
- StackCount (current number of stacks if stackable)
- StartTime
- EndTime (nullable for permanent effects)
- LastTickTime (for effects that tick periodically)
- RemainingDuration (calculated field for UI)
- Potency (multiplier for effect strength, default 1.0)
- CustomData (JSON for effect-specific data)
- IsActive
```

### Magic & Mana System

```sql
-- Magic schools for mana management
MagicSchools
- Id (Primary Key)
- Name (Fire, Healing, Illusion, etc.)
- Description
- Color (for UI theming)

-- Link spells to their magic schools
SpellSchools
- SpellSkillId (Foreign Key -> SkillDefinitions.Id)
- MagicSchoolId (Foreign Key -> MagicSchools.Id)
- PRIMARY KEY(SpellSkillId, MagicSchoolId)

-- Character mana pools per school
CharacterMana
- Id (Primary Key)
- CharacterId (Foreign Key -> Characters.Id)
- MagicSchoolId (Foreign Key -> MagicSchools.Id)
- CurrentMana
- MaximumMana
- RecoveryRate (mana per minute)
- LastRecoveryUpdate
- UNIQUE(CharacterId, MagicSchoolId)
```

### World & Rooms

```sql
-- Game world zones
Zones
- Id (Primary Key)
- Name
- Description
- DifficultyLevel
- CreatedBy (Foreign Key -> Users.Id)
- IsActive

-- Room type definitions and behaviors
RoomTypes
- Id (Primary Key)
- Name (Normal, Shop, Temple, Inn, Bank, etc.)
- Description
- AllowsCombat (boolean)
- AllowsLogout (boolean)
- HasSpecialCommands (boolean)
- HealingRate (multiplier for health/mana recovery)
- SkillLearningBonus (multiplier for skill advancement)
- MaxOccupancy (0 = unlimited)
- RequiredSkillId (Foreign Key -> SkillDefinitions.Id, nullable)
- RequiredSkillLevel
- EntryMessage
- ExitMessage
- BackgroundColor (for UI theming)
- IsActive

-- Individual rooms in the world
Rooms
- Id (Primary Key)
- ZoneId (Foreign Key -> Zones.Id)
- RoomTypeId (Foreign Key -> RoomTypes.Id)
- Name
- Description
- Coordinates (X, Y, Z for mapping)
- CreatedBy (Foreign Key -> Users.Id)
- IsActive
- CustomProperties (JSON for room-specific overrides)

-- Connections between rooms (exits)
RoomExits
- Id (Primary Key)
- FromRoomId (Foreign Key -> Rooms.Id)
- ToRoomId (Foreign Key -> Rooms.Id)
- Direction (North, South, East, West, Up, Down, etc.)
- ExitDescription
- IsHidden
- SkillRequired (Foreign Key -> SkillDefinitions.Id, nullable)
- SkillLevelRequired

-- Atmospheric events for zones
ZoneAtmosphericEvents
- Id (Primary Key)
- ZoneId (Foreign Key -> Zones.Id)
- EventText
- EventType (Sound, Smell, Sight, Weather, General)
- Frequency (VeryRare, Rare, Uncommon, Common, Frequent)
- TimeOfDay (Any, Dawn, Morning, Midday, Afternoon, Evening, Night, Midnight)
- WeatherType (Any, Clear, Cloudy, Rainy, Stormy, Snowy, Foggy)
- IsActive
- CreatedBy (Foreign Key -> Users.Id)

-- Atmospheric events for room types
RoomTypeAtmosphericEvents
- Id (Primary Key)
- RoomTypeId (Foreign Key -> RoomTypes.Id)
- EventText
- EventType (Sound, Smell, Sight, Ambient, Activity)
- Frequency (VeryRare, Rare, Uncommon, Common, Frequent)
- RequiredOccupancy (minimum players in room for event to trigger)
- IsActive
- CreatedBy (Foreign Key -> Users.Id)

-- Track when atmospheric events last occurred in specific rooms
RoomAtmosphericEventLog
- Id (Primary Key)
- RoomId (Foreign Key -> Rooms.Id)
- EventId (Foreign Key -> ZoneAtmosphericEvents.Id or RoomTypeAtmosphericEvents.Id)
- EventSource (Zone, RoomType)
- LastTriggered
- TriggerCount
```

### Items & Equipment

```sql
-- Template definitions for items
ItemTemplates
- Id (Primary Key)
- Name
- Description
- ItemType (Weapon, Armor, Consumable, etc.)
- WeaponType (Sword, Bow, Staff, etc.) -- nullable
- ArmorSlot (Head, Chest, Legs, etc.) -- nullable
- Weight
- Value
- IsStackable
- MaxStackSize
- CreatedBy (Foreign Key -> Users.Id)

-- Actual item instances in the game world
Items
- Id (Primary Key)
- ItemTemplateId (Foreign Key -> ItemTemplates.Id)
- CurrentRoomId (Foreign Key -> Rooms.Id, nullable)
- OwnerCharacterId (Foreign Key -> Characters.Id, nullable)
- EquipSlot (nullable, if equipped)
- StackSize
- Condition (durability)
- IsEquipped
- CustomProperties (JSON for magical bonuses, etc.)

-- Item effects on skills
ItemSkillBonuses
- Id (Primary Key)
- ItemTemplateId (Foreign Key -> ItemTemplates.Id)
- SkillDefinitionId (Foreign Key -> SkillDefinitions.Id)
- BonusType (FlatBonus, PercentageBonus, CooldownReduction)
- BonusValue
```

### Combat System

```sql
-- Combat encounters and participants
CombatEncounters
- Id (Primary Key)
- RoomId (Foreign Key -> Rooms.Id)
- StartTime
- EndTime
- Status (Active, Completed, Fled)

-- Participants in combat
CombatParticipants
- Id (Primary Key)
- EncounterId (Foreign Key -> CombatEncounters.Id)
- CharacterId (Foreign Key -> Characters.Id, nullable for NPCs)
- NPCId (Foreign Key -> NPCs.Id, nullable for characters)
- CurrentHitPoints
- MaximumHitPoints
- LastActionTime
- IsDefeated

-- Combat action queue for real-time processing
CombatActions
- Id (Primary Key)
- EncounterId (Foreign Key -> CombatEncounters.Id)
- ActorId (Foreign Key -> CombatParticipants.Id)
- TargetId (Foreign Key -> CombatParticipants.Id, nullable)
- ActionType (Attack, Cast, Use, Move)
- SkillUsed (Foreign Key -> SkillDefinitions.Id)
- ScheduledTime
- CooldownUntil
- Status (Queued, Executing, Completed)
```

### NPCs & Quests

```sql
-- Non-player characters
NPCs
- Id (Primary Key)
- TemplateId (Foreign Key -> NPCTemplates.Id)
- Name
- CurrentRoomId (Foreign Key -> Rooms.Id)
- CurrentHitPoints
- MaximumHitPoints
- LastAction
- Status (Alive, Dead, Disabled)

-- NPC templates for spawning
NPCTemplates
- Id (Primary Key)
- Name
- Description
- NPCType (Monster, Merchant, Trainer, Quest)
- BaseHitPoints
- RespawnTime (minutes)
- CreatedBy (Foreign Key -> Users.Id)

-- Zone encounter tables for spawning
ZoneEncounterTables
- Id (Primary Key)
- ZoneId (Foreign Key -> Zones.Id)
- NPCTemplateId (Foreign Key -> NPCTemplates.Id)
- SpawnProbability (0.0 to 1.0, percentage chance)
- MinimumLevel (minimum player skill level to encounter)
- MaximumLevel (maximum player skill level to encounter)
- MaxConcurrentSpawns (max of this NPC type in zone at once)
- SpawnCooldown (minutes between spawns)
- RequiredRoomTypes (JSON array of room type IDs where this can spawn)
- TimeOfDay (Any, Day, Night, Dawn, Dusk)
- IsActive
- CreatedBy (Foreign Key -> Users.Id)

-- Track active spawns in zones
ZoneActiveSpawns
- Id (Primary Key)
- ZoneId (Foreign Key -> Zones.Id)
- NPCTemplateId (Foreign Key -> NPCTemplates.Id)
- NPCId (Foreign Key -> NPCs.Id)
- SpawnedAt
- LastEncounterCheck
- PlayerLevel (skill level of player who triggered spawn)

-- NPC skill levels
NPCSkills
- Id (Primary Key)
- NPCTemplateId (Foreign Key -> NPCTemplates.Id)
- SkillDefinitionId (Foreign Key -> SkillDefinitions.Id)
- SkillLevel

-- Quest definitions
Quests
- Id (Primary Key)
- Name
- Description
- QuestType (Kill, Deliver, Collect, etc.)
- RequiredLevel (minimum skill level)
- RewardExperience
- RewardGold
- CreatedBy (Foreign Key -> Users.Id)
- IsActive

-- Character quest progress
CharacterQuests
- Id (Primary Key)
- CharacterId (Foreign Key -> Characters.Id)
- QuestId (Foreign Key -> Quests.Id)
- Status (Available, InProgress, Completed, Failed)
- Progress (JSON for quest-specific data)
- StartedAt
- CompletedAt
```

### Communication & Social

```sql
-- Chat channels and messages
ChatChannels
- Id (Primary Key)
- Name (Global, Local, Guild, etc.)
- ChannelType
- Description
- IsActive

-- Message history
ChatMessages
- Id (Primary Key)
- ChannelId (Foreign Key -> ChatChannels.Id)
- CharacterId (Foreign Key -> Characters.Id)
- Message
- Timestamp
- MessageType (Say, Tell, Emote, etc.)

-- Player guilds
Guilds
- Id (Primary Key)
- Name (Unique)
- Description
- Founded
- LeaderCharacterId (Foreign Key -> Characters.Id)
- IsActive

-- Guild membership
GuildMembers
- Id (Primary Key)
- GuildId (Foreign Key -> Guilds.Id)
- CharacterId (Foreign Key -> Characters.Id)
- Rank
- JoinedAt
- UNIQUE(GuildId, CharacterId)
```

## Indexes for Performance

```sql
-- Critical indexes for game performance
CREATE INDEX IX_Characters_UserId ON Characters(UserId);
CREATE INDEX IX_Characters_CurrentRoomId ON Characters(CurrentRoomId);
CREATE INDEX IX_Characters_IsOnline ON Characters(IsOnline);

CREATE INDEX IX_CharacterSkills_CharacterId ON CharacterSkills(CharacterId);
CREATE INDEX IX_CharacterSkills_SkillDefinitionId ON CharacterSkills(SkillDefinitionId);

CREATE INDEX IX_CharacterEffects_CharacterId ON CharacterEffects(CharacterId);
CREATE INDEX IX_CharacterEffects_EffectDefinitionId ON CharacterEffects(EffectDefinitionId);
CREATE INDEX IX_CharacterEffects_EndTime ON CharacterEffects(EndTime);
CREATE INDEX IX_CharacterEffects_IsActive ON CharacterEffects(IsActive);
CREATE INDEX IX_EffectDefinitions_IsActive ON EffectDefinitions(IsActive);

CREATE INDEX IX_Rooms_RoomTypeId ON Rooms(RoomTypeId);
CREATE INDEX IX_Items_CurrentRoomId ON Items(CurrentRoomId);
CREATE INDEX IX_Items_OwnerCharacterId ON Items(OwnerCharacterId);

CREATE INDEX IX_ZoneAtmosphericEvents_ZoneId ON ZoneAtmosphericEvents(ZoneId);
CREATE INDEX IX_ZoneAtmosphericEvents_IsActive ON ZoneAtmosphericEvents(IsActive);
CREATE INDEX IX_RoomTypeAtmosphericEvents_RoomTypeId ON RoomTypeAtmosphericEvents(RoomTypeId);
CREATE INDEX IX_RoomTypeAtmosphericEvents_IsActive ON RoomTypeAtmosphericEvents(IsActive);
CREATE INDEX IX_RoomAtmosphericEventLog_RoomId ON RoomAtmosphericEventLog(RoomId);

CREATE INDEX IX_ZoneEncounterTables_ZoneId ON ZoneEncounterTables(ZoneId);
CREATE INDEX IX_ZoneEncounterTables_IsActive ON ZoneEncounterTables(IsActive);
CREATE INDEX IX_ZoneActiveSpawns_ZoneId ON ZoneActiveSpawns(ZoneId);
CREATE INDEX IX_ZoneActiveSpawns_NPCTemplateId ON ZoneActiveSpawns(NPCTemplateId);

CREATE INDEX IX_CombatActions_EncounterId ON CombatActions(EncounterId);
CREATE INDEX IX_CombatActions_ScheduledTime ON CombatActions(ScheduledTime);

CREATE INDEX IX_ChatMessages_ChannelId_Timestamp ON ChatMessages(ChannelId, Timestamp);
```

## Key Design Decisions

### Practice-Based Progression
- **No Traditional Levels**: Characters advance individual skills through use
- **Fractional Progression**: Skills can have decimal values for smooth advancement
- **Usage Tracking**: All skill usage is logged for progression calculations

### Character Effects System
- **Long-Term Effects**: Support for buffs, debuffs, poisons, diseases, and spell effects
- **Flexible Duration**: Effects can be temporary with timers or permanent until removed
- **Multiple Sources**: Effects can come from spells, items, NPCs, or environmental factors
- **Stacking Control**: Effects can stack with limits or be mutually exclusive
- **Periodic Effects**: Support for damage/healing over time with configurable intervals
- **Complex Impacts**: Effects can modify skills, attributes, regeneration, or custom properties

### Real-Time Combat
- **Action Queue**: Combat actions are scheduled and executed in real-time
- **Cooldown System**: Each action has a cooldown period based on skill level
- **Persistent State**: Combat continues even when players disconnect

### Flexible Skill System
- **Skill Definitions**: All skills are data-driven, not hard-coded
- **Skill Dependencies**: Advanced skills can require prerequisite skills
- **Starting Skills**: All characters begin with the 7 attribute skills

### Room Type Behavior System
- **Data-Driven Room Behaviors**: Room types define specific behaviors and rules
- **Combat Restrictions**: Some rooms can disable combat (shops, temples)
- **Skill Learning Bonuses**: Certain room types boost skill advancement (training halls)
- **Healing Rates**: Temples and inns can provide enhanced recovery
- **Access Control**: Rooms can require specific skills or levels to enter
- **Special Commands**: Room types can enable unique interactions (banking, shopping)

### Atmospheric Events System
- **Immersive Environment**: Random atmospheric messages enhance world immersion
- **Zone-Level Events**: Weather, ambient sounds, and environmental effects
- **Room Type Events**: Specific atmospheric messages for different room types
- **Frequency Control**: Events can be tuned from very rare to frequent
- **Contextual Triggers**: Events can depend on time of day, weather, or occupancy
- **Admin Configurable**: Content creators can easily add new atmospheric events

### Zone Encounter System
- **Zone-Specific Spawning**: Different NPCs and monsters spawn in appropriate zones
- **Probability Control**: Fine-tuned spawn rates for balanced encounters
- **Level-Appropriate**: Encounters scale to player skill levels
- **Spawn Management**: Cooldowns and limits prevent overcrowding
- **Room Type Restrictions**: Certain NPCs only spawn in appropriate room types
- **Time-Based Spawning**: Day/night cycles affect which creatures appear

### Performance Considerations
- **Optimized Queries**: Indexes on frequently-accessed columns
- **Real-Time Updates**: Efficient SignalR group management
- **Caching Strategy**: Room and character state caching for performance

## Entity Framework Considerations

### Migrations Strategy
- Use EF Core migrations for database versioning
- Seed data for initial skill definitions and world content
- Backup strategy for production data safety

### Relationships
- Configure cascade delete carefully to prevent accidental data loss
- Use navigation properties for efficient querying
- Consider lazy loading vs. explicit loading for performance

---

*This database design supports the core gameplay mechanics while maintaining flexibility for future enhancements and content expansion.*