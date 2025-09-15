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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Character configuration
        builder.Entity<Character>(entity =>
        {
            entity.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
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
    }
}