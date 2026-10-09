using froyo_fulfillment_agent.Models;

namespace froyo_fulfillment_agent.Services;

/// <summary>
/// Source of raw per-store stock. Implement this to move inventory to a database or external service.
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Gets raw stock for a store, or <c>null</c> when the store is unknown.
    /// </summary>
    Task<StoreStock?> GetStoreStockAsync(string storeId, CancellationToken cancellationToken = default);
}
