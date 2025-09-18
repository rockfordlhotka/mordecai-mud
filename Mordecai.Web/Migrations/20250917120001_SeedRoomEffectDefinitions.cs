using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mordecai.Web.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoomEffectDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert basic room effect definitions for testing and common spell effects
            migrationBuilder.Sql(@"
                INSERT INTO RoomEffectDefinitions (Name, Description, EffectType, Category, IconName, EffectColor, IsVisible, DefaultDuration, DefaultIntensity, IsStackable, MaxStacks, TickInterval, RemovalMethods, CreatedBy, CreatedAt, IsActive)
                VALUES 
                ('Wall of Fire', 'A crackling barrier of flames that damages anyone who enters or remains in the room.', 'Elemental', 'Fire', 'fire', '#FF4500', 1, 300, 1.0, 0, 1, 10, '[""time"", ""dispel"", ""zone_reset""]', 'System', '" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") + @"', 1),
                ('Fog Cloud', 'A thick cloud of fog that reduces visibility and makes it harder to see exits and other players.', 'Environmental', 'Illusion', 'cloud', '#C0C0C0', 1, 180, 1.0, 0, 1, 0, '[""time"", ""dispel"", ""manual""]', 'System', '" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") + @"', 1),
                ('Entangle', 'Thick vines and roots burst from the ground, preventing movement from the room.', 'Movement', 'Nature', 'vine', '#228B22', 1, 120, 1.0, 0, 1, 0, '[""time"", ""dispel"", ""manual""]', 'System', '" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") + @"', 1),
                ('Blessed Ground', 'The ground is consecrated with divine energy, providing healing to all who stand upon it.', 'Combat', 'Blessing', 'blessing', '#FFD700', 1, 600, 1.0, 0, 1, 15, '[""time"", ""dispel"", ""zone_reset""]', 'System', '" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") + @"', 1),
                ('Darkness', 'Magical darkness shrouds the room, making it difficult to see and limiting visibility.', 'Environmental', 'Shadow', 'darkness', '#1C1C1C', 1, 240, 1.0, 0, 1, 0, '[""time"", ""dispel"", ""manual""]', 'System', '" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") + @"', 1),
                ('Silence', 'A magical field of silence prevents all spellcasting and verbal communication in the room.', 'Magical', 'Enchantment', 'silence', '#4B0082', 1, 150, 1.0, 0, 1, 0, '[""time"", ""dispel"", ""manual""]', 'System', '" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") + @"', 1),
                ('Ice Storm', 'Freezing winds and ice shards fill the room, dealing cold damage and slowing movement.', 'Elemental', 'Ice', 'snowflake', '#87CEEB', 1, 200, 1.0, 0, 1, 8, '[""time"", ""dispel"", ""zone_reset""]', 'System', '" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") + @"', 1),
                ('Poison Gas', 'Noxious fumes fill the room, poisoning anyone who breathes the toxic air.', 'Elemental', 'Poison', 'poison', '#9ACD32', 1, 180, 1.0, 0, 1, 12, '[""time"", ""dispel"", ""zone_reset""]', 'System', '" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") + @"', 1);
            ");

            // Insert impact definitions using SQL for better control
            migrationBuilder.Sql(@"
                INSERT INTO RoomEffectImpacts (RoomEffectDefinitionId, ImpactType, TargetType, TargetAttribute, ImpactValue, IsPercentage, DamageType, TargetSkillId, ImpactFormula, ResistanceSkillId)
                VALUES 
                -- Wall of Fire impacts
                (1, 'PeriodicDamage', 'PeriodicTrigger', 'Health', 5, 0, 'Fire', NULL, NULL, NULL),
                (1, 'PeriodicDamage', 'EntryTrigger', 'Health', 8, 0, 'Fire', NULL, NULL, NULL),
                
                -- Fog Cloud impacts
                (2, 'VisibilityReduction', 'AllOccupants', 'Vision', -50, 1, NULL, NULL, NULL, NULL),
                
                -- Entangle impacts
                (3, 'MovementPrevention', 'ExitTrigger', 'MovementSpeed', 100, 1, NULL, NULL, NULL, NULL),
                
                -- Blessed Ground impacts
                (4, 'PeriodicHealing', 'PeriodicTrigger', 'Health', 3, 0, NULL, NULL, NULL, NULL),
                
                -- Darkness impacts
                (5, 'VisibilityReduction', 'AllOccupants', 'Vision', -75, 1, NULL, NULL, NULL, NULL),
                
                -- Silence impacts
                (6, 'SpellcastingPrevention', 'AllOccupants', 'Mana', 100, 1, NULL, NULL, NULL, NULL),
                (6, 'CommunicationPenalty', 'AllOccupants', 'Communication', 100, 1, NULL, NULL, NULL, NULL),
                
                -- Ice Storm impacts
                (7, 'PeriodicDamage', 'PeriodicTrigger', 'Health', 4, 0, 'Ice', NULL, NULL, NULL),
                (7, 'MovementPenalty', 'AllOccupants', 'MovementSpeed', -25, 1, NULL, NULL, NULL, NULL),
                
                -- Poison Gas impacts
                (8, 'PeriodicDamage', 'PeriodicTrigger', 'Health', 3, 0, 'Poison', NULL, NULL, NULL),
                (8, 'PeriodicDamage', 'EntryTrigger', 'Health', 5, 0, 'Poison', NULL, NULL, NULL);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seed data
            migrationBuilder.Sql(@"
                DELETE FROM RoomEffectImpacts WHERE RoomEffectDefinitionId IN (1, 2, 3, 4, 5, 6, 7, 8);
                DELETE FROM RoomEffectDefinitions WHERE Name IN ('Wall of Fire', 'Fog Cloud', 'Entangle', 'Blessed Ground', 'Darkness', 'Silence', 'Ice Storm', 'Poison Gas');
            ");
        }
    }
}