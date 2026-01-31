using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing item instances - spawning, destruction, and queries.
/// </summary>
public interface IItemService
{
    /// <summary>
    /// Retrieves an item by its unique identifier.
    /// </summary>
    Task<Item?> GetItemAsync(Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Spawns a new item instance from a template.
    /// </summary>
    /// <param name="templateId">The item template to instantiate</param>
    /// <param name="roomId">Optional room to spawn the item in</param>
    /// <param name="ownerId">Optional character to give the item to</param>
    /// <param name="stackSize">Number of items in the stack (for stackable items)</param>
    /// <param name="ct">Cancellation token</param>
    Task<ItemSpawnResult> SpawnItemAsync(
        int templateId,
        int? roomId = null,
        Guid? ownerId = null,
        int stackSize = 1,
        CancellationToken ct = default);

    /// <summary>
    /// Destroys an item, removing it from the game world.
    /// </summary>
    Task<ItemDestroyResult> DestroyItemAsync(Guid itemId, string reason, CancellationToken ct = default);

    /// <summary>
    /// Gets all items in a specific room.
    /// </summary>
    Task<IReadOnlyList<Item>> GetItemsInRoomAsync(int roomId, CancellationToken ct = default);

    /// <summary>
    /// Gets all items owned by a character (inventory + equipped).
    /// </summary>
    Task<IReadOnlyList<Item>> GetCharacterItemsAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets all items contained within a container item.
    /// </summary>
    Task<IReadOnlyList<Item>> GetContainedItemsAsync(Guid containerItemId, CancellationToken ct = default);

    /// <summary>
    /// Splits a stack of items into two separate stacks.
    /// </summary>
    /// <param name="itemId">The item stack to split</param>
    /// <param name="splitAmount">Number of items to split off</param>
    /// <param name="ct">Cancellation token</param>
    Task<ItemSplitResult> SplitStackAsync(Guid itemId, int splitAmount, CancellationToken ct = default);

    /// <summary>
    /// Merges two stacks of the same item type.
    /// </summary>
    /// <param name="sourceItemId">The item to merge from (will be destroyed if fully merged)</param>
    /// <param name="targetItemId">The item to merge into</param>
    /// <param name="ct">Cancellation token</param>
    Task<ItemMergeResult> MergeStacksAsync(Guid sourceItemId, Guid targetItemId, CancellationToken ct = default);

    /// <summary>
    /// Reduces an item's durability by the specified amount.
    /// </summary>
    Task<DurabilityResult> ReduceDurabilityAsync(Guid itemId, int amount, CancellationToken ct = default);

    /// <summary>
    /// Repairs an item's durability.
    /// </summary>
    Task<DurabilityResult> RepairDurabilityAsync(Guid itemId, int amount, CancellationToken ct = default);
}

public sealed class ItemService : IItemService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ItemService> _logger;

