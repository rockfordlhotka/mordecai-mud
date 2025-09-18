using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;

namespace Mordecai.Web.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Character> Characters => Set<Character>();
    public DbSet<SkillDefinition> SkillDefinitions => Set<SkillDefinition>();
    public DbSet<CharacterSkill> CharacterSkills => Set<CharacterSkill>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomExit> RoomExits => Set<RoomExit>();
    
    // Room Effects System
    public DbSet<RoomEffectDefinition> RoomEffectDefinitions => Set<RoomEffectDefinition>();
    public DbSet<RoomEffectImpact> RoomEffectImpacts => Set<RoomEffectImpact>();
    public DbSet<RoomEffect> RoomEffects => Set<RoomEffect>();
    public DbSet<RoomEffectApplicationLog> RoomEffectApplicationLogs => Set<RoomEffectApplicationLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Character configuration
        builder.Entity<Character>(entity =>
        {
            entity.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
        });

        // SkillDefinition configuration
        builder.Entity<SkillDefinition>(entity =>
        {
            entity.HasIndex(sd => sd.Name).IsUnique();
            entity.HasIndex(sd => sd.SkillType);
            entity.HasIndex(sd => sd.RelatedAttribute);
            entity.HasIndex(sd => sd.MagicSchool);
            entity.HasIndex(sd => sd.IsActive);
            entity.HasIndex(sd => sd.IsStartingSkill);
        });

        // CharacterSkill configuration
        builder.Entity<CharacterSkill>(entity =>
        {
            entity.HasIndex(cs => new { cs.CharacterId, cs.SkillDefinitionId }).IsUnique();
            entity.HasIndex(cs => cs.CharacterId);
            entity.HasIndex(cs => cs.SkillDefinitionId);
            entity.HasIndex(cs => cs.Level);
            entity.HasIndex(cs => cs.LastUsedAt);

            // Configure relationships
            entity.HasOne(cs => cs.Character)
                .WithMany(c => c.Skills)
                .HasForeignKey(cs => cs.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cs => cs.SkillDefinition)
                .WithMany(sd => sd.CharacterSkills)
                .HasForeignKey(cs => cs.SkillDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Zone configuration
        builder.Entity<Zone>(entity =>
        {
            entity.HasIndex(z => z.Name).IsUnique();
            entity.HasIndex(z => z.IsActive);
        });

        // RoomType configuration
        builder.Entity<RoomType>(entity =>
        {
            entity.HasIndex(rt => rt.Name).IsUnique();
            entity.HasIndex(rt => rt.IsActive);
        });

        // Room configuration
        builder.Entity<Room>(entity =>
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
        builder.Entity<RoomExit>(entity =>
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

        // Room Effects System configuration
        builder.Entity<RoomEffectDefinition>(entity =>
        {
            entity.HasIndex(red => red.Name).IsUnique();
            entity.HasIndex(red => red.IsActive);
            entity.HasIndex(red => red.EffectType);
            entity.HasIndex(red => red.Category);
        });

        builder.Entity<RoomEffectImpact>(entity =>
        {
            entity.HasIndex(rei => rei.RoomEffectDefinitionId);
            entity.HasIndex(rei => rei.ImpactType);
            entity.HasIndex(rei => rei.TargetType);

            // Configure relationships
            entity.HasOne(rei => rei.RoomEffectDefinition)
                .WithMany(red => red.Impacts)
                .HasForeignKey(rei => rei.RoomEffectDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RoomEffect>(entity =>
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
        });

        builder.Entity<RoomEffectApplicationLog>(entity =>
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
        });
    }
}