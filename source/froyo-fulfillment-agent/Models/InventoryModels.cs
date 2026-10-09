using System.Text.Json.Serialization;

namespace froyo_fulfillment_agent.Models;

/// <summary>
/// Raw stock for one store, as provided by an inventory repository.
/// Keys are canonical catalog names.
/// </summary>
public sealed record StoreStock(
    string StoreId,
    IReadOnlyDictionary<string, int> FlavorOunces,
    IReadOnlyDictionary<string, int> ToppingPortions,
    IReadOnlyDictionary<string, int> ConeUnits);

/// <summary>
/// Available inventory for a store, including servings derived per size.
/// </summary>
public sealed record StoreInventory(
    string StoreId,
    bool StoreFound,
    IReadOnlyList<FlavorAvailability> Flavors,
    IReadOnlyList<StockLevel> Toppings,
    IReadOnlyList<StockLevel> Cones)
{
    public static StoreInventory NotFound(string storeId) => new(storeId, false, [], [], []);
}

public sealed record FlavorAvailability(
    string Name,
    int OuncesOnHand,
    IReadOnlyDictionary<CupSize, int> ServingsBySize);

public sealed record StockLevel(string Name, int Quantity, string Unit);

/// <summary>
/// One line of an order to check against inventory.
/// </summary>
public sealed record OrderItem(
    string Flavor,
    CupSize Size,
    int Quantity,
    IReadOnlyList<string>? Toppings = null,
    string Container = FroyoCatalog.Cup);

[JsonConverter(typeof(JsonStringEnumConverter<ItemType>))]
public enum ItemType
{
    Flavor,
    Topping,
    Cone,
}

[JsonConverter(typeof(JsonStringEnumConverter<StockStatus>))]
public enum StockStatus
{
    Sufficient,
    Insufficient,
    Unknown,
    Invalid,
}

/// <summary>
/// Total demand for one stock resource across an order, compared with what the store has.
/// </summary>
public sealed record InventoryCheckLine(
    ItemType ItemType,
    string Name,
    int Required,
    int Available,
    string Unit,
    StockStatus Status);

public sealed record InventoryCheckResult(string StoreId, bool StoreFound, IReadOnlyList<InventoryCheckLine> Lines)
{
    public bool AllSufficient =>
        StoreFound && Lines.Count > 0 && Lines.All(line => line.Status == StockStatus.Sufficient);

    public static InventoryCheckResult NotFound(string storeId) => new(storeId, false, []);
}
