using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mordecai.Game.Entities;

/// <summary>
/// Represents item type categories for organization
/// </summary>
public enum ItemType
{
    Weapon,
    Armor,
    Container,
    Consumable,
    Treasure,
    Key,
    Magic,
    Food,
    Drink,
    Tool,
    QuestItem,
    Miscellaneous
}

/// <summary>
/// Represents weapon type categories
/// </summary>
public enum WeaponType
{
    None,
    Sword,
    Axe,
    Mace,
    Polearm,
    Bow,
    Crossbow,
    Dagger,
    Staff,
    Wand
}

/// <summary>
/// Represents equipment slots for armor and wearable items
/// </summary>
public enum ArmorSlot
{
    None,
    Head,           // Hats, helms, crowns
    Face,           // Masks, goggles, eyewear
    Ears,           // Earrings
    Neck,           // Necklaces, amulets, collars
    Shoulders,      // Pauldrons, mantle, epaulettes
    Back,           // Cloaks, capes
    Chest,          // Shirts, coats, armor, robes (torso)
    ArmLeft,        // Left arm armor, bracers
    ArmRight,       // Right arm armor, bracers
    WristLeft,      // Left wrist bracelet, bracer
    WristRight,     // Right wrist bracelet, bracer
    HandLeft,       // Left glove, gauntlet
    HandRight,      // Right glove, gauntlet
    MainHand,       // Primary weapon, tool, wand
    OffHand,        // Shield, secondary weapon, torch
    TwoHand,        // Two-handed weapons (greatswords, staves)
    FingerLeft1,    // Left hand, finger 1 (thumb)
    FingerLeft2,    // Left hand, finger 2
    FingerLeft3,    // Left hand, finger 3
    FingerLeft4,    // Left hand, finger 4
    FingerLeft5,    // Left hand, finger 5 (pinky)
    FingerRight1,   // Right hand, finger 1 (thumb)
    FingerRight2,   // Right hand, finger 2
    FingerRight3,   // Right hand, finger 3
    FingerRight4,   // Right hand, finger 4
    FingerRight5,   // Right hand, finger 5 (pinky)
    Waist,          // Belts, sashes
    Legs,           // Pants, leg armor
    AnkleLeft,      // Left ankle jewelry, armor
    AnkleRight,     // Right ankle jewelry, armor
    FootLeft,       // Left boot, shoe
    FootRight       // Right boot, shoe
}

/// <summary>
/// Damage types supported by weapons, armor, and combat resolution
/// </summary>
public enum DamageType
{
    Bashing,
    Cutting,
    Piercing,
    Projectile,
    Energy,
    Heat,
    Cold,
    Acid
}

/// <summary>
/// Scaling tiers for damage and absorption interactions
/// </summary>
public enum DamageClass
{
    Class1 = 1,
    Class2 = 2,
    Class3 = 3,
    Class4 = 4
}

/// <summary>
/// Effective range bands for weapon templates
/// </summary>
public enum WeaponRange
{
    Melee = 0,
    SameRoom = 1,
    AdjacentRoom = 2
}

