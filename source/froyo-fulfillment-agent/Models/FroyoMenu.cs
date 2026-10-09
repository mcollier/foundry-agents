using System.Diagnostics.CodeAnalysis;

namespace froyo_fulfillment_agent.Models;

/// <summary>
/// Menu facts the tools need for stock and price math. Must stay in sync with docs/froyo-menu.md,
/// which is the knowledge base the agent reads for descriptions and allergens.
/// </summary>
public static class FroyoMenu
{
    public const string Cup = "Cup";
    public const string WaffleCone = "Waffle Cone";
    public const int MaxItemsPerOrder = 20;

    public const decimal ToppingPrice = 0.95m;
    public const int ToppingBundleSize = 3;
    public const decimal ToppingBundlePrice = 2.50m;
    public const decimal WaffleConePrice = 1.25m;

    public static IReadOnlyList<string> Flavors { get; } =
    [
        FlavorNames.MintCondition,
        FlavorNames.BerryBlockchainBlast,
        FlavorNames.CookieContainer,
        FlavorNames.RecursiveRaspberry,
        FlavorNames.VanillaException,
        FlavorNames.NullPointerPistachio,
        FlavorNames.JavaJolt,
        FlavorNames.PeanutButterProtocol,
        FlavorNames.CloudCaramelCache,
        FlavorNames.AicaiBowl,
    ];

    public static IReadOnlyList<string> Toppings { get; } =
    [
        ToppingNames.RainbowSprinkles,
        ToppingNames.ChocolateChips,
        ToppingNames.CrushedCookies,
        ToppingNames.GrahamCrackerCrumble,
        ToppingNames.AlmondGranola,
        ToppingNames.ChoppedPeanuts,
        ToppingNames.HotFudge,
        ToppingNames.CaramelDrizzle,
        ToppingNames.FreshBerries,
        ToppingNames.MochiBits,
    ];

    public static IReadOnlyList<string> Containers { get; } = [Cup, WaffleCone];

    public static IReadOnlyList<string> Stores { get; } = [StoreIds.ShortNorth, StoreIds.TheFlats, StoreIds.OverTheRhine];

    public static int OuncesFor(CupSize size) => size switch
    {
        CupSize.Byte => 6,
        CupSize.Kilobyte => 10,
        CupSize.Megabyte => 14,
        CupSize.Terabyte => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown cup size."),
    };

    public static decimal PriceFor(CupSize size) => size switch
    {
        CupSize.Byte => 4.50m,
        CupSize.Kilobyte => 6.25m,
        CupSize.Megabyte => 7.75m,
        CupSize.Terabyte => 9.50m,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown cup size."),
    };

    /// <summary>
    /// Price of the toppings on one serving: each full group of 3 costs the bundle price, the rest cost the single price.
    /// </summary>
    public static decimal ToppingsPriceFor(int toppingCount) =>
        toppingCount / ToppingBundleSize * ToppingBundlePrice + toppingCount % ToppingBundleSize * ToppingPrice;

    public static bool TryResolveFlavor(string? input, [NotNullWhen(true)] out string? name) =>
        TryResolve(Flavors, input, out name);

    public static bool TryResolveTopping(string? input, [NotNullWhen(true)] out string? name) =>
        TryResolve(Toppings, input, out name);

    public static bool TryResolveContainer(string? input, [NotNullWhen(true)] out string? name) =>
        TryResolve(Containers, input, out name);

    public static bool TryResolveStore(string? input, [NotNullWhen(true)] out string? storeId) =>
        TryResolve(Stores, input, out storeId);

    /// <summary>
    /// Validates order items against the menu. Returns the items with canonical names, plus a plain-language
    /// error for each problem. Invalid items are left out of the result.
    /// </summary>
    public static (IReadOnlyList<OrderItem> Items, IReadOnlyList<string> Errors) Resolve(IReadOnlyList<OrderItem>? items)
    {
        var resolved = new List<OrderItem>();
        var errors = new List<string>();

        if (items is null || items.Count == 0)
        {
            errors.Add("The order has no items.");
            return (resolved, errors);
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            if (item is null)
            {
                errors.Add($"Item {index + 1} is empty.");
                continue;
            }

            var itemErrors = new List<string>();
            if (!TryResolveFlavor(item.Flavor, out var flavor))
            {
                itemErrors.Add($"'{item.Flavor}' is not a flavor on the menu.");
            }
            if (!Enum.IsDefined(item.Size))
            {
                itemErrors.Add($"'{item.Size}' is not a size on the menu.");
            }
            if (item.Quantity < 1)
            {
                itemErrors.Add($"Quantity for {flavor ?? item.Flavor} must be at least 1.");
            }
            if (!TryResolveContainer(item.Container, out var container))
            {
                itemErrors.Add($"'{item.Container}' is not a container on the menu. Choose Cup or Waffle Cone.");
            }

            var toppings = new List<string>();
            foreach (var topping in item.Toppings ?? [])
            {
                if (TryResolveTopping(topping, out var toppingName))
                {
                    toppings.Add(toppingName);
                }
                else
                {
                    itemErrors.Add($"'{topping}' is not a topping on the menu.");
                }
            }

            // An item over the order limit is reported by the total check below. Leaving it out keeps
            // stock and price math within int and decimal range.
            if (itemErrors.Count == 0 && item.Quantity <= MaxItemsPerOrder)
            {
                resolved.Add(new OrderItem(flavor!, item.Size, item.Quantity, toppings, container!));
            }
            errors.AddRange(itemErrors);
        }

        if (items.Sum(i => (long)Math.Max(i?.Quantity ?? 0, 0)) > MaxItemsPerOrder)
        {
            errors.Add($"Orders are limited to {MaxItemsPerOrder} items. Larger orders go through catering.");
        }

        return (resolved, errors);
    }

    private static bool TryResolve(IReadOnlyList<string> names, string? input, [NotNullWhen(true)] out string? match)
    {
        var trimmed = input?.Trim();
        match = string.IsNullOrEmpty(trimmed)
            ? null
            : names.FirstOrDefault(n => string.Equals(n, trimmed, StringComparison.OrdinalIgnoreCase));
        return match is not null;
    }
}
