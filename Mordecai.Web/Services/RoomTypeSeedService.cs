using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for seeding initial room type data
/// </summary>
public class RoomTypeSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoomTypeSeedService> _logger;

    public RoomTypeSeedService(ApplicationDbContext context, ILogger<RoomTypeSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with standard room types
    /// </summary>
    public async Task SeedRoomTypesAsync()
    {
        // Check if we already have room types
        if (await _context.RoomTypes.AnyAsync())
        {
            _logger.LogInformation("Room types already exist, skipping seed");
            return;
        }

        _logger.LogInformation("Seeding standard room types...");

        var roomTypes = new List<RoomType>
        {
            new()
            {
                Name = "Normal",
                Description = "A standard room where most activities can take place.",
                AllowsCombat = true,
                AllowsLogout = true,
                HasSpecialCommands = false,
                HealingRate = 1.0m,
                SkillLearningBonus = 1.0m,
                MaxOccupancy = 0,
                IsIndoor = false,
                EntryMessage = null,
                ExitMessage = null,
                IsActive = true
            },
            new()
            {
                Name = "Safe Room",
                Description = "A protected area where combat is not allowed and players can safely log out.",
                AllowsCombat = false,
                AllowsLogout = true,
                HasSpecialCommands = false,
                HealingRate = 1.5m,
                SkillLearningBonus = 1.0m,
                MaxOccupancy = 0,
                IsIndoor = false,
                EntryMessage = "You feel a sense of peace and safety here.",
                ExitMessage = "You leave the safety of this area.",
                IsActive = true
            },
            new()
            {
                Name = "Training Hall",
                Description = "A dedicated space for practicing skills with enhanced learning.",
                AllowsCombat = true,
                AllowsLogout = true,
                HasSpecialCommands = true,
                HealingRate = 1.0m,
                SkillLearningBonus = 1.5m,
                MaxOccupancy = 0,
                IsIndoor = true,
                EntryMessage = "The air hums with focused energy and determination.",
                ExitMessage = "You leave the training area behind.",
                IsActive = true
            },
            new()
            {
                Name = "Shop",
                Description = "A merchant establishment where goods can be bought and sold.",
                AllowsCombat = false,
                AllowsLogout = true,
                HasSpecialCommands = true,
                HealingRate = 1.0m,
                SkillLearningBonus = 1.0m,
                MaxOccupancy = 0,
                IsIndoor = true,
                EntryMessage = "The scent of commerce fills the air.",
                ExitMessage = "You step away from the bustling marketplace.",
                IsActive = true
            },
            new()
            {
                Name = "Temple",
                Description = "A sacred place offering healing and spiritual guidance.",
                AllowsCombat = false,
                AllowsLogout = true,
                HasSpecialCommands = true,
                HealingRate = 2.0m,
                SkillLearningBonus = 1.0m,
                MaxOccupancy = 0,
                IsIndoor = true,
                EntryMessage = "A sense of divine presence washes over you.",
                ExitMessage = "You leave the hallowed grounds.",
                IsActive = true
            },
            new()
            {
                Name = "Inn",
                Description = "A place of rest and recuperation with enhanced healing.",
                AllowsCombat = false,
                AllowsLogout = true,
                HasSpecialCommands = true,
                HealingRate = 2.0m,
                SkillLearningBonus = 1.0m,
                MaxOccupancy = 0,
                IsIndoor = true,
                EntryMessage = "The warmth and comfort of the inn welcomes you.",
                ExitMessage = "You step out into the world once more.",
                IsActive = true
            },
            new()
            {
                Name = "Tavern",
                Description = "A social gathering place where adventurers meet and share tales.",
                AllowsCombat = false,
                AllowsLogout = true,
                HasSpecialCommands = true,
                HealingRate = 1.2m,
                SkillLearningBonus = 1.0m,
                MaxOccupancy = 0,
                IsIndoor = true,
                EntryMessage = "Laughter and conversation fill the air.",
                ExitMessage = "You leave the convivial atmosphere behind.",
                IsActive = true
            },
            new()
            {
                Name = "Bank",
                Description = "A secure institution for storing valuables and currency.",
                AllowsCombat = false,
                AllowsLogout = true,
                HasSpecialCommands = true,
                HealingRate = 1.0m,
                SkillLearningBonus = 1.0m,
                MaxOccupancy = 10,
                IsIndoor = true,
                EntryMessage = "The security and order of the bank surrounds you.",
                ExitMessage = "You exit the financial institution.",
                IsActive = true
            },
            new()
            {
                Name = "Dungeon",
                Description = "A dangerous underground area filled with threats and treasures.",
                AllowsCombat = true,
                AllowsLogout = false,
                HasSpecialCommands = false,
                HealingRate = 0.8m,
                SkillLearningBonus = 1.2m,
                MaxOccupancy = 0,
                IsIndoor = true,
                EntryMessage = "Darkness and danger lurk in every shadow.",
                ExitMessage = "You emerge from the depths.",
                IsActive = true
            },
            new()
            {
                Name = "Wilderness",
                Description = "An outdoor area exposed to the elements and natural dangers.",
                AllowsCombat = true,
                AllowsLogout = true,
                HasSpecialCommands = false,
                HealingRate = 0.9m,
                SkillLearningBonus = 1.1m,
                MaxOccupancy = 0,
                IsIndoor = false,
                EntryMessage = "The wild calls to your adventurous spirit.",
                ExitMessage = "You leave the untamed lands.",
                IsActive = true
            },
            new()
            {
                Name = "Cave",
                Description = "A natural underground formation, partially sheltered but still dangerous.",
                AllowsCombat = true,
                AllowsLogout = true,
                HasSpecialCommands = false,
                HealingRate = 0.9m,
                SkillLearningBonus = 1.0m,
                MaxOccupancy = 0,
                IsIndoor = true,
                EntryMessage = "The cool, damp air of the cave surrounds you.",
                ExitMessage = "You step back into the light.",
                IsActive = true
            },
            new()
            {
                Name = "Tower",
                Description = "A tall structure offering strategic advantages but exposure to weather.",
                AllowsCombat = true,
                AllowsLogout = true,
                HasSpecialCommands = false,
                HealingRate = 1.0m,
                SkillLearningBonus = 1.0m,
                MaxOccupancy = 0,
                IsIndoor = false,
                EntryMessage = "The height gives you a commanding view.",
                ExitMessage = "You descend from the lofty perch.",
                IsActive = true
            }
        };

        _context.RoomTypes.AddRange(roomTypes);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} room types successfully", roomTypes.Count);
    }
}
