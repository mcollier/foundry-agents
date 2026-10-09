using froyo_fulfillment_agent.Models;

namespace froyo_fulfillment_agent.Services;

/// <summary>
/// Reports per-store froyo inventory.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Gets every catalog flavor, topping, and cone with its stock for a store.
    /// Unknown stores return <see cref="StoreInventory.StoreFound"/> = false.
    /// </summary>
    Task<StoreInventory> GetAvailableInventoryAsync(string storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a store has enough stock for every flavor, topping, and cone the items need.
    /// Never throws for unknown stores, unknown names, or invalid quantities; these are reported as line statuses.
    /// </summary>
    Task<InventoryCheckResult> CheckInventoryAsync(
        string storeId, IReadOnlyList<OrderItem>? items, CancellationToken cancellationToken = default);
}
