using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing character inventory capacity and item carrying.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Gets or creates the inventory record for a character.
    /// </summary>
    Task<CharacterInventory> GetOrCreateInventoryAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Recalculates and updates the inventory capacity based on character's Physicality.
    /// </summary>
    Task<CharacterInventory> UpdateInventoryCapacityAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current weight and volume of items carried by a character.
    /// </summary>
    Task<InventoryUsage> GetCurrentUsageAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the character can carry the specified item (based on weight/volume limits).
    /// </summary>
    Task<CapacityCheckResult> CanCarryItemAsync(Guid characterId, Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the character can carry an item of the specified weight and volume.
    /// </summary>
    Task<CapacityCheckResult> CanCarryWeightVolumeAsync(
        Guid characterId,
        decimal weight,
        decimal volume,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if the character is currently overweight.
    /// </summary>
    Task<bool> IsOverweightAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the character is currently over volume capacity.
    /// </summary>
    Task<bool> IsOverVolumeAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets the encumbrance level of the character (0 = no encumbrance, higher = more burdened).
    /// </summary>
    Task<EncumbranceLevel> GetEncumbranceLevelAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets an inventory summary including capacity, usage, and item counts.
    /// </summary>
    Task<InventorySummary> GetInventorySummaryAsync(Guid characterId, CancellationToken ct = default);
}

public sealed class InventoryService : IInventoryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<InventoryService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<CharacterInventory> GetOrCreateInventoryAsync(Guid characterId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var inventory = await context.CharacterInventories
            .FirstOrDefaultAsync(i => i.CharacterId == characterId, ct);

        if (inventory != null)
        {
            return inventory;
        }

        // Need to create inventory - get character's Physicality
        var character = await context.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId, ct);

        if (character == null)
        {
            throw new InvalidOperationException($"Character {characterId} not found.");
        }

        inventory = new CharacterInventory
        {
            CharacterId = characterId,
            MaxWeight = CharacterInventory.CalculateMaxWeight(character.Physicality),
            MaxVolume = CharacterInventory.CalculateMaxVolume(character.Physicality),
            LastCalculatedAt = DateTimeOffset.UtcNow
        };

        context.CharacterInventories.Add(inventory);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created inventory for character {CharacterId}: MaxWeight={MaxWeight:F1}, MaxVolume={MaxVolume:F1}",
            characterId, inventory.MaxWeight, inventory.MaxVolume);

        return inventory;
    }

    public async Task<CharacterInventory> UpdateInventoryCapacityAsync(Guid characterId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var character = await context.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId, ct);

        if (character == null)
        {
            throw new InvalidOperationException($"Character {characterId} not found.");
        }

        var inventory = await context.CharacterInventories
            .FirstOrDefaultAsync(i => i.CharacterId == characterId, ct);

        if (inventory == null)
        {
            inventory = new CharacterInventory
            {
                CharacterId = characterId,
                MaxWeight = CharacterInventory.CalculateMaxWeight(character.Physicality),
                MaxVolume = CharacterInventory.CalculateMaxVolume(character.Physicality),
                LastCalculatedAt = DateTimeOffset.UtcNow
            };
            context.CharacterInventories.Add(inventory);
        }
        else
        {
            inventory.MaxWeight = CharacterInventory.CalculateMaxWeight(character.Physicality);
            inventory.MaxVolume = CharacterInventory.CalculateMaxVolume(character.Physicality);
            inventory.LastCalculatedAt = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(ct);

        _logger.LogDebug(
            "Updated inventory capacity for character {CharacterId}: MaxWeight={MaxWeight:F1}, MaxVolume={MaxVolume:F1}",
            characterId, inventory.MaxWeight, inventory.MaxVolume);

        return inventory;
    }

    public async Task<InventoryUsage> GetCurrentUsageAsync(Guid characterId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Get all items owned by the character (not in containers - those are counted via container's TotalWeight)
        var items = await context.Items
            .Include(i => i.ItemTemplate)
            .Include(i => i.ContainedItems)
                .ThenInclude(ci => ci.ItemTemplate)
            .Where(i => i.OwnerCharacterId == characterId && !i.ContainerItemId.HasValue)
            .AsNoTracking()
            .ToListAsync(ct);

        var totalWeight = 0m;
        var totalVolume = 0m;

        foreach (var item in items)
        {
            totalWeight += CalculateItemWeight(item);
            totalVolume += CalculateItemVolume(item);
        }

        return new InventoryUsage(totalWeight, totalVolume, items.Count);
    }

    public async Task<CapacityCheckResult> CanCarryItemAsync(Guid characterId, Guid itemId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var item = await context.Items
            .Include(i => i.ItemTemplate)
            .Include(i => i.ContainedItems)
                .ThenInclude(ci => ci.ItemTemplate)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item == null)
        {
            return new CapacityCheckResult(false, "Item not found.", 0, 0, 0, 0);
        }

        var itemWeight = CalculateItemWeight(item);
        var itemVolume = CalculateItemVolume(item);

        return await CanCarryWeightVolumeAsync(characterId, itemWeight, itemVolume, ct);
    }

    public async Task<CapacityCheckResult> CanCarryWeightVolumeAsync(
        Guid characterId,
        decimal weight,
        decimal volume,
        CancellationToken ct = default)
    {
        var inventory = await GetOrCreateInventoryAsync(characterId, ct);
        var usage = await GetCurrentUsageAsync(characterId, ct);

        var newTotalWeight = usage.CurrentWeight + weight;
        var newTotalVolume = usage.CurrentVolume + volume;

        var canCarry = newTotalWeight <= inventory.MaxWeight && newTotalVolume <= inventory.MaxVolume;

        string message;
        if (canCarry)
        {
            message = "You can carry this item.";
        }
        else if (newTotalWeight > inventory.MaxWeight && newTotalVolume > inventory.MaxVolume)
        {
            message = "This item is too heavy and bulky for you to carry.";
        }
        else if (newTotalWeight > inventory.MaxWeight)
        {
            message = "This item is too heavy for you to carry.";
        }
        else
        {
            message = "This item is too bulky for you to carry.";
        }

        return new CapacityCheckResult(
            canCarry,
            message,
            usage.CurrentWeight,
            inventory.MaxWeight,
            usage.CurrentVolume,
            inventory.MaxVolume);
    }

    public async Task<bool> IsOverweightAsync(Guid characterId, CancellationToken ct = default)
    {
        var inventory = await GetOrCreateInventoryAsync(characterId, ct);
        var usage = await GetCurrentUsageAsync(characterId, ct);

        return usage.CurrentWeight > inventory.MaxWeight;
    }

    public async Task<bool> IsOverVolumeAsync(Guid characterId, CancellationToken ct = default)
    {
        var inventory = await GetOrCreateInventoryAsync(characterId, ct);
        var usage = await GetCurrentUsageAsync(characterId, ct);

        return usage.CurrentVolume > inventory.MaxVolume;
    }

    public async Task<EncumbranceLevel> GetEncumbranceLevelAsync(Guid characterId, CancellationToken ct = default)
    {
        var inventory = await GetOrCreateInventoryAsync(characterId, ct);
        var usage = await GetCurrentUsageAsync(characterId, ct);

        // Calculate encumbrance based on the higher of weight% or volume%
        var weightPercent = inventory.MaxWeight > 0 ? usage.CurrentWeight / inventory.MaxWeight : 0;
        var volumePercent = inventory.MaxVolume > 0 ? usage.CurrentVolume / inventory.MaxVolume : 0;
        var encumbrancePercent = Math.Max(weightPercent, volumePercent);

        // Determine encumbrance level
        // 0-50%: None
        // 50-75%: Light
        // 75-100%: Medium
        // 100-125%: Heavy
        // 125%+: Severe
        var level = encumbrancePercent switch
        {
            <= 0.50m => EncumbranceLevel.None,
            <= 0.75m => EncumbranceLevel.Light,
            <= 1.00m => EncumbranceLevel.Medium,
            <= 1.25m => EncumbranceLevel.Heavy,
            _ => EncumbranceLevel.Severe
        };

        return level;
    }

    public async Task<InventorySummary> GetInventorySummaryAsync(Guid characterId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var inventory = await GetOrCreateInventoryAsync(characterId, ct);
        var usage = await GetCurrentUsageAsync(characterId, ct);
        var encumbrance = await GetEncumbranceLevelAsync(characterId, ct);

        // Count equipped items
        var equippedCount = await context.Items
            .Where(i => i.OwnerCharacterId == characterId && i.IsEquipped)
            .CountAsync(ct);

        // Count items in containers owned by character
        var containedCount = await context.Items
            .Where(i => i.OwnerCharacterId == characterId && i.ContainerItemId.HasValue)
            .CountAsync(ct);

        // Count containers
        var containerCount = await context.Items
            .Include(i => i.ItemTemplate)
            .Where(i => i.OwnerCharacterId == characterId && i.ItemTemplate.IsContainer)
            .CountAsync(ct);

        return new InventorySummary(
            CurrentWeight: usage.CurrentWeight,
            MaxWeight: inventory.MaxWeight,
            CurrentVolume: usage.CurrentVolume,
            MaxVolume: inventory.MaxVolume,
            TotalItemCount: usage.ItemCount,
            EquippedItemCount: equippedCount,
            ContainedItemCount: containedCount,
            ContainerCount: containerCount,
            Encumbrance: encumbrance);
    }

    /// <summary>
    /// Calculates the total weight of an item including contained items with weight reduction.
    /// </summary>
    private static decimal CalculateItemWeight(Item item)
    {
        var baseWeight = item.ItemTemplate.Weight * item.StackSize;

        if (item.ItemTemplate.IsContainer && item.ContainedItems.Count > 0)
        {
            var containedWeight = item.ContainedItems.Sum(ci => CalculateItemWeight(ci));
            var weightReduction = item.ItemTemplate.ContainerWeightReduction ?? 1.0m;
            return baseWeight + (containedWeight * weightReduction);
        }

        return baseWeight;
    }

    /// <summary>
    /// Calculates the total volume of an item including contained items with volume reduction.
    /// </summary>
    private static decimal CalculateItemVolume(Item item)
    {
        var baseVolume = item.ItemTemplate.Volume * item.StackSize;

        if (item.ItemTemplate.IsContainer && item.ContainedItems.Count > 0)
        {
            var containedVolume = item.ContainedItems.Sum(ci => CalculateItemVolume(ci));
            var volumeReduction = item.ItemTemplate.ContainerVolumeReduction ?? 1.0m;
            return baseVolume + (containedVolume * volumeReduction);
        }

        return baseVolume;
    }
}