/// <summary>
/// Template definitions for items - the "blueprint" for creating item instances
/// </summary>
public class ItemTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Short description for inventory lists and quick identification
    /// </summary>
    [StringLength(200)]
    public string? ShortDescription { get; set; }

    [Required]
    public ItemType ItemType { get; set; }

    /// <summary>
    /// For weapons - specifies the weapon category
    /// </summary>
    public WeaponType? WeaponType { get; set; }

    /// <summary>
    /// For armor and equipment - specifies which slot(s) it occupies
    /// </summary>
    public ArmorSlot? ArmorSlot { get; set; }

    /// <summary>
    /// Weight in pounds (decimal for fractional weights)
    /// </summary>
    public decimal Weight { get; set; } = 0m;

    /// <summary>
    /// Volume in cubic feet (for inventory/container capacity calculations)
    /// </summary>
    public decimal Volume { get; set; } = 0m;

    /// <summary>
    /// Base value in copper pieces (economy may adjust this)
    /// </summary>
    public int Value { get; set; } = 0;

    /// <summary>
    /// Whether multiple instances can stack in a single inventory slot
    /// </summary>
    public bool IsStackable { get; set; } = false;

    /// <summary>
    /// Maximum stack size if stackable
    /// </summary>
    public int MaxStackSize { get; set; } = 1;

    /// <summary>
    /// Whether the item can be dropped/destroyed
    /// </summary>
    public bool IsDroppable { get; set; } = true;

    /// <summary>
    /// Whether the item can be traded between players
    /// </summary>
    public bool IsTradeable { get; set; } = true;

    /// <summary>
    /// Whether the item binds to the character when picked up
    /// </summary>
    public bool BindOnPickup { get; set; } = false;

    /// <summary>
    /// Whether the item binds when equipped
    /// </summary>
    public bool BindOnEquip { get; set; } = false;

    /// <summary>
    /// Whether this item is a container that can hold other items
    /// </summary>
    public bool IsContainer { get; set; } = false;

    /// <summary>
    /// Maximum weight the container can hold (only applicable if IsContainer = true)
    /// </summary>
    public decimal? ContainerMaxWeight { get; set; }

    /// <summary>
    /// Maximum volume the container can hold (only applicable if IsContainer = true)
    /// </summary>
    public decimal? ContainerMaxVolume { get; set; }

    /// <summary>
    /// Comma-separated list of item types the container can hold (e.g., "Weapon,Armor" or "Arrow,Bolt" for quivers)
    /// Null or empty means it can hold any item type
    /// </summary>
    [StringLength(500)]
    public string? ContainerAllowedTypes { get; set; }

    /// <summary>
    /// Weight reduction multiplier for contained items (magical containers)
    /// 1.0 = no reduction, 0.5 = half weight, 0.0 = weightless
    /// Only applicable if IsContainer = true
    /// </summary>
    public decimal? ContainerWeightReduction { get; set; } = 1.0m;

    /// <summary>
    /// Volume reduction multiplier for contained items (magical containers)
    /// 1.0 = no reduction, 0.5 = half volume, 0.0 = no volume
    /// Only applicable if IsContainer = true
    /// </summary>
    public decimal? ContainerVolumeReduction { get; set; } = 1.0m;

    /// <summary>
    /// Whether the item has durability and can break
    /// </summary>
    public bool HasDurability { get; set; } = false;

    /// <summary>
    /// Maximum durability points for new items
    /// </summary>
    public int? MaxDurability { get; set; }

    /// <summary>
    /// For food/drink - amount of hunger/thirst restored
    /// </summary>
    public int? ConsumableValue { get; set; }

    /// <summary>
    /// For magical items - the level or rarity tier
    /// </summary>
    public int? MagicLevel { get; set; }

    /// <summary>
    /// Rarity level (Common, Uncommon, Rare, Epic, Legendary)
    /// </summary>
    [StringLength(50)]
    public string Rarity { get; set; } = "Common";

    /// <summary>
    /// Display order for sorting in admin interfaces
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    [StringLength(450)]
    public string CreatedBy { get; set; } = string.Empty; // FK to AspNetUsers

    /// <summary>
    /// JSON field for item-specific properties and configuration
    /// Examples: special abilities, quest flags, enchantments, etc.
    /// </summary>
    [StringLength(4000)]
    public string? CustomProperties { get; set; }

    // Navigation properties
    public virtual WeaponTemplateProperties? WeaponProperties { get; set; }
    public virtual ArmorTemplateProperties? ArmorProperties { get; set; }
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
    public virtual ICollection<ItemSkillBonus> SkillBonuses { get; set; } = new List<ItemSkillBonus>();
    public virtual ICollection<ItemAttributeModifier> AttributeModifiers { get; set; } = new List<ItemAttributeModifier>();
}

