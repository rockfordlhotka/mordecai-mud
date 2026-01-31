using Microsoft.EntityFrameworkCore;
using Mordecai.Game.Entities;
using Mordecai.Web.Data;

namespace Mordecai.Web.Services;

/// <summary>
/// Service for managing container items - bags, chests, and storage.
/// </summary>
public interface IContainerService
{
    /// <summary>
    /// Checks if an item is a container.
    /// </summary>
    Task<bool> IsContainerAsync(Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Gets the contents of a container.
    /// </summary>
    Task<IReadOnlyList<Item>> GetContainerContentsAsync(Guid containerId, CancellationToken ct = default);

    /// <summary>
    /// Checks if an item can be stored in a container.
    /// </summary>
    Task<ContainerValidationResult> CanStoreInContainerAsync(
        Guid containerId,
        Guid itemId,
        CancellationToken ct = default);

    /// <summary>
    /// Puts an item into a container.
    /// </summary>
    Task<ContainerOperationResult> PutItemInContainerAsync(
        Guid itemId,
        Guid containerId,
        CancellationToken ct = default);

    /// <summary>
    /// Takes an item out of a container.
    /// </summary>
    Task<ContainerOperationResult> TakeItemFromContainerAsync(Guid itemId, CancellationToken ct = default);

    /// <summary>
    /// Gets container capacity information.
    /// </summary>
    Task<ContainerCapacity?> GetContainerCapacityAsync(Guid containerId, CancellationToken ct = default);

    /// <summary>
    /// Transfers an item from one container to another.
    /// </summary>
    Task<ContainerOperationResult> TransferBetweenContainersAsync(
        Guid itemId,
        Guid sourceContainerId,
        Guid targetContainerId,
        CancellationToken ct = default);

    /// <summary>
    /// Empties all contents from a container into the owner's inventory or a room.
    /// </summary>
    Task<ContainerEmptyResult> EmptyContainerAsync(
        Guid containerId,
        int? targetRoomId = null,
        CancellationToken ct = default);
}

public sealed class ContainerService : IContainerService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ContainerService> _logger;

    // Maximum nesting depth to prevent infinite loops
    private const int MaxNestingDepth = 5;

