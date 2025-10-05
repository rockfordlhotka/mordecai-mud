using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GameEntities = Mordecai.Game.Entities;
using WebEntities = Mordecai.Web.Data;

namespace Mordecai.Web.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Use Game entities for world-related items
    public DbSet<GameEntities.Character> Characters => Set<GameEntities.Character>();
    public DbSet<GameEntities.Zone> Zones => Set<GameEntities.Zone>();
    public DbSet<GameEntities.RoomType> RoomTypes => Set<GameEntities.RoomType>();
    public DbSet<GameEntities.Room> Rooms => Set<GameEntities.Room>();
    public DbSet<GameEntities.RoomExit> RoomExits => Set<GameEntities.RoomExit>();
    
    // Room Effects System
    public DbSet<GameEntities.RoomEffectDefinition> RoomEffectDefinitions => Set<GameEntities.RoomEffectDefinition>();
    public DbSet<GameEntities.RoomEffectImpact> RoomEffectImpacts => Set<GameEntities.RoomEffectImpact>();
    public DbSet<GameEntities.RoomEffect> RoomEffects => Set<GameEntities.RoomEffect>();
    public DbSet<GameEntities.RoomEffectApplicationLog> RoomEffectApplicationLogs => Set<GameEntities.RoomEffectApplicationLog>();
    
    // Use Web entities for skill system (these are the working ones for the UI)
    public DbSet<WebEntities.SkillCategory> SkillCategories => Set<WebEntities.SkillCategory>();
    public DbSet<WebEntities.SkillDefinition> SkillDefinitions => Set<WebEntities.SkillDefinition>();
    public DbSet<WebEntities.CharacterSkill> CharacterSkills => Set<WebEntities.CharacterSkill>();
    public DbSet<WebEntities.SkillUsageLog> SkillUsageLogs => Set<WebEntities.SkillUsageLog>();
    
    // Game configuration
    public DbSet<GameEntities.GameConfiguration> GameConfigurations => Set<GameEntities.GameConfiguration>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Character configuration
        builder.Entity<GameEntities.Character>(entity =>
        {
            entity.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
            entity.HasIndex(c => c.CurrentRoomId);
            
            // Configure relationship to current room
            entity.HasOne(c => c.CurrentRoom)
                .WithMany()
                .HasForeignKey(c => c.CurrentRoomId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Zone configuration
        builder.Entity<GameEntities.Zone>(entity =>
        {
            entity.HasIndex(z => z.Name).IsUnique();
            entity.HasIndex(z => z.IsActive);
        });

        // RoomType configuration
        builder.Entity<GameEntities.RoomType>(entity =>
        {
            entity.HasIndex(rt => rt.Name).IsUnique();
            entity.HasIndex(rt => rt.IsActive);
        });

        // Room configuration
        builder.Entity<GameEntities.Room>(entity =>
        {
            entity.HasIndex(r => r.ZoneId);
            entity.HasIndex(r => r.RoomTypeId);
            entity.HasIndex(r => new { r.X, r.Y, r.Z });
            entity.HasIndex(r => r.IsActive);

            // Configure relationships
            entity.HasOne(r => r.Zone)
                .WithMany(z => z.Rooms)
                .HasForeignKey(r => r.ZoneId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RoomExit configuration
        builder.Entity<GameEntities.RoomExit>(entity =>
        {
            entity.HasIndex(re => new { re.FromRoomId, re.Direction });
            entity.HasIndex(re => re.ToRoomId);
            entity.HasIndex(re => re.IsActive);

            // Configure relationships
            entity.HasOne(re => re.FromRoom)
                .WithMany(r => r.ExitsFromHere)
                .HasForeignKey(re => re.FromRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(re => re.ToRoom)
                .WithMany(r => r.ExitsToHere)
                .HasForeignKey(re => re.ToRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate exits in the same direction from the same room
            entity.HasIndex(re => new { re.FromRoomId, re.Direction }).IsUnique();
        });

        // Room Effects System Configuration
        
        // RoomEffectDefinition configuration
        builder.Entity<GameEntities.RoomEffectDefinition>(entity =>
        {
            entity.HasIndex(red => red.Name).IsUnique();
            entity.HasIndex(red => red.IsActive);
            entity.HasIndex(red => red.EffectType);
            entity.HasIndex(red => red.Category);
            
            // Configure precision for decimal fields
            entity.Property(red => red.DetectionDifficulty)
                .HasPrecision(5, 2);
                
            entity.Property(red => red.DefaultIntensity)
                .HasPrecision(4, 2);
        });

        // RoomEffectImpact configuration
        builder.Entity<GameEntities.RoomEffectImpact>(entity =>
        {
            entity.HasIndex(rei => rei.RoomEffectDefinitionId);
            entity.HasIndex(rei => rei.ImpactType);
            entity.HasIndex(rei => rei.TargetType);

            // Configure relationships
            entity.HasOne(rei => rei.RoomEffectDefinition)
                .WithMany(red => red.Impacts)
                .HasForeignKey(rei => rei.RoomEffectDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure precision for decimal fields
            entity.Property(rei => rei.ImpactValue)
                .HasPrecision(10, 2);
        });

        // RoomEffect configuration
        builder.Entity<GameEntities.RoomEffect>(entity =>
        {
            entity.HasIndex(re => re.RoomId);
            entity.HasIndex(re => re.RoomEffectDefinitionId);
            entity.HasIndex(re => re.EndTime);
            entity.HasIndex(re => re.IsActive);
            entity.HasIndex(re => re.CasterCharacterId);

            // Configure relationships
            entity.HasOne(re => re.Room)
                .WithMany()
                .HasForeignKey(re => re.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(re => re.RoomEffectDefinition)
                .WithMany(red => red.ActiveEffects)
                .HasForeignKey(re => re.RoomEffectDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Configure precision for decimal fields
            entity.Property(re => re.Intensity)
                .HasPrecision(4, 2);
        });

        // RoomEffectApplicationLog configuration
        builder.Entity<GameEntities.RoomEffectApplicationLog>(entity =>
        {
            entity.HasIndex(real => real.RoomEffectId);
            entity.HasIndex(real => real.CharacterId);
            entity.HasIndex(real => real.Timestamp);
            entity.HasIndex(real => real.ApplicationType);

            // Configure relationships
            entity.HasOne(real => real.RoomEffect)
                .WithMany(re => re.ApplicationLogs)
                .HasForeignKey(real => real.RoomEffectId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure precision for decimal fields
            entity.Property(real => real.ImpactValue)
                .HasPrecision(10, 2);
                
            entity.Property(real => real.ResistanceRoll)
                .HasPrecision(6, 2);
        });

        // SkillCategory configuration
        builder.Entity<WebEntities.SkillCategory>(entity =>
        {
            entity.HasIndex(sc => sc.Name).IsUnique();
            entity.HasIndex(sc => sc.IsActive);
            entity.HasIndex(sc => sc.DisplayOrder);
        });

        // SkillDefinition configuration
        builder.Entity<WebEntities.SkillDefinition>(entity =>
        {
            entity.HasIndex(sd => sd.Name).IsUnique();
            entity.HasIndex(sd => new { sd.CategoryId, sd.SkillType });
            entity.HasIndex(sd => sd.MagicSchool);
            entity.HasIndex(sd => sd.IsActive);
            entity.HasIndex(sd => sd.IsStartingSkill);

            // Configure relationships
            entity.HasOne(sd => sd.Category)
                .WithMany(sc => sc.Skills)
                .HasForeignKey(sd => sd.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure precision for decimal fields
            entity.Property(sd => sd.Multiplier)
                .HasPrecision(4, 2);
            
            entity.Property(sd => sd.CooldownSeconds)
                .HasPrecision(6, 2);
        });

        // CharacterSkill configuration
        builder.Entity<WebEntities.CharacterSkill>(entity =>
        {
            // Composite unique index - each character can have each skill only once
            entity.HasIndex(cs => new { cs.CharacterId, cs.SkillDefinitionId }).IsUnique();
            entity.HasIndex(cs => cs.CharacterId);
            entity.HasIndex(cs => cs.SkillDefinitionId);
            entity.HasIndex(cs => cs.Level);
            entity.HasIndex(cs => cs.LastUsedAt);

            // Configure relationships
            entity.HasOne(cs => cs.SkillDefinition)
                .WithMany(sd => sd.CharacterSkills)
                .HasForeignKey(cs => cs.SkillDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SkillUsageLog configuration
        builder.Entity<WebEntities.SkillUsageLog>(entity =>
        {
            entity.HasIndex(sul => sul.CharacterId);
            entity.HasIndex(sul => sul.SkillDefinitionId);
            entity.HasIndex(sul => sul.UsedAt);
            entity.HasIndex(sul => new { sul.CharacterId, sul.UsedAt });

            // Configure relationships
            entity.HasOne(sul => sul.SkillDefinition)
                .WithMany()
                .HasForeignKey(sul => sul.SkillDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure precision for decimal fields
            entity.Property(sul => sul.UsageMultiplier)
                .HasPrecision(3, 2);
        });
    }
}