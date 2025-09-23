using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for seeding initial skill data
/// </summary>
public class SkillSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SkillSeedService> _logger;

    public SkillSeedService(ApplicationDbContext context, ILogger<SkillSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with initial skill categories and definitions
    /// </summary>
    public async Task SeedSkillDataAsync()
    {
        // Check if we already have skill categories
        if (await _context.SkillCategories.AnyAsync())
        {
            _logger.LogInformation("Skill categories already exist, skipping seed");
            return;
        }

        _logger.LogInformation("Seeding skill categories and definitions...");

        // Create skill categories based on the specification
        var categories = new List<SkillCategory>
        {
            new() 
            {
                Name = "Core Skills",
                Description = "Fundamental abilities that all characters possess",
                DefaultBaseCost = 15,
                DefaultMultiplier = 2.5m,
                AllowsPassiveAdvancement = false,
                AllowsTeaching = true,
                DisplayOrder = 1,
                IsActive = true
            },
            new() 
            {
                Name = "Weapon Skills",
                Description = "Combat skills with various weapon types",
                DefaultBaseCost = 25,
                DefaultMultiplier = 2.2m,
                AllowsPassiveAdvancement = false,
                AllowsTeaching = true,
                DisplayOrder = 2,
                IsActive = true
            },
            new() 
            {
                Name = "Spell Skills",
                Description = "Individual magical spells organized by schools",
                DefaultBaseCost = 40,
                DefaultMultiplier = 2.3m,
                AllowsPassiveAdvancement = false,
                AllowsTeaching = true,
                DisplayOrder = 3,
                IsActive = true
            },
            new() 
            {
                Name = "Mana Recovery",
                Description = "Skills that govern mana regeneration rates by magic school",
                DefaultBaseCost = 30,
                DefaultMultiplier = 2.1m,
                AllowsPassiveAdvancement = true,
                AllowsTeaching = true,
                DisplayOrder = 4,
                IsActive = true
            },
            new() 
            {
                Name = "Crafting Skills",
                Description = "Skills for creating and repairing items",
                DefaultBaseCost = 35,
                DefaultMultiplier = 2.4m,
                AllowsPassiveAdvancement = false,
                AllowsTeaching = true,
                DisplayOrder = 5,
                IsActive = true
            },
            new() 
            {
                Name = "Social Skills",
                Description = "Skills for interacting with other characters and NPCs",
                DefaultBaseCost = 20,
                DefaultMultiplier = 2.0m,
                AllowsPassiveAdvancement = false,
                AllowsTeaching = true,
                DisplayOrder = 6,
                IsActive = true
            }
        };

        _context.SkillCategories.AddRange(categories);
        await _context.SaveChangesAsync();

        // Now create skill definitions for each category
        await SeedCoreAttributeSkills();
        await SeedWeaponSkills();
        await SeedSpellSkills();
        await SeedManaRecoverySkills();
        await SeedCraftingSkills();
        await SeedSocialSkills();

        _logger.LogInformation("Skill seeding completed successfully");
    }

    private async Task SeedCoreAttributeSkills()
    {
        var category = await _context.SkillCategories.FirstAsync(c => c.Name == "Core Skills");

        var coreSkills = new List<SkillDefinition>
        {
            new()
            {
                CategoryId = category.Id,
                Name = "Physicality",
                Description = "Physical strength and power - affects melee damage, carrying capacity, and physical tasks",
                SkillType = "CoreAttribute",
                BaseCost = 15,
                Multiplier = 2.5m,
                RelatedAttribute = "STR", // Primary attribute: STR (Strength)
                IsStartingSkill = true,
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Dodge",
                Description = "Agility and evasion ability - affects avoiding attacks and movement speed",
                SkillType = "CoreAttribute",
                BaseCost = 15,
                Multiplier = 2.5m,
                RelatedAttribute = "DEX", // Primary attribute: DEX (Dexterity)
                IsStartingSkill = true,
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Drive",
                Description = "Endurance and stamina - affects health pools and sustained activity",
                SkillType = "CoreAttribute",
                BaseCost = 15,
                Multiplier = 2.5m,
                RelatedAttribute = "END", // Primary attribute: END (Endurance)
                IsStartingSkill = true,
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Reasoning",
                Description = "Intelligence and logical thinking - affects learning and complex tasks",
                SkillType = "CoreAttribute",
                BaseCost = 15,
                Multiplier = 2.5m,
                RelatedAttribute = "INT", // Primary attribute: INT (Intelligence)
                IsStartingSkill = true,
                DisplayOrder = 4,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Awareness",
                Description = "Intuition and perception - affects detecting hidden things and reading situations",
                SkillType = "CoreAttribute",
                BaseCost = 15,
                Multiplier = 2.5m,
                RelatedAttribute = "ITT", // Primary attribute: ITT (Intuition)
                IsStartingSkill = true,
                DisplayOrder = 5,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Focus",
                Description = "Willpower and mental concentration - affects mana and spell effectiveness",
                SkillType = "CoreAttribute",
                BaseCost = 15,
                Multiplier = 2.5m,
                RelatedAttribute = "WIL", // Primary attribute: WIL (Willpower)
                IsStartingSkill = true,
                DisplayOrder = 6,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Bearing",
                Description = "Physical beauty and social presence - affects social interactions and leadership",
                SkillType = "CoreAttribute",
                BaseCost = 15,
                Multiplier = 2.5m,
                RelatedAttribute = "PHY", // Primary attribute: PHY (Physical beauty)
                IsStartingSkill = true,
                DisplayOrder = 7,
                IsActive = true
            }
        };

        _context.SkillDefinitions.AddRange(coreSkills);
        await _context.SaveChangesAsync();
    }

    private async Task SeedWeaponSkills()
    {
        var category = await _context.SkillCategories.FirstAsync(c => c.Name == "Weapon Skills");

        var weaponSkills = new List<SkillDefinition>
        {
            new()
            {
                CategoryId = category.Id,
                Name = "Swords",
                Description = "Skill with blade weapons including shortswords, longswords, and scimitars",
                SkillType = "Weapon",
                BaseCost = 25,
                Multiplier = 2.2m,
                RelatedAttribute = "STR", // Primary attribute: STR for melee weapons
                CooldownSeconds = 3.0m,
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Axes",
                Description = "Skill with axe weapons including hatchets, battle axes, and great axes",
                SkillType = "Weapon",
                BaseCost = 25,
                Multiplier = 2.2m,
                RelatedAttribute = "STR", // Primary attribute: STR for heavy weapons
                CooldownSeconds = 3.5m,
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Maces",
                Description = "Skill with blunt weapons including clubs, maces, and war hammers",
                SkillType = "Weapon",
                BaseCost = 25,
                Multiplier = 2.2m,
                RelatedAttribute = "STR", // Primary attribute: STR for blunt weapons
                CooldownSeconds = 3.2m,
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Polearms",
                Description = "Skill with long weapons including spears, halberds, and glaives",
                SkillType = "Weapon",
                BaseCost = 25,
                Multiplier = 2.2m,
                RelatedAttribute = "STR", // Primary attribute: STR for reach weapons
                CooldownSeconds = 3.8m,
                DisplayOrder = 4,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Bows",
                Description = "Skill with ranged bow weapons including shortbows, longbows, and composite bows",
                SkillType = "Weapon",
                BaseCost = 25,
                Multiplier = 2.2m,
                RelatedAttribute = "DEX", // Primary attribute: DEX for ranged accuracy and draw speed
                CooldownSeconds = 2.5m,
                DisplayOrder = 5,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Crossbows",
                Description = "Skill with crossbow weapons including light and heavy crossbows",
                SkillType = "Weapon",
                BaseCost = 25,
                Multiplier = 2.2m,
                RelatedAttribute = "INT", // Primary attribute: INT for mechanical operation and precision
                CooldownSeconds = 4.0m,
                DisplayOrder = 6,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Throwing",
                Description = "Skill with thrown weapons including daggers, javelins, and throwing axes",
                SkillType = "Weapon",
                BaseCost = 25,
                Multiplier = 2.2m,
                RelatedAttribute = "DEX", // Primary attribute: DEX for throwing accuracy and timing
                CooldownSeconds = 2.0m,
                DisplayOrder = 7,
                IsActive = true
            }
        };

        _context.SkillDefinitions.AddRange(weaponSkills);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSpellSkills()
    {
        var category = await _context.SkillCategories.FirstAsync(c => c.Name == "Spell Skills");

        var spellSkills = new List<SkillDefinition>
        {
            // Fire School Spells - All tied to WIL for magical concentration
            new()
            {
                CategoryId = category.Id,
                Name = "Fire Bolt",
                Description = "A basic projectile of fire that burns the target",
                SkillType = "Spell",
                BaseCost = 20, // Cantrip
                Multiplier = 2.0m,
                RelatedAttribute = "WIL", // Primary attribute: WIL for magical concentration
                MagicSchool = "Fire",
                ManaCost = 5,
                CooldownSeconds = 2.0m,
                UsesExplodingDice = true,
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Flame Strike",
                Description = "A powerful column of fire that engulfs the target",
                SkillType = "Spell",
                BaseCost = 40, // Standard spell
                Multiplier = 2.3m,
                RelatedAttribute = "WIL", // Primary attribute: WIL for magical concentration
                MagicSchool = "Fire",
                ManaCost = 15,
                CooldownSeconds = 5.0m,
                UsesExplodingDice = true,
                DisplayOrder = 2,
                IsActive = true
            },

            // Healing School Spells - All tied to WIL for magical concentration
            new()
            {
                CategoryId = category.Id,
                Name = "Heal",
                Description = "Restores health to yourself or another",
                SkillType = "Spell",
                BaseCost = 20, // Cantrip
                Multiplier = 2.0m,
                RelatedAttribute = "WIL", // Primary attribute: WIL for magical concentration
                MagicSchool = "Healing",
                ManaCost = 8,
                CooldownSeconds = 3.0m,
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Greater Heal",
                Description = "A more powerful healing spell that restores significant health",
                SkillType = "Spell",
                BaseCost = 40, // Standard spell
                Multiplier = 2.3m,
                RelatedAttribute = "WIL", // Primary attribute: WIL for magical concentration
                MagicSchool = "Healing",
                ManaCost = 20,
                CooldownSeconds = 8.0m,
                DisplayOrder = 4,
                IsActive = true
            },

            // Lightning School Spells - All tied to WIL for magical concentration
            new()
            {
                CategoryId = category.Id,
                Name = "Lightning Strike",
                Description = "A bolt of lightning that shocks the target",
                SkillType = "Spell",
                BaseCost = 40, // Standard spell
                Multiplier = 2.3m,
                RelatedAttribute = "WIL", // Primary attribute: WIL for magical concentration
                MagicSchool = "Lightning",
                ManaCost = 12,
                CooldownSeconds = 4.0m,
                UsesExplodingDice = true,
                DisplayOrder = 5,
                IsActive = true
            },

            // Illusion School Spells - All tied to WIL for magical concentration
            new()
            {
                CategoryId = category.Id,
                Name = "Invisibility",
                Description = "Makes the caster invisible for a short time",
                SkillType = "Spell",
                BaseCost = 80, // Advanced spell
                Multiplier = 2.8m,
                RelatedAttribute = "WIL", // Primary attribute: WIL for magical concentration
                MagicSchool = "Illusion",
                ManaCost = 25,
                CooldownSeconds = 15.0m,
                DisplayOrder = 6,
                IsActive = true
            }
        };

        _context.SkillDefinitions.AddRange(spellSkills);
        await _context.SaveChangesAsync();
    }

    private async Task SeedManaRecoverySkills()
    {
        var category = await _context.SkillCategories.FirstAsync(c => c.Name == "Mana Recovery");

        var manaSkills = new List<SkillDefinition>
        {
            new()
            {
                CategoryId = category.Id,
                Name = "Fire Mana Recovery",
                Description = "Governs how quickly Fire school mana regenerates",
                SkillType = "ManaRecovery",
                BaseCost = 30,
                Multiplier = 2.1m,
                RelatedAttribute = "WIL", // Primary attribute: WIL for mana management
                MagicSchool = "Fire",
                AllowsPassiveAdvancement = true,
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Healing Mana Recovery",
                Description = "Governs how quickly Healing school mana regenerates",
                SkillType = "ManaRecovery",
                BaseCost = 30,
                Multiplier = 2.1m,
                RelatedAttribute = "WIL", // Primary attribute: WIL for mana management
                MagicSchool = "Healing",
                AllowsPassiveAdvancement = true,
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Lightning Mana Recovery",
                Description = "Governs how quickly Lightning school mana regenerates",
                SkillType = "ManaRecovery",
                BaseCost = 30,
                Multiplier = 2.1m,
                RelatedAttribute = "WIL", // Primary attribute: WIL for mana management
                MagicSchool = "Lightning",
                AllowsPassiveAdvancement = true,
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Illusion Mana Recovery",
                Description = "Governs how quickly Illusion school mana regenerates",
                SkillType = "ManaRecovery",
                BaseCost = 30,
                Multiplier = 2.1m,
                RelatedAttribute = "WIL", // Primary attribute: WIL for mana management
                MagicSchool = "Illusion",
                AllowsPassiveAdvancement = true,
                DisplayOrder = 4,
                IsActive = true
            }
        };

        _context.SkillDefinitions.AddRange(manaSkills);
        await _context.SaveChangesAsync();
    }

    private async Task SeedCraftingSkills()
    {
        var category = await _context.SkillCategories.FirstAsync(c => c.Name == "Crafting Skills");

        var craftingSkills = new List<SkillDefinition>
        {
            new()
            {
                CategoryId = category.Id,
                Name = "Blacksmithing",
                Description = "The art of forging metal weapons and armor",
                SkillType = "Crafting",
                BaseCost = 35,
                Multiplier = 2.4m,
                RelatedAttribute = "STR", // Primary attribute: STR for hammer work and strength
                CooldownSeconds = 10.0m,
                UsesExplodingDice = true, // For masterwork attempts
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Alchemy",
                Description = "The craft of brewing potions and magical concoctions",
                SkillType = "Crafting",
                BaseCost = 35,
                Multiplier = 2.4m,
                RelatedAttribute = "INT", // Primary attribute: INT for formulas and precision
                CooldownSeconds = 8.0m,
                UsesExplodingDice = true,
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Carpentry",
                Description = "Working with wood to create tools, furniture, and structures",
                SkillType = "Crafting",
                BaseCost = 35,
                Multiplier = 2.4m,
                RelatedAttribute = "INT", // Primary attribute: INT for measurements and design
                CooldownSeconds = 12.0m,
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Cooking",
                Description = "Preparing food that provides various benefits",
                SkillType = "Crafting",
                BaseCost = 35,
                Multiplier = 2.4m,
                RelatedAttribute = "ITT", // Primary attribute: ITT for taste, timing, and ingredients
                CooldownSeconds = 5.0m,
                DisplayOrder = 4,
                IsActive = true
            }
        };

        _context.SkillDefinitions.AddRange(craftingSkills);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSocialSkills()
    {
        var category = await _context.SkillCategories.FirstAsync(c => c.Name == "Social Skills");

        var socialSkills = new List<SkillDefinition>
        {
            new()
            {
                CategoryId = category.Id,
                Name = "Persuasion",
                Description = "The ability to convince others through logical argument and charm",
                SkillType = "Social",
                BaseCost = 20,
                Multiplier = 2.0m,
                RelatedAttribute = "PHY", // Primary attribute: PHY for presence and charisma
                CooldownSeconds = 5.0m,
                UsesExplodingDice = true, // For dramatic social encounters
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Intimidation",
                Description = "The ability to frighten or coerce others through presence and threat",
                SkillType = "Social",
                BaseCost = 20,
                Multiplier = 2.0m,
                RelatedAttribute = "STR", // Primary attribute: STR for physical intimidation
                CooldownSeconds = 5.0m,
                UsesExplodingDice = true,
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Deception",
                Description = "The ability to lie convincingly and mislead others",
                SkillType = "Social",
                BaseCost = 20,
                Multiplier = 2.0m,
                RelatedAttribute = "INT", // Primary attribute: INT for clever lies and quick thinking
                CooldownSeconds = 5.0m,
                UsesExplodingDice = true,
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                CategoryId = category.Id,
                Name = "Leadership",
                Description = "The ability to inspire and guide others effectively",
                SkillType = "Social",
                BaseCost = 20,
                Multiplier = 2.0m,
                RelatedAttribute = "PHY", // Primary attribute: PHY for leadership presence
                CooldownSeconds = 10.0m,
                DisplayOrder = 4,
                IsActive = true
            }
        };

        _context.SkillDefinitions.AddRange(socialSkills);
        await _context.SaveChangesAsync();
    }
}