    public ContainerService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<ContainerService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<bool> IsContainerAsync(Guid itemId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Items
            .Include(i => i.ItemTemplate)
            .Where(i => i.Id == itemId)
            .Select(i => i.ItemTemplate.IsContainer)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Item>> GetContainerContentsAsync(Guid containerId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Items
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.WeaponProperties)
            .Include(i => i.ItemTemplate)
                .ThenInclude(t => t.ArmorProperties)
            .Where(i => i.ContainerItemId == containerId)
            .OrderBy(i => i.ItemTemplate.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<ContainerValidationResult> CanStoreInContainerAsync(
        Guid containerId,
        Guid itemId,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Get the container
        var container = await context.Items
            .Include(i => i.ItemTemplate)
            .Include(i => i.ContainedItems)
                .ThenInclude(ci => ci.ItemTemplate)
            .FirstOrDefaultAsync(i => i.Id == containerId, ct);

        if (container == null)
        {
            return new ContainerValidationResult(false, "Container not found.");
        }

        if (!container.ItemTemplate.IsContainer)
        {
            return new ContainerValidationResult(false, $"{container.ItemTemplate.Name} is not a container.");
        }

        // Get the item to store
        var item = await context.Items
            .Include(i => i.ItemTemplate)
            .Include(i => i.ContainedItems)
                .ThenInclude(ci => ci.ItemTemplate)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item == null)
        {
            return new ContainerValidationResult(false, "Item not found.");
        }

        // Cannot store an item in itself
        if (itemId == containerId)
        {
            return new ContainerValidationResult(false, "Cannot put a container inside itself.");
        }

        // Check for circular references (item contains the container)
        if (await WouldCreateCircularReferenceAsync(context, itemId, containerId, ct))
        {
            return new ContainerValidationResult(false, "Cannot create a circular container reference.");
        }

        // Check nesting depth
        var currentDepth = await GetContainerNestingDepthAsync(context, containerId, ct);
        var itemDepth = item.ItemTemplate.IsContainer
            ? await GetMaxContainedDepthAsync(context, itemId, ct)
            : 0;

        if (currentDepth + itemDepth + 1 > MaxNestingDepth)
        {
            return new ContainerValidationResult(
                false,
                $"Maximum container nesting depth of {MaxNestingDepth} would be exceeded.");
        }

        // Check type restrictions
        if (!string.IsNullOrWhiteSpace(container.ItemTemplate.ContainerAllowedTypes))
        {
            var allowedTypes = container.ItemTemplate.ContainerAllowedTypes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var itemTypeName = item.ItemTemplate.ItemType.ToString();
            if (!allowedTypes.Contains(itemTypeName))
            {
                return new ContainerValidationResult(
                    false,
                    $"{container.ItemTemplate.Name} can only hold: {container.ItemTemplate.ContainerAllowedTypes}");
            }
        }

        // Calculate weight/volume
        var itemWeight = CalculateItemWeight(item);
        var itemVolume = CalculateItemVolume(item);

        var currentContainedWeight = container.ContainedItems.Sum(i => CalculateItemWeight(i));
        var currentContainedVolume = container.ContainedItems.Sum(i => CalculateItemVolume(i));

        var maxWeight = container.ItemTemplate.ContainerMaxWeight ?? decimal.MaxValue;
        var maxVolume = container.ItemTemplate.ContainerMaxVolume ?? decimal.MaxValue;

        if (currentContainedWeight + itemWeight > maxWeight)
        {
            return new ContainerValidationResult(
                false,
                $"{item.ItemTemplate.Name} is too heavy for {container.ItemTemplate.Name}. " +
                $"({currentContainedWeight + itemWeight:F1}/{maxWeight:F1} lbs)");
        }

        if (currentContainedVolume + itemVolume > maxVolume)
        {
            return new ContainerValidationResult(
                false,
                $"{item.ItemTemplate.Name} is too bulky for {container.ItemTemplate.Name}. " +
                $"({currentContainedVolume + itemVolume:F1}/{maxVolume:F1} cu.ft.)");
        }

        return new ContainerValidationResult(true, $"{item.ItemTemplate.Name} can be stored in {container.ItemTemplate.Name}.");
    }

    public async Task<ContainerOperationResult> PutItemInContainerAsync(
        Guid itemId,
        Guid containerId,
        CancellationToken ct = default)
    {
        try
        {
            // Validate first
            var validation = await CanStoreInContainerAsync(containerId, itemId, ct);
            if (!validation.CanStore)
            {
                return new ContainerOperationResult(false, validation.Message, null);
            }

            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var item = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == itemId, ct);

            var container = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == containerId, ct);

            if (item == null || container == null)
            {
                return new ContainerOperationResult(false, "Item or container not found.", null);
            }

            // Verify item is not equipped
            if (item.IsEquipped)
            {
                return new ContainerOperationResult(
                    false,
                    $"Unequip {item.ItemTemplate.Name} before storing it.",
                    null);
            }

            // Verify ownership compatibility
            // Container and item must have the same owner, or item must be in the same room as container
            if (container.OwnerCharacterId.HasValue)
            {
                // Container is owned - item must be owned by the same character
                if (item.OwnerCharacterId != container.OwnerCharacterId)
                {
                    return new ContainerOperationResult(false, "You don't own that item.", null);
                }
            }
            else if (container.CurrentRoomId.HasValue)
            {
                // Container is in a room - item must be in the same room or owned by someone in that room
                if (item.CurrentRoomId != container.CurrentRoomId && !item.OwnerCharacterId.HasValue)
                {
                    return new ContainerOperationResult(false, "The item is not accessible.", null);
                }
            }

            // Perform the storage
            item.ContainerItemId = containerId;
            item.CurrentRoomId = null;
            item.LastModifiedAt = DateTimeOffset.UtcNow;

            // Inherit container's ownership if item was in a room
            if (!item.OwnerCharacterId.HasValue && container.OwnerCharacterId.HasValue)
            {
                item.OwnerCharacterId = container.OwnerCharacterId;
                item.PickedUpAt = DateTimeOffset.UtcNow;

                if (item.ItemTemplate.BindOnPickup)
                {
                    item.IsBound = true;
                }
            }

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Stored item {ItemId} ({ItemName}) in container {ContainerId} ({ContainerName})",
                itemId, item.ItemTemplate.Name, containerId, container.ItemTemplate.Name);