    public ItemService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<ItemService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<Item?> GetItemAsync(Guid itemId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Items
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.WeaponProperties)
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.ArmorProperties)
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.SkillBonuses)
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.AttributeModifiers)
            .Include(i => i.ContainedItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);
    }

    public async Task<ItemSpawnResult> SpawnItemAsync(
        int templateId,
        int? roomId = null,
        Guid? ownerId = null,
        int stackSize = 1,
        CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var template = await context.ItemTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive, ct);

            if (template == null)
            {
                return new ItemSpawnResult(false, "Item template not found or inactive.", null);
            }

            // Validate location - must be in a room OR owned by a character, not both
            if (roomId.HasValue && ownerId.HasValue)
            {
                return new ItemSpawnResult(false, "Item cannot be in a room and owned by a character simultaneously.", null);
            }

            // Validate room exists if specified
            if (roomId.HasValue)
            {
                var roomExists = await context.Rooms.AnyAsync(r => r.Id == roomId.Value, ct);
                if (!roomExists)
                {
                    return new ItemSpawnResult(false, "Specified room does not exist.", null);
                }
            }

            // Validate character exists if specified
            if (ownerId.HasValue)
            {
                var characterExists = await context.Characters.AnyAsync(c => c.Id == ownerId.Value, ct);
                if (!characterExists)
                {
                    return new ItemSpawnResult(false, "Specified character does not exist.", null);
                }
            }

            // Validate stack size
            if (stackSize < 1)
            {
                return new ItemSpawnResult(false, "Stack size must be at least 1.", null);
            }

            if (stackSize > 1 && !template.IsStackable)
            {
                return new ItemSpawnResult(false, $"{template.Name} is not stackable.", null);
            }

            if (stackSize > template.MaxStackSize)
            {
                return new ItemSpawnResult(false, $"Stack size exceeds maximum of {template.MaxStackSize} for {template.Name}.", null);
            }

            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemTemplateId = templateId,
                CurrentRoomId = roomId,
                OwnerCharacterId = ownerId,
                StackSize = stackSize,
                IsEquipped = false,
                EquippedSlot = null,
                ContainerItemId = null,
                IsBound = ownerId.HasValue && template.BindOnPickup,
                CurrentDurability = template.HasDurability ? template.MaxDurability : null,
                CreatedAt = DateTimeOffset.UtcNow,
                PickedUpAt = ownerId.HasValue ? DateTimeOffset.UtcNow : null,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            context.Items.Add(item);
            await context.SaveChangesAsync(ct);

            // Re-fetch with includes for the return value
            item = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstAsync(i => i.Id == item.Id, ct);

            _logger.LogInformation(
                "Spawned item {ItemId} ({ItemName}) x{StackSize} in {Location}",
                item.Id,
                template.Name,
                stackSize,
                roomId.HasValue ? $"room {roomId}" : ownerId.HasValue ? $"character {ownerId}" : "limbo");

            return new ItemSpawnResult(true, $"Spawned {template.Name}.", item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error spawning item from template {TemplateId}", templateId);
            return new ItemSpawnResult(false, "An error occurred while spawning the item.", null);
        }
    }

    public async Task<ItemDestroyResult> DestroyItemAsync(Guid itemId, string reason, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var item = await context.Items
                .Include(i => i.ItemTemplate)
                .Include(i => i.ContainedItems)
                .FirstOrDefaultAsync(i => i.Id == itemId, ct);

            if (item == null)
            {
                return new ItemDestroyResult(false, "Item not found.", null);
            }

            // Check if item contains other items
            if (item.ContainedItems.Count > 0)
            {
                return new ItemDestroyResult(
                    false,
                    $"Cannot destroy {item.ItemTemplate.Name} - it contains {item.ContainedItems.Count} item(s).",
                    item.ItemTemplate.Name);
            }

            var itemName = item.ItemTemplate.Name;
            context.Items.Remove(item);
            await context.SaveChangesAsync(ct);

            _logger.LogInformation("Destroyed item {ItemId} ({ItemName}): {Reason}", itemId, itemName, reason);

            return new ItemDestroyResult(true, $"{itemName} has been destroyed.", itemName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error destroying item {ItemId}", itemId);
            return new ItemDestroyResult(false, "An error occurred while destroying the item.", null);
        }
    }

    public async Task<IReadOnlyList<Item>> GetItemsInRoomAsync(int roomId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Items
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.WeaponProperties)
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.ArmorProperties)
            .Where(i => i.CurrentRoomId == roomId && !i.ContainerItemId.HasValue)
            .OrderBy(i => i.ItemTemplate.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Item>> GetCharacterItemsAsync(Guid characterId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Items
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.WeaponProperties)
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.ArmorProperties)
            .Include(i => i.ContainedItems)
                .ThenInclude(ci => ci.ItemTemplate)
            .Where(i => i.OwnerCharacterId == characterId && !i.ContainerItemId.HasValue)
            .OrderBy(i => i.IsEquipped ? 0 : 1)
            .ThenBy(i => i.ItemTemplate.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Item>> GetContainedItemsAsync(Guid containerItemId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Items
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.WeaponProperties)
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.ArmorProperties)
            .Where(i => i.ContainerItemId == containerItemId)
            .OrderBy(i => i.ItemTemplate.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<ItemSplitResult> SplitStackAsync(Guid itemId, int splitAmount, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var item = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == itemId, ct);

            if (item == null)
            {
                return new ItemSplitResult(false, "Item not found.", null, null);
            }

            if (!item.ItemTemplate.IsStackable)
            {
                return new ItemSplitResult(false, $"{item.ItemTemplate.Name} is not stackable.", null, null);
            }

            if (splitAmount < 1)
            {
                return new ItemSplitResult(false, "Split amount must be at least 1.", null, null);
            }

            if (splitAmount >= item.StackSize)
            {
                return new ItemSplitResult(false, "Split amount must be less than the current stack size.", null, null);
            }

            // Create the new stack
            var newItem = new Item
            {
                Id = Guid.NewGuid(),
                ItemTemplateId = item.ItemTemplateId,
                CurrentRoomId = item.CurrentRoomId,
                OwnerCharacterId = item.OwnerCharacterId,
                ContainerItemId = item.ContainerItemId,
                StackSize = splitAmount,
                IsEquipped = false,
                EquippedSlot = null,
                IsBound = item.IsBound,
                CurrentDurability = item.CurrentDurability,
                CreatedAt = DateTimeOffset.UtcNow,
                PickedUpAt = item.PickedUpAt,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            // Reduce original stack
            item.StackSize -= splitAmount;
            item.LastModifiedAt = DateTimeOffset.UtcNow;

            context.Items.Add(newItem);
            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Split stack {ItemId} ({ItemName}): {SplitAmount} -> new stack {NewItemId}",
                itemId, item.ItemTemplate.Name, splitAmount, newItem.Id);

            return new ItemSplitResult(
                true,
                $"Split {splitAmount} {item.ItemTemplate.Name} into a new stack.",
                item,
                newItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting item stack {ItemId}", itemId);
            return new ItemSplitResult(false, "An error occurred while splitting the stack.", null, null);
        }
    }

    public async Task<ItemMergeResult> MergeStacksAsync(Guid sourceItemId, Guid targetItemId, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var sourceItem = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == sourceItemId, ct);

            var targetItem = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == targetItemId, ct);

            if (sourceItem == null || targetItem == null)
            {
                return new ItemMergeResult(false, "One or both items not found.", null, false);
            }

            if (sourceItem.ItemTemplateId != targetItem.ItemTemplateId)
            {
                return new ItemMergeResult(false, "Cannot merge different item types.", null, false);
            }

            if (!sourceItem.ItemTemplate.IsStackable)
            {
                return new ItemMergeResult(false, $"{sourceItem.ItemTemplate.Name} is not stackable.", null, false);
            }

            var totalSize = sourceItem.StackSize + targetItem.StackSize;
            var maxStackSize = sourceItem.ItemTemplate.MaxStackSize;

            if (totalSize > maxStackSize)
            {
                // Partial merge
                var overflow = totalSize - maxStackSize;
                targetItem.StackSize = maxStackSize;
                sourceItem.StackSize = overflow;
                sourceItem.LastModifiedAt = DateTimeOffset.UtcNow;
                targetItem.LastModifiedAt = DateTimeOffset.UtcNow;

                await context.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Partially merged {SourceId} into {TargetId} ({ItemName}), {Overflow} remaining",
                    sourceItemId, targetItemId, sourceItem.ItemTemplate.Name, overflow);

                return new ItemMergeResult(
                    true,
                    $"Merged {maxStackSize - (totalSize - sourceItem.StackSize)} {sourceItem.ItemTemplate.Name}. {overflow} remaining in original stack.",
                    targetItem,
                    false);
            }

            // Full merge - destroy source
            targetItem.StackSize = totalSize;
            targetItem.LastModifiedAt = DateTimeOffset.UtcNow;
            context.Items.Remove(sourceItem);

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Fully merged {SourceId} into {TargetId} ({ItemName})",
                sourceItemId, targetItemId, targetItem.ItemTemplate.Name);

            return new ItemMergeResult(
                true,
                $"Merged all {sourceItem.ItemTemplate.Name} into one stack of {totalSize}.",
                targetItem,
                true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging stacks {SourceId} -> {TargetId}", sourceItemId, targetItemId);
            return new ItemMergeResult(false, "An error occurred while merging stacks.", null, false);
        }
    }

    public async Task<DurabilityResult> ReduceDurabilityAsync(Guid itemId, int amount, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var item = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == itemId, ct);

            if (item == null)
            {
                return new DurabilityResult(false, "Item not found.", null, false);
            }

            if (!item.ItemTemplate.HasDurability || !item.CurrentDurability.HasValue)
            {
                return new DurabilityResult(false, $"{item.ItemTemplate.Name} does not have durability.", null, false);
            }

            var previousDurability = item.CurrentDurability.Value;
            item.CurrentDurability = Math.Max(0, item.CurrentDurability.Value - amount);
            item.LastModifiedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(ct);

            var isBroken = item.CurrentDurability.Value <= 0;
            var message = isBroken
                ? $"{item.ItemTemplate.Name} has broken!"
                : $"{item.ItemTemplate.Name} durability: {item.CurrentDurability}/{item.ItemTemplate.MaxDurability}";

            _logger.LogInformation(
                "Item {ItemId} ({ItemName}) durability reduced: {Previous} -> {Current}",
                itemId, item.ItemTemplate.Name, previousDurability, item.CurrentDurability.Value);

            return new DurabilityResult(true, message, item, isBroken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reducing durability for item {ItemId}", itemId);
            return new DurabilityResult(false, "An error occurred while updating durability.", null, false);
        }
    }

    public async Task<DurabilityResult> RepairDurabilityAsync(Guid itemId, int amount, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var item = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == itemId, ct);

            if (item == null)
            {
                return new DurabilityResult(false, "Item not found.", null, false);
            }

            if (!item.ItemTemplate.HasDurability || !item.CurrentDurability.HasValue)
            {
                return new DurabilityResult(false, $"{item.ItemTemplate.Name} does not have durability.", null, false);
            }

            var maxDurability = item.ItemTemplate.MaxDurability ?? 100;
            var previousDurability = item.CurrentDurability.Value;
            item.CurrentDurability = Math.Min(maxDurability, item.CurrentDurability.Value + amount);
            item.LastModifiedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Item {ItemId} ({ItemName}) durability repaired: {Previous} -> {Current}",
                itemId, item.ItemTemplate.Name, previousDurability, item.CurrentDurability.Value);

            return new DurabilityResult(
                true,
                $"{item.ItemTemplate.Name} repaired: {item.CurrentDurability}/{maxDurability}",
                item,
                false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error repairing durability for item {ItemId}", itemId);
            return new DurabilityResult(false, "An error occurred while repairing the item.", null, false);
        }
    }
}

// Result records for item operations

public sealed record ItemSpawnResult(bool Success, string Message, Item? Item);

public sealed record ItemDestroyResult(bool Success, string Message, string? DestroyedItemName);

public sealed record ItemSplitResult(bool Success, string Message, Item? OriginalItem, Item? NewItem);

public sealed record ItemMergeResult(bool Success, string Message, Item? MergedItem, bool SourceDestroyed);

public sealed record DurabilityResult(bool Success, string Message, Item? Item, bool IsBroken);
