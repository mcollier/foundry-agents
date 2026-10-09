using System.ComponentModel;
using froyo_fulfillment_agent.Models;
using froyo_fulfillment_agent.Services;
using Microsoft.Extensions.AI;

namespace froyo_fulfillment_agent.Tools;

/// <summary>
/// Agent tools for stock and pricing. Menu descriptions and allergens come from the knowledge base, not from here.
/// </summary>
public sealed class FroyoTools(IInventoryRepository inventory)
{
    private const string DefaultStoreId = StoreIds.ShortNorth;
    private const string StoreIdDescription =
        "Store ID: cmh-short-north (Columbus, default), cle-the-flats (Cleveland), or cvg-otr (Cincinnati).";

    public IReadOnlyList<AIFunction> AsAIFunctions() =>
    [
        AIFunctionFactory.Create(GetStoreStock, name: nameof(GetStoreStock)),
        AIFunctionFactory.Create(CheckStock, name: nameof(CheckStock)),
        AIFunctionFactory.Create(PriceOrder, name: nameof(PriceOrder)),
    ];

    [Description("Lists everything a store has on hand: servings per size for each flavor, topping portions, " +
        "and waffle cones. Cups are always available. Use it to find in-stock alternatives.")]
    public async Task<StoreStockReport> GetStoreStock(
        [Description(StoreIdDescription)] string storeId = DefaultStoreId,
        CancellationToken cancellationToken = default)
    {
        var (id, stock) = await GetStockAsync(storeId, cancellationToken);
        if (stock is null)
        {
            return new StoreStockReport(id, false, [], new Dictionary<string, int>(), 0);
        }

        var flavors = FroyoMenu.Flavors
            .Select(name => ToFlavorStock(name, stock.GetValueOrDefault(name)))
            .ToList();
        var toppings = FroyoMenu.Toppings.ToDictionary(name => name, name => stock.GetValueOrDefault(name));

        return new StoreStockReport(id, true, flavors, toppings, stock.GetValueOrDefault(FroyoMenu.WaffleCone));
    }

    [Description("Checks whether a store has enough stock for an order. Totals what the whole order needs " +
        "(flavor ounces, topping portions, waffle cones) and returns any shortages and invalid items.")]
    public async Task<StockCheckResult> CheckStock(
        [Description("The order items to check.")] IReadOnlyList<OrderItem> items,
        [Description(StoreIdDescription)] string storeId = DefaultStoreId,
        CancellationToken cancellationToken = default)
    {
        var (id, stock) = await GetStockAsync(storeId, cancellationToken);
        if (stock is null)
        {
            return new StockCheckResult(id, false, [], [$"'{storeId}' is not a Froyo Foundry store."]);
        }

        var (resolved, errors) = FroyoMenu.Resolve(items);

        // Ordered so shortages are reported in the order items first appear.
        var needed = new OrderedDictionary<string, (int Amount, string Unit)>();
        foreach (var item in resolved)
        {
            Add(needed, item.Flavor, FroyoMenu.OuncesFor(item.Size) * item.Quantity, "oz");
            foreach (var topping in item.Toppings ?? [])
            {
                Add(needed, topping, item.Quantity, "portions");
            }
            if (item.Container == FroyoMenu.WaffleCone)
            {
                Add(needed, FroyoMenu.WaffleCone, item.Quantity, "cones");
            }
        }

        var shortages = needed
            .Select(n => new Shortage(n.Key, n.Value.Amount, stock.GetValueOrDefault(n.Key), n.Value.Unit))
            .Where(s => s.Available < s.Needed)
            .ToList();

        return new StockCheckResult(id, errors.Count == 0 && shortages.Count == 0, shortages, errors);
    }

    [Description("Prices an order using menu prices. Returns the price of each line and the subtotal before tax, " +
        "including topping and waffle cone charges.")]
    public PriceQuote PriceOrder([Description("The order items to price.")] IReadOnlyList<OrderItem> items)
    {
        var (resolved, errors) = FroyoMenu.Resolve(items);

        var lines = resolved.Select(ToPriceLine).ToList();
        decimal? subtotal = errors.Count == 0 ? lines.Sum(l => l.LineTotal) : null;

        return new PriceQuote(lines, subtotal, errors);
    }

    private async Task<(string StoreId, IReadOnlyDictionary<string, int>? Stock)> GetStockAsync(
        string? storeId, CancellationToken cancellationToken)
    {
        if (!FroyoMenu.TryResolveStore(storeId, out var canonical))
        {
            return (storeId ?? string.Empty, null);
        }

        return (canonical, await inventory.GetStoreStockAsync(canonical, cancellationToken));
    }

    private static void Add(OrderedDictionary<string, (int Amount, string Unit)> needed, string name, int amount, string unit) =>
        needed[name] = (needed.GetValueOrDefault(name).Amount + amount, unit);

    // Integer division floors: partial servings are not sellable.
    private static FlavorStock ToFlavorStock(string name, int ounces) =>
        new(name, ounces, Enum.GetValues<CupSize>().ToDictionary(size => size, size => ounces / FroyoMenu.OuncesFor(size)));

    private static PriceLine ToPriceLine(OrderItem item)
    {
        var toppings = item.Toppings ?? [];
        var basePrice = FroyoMenu.PriceFor(item.Size);
        var toppingsPrice = FroyoMenu.ToppingsPriceFor(toppings.Count);
        var containerPrice = item.Container == FroyoMenu.WaffleCone ? FroyoMenu.WaffleConePrice : 0m;
        var unitPrice = basePrice + toppingsPrice + containerPrice;

        var description = $"{item.Size} {item.Flavor} in a {item.Container.ToLowerInvariant()}" +
            (toppings.Count > 0 ? $" with {string.Join(", ", toppings)}" : string.Empty);

        return new PriceLine(description, item.Quantity, basePrice, toppingsPrice, containerPrice, unitPrice, unitPrice * item.Quantity);
    }
}