/// <summary>
/// Actual item instances in the game world or in player inventories
/// </summary>
public class Item
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public int ItemTemplateId { get; set; }

    /// <summary>
    /// Room where the item is located (null if in inventory or container)
    /// </summary>
    public int? CurrentRoomId { get; set; }

    /// <summary>
    /// Character who owns this item (null if in room or other container)
    /// </summary>
    public Guid? OwnerCharacterId { get; set; }

    /// <summary>
    /// Container item this item is stored in (null if in room or directly in character inventory)
    /// </summary>
    public Guid? ContainerItemId { get; set; }

    /// <summary>
    /// Equipment slot if the item is currently equipped (null if not equipped)
    /// </summary>
    public ArmorSlot? EquippedSlot { get; set; }

    /// <summary>
    /// Number of items in this stack (1 if not stackable)
    /// </summary>
    public int StackSize { get; set; } = 1;

    /// <summary>
    /// Current durability if the item has durability
    /// </summary>
    public int? CurrentDurability { get; set; }

    /// <summary>
    /// Whether the item is currently equipped
    /// </summary>
    public bool IsEquipped { get; set; } = false;

    /// <summary>
    /// Whether the item is bound to the character and cannot be traded
    /// </summary>
    public bool IsBound { get; set; } = false;

    /// <summary>
    /// Custom name for the item instance (e.g., player-renamed items)
    /// </summary>
    [StringLength(100)]
    public string? CustomName { get; set; }

    /// <summary>
    /// When the item was created/spawned
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the item was last modified (moved, equipped, etc.)
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    /// When the item was picked up by the current owner
    /// </summary>
    public DateTimeOffset? PickedUpAt { get; set; }

    /// <summary>
    /// JSON field for instance-specific properties
    /// Examples: temporary buffs, charges remaining, crafting quality, etc.
    /// </summary>
    [StringLength(4000)]
    public string? CustomProperties { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ItemTemplateId))]
    public virtual ItemTemplate ItemTemplate { get; set; } = null!;

    public virtual Room? CurrentRoom { get; set; }

    public virtual Character? OwnerCharacter { get; set; }

    public virtual Item? ContainerItem { get; set; }

    /// <summary>
    /// Items contained within this item (if this item is a container)
    /// </summary>
    public virtual ICollection<Item> ContainedItems { get; set; } = new List<Item>();

    /// <summary>
    /// Calculates the total weight including contained items
    /// Applies weight reduction for magical containers
    /// </summary>
    [NotMapped]
    public decimal TotalWeight
    {
        get
        {
            var baseWeight = ItemTemplate.Weight * StackSize;
            if (ItemTemplate.IsContainer)
            {
                var containedWeight = ContainedItems.Sum(item => item.TotalWeight);
                var weightReduction = ItemTemplate.ContainerWeightReduction ?? 1.0m;
                return baseWeight + (containedWeight * weightReduction);
            }
            return baseWeight;
        }
    }

    /// <summary>
    /// Calculates the total volume including contained items
    /// Applies volume reduction for magical containers
    /// </summary>
    [NotMapped]
    public decimal TotalVolume
    {
        get
        {
            var baseVolume = ItemTemplate.Volume * StackSize;
            if (ItemTemplate.IsContainer)
            {
                var containedVolume = ContainedItems.Sum(item => item.TotalVolume);
                var volumeReduction = ItemTemplate.ContainerVolumeReduction ?? 1.0m;
                return baseVolume + (containedVolume * volumeReduction);
            }
            return baseVolume;
        }
    }

    /// <summary>
    /// Gets the effective name of the item (custom name if set, otherwise template name)
    /// </summary>
    [NotMapped]
    public string EffectiveName => CustomName ?? ItemTemplate.Name;

    /// <summary>
    /// Checks if the item is broken (durability reached 0)
    /// </summary>
    [NotMapped]
    public bool IsBroken => ItemTemplate.HasDurability && CurrentDurability.HasValue && CurrentDurability.Value <= 0;
}

/// <summary>
/// Extended configuration for weapon item templates
/// </summary>
public class WeaponTemplateProperties
{
    [Key]
    [ForeignKey(nameof(ItemTemplate))]
    public int ItemTemplateId { get; set; }

