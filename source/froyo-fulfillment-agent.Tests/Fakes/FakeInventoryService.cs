using froyo_fulfillment_agent.Models;
using froyo_fulfillment_agent.Services;

namespace froyo_fulfillment_agent.Tests.Fakes;

/// <summary>
/// Records calls and returns canned results so tool tests can verify delegation.
/// </summary>
public sealed class FakeInventoryService : IInventoryService
{
    public string? LastStoreId { get; private set; }

    public IReadOnlyList<OrderItem>? LastItems { get; private set; }

    public Task<StoreInventory> GetAvailableInventoryAsync(string storeId, CancellationToken cancellationToken = default)
    {
        LastStoreId = storeId;
        return Task.FromResult(new StoreInventory(storeId, true, [], [], []));
    }

    public Task<InventoryCheckResult> CheckInventoryAsync(
        string storeId, IReadOnlyList<OrderItem>? items, CancellationToken cancellationToken = default)
    {
        LastStoreId = storeId;
        LastItems = items;
        return Task.FromResult(new InventoryCheckResult(storeId, true, []));
    }
}
