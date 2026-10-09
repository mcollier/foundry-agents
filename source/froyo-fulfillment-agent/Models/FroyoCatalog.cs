using System.Diagnostics.CodeAnalysis;

namespace froyo_fulfillment_agent.Models;

/// <summary>
/// Static menu facts from docs/froyo-menu.md: flavors, toppings, containers, sizes, and stores.
/// </summary>
public static class FroyoCatalog
{
    public const string Cup = "Cup";
    public const string WaffleCone = "Waffle Cone";

    public static IReadOnlyList<string> Flavors { get; } =
    [
        "Mint Condition", "Berry Blockchain Blast", "Cookie Container", "Recursive Raspberry",
        "Vanilla Exception", "Null Pointer Pistachio", "Java Jolt", "Peanut Butter Protocol",
        "Cloud Caramel Cache", "Aicai Bowl",
    ];

    public static IReadOnlyList<string> Toppings { get; } =
    [
        "Rainbow Sprinkles", "Chocolate Chips", "Crushed Cookies", "Graham Cracker Crumble",
        "Almond Granola", "Chopped Peanuts", "Hot Fudge", "Caramel Drizzle", "Fresh Berries", "Mochi Bits",
    ];

    public static IReadOnlyList<string> Containers { get; } = [Cup, WaffleCone];

    /// <summary>
    /// Containers whose stock is tracked. Cups are treated as unlimited.
    /// </summary>
    public static IReadOnlyList<string> Cones { get; } = [WaffleCone];

    public static IReadOnlyList<string> Stores { get; } = ["cmh-short-north", "cle-the-flats", "cvg-otr"];

    public static int OuncesFor(CupSize size) => size switch
    {
        CupSize.Byte => 6,
        CupSize.Kilobyte => 10,
        CupSize.Megabyte => 14,
        CupSize.Terabyte => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown cup size."),
    };

    public static bool TryResolveFlavor(string? input, [NotNullWhen(true)] out string? name) =>
        TryResolve(Flavors, input, out name);

    public static bool TryResolveTopping(string? input, [NotNullWhen(true)] out string? name) =>
        TryResolve(Toppings, input, out name);

    public static bool TryResolveContainer(string? input, [NotNullWhen(true)] out string? name) =>
        TryResolve(Containers, input, out name);

    public static bool TryResolveStore(string? input, [NotNullWhen(true)] out string? storeId) =>
        TryResolve(Stores, input, out storeId);

    private static bool TryResolve(IReadOnlyList<string> names, string? input, [NotNullWhen(true)] out string? match)
    {
        var trimmed = input?.Trim();
        match = string.IsNullOrEmpty(trimmed)
            ? null
            : names.FirstOrDefault(n => string.Equals(n, trimmed, StringComparison.OrdinalIgnoreCase));
        return match is not null;
    }
}