    public int? SkillDefinitionId { get; set; }

    /// <summary>
    /// Minimum effective skill level required to wield the weapon without penalties
    /// </summary>
    public int MinimumSkillLevel { get; set; } = 0;

    public DamageType DamageType { get; set; } = DamageType.Cutting;

    public DamageClass DamageClass { get; set; } = DamageClass.Class1;

    /// <summary>
    /// Flat modifier applied to the success value after a hit is confirmed
    /// </summary>
    public int BaseSuccessValueModifier { get; set; } = 0;

    /// <summary>
    /// Modifier applied directly to the attack value roll
    /// </summary>
    public int AttackValueModifier { get; set; } = 0;

    /// <summary>
    /// Modifier applied to the wielder's dodge ability while the weapon is equipped
    /// </summary>
    public int DodgeModifier { get; set; } = 0;

    public WeaponRange Range { get; set; } = WeaponRange.Melee;

    public bool CanKnockback { get; set; } = false;

    public bool IsTwoHanded { get; set; } = false;

    /// <summary>
    /// Whether the weapon consumes ammunition on use
    /// </summary>
    public bool RequiresAmmunition { get; set; } = false;

    // Navigation properties
    public virtual ItemTemplate ItemTemplate { get; set; } = null!;
}

/// <summary>
/// Extended configuration for armor item templates
/// </summary>
public class ArmorTemplateProperties
{
    [Key]
    [ForeignKey(nameof(ItemTemplate))]
    public int ItemTemplateId { get; set; }

    public int? SkillDefinitionId { get; set; }

    /// <summary>
    /// Minimum effective skill level required to wear the armor without penalties
    /// </summary>
    public int MinimumSkillLevel { get; set; } = 0;

    public DamageClass DamageClass { get; set; } = DamageClass.Class1;

    public int BashingAbsorption { get; set; } = 0;
    public int CuttingAbsorption { get; set; } = 0;
    public int PiercingAbsorption { get; set; } = 0;
    public int ProjectileAbsorption { get; set; } = 0;
    public int EnergyAbsorption { get; set; } = 0;
    public int HeatAbsorption { get; set; } = 0;
    public int ColdAbsorption { get; set; } = 0;
    public int AcidAbsorption { get; set; } = 0;

    /// <summary>
    /// Modifier applied to dodge while this armor is equipped (negative values apply penalties)
    /// </summary>
    public int DodgeModifier { get; set; } = 0;

    /// <summary>
    /// Modifier applied to physicality while this armor is equipped (negative values apply penalties)
    /// </summary>
    public int StrengthModifier { get; set; } = 0;

    /// <summary>
    /// Comma-separated list of armor slots covered for layered damage mitigation
    /// </summary>
    [StringLength(200)]
    public string? HitLocationCoverage { get; set; }

    /// <summary>
    /// Determines processing order when multiple armor pieces overlap (higher values resolve later)
    /// </summary>
    public int LayerPriority { get; set; } = 0;

    // Navigation properties
    public virtual ItemTemplate ItemTemplate { get; set; } = null!;
}

/// <summary>
/// Defines skill bonuses provided by item templates
/// </summary>
public class ItemSkillBonus
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ItemTemplateId { get; set; }

    [Required]
    public int SkillDefinitionId { get; set; }

    /// <summary>
    /// Type of bonus (FlatBonus, PercentageBonus, CooldownReduction)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string BonusType { get; set; } = "FlatBonus";

    /// <summary>
    /// Numeric value of the bonus
    /// For FlatBonus: added to skill level
    /// For PercentageBonus: multiplied by skill effectiveness (e.g., 10 = 10% bonus)
    /// For CooldownReduction: reduces cooldown in seconds
    /// </summary>
    public decimal BonusValue { get; set; } = 0m;

    /// <summary>
    /// Condition or requirement for the bonus to apply (optional)
    /// Examples: "InCombat", "AtNight", "WhenHealthBelowHalf"
    /// </summary>
    [StringLength(200)]
    public string? Condition { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ItemTemplateId))]
    public virtual ItemTemplate ItemTemplate { get; set; } = null!;

    [ForeignKey(nameof(SkillDefinitionId))]
    public virtual SkillDefinition SkillDefinition { get; set; } = null!;
}