            return new ContainerOperationResult(
                true,
                $"You put {item.ItemTemplate.Name} in {container.ItemTemplate.Name}.",
                item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing item {ItemId} in container {ContainerId}", itemId, containerId);
            return new ContainerOperationResult(false, "An error occurred while storing the item.", null);
        }
    }

    public async Task<ContainerOperationResult> TakeItemFromContainerAsync(Guid itemId, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var item = await context.Items
                .Include(i => i.ItemTemplate)
                .Include(i => i.ContainerItem)
                    .ThenInclude(c => c!.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == itemId, ct);

            if (item == null)
            {
                return new ContainerOperationResult(false, "Item not found.", null);
            }

            if (!item.ContainerItemId.HasValue)
            {
                return new ContainerOperationResult(
                    false,
                    $"{item.ItemTemplate.Name} is not in a container.",
                    null);
            }

            var containerName = item.ContainerItem?.ItemTemplate.Name ?? "container";

            // Remove from container
            item.ContainerItemId = null;
            item.LastModifiedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Removed item {ItemId} ({ItemName}) from container",
                itemId, item.ItemTemplate.Name);

            return new ContainerOperationResult(
                true,
                $"You take {item.ItemTemplate.Name} from {containerName}.",
                item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {ItemId} from container", itemId);
            return new ContainerOperationResult(false, "An error occurred while removing the item.", null);
        }
    }

    public async Task<ContainerCapacity?> GetContainerCapacityAsync(Guid containerId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var container = await context.Items
            .Include(i => i.ItemTemplate)
            .Include(i => i.ContainedItems)
                .ThenInclude(ci => ci.ItemTemplate)
            .FirstOrDefaultAsync(i => i.Id == containerId, ct);

        if (container == null || !container.ItemTemplate.IsContainer)
        {
            return null;
        }

        var currentWeight = container.ContainedItems.Sum(i => CalculateItemWeight(i));
        var currentVolume = container.ContainedItems.Sum(i => CalculateItemVolume(i));

        return new ContainerCapacity(
            ContainerId: containerId,
            ContainerName: container.ItemTemplate.Name,
            CurrentWeight: currentWeight,
            MaxWeight: container.ItemTemplate.ContainerMaxWeight,
            CurrentVolume: currentVolume,
            MaxVolume: container.ItemTemplate.ContainerMaxVolume,
            ItemCount: container.ContainedItems.Count,
            WeightReduction: container.ItemTemplate.ContainerWeightReduction ?? 1.0m,
            VolumeReduction: container.ItemTemplate.ContainerVolumeReduction ?? 1.0m,
            AllowedTypes: container.ItemTemplate.ContainerAllowedTypes);
    }

    public async Task<ContainerOperationResult> TransferBetweenContainersAsync(
        Guid itemId,
        Guid sourceContainerId,
        Guid targetContainerId,
        CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var item = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == itemId, ct);

            if (item == null)
            {
                return new ContainerOperationResult(false, "Item not found.", null);
            }

            if (item.ContainerItemId != sourceContainerId)
            {
                return new ContainerOperationResult(false, "Item is not in the source container.", null);
            }

            // Validate target container can accept the item
            var validation = await CanStoreInContainerAsync(targetContainerId, itemId, ct);
            if (!validation.CanStore)
            {
                return new ContainerOperationResult(false, validation.Message, null);
            }

            var targetContainer = await context.Items
                .Include(i => i.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == targetContainerId, ct);

            if (targetContainer == null)
            {
                return new ContainerOperationResult(false, "Target container not found.", null);
            }

            // Perform the transfer
            item.ContainerItemId = targetContainerId;
            item.LastModifiedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Transferred item {ItemId} ({ItemName}) from container {SourceId} to {TargetId}",
                itemId, item.ItemTemplate.Name, sourceContainerId, targetContainerId);

            return new ContainerOperationResult(
                true,
                $"You move {item.ItemTemplate.Name} to {targetContainer.ItemTemplate.Name}.",
                item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error transferring item {ItemId} from container {SourceId} to {TargetId}",
                itemId, sourceContainerId, targetContainerId);
            return new ContainerOperationResult(false, "An error occurred while transferring the item.", null);
        }
    }

    public async Task<ContainerEmptyResult> EmptyContainerAsync(
        Guid containerId,
        int? targetRoomId = null,
        CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var container = await context.Items
                .Include(i => i.ItemTemplate)
                .Include(i => i.ContainedItems)
                    .ThenInclude(ci => ci.ItemTemplate)
                .FirstOrDefaultAsync(i => i.Id == containerId, ct);

            if (container == null)
            {
                return new ContainerEmptyResult(false, "Container not found.", 0);
            }

            if (!container.ItemTemplate.IsContainer)
            {
                return new ContainerEmptyResult(false, $"{container.ItemTemplate.Name} is not a container.", 0);
            }

            var itemCount = container.ContainedItems.Count;

            if (itemCount == 0)
            {
                return new ContainerEmptyResult(true, $"{container.ItemTemplate.Name} is already empty.", 0);
            }

            // Determine where items should go
            Guid? targetOwnerId = container.OwnerCharacterId;
            int? roomId = targetRoomId ?? container.CurrentRoomId;

            foreach (var item in container.ContainedItems.ToList())
            {
                item.ContainerItemId = null;
                item.LastModifiedAt = DateTimeOffset.UtcNow;

                if (roomId.HasValue && !targetOwnerId.HasValue)
                {
                    // Drop to room
                    item.CurrentRoomId = roomId;
                    item.OwnerCharacterId = null;
                }
                // Otherwise item stays with the owner (or becomes unowned if container was unowned)
            }

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Emptied container {ContainerId} ({ContainerName}), {ItemCount} items",
                containerId, container.ItemTemplate.Name, itemCount);

            var destination = roomId.HasValue && !targetOwnerId.HasValue
                ? "the ground"
                : "your inventory";

            return new ContainerEmptyResult(
                true,
                $"You empty {container.ItemTemplate.Name}. {itemCount} item(s) moved to {destination}.",
                itemCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emptying container {ContainerId}", containerId);
            return new ContainerEmptyResult(false, "An error occurred while emptying the container.", 0);
        }
    }

    // Helper methods

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

    private static async Task<bool> WouldCreateCircularReferenceAsync(
        ApplicationDbContext context,
        Guid itemId,
        Guid containerId,
        CancellationToken ct)
    {
        // Check if the item (which may be a container) contains the target container
        // This prevents putting a bag inside a bag that's inside the first bag

        var item = await context.Items
            .Include(i => i.ContainedItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item == null || !item.ItemTemplate?.IsContainer == true)
        {
            return false;
        }

        return await ContainsItemRecursiveAsync(context, itemId, containerId, 0, ct);
    }

    private static async Task<bool> ContainsItemRecursiveAsync(
        ApplicationDbContext context,
        Guid containerId,
        Guid targetItemId,
        int depth,
        CancellationToken ct)
    {
        if (depth > MaxNestingDepth)
        {
            return false; // Prevent infinite recursion
        }

        var containedItems = await context.Items
            .Where(i => i.ContainerItemId == containerId)
            .Select(i => i.Id)
            .ToListAsync(ct);

        if (containedItems.Contains(targetItemId))
        {
            return true;
        }

        foreach (var childId in containedItems)
        {
            if (await ContainsItemRecursiveAsync(context, childId, targetItemId, depth + 1, ct))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<int> GetContainerNestingDepthAsync(
        ApplicationDbContext context,
        Guid containerId,
        CancellationToken ct)
    {
        var depth = 0;
        var currentId = containerId;

        while (depth < MaxNestingDepth + 1)
        {
            var parentId = await context.Items
                .Where(i => i.Id == currentId)
                .Select(i => i.ContainerItemId)
                .FirstOrDefaultAsync(ct);

            if (!parentId.HasValue)
            {
                break;
            }

            depth++;
            currentId = parentId.Value;
        }

        return depth;
    }

    private static async Task<int> GetMaxContainedDepthAsync(
        ApplicationDbContext context,
        Guid containerId,
        CancellationToken ct)
    {
        var containedItems = await context.Items
            .Include(i => i.ItemTemplate)
            .Where(i => i.ContainerItemId == containerId)
            .AsNoTracking()
            .ToListAsync(ct);

        if (containedItems.Count == 0)
        {
            return 0;
        }

        var maxDepth = 1;
        foreach (var item in containedItems.Where(i => i.ItemTemplate.IsContainer))
        {
            var childDepth = await GetMaxContainedDepthAsync(context, item.Id, ct);
            maxDepth = Math.Max(maxDepth, childDepth + 1);
        }

        return maxDepth;
    }
}

// Result records for container operations

public sealed record ContainerValidationResult(bool CanStore, string Message);

public sealed record ContainerOperationResult(bool Success, string Message, Item? Item);

public sealed record ContainerEmptyResult(bool Success, string Message, int ItemsEmptied);

public sealed record ContainerCapacity(
    Guid ContainerId,
    string ContainerName,
    decimal CurrentWeight,
    decimal? MaxWeight,
    decimal CurrentVolume,
    decimal? MaxVolume,
    int ItemCount,
    decimal WeightReduction,
    decimal VolumeReduction,
    string? AllowedTypes)
{
    public decimal WeightPercent => MaxWeight.HasValue && MaxWeight.Value > 0
        ? CurrentWeight / MaxWeight.Value * 100
        : 0;

    public decimal VolumePercent => MaxVolume.HasValue && MaxVolume.Value > 0
        ? CurrentVolume / MaxVolume.Value * 100
        : 0;

    public bool HasTypeRestrictions => !string.IsNullOrWhiteSpace(AllowedTypes);
}
