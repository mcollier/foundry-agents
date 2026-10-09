using froyo_fulfillment_agent.Models;

namespace froyo_fulfillment_agent.Services;

/// <summary>
/// Applies inventory rules on top of raw stock from an <see cref="IInventoryRepository"/>.
/// </summary>
public sealed class InventoryService(IInventoryRepository repository) : IInventoryService
{
    public async Task<StoreInventory> GetAvailableInventoryAsync(
        string storeId, CancellationToken cancellationToken = default)
    {
        var (canonicalStoreId, stock) = await GetStockAsync(storeId, cancellationToken);
        if (stock is null)
        {
            return StoreInventory.NotFound(canonicalStoreId);
        }

        var flavors = FroyoCatalog.Flavors
            .Select(name => ToFlavorAvailability(name, stock.FlavorOunces.GetValueOrDefault(name)))
            .ToList();
        var toppings = FroyoCatalog.Toppings
            .Select(name => new StockLevel(name, stock.ToppingPortions.GetValueOrDefault(name), "portions"))
            .ToList();
        var cones = FroyoCatalog.Cones
            .Select(name => new StockLevel(name, stock.ConeUnits.GetValueOrDefault(name), "units"))
            .ToList();

        return new StoreInventory(canonicalStoreId, true, flavors, toppings, cones);
    }

    public async Task<InventoryCheckResult> CheckInventoryAsync(
        string storeId, IReadOnlyList<OrderItem>? items, CancellationToken cancellationToken = default)
    {
        var (canonicalStoreId, stock) = await GetStockAsync(storeId, cancellationToken);
        if (stock is null)
        {
            return InventoryCheckResult.NotFound(canonicalStoreId);
        }

        // Insertion-ordered so lines within each item type follow the order they first appear in.
        var demand = new OrderedDictionary<DemandKey, int>();
        foreach (var item in items ?? [])
        {
            AddItemDemand(demand, item);
        }

        var lines = demand
            .OrderBy(pair => pair.Key.Type)
            .Select(pair => ToLine(pair.Key, pair.Value, stock))
            .ToList();

        return new InventoryCheckResult(canonicalStoreId, true, lines);
    }

    private async Task<(string StoreId, StoreStock? Stock)> GetStockAsync(string storeId, CancellationToken cancellationToken)
    {
        if (!FroyoCatalog.TryResolveStore(storeId, out var canonicalStoreId))
        {
            return (storeId, null);
        }

        return (canonicalStoreId, await repository.GetStoreStockAsync(canonicalStoreId, cancellationToken));
    }

    private static void AddItemDemand(OrderedDictionary<DemandKey, int> demand, OrderItem item)
    {
        var flavorKnown = FroyoCatalog.TryResolveFlavor(item.Flavor, out var flavor);
        var flavorName = flavor ?? item.Flavor?.Trim() ?? string.Empty;

        if (item.Quantity < 1 || !Enum.IsDefined(item.Size))
        {
            Add(demand, new DemandKey(ItemType.Flavor, flavorName, StockStatus.Invalid), 0);
            return;
        }

        Add(demand, Key(ItemType.Flavor, flavorName, flavorKnown), FroyoCatalog.OuncesFor(item.Size) * item.Quantity);

        foreach (var topping in item.Toppings ?? [])
        {
            var toppingKnown = FroyoCatalog.TryResolveTopping(topping, out var toppingName);
            Add(demand, Key(ItemType.Topping, toppingName ?? topping?.Trim() ?? string.Empty, toppingKnown), item.Quantity);
        }

        var containerKnown = FroyoCatalog.TryResolveContainer(item.Container, out var container);
        if (container != FroyoCatalog.Cup)
        {
            Add(demand, Key(ItemType.Cone, container ?? item.Container?.Trim() ?? string.Empty, containerKnown), item.Quantity);
        }
    }

    private static DemandKey Key(ItemType type, string name, bool known) =>
        new(type, name, known ? null : StockStatus.Unknown);

    private static void Add(OrderedDictionary<DemandKey, int> demand, DemandKey key, int amount) =>
        demand[key] = demand.GetValueOrDefault(key) + amount;

    private static InventoryCheckLine ToLine(DemandKey key, int required, StoreStock stock)
    {
        var (available, unit) = key.Type switch
        {
            ItemType.Flavor => (stock.FlavorOunces.GetValueOrDefault(key.Name), "oz"),
            ItemType.Topping => (stock.ToppingPortions.GetValueOrDefault(key.Name), "portions"),
            _ => (stock.ConeUnits.GetValueOrDefault(key.Name), "units"),
        };
        if (key.Status == StockStatus.Unknown)
        {
            available = 0;
        }

        var status = key.Status ?? (available >= required ? StockStatus.Sufficient : StockStatus.Insufficient);
        return new InventoryCheckLine(key.Type, key.Name, required, available, unit, status);
    }

    /// <summary>
    /// Groups demand by resource. <see cref="Status"/> is preset for Unknown and Invalid lines.
    /// </summary>
    private readonly record struct DemandKey(ItemType Type, string Name, StockStatus? Status);

    private static FlavorAvailability ToFlavorAvailability(string name, int ounces)
    {
        // Integer division floors: partial servings are not sellable.
        var servingsBySize = Enum.GetValues<CupSize>()
            .ToDictionary(size => size, size => ounces / FroyoCatalog.OuncesFor(size));
        return new FlavorAvailability(name, ounces, servingsBySize);
    }
}