// Result and data records

/// <summary>
/// Current inventory weight and volume usage.
/// </summary>
public sealed record InventoryUsage(decimal CurrentWeight, decimal CurrentVolume, int ItemCount);

/// <summary>
/// Result of a capacity check for carrying an item.
/// </summary>
public sealed record CapacityCheckResult(
    bool CanCarry,
    string Message,
    decimal CurrentWeight,
    decimal MaxWeight,
    decimal CurrentVolume,
    decimal MaxVolume);

/// <summary>
/// Encumbrance levels affecting character movement and actions.
/// </summary>
public enum EncumbranceLevel
{
    /// <summary>No encumbrance penalties (0-50% capacity)</summary>
    None = 0,

    /// <summary>Light encumbrance - minor movement penalty (50-75% capacity)</summary>
    Light = 1,

    /// <summary>Medium encumbrance - moderate penalties (75-100% capacity)</summary>
    Medium = 2,

    /// <summary>Heavy encumbrance - significant penalties (100-125% capacity)</summary>
    Heavy = 3,

    /// <summary>Severe encumbrance - major penalties, may prevent actions (125%+ capacity)</summary>
    Severe = 4
}

/// <summary>
/// Complete summary of a character's inventory state.
/// </summary>
public sealed record InventorySummary(
    decimal CurrentWeight,
    decimal MaxWeight,
    decimal CurrentVolume,
    decimal MaxVolume,
    int TotalItemCount,
    int EquippedItemCount,
    int ContainedItemCount,
    int ContainerCount,
    EncumbranceLevel Encumbrance)
{
    public decimal WeightPercent => MaxWeight > 0 ? CurrentWeight / MaxWeight * 100 : 0;
    public decimal VolumePercent => MaxVolume > 0 ? CurrentVolume / MaxVolume * 100 : 0;
}