/// <summary>
/// Defines attribute modifiers provided by item templates
/// </summary>
public class ItemAttributeModifier
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ItemTemplateId { get; set; }

    /// <summary>
    /// The attribute being modified (STR, DEX, END, INT, ITT, WIL, PHY)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// Type of modifier (FlatBonus, PercentageBonus)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ModifierType { get; set; } = "FlatBonus";

    /// <summary>
    /// Numeric value of the modifier
    /// </summary>
    public int ModifierValue { get; set; } = 0;

    /// <summary>
    /// Condition for the modifier to apply (optional)
    /// </summary>
    [StringLength(200)]
    public string? Condition { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ItemTemplateId))]
    public virtual ItemTemplate ItemTemplate { get; set; } = null!;
}

/// <summary>
/// Tracks character inventory capacity and current usage
/// </summary>
public class CharacterInventory
{
    [Key]
    public Guid CharacterId { get; set; }

    /// <summary>
    /// Maximum weight the character can carry (based on Physicality)
    /// </summary>
    public decimal MaxWeight { get; set; }

    /// <summary>
    /// Maximum volume the character can carry (based on containers and base capacity)
    /// </summary>
    public decimal MaxVolume { get; set; }

    /// <summary>
    /// When the inventory capacity was last calculated
    /// </summary>
    public DateTimeOffset LastCalculatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation property
    [ForeignKey(nameof(CharacterId))]
    public virtual Character Character { get; set; } = null!;

    /// <summary>
    /// Calculates base carrying capacity based on character's Physicality
    /// Uses exponential scaling to reflect the bell-curve distribution of 4dF attributes.
    /// Formula: Base(50) × (1.15 ^ (Physicality - 10))
    /// 
    /// Examples:
    /// - Physicality 6:  50 × 1.15^(-4) = ~28.6 lbs (very weak)
    /// - Physicality 8:  50 × 1.15^(-2) = ~37.8 lbs (weak)
    /// - Physicality 10: 50 × 1.15^(0)  = 50 lbs (average)
    /// - Physicality 12: 50 × 1.15^(2)  = ~66.1 lbs (strong)
    /// - Physicality 14: 50 × 1.15^(4)  = ~87.4 lbs (very strong)
    /// </summary>
    public static decimal CalculateMaxWeight(int physicality)
    {
        const decimal baseWeight = 50m;
        const double scalingFactor = 1.15; // 15% increase per point above 10
        
        var exponent = physicality - 10;
        var multiplier = Math.Pow(scalingFactor, exponent);
        
        return baseWeight * (decimal)multiplier;
    }

    /// <summary>
    /// Calculates base volume capacity based on character's Physicality
    /// Uses exponential scaling to reflect the bell-curve distribution of 4dF attributes.
    /// Formula: Base(10) × (1.15 ^ (Physicality - 10))
    /// 
    /// Examples:
    /// - Physicality 6:  10 × 1.15^(-4) = ~5.7 cu.ft. (very weak)
    /// - Physicality 8:  10 × 1.15^(-2) = ~7.6 cu.ft. (weak)
    /// - Physicality 10: 10 × 1.15^(0)  = 10 cu.ft. (average)
    /// - Physicality 12: 10 × 1.15^(2)  = ~13.2 cu.ft. (strong)
    /// - Physicality 14: 10 × 1.15^(4)  = ~17.5 cu.ft. (very strong)
    /// </summary>
    public static decimal CalculateMaxVolume(int physicality)
    {
        const decimal baseVolume = 10m;
        const double scalingFactor = 1.15; // 15% increase per point above 10
        
        var exponent = physicality - 10;
        var multiplier = Math.Pow(scalingFactor, exponent);
        
        return baseVolume * (decimal)multiplier;
    }
}
