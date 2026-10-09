namespace froyo_fulfillment_agent.Services;

/// <summary>
/// Source of per-store stock. Implement this to move inventory to a database or external service.
/// </summary>
/// <remarks>
/// Quantities are keyed by menu item name: flavors in ounces, toppings in portions, and waffle cones in units.
/// Cups are not tracked. Missing items are treated as zero.
/// </remarks>
public interface IInventoryRepository
{
    /// <summary>
    /// Gets stock for a store by its canonical ID, or <c>null</c> when the store is unknown.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>?> GetStoreStockAsync(string storeId, CancellationToken cancellationToken = default);
}
