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
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomExit> RoomExits => Set<RoomExit>();
    
    // Skill System DbSets
    public DbSet<SkillCategory> SkillCategories => Set<SkillCategory>();
    public DbSet<SkillDefinition> SkillDefinitions => Set<SkillDefinition>();
    public DbSet<CharacterSkill> CharacterSkills => Set<CharacterSkill>();
    public DbSet<SkillUsageLog> SkillUsageLogs => Set<SkillUsageLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Character configuration
        builder.Entity<Character>(entity =>
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

        // SkillCategory configuration
        builder.Entity<SkillCategory>(entity =>
        {
            entity.HasIndex(sc => sc.Name).IsUnique();
            entity.HasIndex(sc => sc.IsActive);
            entity.HasIndex(sc => sc.DisplayOrder);
        });

        // SkillDefinition configuration
        builder.Entity<SkillDefinition>(entity =>
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
        builder.Entity<CharacterSkill>(entity =>
        {
            // Composite unique index - each character can have each skill only once
            entity.HasIndex(cs => new { cs.CharacterId, cs.SkillDefinitionId }).IsUnique();
            entity.HasIndex(cs => cs.CharacterId);
            entity.HasIndex(cs => cs.SkillDefinitionId);
            entity.HasIndex(cs => cs.CurrentLevel);
            entity.HasIndex(cs => cs.LastUsedAt);

            // Configure relationships
            entity.HasOne(cs => cs.Character)
                .WithMany(c => c.Skills)
                .HasForeignKey(cs => cs.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cs => cs.SkillDefinition)
                .WithMany(sd => sd.CharacterSkills)
                .HasForeignKey(cs => cs.SkillDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SkillUsageLog configuration
        builder.Entity<SkillUsageLog>(entity =>
        {
            entity.HasIndex(sul => sul.CharacterId);
            entity.HasIndex(sul => sul.SkillDefinitionId);
            entity.HasIndex(sul => sul.UsedAt);
            entity.HasIndex(sul => new { sul.CharacterId, sul.UsedAt });

            // Configure relationships
            entity.HasOne(sul => sul.Character)
                .WithMany()
                .HasForeignKey(sul => sul.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);

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