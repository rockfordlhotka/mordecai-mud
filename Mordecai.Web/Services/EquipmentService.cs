using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

public interface IEquipmentService
{
    Task<EquipResult> EquipAsync(Guid characterId, string userId, Guid itemId);
    Task<UnequipResult> UnequipAsync(Guid characterId, string userId, Guid itemId);
    Task<UnequipResult> UnequipSlotAsync(Guid characterId, string userId, ArmorSlot slot);
    Task<IReadOnlyList<Item>> GetEquippedItemsAsync(Guid characterId, string userId);
    Task<IReadOnlyList<Item>> GetInventoryItemsAsync(Guid characterId, string userId);
    Task<Dictionary<int, int>> GetActiveSkillBonuses(Guid characterId, string userId);
    Task<Dictionary<string, int>> GetActiveAttributeModifiers(Guid characterId, string userId);
    Task<int> CalculateEffectiveAttributeValue(Guid characterId, string userId, string attributeName, int baseValue);
}

public class EquipmentService : IEquipmentService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<EquipmentService> _logger;

    public EquipmentService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<EquipmentService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<EquipResult> EquipAsync(Guid characterId, string userId, Guid itemId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // Load character to verify ownership
            var character = await context.Characters
                .FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId);
            
            if (character == null)
            {
                return new EquipResult(false, "Character not found.", null);
            }

            // Load the item with its template
            var item = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
            {
                return new EquipResult(false, "Item not found.", null);
            }

            // Verify the character owns this item
            if (item.OwnerCharacterId != characterId)
            {
                return new EquipResult(false, "You don't own that item.", null);
            }

            // Check if item is in a container (must be in direct inventory to equip)
            if (item.ContainerItemId.HasValue)
            {
                return new EquipResult(false, "You must take the item out of its container before equipping it.", null);
            }

            // Check if item is already equipped
            if (item.IsEquipped)
            {
                return new EquipResult(false, $"{item.ItemTemplate.Name} is already equipped.", item);
            }

            // Determine which slot to equip to
            var targetSlot = item.ItemTemplate.ArmorSlot ?? ArmorSlot.None;
            if (targetSlot == ArmorSlot.None)
            {
                return new EquipResult(false, $"{item.ItemTemplate.Name} cannot be equipped.", item);
            }

            // Check if the slot is already occupied
            var existingItemInSlot = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => 
                    i.OwnerCharacterId == characterId &&
                    i.IsEquipped &&
                    i.EquippedSlot == targetSlot);

            if (existingItemInSlot != null)
            {
                // Auto-unequip the existing item
                existingItemInSlot.IsEquipped = false;
                existingItemInSlot.EquippedSlot = null;
                _logger.LogDebug("Auto-unequipped {ItemName} from {Slot} for character {CharacterId}",
                    existingItemInSlot.ItemTemplate.Name, targetSlot, characterId);
            }

            // Special handling for two-handed weapons
            if (targetSlot == ArmorSlot.TwoHand)
            {
                // Unequip MainHand and OffHand items
                var handheldItems = await context.Items
                    .Where(i => 
                        i.OwnerCharacterId == characterId &&
                        i.IsEquipped &&
                        (i.EquippedSlot == ArmorSlot.MainHand || i.EquippedSlot == ArmorSlot.OffHand))
                    .ToListAsync();

                foreach (var handheldItem in handheldItems)
                {
                    handheldItem.IsEquipped = false;
                    handheldItem.EquippedSlot = null;
                }
            }
            // If equipping to MainHand or OffHand, unequip two-handed weapon
            else if (targetSlot is ArmorSlot.MainHand or ArmorSlot.OffHand)
            {
                var twoHandedItem = await context.Items
                    .FirstOrDefaultAsync(i =>
                        i.OwnerCharacterId == characterId &&
                        i.IsEquipped &&
                        i.EquippedSlot == ArmorSlot.TwoHand);

                if (twoHandedItem != null)
                {
                    twoHandedItem.IsEquipped = false;
                    twoHandedItem.EquippedSlot = null;
                }
            }

            // Equip the item
            item.IsEquipped = true;
            item.EquippedSlot = targetSlot;

            await context.SaveChangesAsync();

            _logger.LogInformation("Character {CharacterId} equipped {ItemName} to {Slot}",
                characterId, item.ItemTemplate.Name, targetSlot);

            return new EquipResult(true, $"You equip {item.ItemTemplate.Name}.", item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error equipping item {ItemId} for character {CharacterId}", itemId, characterId);
            return new EquipResult(false, "An error occurred while equipping the item.", null);
        }
    }

    public async Task<UnequipResult> UnequipAsync(Guid characterId, string userId, Guid itemId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Load character to verify ownership
            var character = await context.Characters
                .FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId);

            if (character == null)
            {
                return new UnequipResult(false, "Character not found.", null);
            }

            // Load the item with its template
            var item = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
            {
                return new UnequipResult(false, "Item not found.", null);
            }

            // Verify the character owns this item
            if (item.OwnerCharacterId != characterId)
            {
                return new UnequipResult(false, "You don't own that item.", null);
            }

            // Check if item is equipped
            if (!item.IsEquipped)
            {
                return new UnequipResult(false, $"{item.ItemTemplate.Name} is not equipped.", item);
            }

            // Unequip the item
            var previousSlot = item.EquippedSlot;
            item.IsEquipped = false;
            item.EquippedSlot = null;

            await context.SaveChangesAsync();

            _logger.LogInformation("Character {CharacterId} unequipped {ItemName} from {Slot}",
                characterId, item.ItemTemplate.Name, previousSlot);

            return new UnequipResult(true, $"You unequip {item.ItemTemplate.Name}.", item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unequipping item {ItemId} for character {CharacterId}", itemId, characterId);
            return new UnequipResult(false, "An error occurred while unequipping the item.", null);
        }
    }

    public async Task<UnequipResult> UnequipSlotAsync(Guid characterId, string userId, ArmorSlot slot)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Load character to verify ownership
            var character = await context.Characters
                .FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId);

            if (character == null)
            {
                return new UnequipResult(false, "Character not found.", null);
            }

            // Find the item in the specified slot
            var item = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i =>
                    i.OwnerCharacterId == characterId &&
                    i.IsEquipped &&
                    i.EquippedSlot == slot);

            if (item == null)
            {
                return new UnequipResult(false, $"Nothing is equipped in the {slot} slot.", null);
            }

            // Unequip the item
            item.IsEquipped = false;
            item.EquippedSlot = null;

            await context.SaveChangesAsync();

            _logger.LogInformation("Character {CharacterId} unequipped {ItemName} from {Slot}",
                characterId, item.ItemTemplate.Name, slot);

            return new UnequipResult(true, $"You unequip {item.ItemTemplate.Name}.", item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unequipping slot {Slot} for character {CharacterId}", slot, characterId);
            return new UnequipResult(false, "An error occurred while unequipping.", null);
        }
    }

    public async Task<IReadOnlyList<Item>> GetEquippedItemsAsync(Guid characterId, string userId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Verify character ownership
            var characterExists = await context.Characters
                .AnyAsync(c => c.Id == characterId && c.UserId == userId);

            if (!characterExists)
            {
                return Array.Empty<Item>();
            }

            return await context.Items
                .Include(i => i.ItemTemplate)
                    .ThenInclude(it => it.WeaponProperties)
                .Include(i => i.ItemTemplate)
                    .ThenInclude(it => it.ArmorProperties)
                .Include(i => i.ItemTemplate)
                    .ThenInclude(it => it.SkillBonuses)
                .Include(i => i.ItemTemplate)
                    .ThenInclude(it => it.AttributeModifiers)
                .Where(i => i.OwnerCharacterId == characterId && i.IsEquipped)
                .OrderBy(i => i.EquippedSlot)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipped items for character {CharacterId}", characterId);
            return Array.Empty<Item>();
        }
    }

    public async Task<IReadOnlyList<Item>> GetInventoryItemsAsync(Guid characterId, string userId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Verify character ownership
            var characterExists = await context.Characters
                .AnyAsync(c => c.Id == characterId && c.UserId == userId);

            if (!characterExists)
            {
                return Array.Empty<Item>();
            }

            // Get all items owned by the character (not in a room, not in a container)
            return await context.Items
                .Include(i => i.ItemTemplate)
                    .ThenInclude(it => it.WeaponProperties)
                .Include(i => i.ItemTemplate)
                    .ThenInclude(it => it.ArmorProperties)
                .Where(i => 
                    i.OwnerCharacterId == characterId && 
                    !i.CurrentRoomId.HasValue &&
                    !i.ContainerItemId.HasValue)
                .OrderBy(i => i.IsEquipped ? 0 : 1) // Equipped items first
                .ThenBy(i => i.ItemTemplate.Name)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory items for character {CharacterId}", characterId);
            return Array.Empty<Item>();
        }
    }

    public async Task<Dictionary<int, int>> GetActiveSkillBonuses(Guid characterId, string userId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Get all equipped items with skill bonuses
            var equippedItems = await context.Items
                .Include(i => i.ItemTemplate)
                    .ThenInclude(it => it.SkillBonuses)
                .Where(i => i.OwnerCharacterId == characterId && i.IsEquipped)
                .AsNoTracking()
                .ToListAsync();

            // Aggregate bonuses by skill (flat bonuses only for now)
            var bonusesBySkill = new Dictionary<int, int>();

            foreach (var item in equippedItems)
            {
                if (item.ItemTemplate?.SkillBonuses == null)
                {
                    continue;
                }

                foreach (var bonus in item.ItemTemplate.SkillBonuses.Where(b => b.BonusType == "FlatBonus"))
                {
                    var bonusValue = (int)bonus.BonusValue;
                    if (bonusesBySkill.ContainsKey(bonus.SkillDefinitionId))
                    {
                        bonusesBySkill[bonus.SkillDefinitionId] += bonusValue;
                    }
                    else
                    {
                        bonusesBySkill[bonus.SkillDefinitionId] = bonusValue;
                    }
                }
            }

            return bonusesBySkill;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating skill bonuses for character {CharacterId}", characterId);
            return new Dictionary<int, int>();
        }
    }

    public async Task<Dictionary<string, int>> GetActiveAttributeModifiers(Guid characterId, string userId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Get all equipped items with attribute modifiers
            var equippedItems = await context.Items
                .Include(i => i.ItemTemplate)
                    .ThenInclude(it => it.AttributeModifiers)
                .Where(i => i.OwnerCharacterId == characterId && i.IsEquipped)
                .AsNoTracking()
                .ToListAsync();

            // Aggregate modifiers by attribute (flat bonuses only for now)
            var modifiersByAttribute = new Dictionary<string, int>();

            foreach (var item in equippedItems)
            {
                if (item.ItemTemplate?.AttributeModifiers == null)
                {
                    continue;
                }

                foreach (var modifier in item.ItemTemplate.AttributeModifiers.Where(m => m.ModifierType == "FlatBonus"))
                {
                    if (modifiersByAttribute.ContainsKey(modifier.AttributeName))
                    {
                        modifiersByAttribute[modifier.AttributeName] += modifier.ModifierValue;
                    }
                    else
                    {
                        modifiersByAttribute[modifier.AttributeName] = modifier.ModifierValue;
                    }
                }
            }

            return modifiersByAttribute;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating attribute modifiers for character {CharacterId}", characterId);
            return new Dictionary<string, int>();
        }
    }

    public async Task<int> CalculateEffectiveAttributeValue(Guid characterId, string userId, string attributeName, int baseValue)
    {
        var modifiers = await GetActiveAttributeModifiers(characterId, userId);
        
        if (modifiers.TryGetValue(attributeName, out var modifier))
        {
            return baseValue + modifier;
        }

        return baseValue;
    }
}

/// <summary>
/// Result of an equip operation
/// </summary>
public sealed record EquipResult(bool Success, string Message, Item? Item);

/// <summary>
/// Result of an unequip operation
/// </summary>
public sealed record UnequipResult(bool Success, string Message, Item? Item);
