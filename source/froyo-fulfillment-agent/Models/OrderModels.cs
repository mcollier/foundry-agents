using System.ComponentModel;

namespace froyo_fulfillment_agent.Models;

/// <summary>
/// One line of a customer's order.
/// </summary>
public sealed record OrderItem(
    [property: Description("Flavor name from the menu, for example \"Mint Condition\".")] string Flavor,
    [property: Description("Byte (6 oz cup), Kilobyte (10 oz cup), Megabyte (14 oz cup), or Terabyte (pint).")] CupSize Size,
    [property: Description("Number of servings of this item. Must be at least 1.")] int Quantity,
    [property: Description("Topping names from the menu. Each topping is one portion per serving.")] IReadOnlyList<string>? Toppings = null,
    [property: Description("\"Cup\" or \"Waffle Cone\".")] string Container = FroyoMenu.Cup);

/// <summary>
/// Everything a store has on hand. Cups are unlimited and not listed.
/// </summary>
public sealed record StoreStockReport(
    string StoreId,
    bool StoreFound,
    IReadOnlyList<FlavorStock> Flavors,
    IReadOnlyDictionary<string, int> ToppingPortions,
    int WaffleCones);

public sealed record FlavorStock(string Name, int Ounces, IReadOnlyDictionary<CupSize, int> ServingsBySize);

/// <summary>
/// Whether a store can fulfill an order. Shortages are totals across the whole order.
/// </summary>
public sealed record StockCheckResult(
    string StoreId,
    bool AllInStock,
    IReadOnlyList<Shortage> Shortages,
    IReadOnlyList<string> Errors);

public sealed record Shortage(string Item, int Needed, int Available, string Unit);

/// <summary>
/// Price for an order. <see cref="Subtotal"/> is null when there are errors.
/// </summary>
public sealed record PriceQuote(
    IReadOnlyList<PriceLine> Lines,
    decimal? Subtotal,
    IReadOnlyList<string> Errors);

public sealed record PriceLine(
    string Description,
    int Quantity,
    decimal BasePrice,
    decimal ToppingsPrice,
    decimal ContainerPrice,
    decimal UnitPrice,
    decimal LineTotal);
