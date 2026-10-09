using System.ComponentModel;
using froyo_fulfillment_agent.Models;
using froyo_fulfillment_agent.Services;
using Microsoft.Extensions.AI;

namespace froyo_fulfillment_agent.Tools;

/// <summary>
/// Exposes <see cref="IInventoryService"/> to the agent as tools.
/// </summary>
public sealed class InventoryTools(IInventoryService inventoryService)
{
    private const string DefaultStoreId = "cmh-short-north";
    private const string StoreIdDescription =
        "Store ID: cmh-short-north (Columbus, default), cle-the-flats (Cleveland), or cvg-otr (Cincinnati).";

    public IReadOnlyList<AIFunction> AsAIFunctions() =>
    [
        AIFunctionFactory.Create(GetAvailableInventory, name: nameof(GetAvailableInventory)),
        AIFunctionFactory.Create(CheckInventory, name: nameof(CheckInventory)),
    ];

    [Description("Gets a store's current stock: ounces and servings per size for every flavor, " +
        "portions for every topping, and waffle cone units. Cups are always available.")]
    public Task<StoreInventory> GetAvailableInventory(
        [Description(StoreIdDescription)] string storeId = DefaultStoreId,
        CancellationToken cancellationToken = default) =>
        inventoryService.GetAvailableInventoryAsync(storeId, cancellationToken);

    [Description("Checks whether a store has enough stock for every flavor, topping, and waffle cone an order needs. " +
        "Returns one line per resource with a status of Sufficient, Insufficient, Unknown, or Invalid, " +
        "and AllSufficient when the whole order can be fulfilled.")]
    public Task<InventoryCheckResult> CheckInventory(
        [Description("The order items to check.")] IReadOnlyList<OrderItem> items,
        [Description(StoreIdDescription)] string storeId = DefaultStoreId,
        CancellationToken cancellationToken = default) =>
        inventoryService.CheckInventoryAsync(storeId, items, cancellationToken);
}
