using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for seeding initial item templates for the game.
/// </summary>
public class ItemSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ItemSeedService> _logger;

    public ItemSeedService(ApplicationDbContext context, ILogger<ItemSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with starter item templates.
    /// </summary>
    public async Task SeedItemDataAsync()
    {
        if (await _context.ItemTemplates.AnyAsync())
        {
            _logger.LogInformation("Item templates already exist, skipping seed");
            return;
        }

        _logger.LogInformation("Seeding item templates...");

        // Seed in order: weapons, armor, containers, consumables, misc
        await SeedWeaponsAsync();
        await SeedArmorAsync();
        await SeedContainersAsync();
        await SeedConsumablesAsync();
        await SeedMiscItemsAsync();

        _logger.LogInformation("Item template seeding complete");
    }

    private async Task SeedWeaponsAsync()
    {
        _logger.LogInformation("Seeding weapon templates...");

        var weapons = new List<ItemTemplate>
        {
            // Training weapons (common, for new characters)
            CreateWeaponTemplate(
                id: 1,
                name: "Training Sword",
                description: "A blunted practice sword made of wood with a leather grip. Safe for sparring but still capable of delivering a solid blow.",
                shortDesc: "a wooden practice sword",
                weight: 2.0m,
                volume: 0.3m,
                value: 50,
                rarity: "Common",
                weaponType: WeaponType.Sword,
                damageType: DamageType.Bashing,
                damageClass: DamageClass.Class1,
                slot: ArmorSlot.MainHand),

            CreateWeaponTemplate(
                id: 2,
                name: "Rusty Dagger",
                description: "An old dagger showing signs of neglect. The blade is pitted with rust but the edge still holds.",
                shortDesc: "a rusty iron dagger",
                weight: 0.5m,
                volume: 0.05m,
                value: 25,
                rarity: "Common",
                weaponType: WeaponType.Dagger,
                damageType: DamageType.Piercing,
                damageClass: DamageClass.Class1,
                slot: ArmorSlot.MainHand),

            CreateWeaponTemplate(
                id: 3,
                name: "Wooden Club",
                description: "A sturdy length of oak shaped into a crude but effective bludgeon. Heavy and unrefined.",
                shortDesc: "a heavy wooden club",
                weight: 3.0m,
                volume: 0.4m,
                value: 15,
                rarity: "Common",
                weaponType: WeaponType.Mace,
                damageType: DamageType.Bashing,
                damageClass: DamageClass.Class1,
                slot: ArmorSlot.MainHand),

            // Better starting weapons
            CreateWeaponTemplate(
                id: 4,
                name: "Iron Shortsword",
                description: "A well-balanced shortsword forged from iron. The blade is sharp and the grip wrapped in worn leather.",
                shortDesc: "an iron shortsword",
                weight: 2.5m,
                volume: 0.25m,
                value: 150,
                rarity: "Common",
                weaponType: WeaponType.Sword,
                damageType: DamageType.Cutting,
                damageClass: DamageClass.Class2,
                slot: ArmorSlot.MainHand,
                attackModifier: 1),

            CreateWeaponTemplate(
                id: 5,
                name: "Iron Mace",
                description: "A flanged mace with an iron head mounted on a hardwood shaft. Good for crushing armor.",
                shortDesc: "an iron flanged mace",
                weight: 4.0m,
                volume: 0.35m,
                value: 175,
                rarity: "Common",
                weaponType: WeaponType.Mace,
                damageType: DamageType.Bashing,
                damageClass: DamageClass.Class2,
                slot: ArmorSlot.MainHand,
                attackModifier: 1),

            CreateWeaponTemplate(
                id: 6,
                name: "Hunting Bow",
                description: "A simple recurve bow suitable for hunting small game. Made of yew wood with a hemp string.",
                shortDesc: "a hunting bow",
                weight: 1.5m,
                volume: 0.8m,
                value: 100,
                rarity: "Common",
                weaponType: WeaponType.Bow,
                damageType: DamageType.Projectile,
                damageClass: DamageClass.Class1,
                slot: ArmorSlot.TwoHand,
                isTwoHanded: true,
                requiresAmmunition: true,
                range: WeaponRange.AdjacentRoom),

            CreateWeaponTemplate(
                id: 7,
                name: "Quarterstaff",
                description: "A six-foot staff of hardened oak, bound with iron bands at each end. A versatile weapon for the trained wielder.",
                shortDesc: "an iron-banded quarterstaff",
                weight: 4.0m,
                volume: 0.5m,
                value: 50,
                rarity: "Common",
                weaponType: WeaponType.Staff,
                damageType: DamageType.Bashing,
                damageClass: DamageClass.Class1,
                slot: ArmorSlot.TwoHand,
                isTwoHanded: true,
                dodgeModifier: 1),

            // Magic weapons
            CreateWeaponTemplate(
                id: 8,
                name: "Apprentice's Wand",
                description: "A simple wand of polished willow wood, its tip lightly enchanted to focus magical energy.",
                shortDesc: "a willow wand",
                weight: 0.3m,
                volume: 0.05m,
                value: 200,
                rarity: "Uncommon",
                weaponType: WeaponType.Wand,
                damageType: DamageType.Energy,
                damageClass: DamageClass.Class1,
                slot: ArmorSlot.MainHand),
        };

        _context.ItemTemplates.AddRange(weapons);
        await _context.SaveChangesAsync();

        // Add weapon properties
        foreach (var weapon in weapons)
        {
            if (weapon.WeaponProperties != null)
            {
                _context.Entry(weapon).Reference(w => w.WeaponProperties).TargetEntry!.State = EntityState.Added;
            }
        }
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} weapon templates", weapons.Count);
    }

    private async Task SeedArmorAsync()
    {
        _logger.LogInformation("Seeding armor templates...");

        var armor = new List<ItemTemplate>
        {
            // Cloth armor (minimal protection)
            CreateArmorTemplate(
                id: 101,
                name: "Worn Tunic",
                description: "A simple cloth tunic that has seen better days. It provides minimal protection but is comfortable.",
                shortDesc: "a worn cloth tunic",
                weight: 1.0m,
                volume: 0.5m,
                value: 20,
                rarity: "Common",
                slot: ArmorSlot.Chest,
                damageClass: DamageClass.Class1,
                bashingAbsorption: 0,
                cuttingAbsorption: 0,
                coverage: "torso,abdomen"),

            CreateArmorTemplate(
                id: 102,
                name: "Cloth Pants",
                description: "Simple trousers made of sturdy cotton. They offer no protection but are practical.",
                shortDesc: "cloth pants",
                weight: 0.8m,
                volume: 0.3m,
                value: 15,
                rarity: "Common",
                slot: ArmorSlot.Legs,
                damageClass: DamageClass.Class1,
                coverage: "legs,thighs"),

            CreateArmorTemplate(
                id: 103,
                name: "Leather Boots",
                description: "Sturdy leather boots with thick soles. They protect the feet from rough terrain.",
                shortDesc: "leather boots",
                weight: 1.5m,
                volume: 0.4m,
                value: 40,
                rarity: "Common",
                slot: ArmorSlot.FootLeft, // Will need both feet
                damageClass: DamageClass.Class1,
                bashingAbsorption: 1,
                cuttingAbsorption: 1,
                coverage: "feet"),

            // Leather armor (light protection)
            CreateArmorTemplate(
                id: 104,
                name: "Leather Jerkin",
                description: "A fitted jerkin of boiled leather that offers basic protection without restricting movement.",
                shortDesc: "a leather jerkin",
                weight: 5.0m,
                volume: 1.0m,
                value: 100,
                rarity: "Common",
                slot: ArmorSlot.Chest,
                damageClass: DamageClass.Class2,
                bashingAbsorption: 1,
                cuttingAbsorption: 2,
                piercingAbsorption: 1,
                coverage: "torso,abdomen"),

            CreateArmorTemplate(
                id: 105,
                name: "Leather Cap",
                description: "A simple cap of hardened leather that protects the head from glancing blows.",
                shortDesc: "a leather cap",
                weight: 0.5m,
                volume: 0.2m,
                value: 35,
                rarity: "Common",
                slot: ArmorSlot.Head,
                damageClass: DamageClass.Class1,
                bashingAbsorption: 1,
                cuttingAbsorption: 1,
                coverage: "head"),

            CreateArmorTemplate(
                id: 106,
                name: "Leather Bracers",
                description: "Fitted bracers of thick leather that protect the forearms.",
                shortDesc: "leather bracers",
                weight: 0.8m,
                volume: 0.15m,
                value: 30,
                rarity: "Common",
                slot: ArmorSlot.WristLeft,
                damageClass: DamageClass.Class1,
                bashingAbsorption: 1,
                cuttingAbsorption: 1,
                coverage: "wrists"),

            // Metal armor (heavier protection)
            CreateArmorTemplate(
                id: 107,
                name: "Chain Mail Shirt",
                description: "A shirt of interlocking iron rings that provides solid protection against slashing attacks.",
                shortDesc: "a chain mail shirt",
                weight: 20.0m,
                volume: 2.0m,
                value: 500,
                rarity: "Uncommon",
                slot: ArmorSlot.Chest,
                damageClass: DamageClass.Class3,
                bashingAbsorption: 1,
                cuttingAbsorption: 4,
                piercingAbsorption: 2,
                coverage: "torso,abdomen,shoulders",
                dodgeModifier: -1),

            CreateArmorTemplate(
                id: 108,
                name: "Iron Helm",
                description: "A domed iron helmet with a nasal guard. Heavy but protective.",
                shortDesc: "an iron helm",
                weight: 3.0m,
                volume: 0.4m,
                value: 150,
                rarity: "Uncommon",
                slot: ArmorSlot.Head,
                damageClass: DamageClass.Class2,
                bashingAbsorption: 2,
                cuttingAbsorption: 3,
                piercingAbsorption: 2,
                coverage: "head,face"),

            // Shields
            CreateArmorTemplate(
                id: 109,
                name: "Wooden Shield",
                description: "A round shield of layered wood bound with iron. Light enough for extended combat.",
                shortDesc: "a wooden shield",
                weight: 5.0m,
                volume: 0.8m,
                value: 75,
                rarity: "Common",
                slot: ArmorSlot.OffHand,
                damageClass: DamageClass.Class2,
                bashingAbsorption: 2,
                cuttingAbsorption: 2,
                projectileAbsorption: 3,
                coverage: "arms"),

            CreateArmorTemplate(
                id: 110,
                name: "Iron Buckler",
                description: "A small iron shield strapped to the forearm. Useful for deflecting blows while keeping a hand free.",
                shortDesc: "an iron buckler",
                weight: 3.0m,
                volume: 0.3m,
                value: 100,
                rarity: "Common",
                slot: ArmorSlot.OffHand,
                damageClass: DamageClass.Class2,
                bashingAbsorption: 2,
                cuttingAbsorption: 3,
                piercingAbsorption: 2,
                coverage: "hands"),

            // Cloaks and misc
            CreateArmorTemplate(
                id: 111,
                name: "Traveler's Cloak",
                description: "A hooded cloak of oiled wool that protects against the elements. Provides minimal combat protection.",
                shortDesc: "a hooded cloak",
                weight: 2.0m,
                volume: 0.8m,
                value: 50,
                rarity: "Common",
                slot: ArmorSlot.Back,
                damageClass: DamageClass.Class1,
                coldAbsorption: 2,
                coverage: "back,shoulders"),
        };

        _context.ItemTemplates.AddRange(armor);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} armor templates", armor.Count);
    }

    private async Task SeedContainersAsync()
    {
        _logger.LogInformation("Seeding container templates...");

        var containers = new List<ItemTemplate>
        {
            CreateContainerTemplate(
                id: 201,
                name: "Small Pouch",
                description: "A small leather pouch that can be worn on a belt. Good for carrying coins and small items.",
                shortDesc: "a small leather pouch",
                weight: 0.2m,
                volume: 0.05m,
                value: 10,
                maxWeight: 5.0m,
                maxVolume: 0.5m,
                allowedTypes: null), // Any small items

            CreateContainerTemplate(
                id: 202,
                name: "Belt Pouch",
                description: "A medium-sized pouch with a sturdy clasp. Hangs from your belt for easy access.",
                shortDesc: "a belt pouch",
                weight: 0.3m,
                volume: 0.08m,
                value: 25,
                maxWeight: 10.0m,
                maxVolume: 1.0m,
                allowedTypes: null),

            CreateContainerTemplate(
                id: 203,
                name: "Leather Backpack",
                description: "A sturdy backpack of oiled leather with multiple compartments. Essential for any traveler.",
                shortDesc: "a leather backpack",
                weight: 2.0m,
                volume: 0.5m,
                value: 75,
                maxWeight: 40.0m,
                maxVolume: 5.0m,
                allowedTypes: null),

            CreateContainerTemplate(
                id: 204,
                name: "Canvas Sack",
                description: "A large canvas sack with a drawstring closure. Simple but effective for hauling goods.",
                shortDesc: "a canvas sack",
                weight: 0.5m,
                volume: 0.2m,
                value: 15,
                maxWeight: 50.0m,
                maxVolume: 4.0m,
                allowedTypes: null),

            CreateContainerTemplate(
                id: 205,
                name: "Quiver",
                description: "A leather quiver designed to hold arrows. Can be slung across the back or worn at the hip.",
                shortDesc: "a leather quiver",
                weight: 0.5m,
                volume: 0.3m,
                value: 30,
                maxWeight: 5.0m,
                maxVolume: 1.0m,
                allowedTypes: "Ammunition"),

            CreateContainerTemplate(
                id: 206,
                name: "Wooden Chest",
                description: "A small wooden chest with iron bindings and a simple lock. Suitable for storing valuables.",
                shortDesc: "a wooden chest",
                weight: 10.0m,
                volume: 2.0m,
                value: 100,
                maxWeight: 100.0m,
                maxVolume: 8.0m,
                allowedTypes: null,
                isDroppable: true), // Stays in rooms

            CreateContainerTemplate(
                id: 207,
                name: "Coin Purse",
                description: "A small drawstring purse designed specifically for carrying coins.",
                shortDesc: "a coin purse",
                weight: 0.1m,
                volume: 0.02m,
                value: 5,
                maxWeight: 3.0m,
                maxVolume: 0.1m,
                allowedTypes: "Treasure"),
        };

        _context.ItemTemplates.AddRange(containers);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} container templates", containers.Count);
    }

    private async Task SeedConsumablesAsync()
    {
        _logger.LogInformation("Seeding consumable templates...");

        var consumables = new List<ItemTemplate>
        {
            // Healing items
            new ItemTemplate
            {
                Id = 301,
                Name = "Minor Healing Potion",
                Description = "A small vial of red liquid that glows faintly. Drinking it will restore a small amount of vitality.",
                ShortDescription = "a small red potion",
                ItemType = ItemType.Consumable,
                Weight = 0.2m,
                Volume = 0.05m,
                Value = 50,
                IsStackable = true,
                MaxStackSize = 10,
                Rarity = "Common",
                ConsumableValue = 5, // Restores 5 VIT
                IsActive = true,
                CreatedBy = "System"
            },
            new ItemTemplate
            {
                Id = 302,
                Name = "Healing Potion",
                Description = "A vial of ruby-red liquid that pulses with healing magic. Restores a moderate amount of vitality.",
                ShortDescription = "a ruby-red potion",
                ItemType = ItemType.Consumable,
                Weight = 0.3m,
                Volume = 0.08m,
                Value = 150,
                IsStackable = true,
                MaxStackSize = 10,
                Rarity = "Uncommon",
                ConsumableValue = 10,
                IsActive = true,
                CreatedBy = "System"
            },
            new ItemTemplate
            {
                Id = 303,
                Name = "Fatigue Draught",
                Description = "A bitter herbal concoction that refreshes the mind and body. Restores mental energy.",
                ShortDescription = "a green herbal draught",
                ItemType = ItemType.Consumable,
                Weight = 0.2m,
                Volume = 0.05m,
                Value = 40,
                IsStackable = true,
                MaxStackSize = 10,
                Rarity = "Common",
                ConsumableValue = 5, // Restores 5 FAT
                IsActive = true,
                CreatedBy = "System"
            },

            // Food items
            new ItemTemplate
            {
                Id = 304,
                Name = "Bread Loaf",
                Description = "A fresh loaf of hearty bread. A staple food for travelers.",
                ShortDescription = "a loaf of bread",
                ItemType = ItemType.Food,
                Weight = 0.3m,
                Volume = 0.1m,
                Value = 5,
                IsStackable = true,
                MaxStackSize = 5,
                Rarity = "Common",
                ConsumableValue = 3,
                IsActive = true,
                CreatedBy = "System"
            },
            new ItemTemplate
            {
                Id = 305,
                Name = "Dried Meat",
                Description = "Strips of salted and dried meat. Long-lasting and nutritious.",
                ShortDescription = "dried meat strips",
                ItemType = ItemType.Food,
                Weight = 0.2m,
                Volume = 0.05m,
                Value = 10,
                IsStackable = true,
                MaxStackSize = 10,
                Rarity = "Common",
                ConsumableValue = 4,
                IsActive = true,
                CreatedBy = "System"
            },
            new ItemTemplate
            {
                Id = 306,
                Name = "Apple",
                Description = "A crisp red apple, perfect for a quick snack.",
                ShortDescription = "a red apple",
                ItemType = ItemType.Food,
                Weight = 0.1m,
                Volume = 0.02m,
                Value = 2,
                IsStackable = true,
                MaxStackSize = 10,
                Rarity = "Common",
                ConsumableValue = 1,
                IsActive = true,
                CreatedBy = "System"
            },

            // Drink items
            new ItemTemplate
            {
                Id = 307,
                Name = "Waterskin",
                Description = "A leather waterskin filled with fresh water. Essential for any journey.",
                ShortDescription = "a waterskin",
                ItemType = ItemType.Drink,
                Weight = 1.0m,
                Volume = 0.3m,
                Value = 15,
                IsStackable = false,
                Rarity = "Common",
                ConsumableValue = 5,
                IsActive = true,
                CreatedBy = "System"
            },
            new ItemTemplate
            {
                Id = 308,
                Name = "Ale Flask",
                Description = "A flask of common ale. Not particularly refined but satisfying after a long day.",
                ShortDescription = "a flask of ale",
                ItemType = ItemType.Drink,
                Weight = 0.5m,
                Volume = 0.15m,
                Value = 8,
                IsStackable = true,
                MaxStackSize = 5,
                Rarity = "Common",
                ConsumableValue = 2,
                IsActive = true,
                CreatedBy = "System"
            },

            // Ammunition
            new ItemTemplate
            {
                Id = 309,
                Name = "Arrows",
                Description = "A bundle of iron-tipped arrows suitable for hunting bows.",
                ShortDescription = "iron-tipped arrows",
                ItemType = ItemType.Miscellaneous, // Could be a separate Ammunition type
                Weight = 0.1m,
                Volume = 0.02m,
                Value = 2,
                IsStackable = true,
                MaxStackSize = 50,
                Rarity = "Common",
                IsActive = true,
                CreatedBy = "System"
            },
        };

        _context.ItemTemplates.AddRange(consumables);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} consumable templates", consumables.Count);
    }

    private async Task SeedMiscItemsAsync()
    {
        _logger.LogInformation("Seeding miscellaneous item templates...");

        var miscItems = new List<ItemTemplate>
        {
            // Keys
            new ItemTemplate
            {
                Id = 401,
                Name = "Brass Key",
                Description = "A small brass key with simple teeth. It looks like it might fit a common lock.",
                ShortDescription = "a brass key",
                ItemType = ItemType.Key,
                Weight = 0.05m,
                Volume = 0.01m,
                Value = 5,
                IsStackable = false,
                IsDroppable = true,
                IsTradeable = false,
                Rarity = "Common",
                IsActive = true,
                CreatedBy = "System"
            },
            new ItemTemplate
            {
                Id = 402,
                Name = "Iron Key",
                Description = "A heavy iron key with intricate teeth. Looks important.",
                ShortDescription = "an iron key",
                ItemType = ItemType.Key,
                Weight = 0.1m,
                Volume = 0.02m,
                Value = 15,
                IsStackable = false,
                IsDroppable = true,
                IsTradeable = false,
                Rarity = "Uncommon",
                IsActive = true,
                CreatedBy = "System"
            },

            // Tools
            new ItemTemplate
            {
                Id = 403,
                Name = "Torch",
                Description = "A wooden torch wrapped in oil-soaked rags. Provides light when lit.",
                ShortDescription = "an unlit torch",
                ItemType = ItemType.Tool,
                Weight = 0.5m,
                Volume = 0.1m,
                Value = 3,
                IsStackable = true,
                MaxStackSize = 10,
                HasDurability = true,
                MaxDurability = 60, // Minutes of light
                Rarity = "Common",
                IsActive = true,
                CreatedBy = "System"
            },
            new ItemTemplate
            {
                Id = 404,
                Name = "Rope (50 ft)",
                Description = "A coil of sturdy hemp rope, fifty feet in length. Useful for climbing and binding.",
                ShortDescription = "a coil of rope",
                ItemType = ItemType.Tool,
                Weight = 5.0m,
                Volume = 0.5m,
                Value = 25,
                IsStackable = false,
                Rarity = "Common",
                IsActive = true,
                CreatedBy = "System"
            },
            new ItemTemplate
            {
                Id = 405,
                Name = "Flint and Steel",
                Description = "A small kit containing flint and a steel striker. Essential for starting fires.",
                ShortDescription = "flint and steel",
                ItemType = ItemType.Tool,
                Weight = 0.1m,
                Volume = 0.02m,
                Value = 10,
                IsStackable = false,
                Rarity = "Common",
                IsActive = true,
                CreatedBy = "System"
            },

            // Treasure
            new ItemTemplate
            {
                Id = 406,
                Name = "Gold Ring",
                Description = "A simple gold ring with no adornment. Worth a fair amount to any merchant.",
                ShortDescription = "a gold ring",
                ItemType = ItemType.Treasure,
                ArmorSlot = ArmorSlot.FingerLeft1,
                Weight = 0.02m,
                Volume = 0.001m,
                Value = 200,
                IsStackable = false,
                Rarity = "Uncommon",
                IsActive = true,
                CreatedBy = "System"
            },
            new ItemTemplate
            {
                Id = 407,
                Name = "Silver Necklace",
                Description = "A delicate silver chain with a small pendant. Elegant but unenchanted.",
                ShortDescription = "a silver necklace",
                ItemType = ItemType.Treasure,
                ArmorSlot = ArmorSlot.Neck,
                Weight = 0.05m,
                Volume = 0.005m,
                Value = 100,
                IsStackable = false,
                Rarity = "Uncommon",
                IsActive = true,
                CreatedBy = "System"
            },
            new ItemTemplate
            {
                Id = 408,
                Name = "Gemstone",
                Description = "A rough, uncut gemstone that sparkles in the light. Could be valuable to a jeweler.",
                ShortDescription = "an uncut gemstone",
                ItemType = ItemType.Treasure,
                Weight = 0.02m,
                Volume = 0.002m,
                Value = 150,
                IsStackable = true,
                MaxStackSize = 10,
                Rarity = "Uncommon",
                IsActive = true,
                CreatedBy = "System"
            },

            // Quest items (examples)
            new ItemTemplate
            {
                Id = 409,
                Name = "Worn Journal",
                Description = "A leather-bound journal with faded writing. The pages are filled with cramped handwriting.",
                ShortDescription = "a worn leather journal",
                ItemType = ItemType.QuestItem,
                Weight = 0.3m,
                Volume = 0.05m,
                Value = 0, // Quest items have no monetary value
                IsStackable = false,
                IsDroppable = false,
                IsTradeable = false,
                Rarity = "Common",
                IsActive = true,
                CreatedBy = "System"
            },
        };

        _context.ItemTemplates.AddRange(miscItems);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} miscellaneous item templates", miscItems.Count);
    }

    // Helper methods for creating item templates

    private static ItemTemplate CreateWeaponTemplate(
        int id,
        string name,
        string description,
        string shortDesc,
        decimal weight,
        decimal volume,
        int value,
        string rarity,
        WeaponType weaponType,
        DamageType damageType,
        DamageClass damageClass,
        ArmorSlot slot,
        int attackModifier = 0,
        int dodgeModifier = 0,
        bool isTwoHanded = false,
        bool requiresAmmunition = false,
        WeaponRange range = WeaponRange.Melee)
    {
        var template = new ItemTemplate
        {
            Id = id,
            Name = name,
            Description = description,
            ShortDescription = shortDesc,
            ItemType = ItemType.Weapon,
            WeaponType = weaponType,
            ArmorSlot = slot,
            Weight = weight,
            Volume = volume,
            Value = value,
            Rarity = rarity,
            HasDurability = true,
            MaxDurability = 100,
            IsActive = true,
            CreatedBy = "System",
            WeaponProperties = new WeaponTemplateProperties
            {
                ItemTemplateId = id,
                DamageType = damageType,
                DamageClass = damageClass,
                AttackValueModifier = attackModifier,
                DodgeModifier = dodgeModifier,
                IsTwoHanded = isTwoHanded,
                RequiresAmmunition = requiresAmmunition,
                Range = range
            }
        };

        return template;
    }

    private static ItemTemplate CreateArmorTemplate(
        int id,
        string name,
        string description,
        string shortDesc,
        decimal weight,
        decimal volume,
        int value,
        string rarity,
        ArmorSlot slot,
        DamageClass damageClass,
        int bashingAbsorption = 0,
        int cuttingAbsorption = 0,
        int piercingAbsorption = 0,
        int projectileAbsorption = 0,
        int energyAbsorption = 0,
        int heatAbsorption = 0,
        int coldAbsorption = 0,
        int acidAbsorption = 0,
        int dodgeModifier = 0,
        string? coverage = null)
    {
        var template = new ItemTemplate
        {
            Id = id,
            Name = name,
            Description = description,
            ShortDescription = shortDesc,
            ItemType = ItemType.Armor,
            ArmorSlot = slot,
            Weight = weight,
            Volume = volume,
            Value = value,
            Rarity = rarity,
            HasDurability = true,
            MaxDurability = 100,
            IsActive = true,
            CreatedBy = "System",
            ArmorProperties = new ArmorTemplateProperties
            {
                ItemTemplateId = id,
                DamageClass = damageClass,
                BashingAbsorption = bashingAbsorption,
                CuttingAbsorption = cuttingAbsorption,
                PiercingAbsorption = piercingAbsorption,
                ProjectileAbsorption = projectileAbsorption,
                EnergyAbsorption = energyAbsorption,
                HeatAbsorption = heatAbsorption,
                ColdAbsorption = coldAbsorption,
                AcidAbsorption = acidAbsorption,
                DodgeModifier = dodgeModifier,
                HitLocationCoverage = coverage
            }
        };

        return template;
    }

    private static ItemTemplate CreateContainerTemplate(
        int id,
        string name,
        string description,
        string shortDesc,
        decimal weight,
        decimal volume,
        int value,
        decimal maxWeight,
        decimal maxVolume,
        string? allowedTypes = null,
        bool isDroppable = true)
    {
        return new ItemTemplate
        {
            Id = id,
            Name = name,
            Description = description,
            ShortDescription = shortDesc,
            ItemType = ItemType.Container,
            Weight = weight,
            Volume = volume,
            Value = value,
            IsContainer = true,
            ContainerMaxWeight = maxWeight,
            ContainerMaxVolume = maxVolume,
            ContainerAllowedTypes = allowedTypes,
            ContainerWeightReduction = 1.0m,
            ContainerVolumeReduction = 1.0m,
            IsDroppable = isDroppable,
            Rarity = "Common",
            IsActive = true,
            CreatedBy = "System"
        };
    }
}
