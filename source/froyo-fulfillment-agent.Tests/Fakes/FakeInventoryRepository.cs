using froyo_fulfillment_agent.Models;
using froyo_fulfillment_agent.Services;

namespace froyo_fulfillment_agent.Tests.Fakes;

/// <summary>
/// In-memory repository with stock supplied by each test.
/// </summary>
public sealed class FakeInventoryRepository : IInventoryRepository
{
    private readonly Dictionary<string, StoreStock> stores = new(StringComparer.OrdinalIgnoreCase);

    public FakeInventoryRepository WithStore(
        string storeId,
        Dictionary<string, int>? flavorOunces = null,
        Dictionary<string, int>? toppingPortions = null,
        Dictionary<string, int>? coneUnits = null)
    {
        stores[storeId] = new StoreStock(
            storeId,
            flavorOunces ?? [],
            toppingPortions ?? [],
            coneUnits ?? []);
        return this;
    }

    public Task<StoreStock?> GetStoreStockAsync(string storeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(stores.GetValueOrDefault(storeId));
}
