using froyo_fulfillment_agent.Models;

namespace froyo_fulfillment_agent.Services;

/// <summary>
/// In-memory <see cref="IInventoryRepository"/>. Uses <see cref="DefaultStock"/> unless other stock is supplied.
/// </summary>
/// <param name="stock">Stock keyed by store ID and then menu item name.</param>
public sealed class InMemoryInventoryRepository(IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>? stock = null)
    : IInventoryRepository
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> _stock = stock ?? DefaultStock;

    public Task<IReadOnlyDictionary<string, int>?> GetStoreStockAsync(string storeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stock.GetValueOrDefault(storeId));

    /// <summary>
    /// Hardcoded demo stock: flavor ounces, topping portions, and waffle cone units.
    /// Each store has a few deliberate shortages so the alternative-recommendation flow can be demoed.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> DefaultStock { get; } =
        new Dictionary<string, IReadOnlyDictionary<string, int>>
        {
            [StoreIds.ShortNorth] = new Dictionary<string, int>
            {
                // Flavors: ounces
                [FlavorNames.MintCondition] = 320,
                [FlavorNames.BerryBlockchainBlast] = 240,
                [FlavorNames.CookieContainer] = 400,
                [FlavorNames.RecursiveRaspberry] = 180,
                [FlavorNames.VanillaException] = 480,
                [FlavorNames.NullPointerPistachio] = 12,
                [FlavorNames.JavaJolt] = 200,
                [FlavorNames.PeanutButterProtocol] = 150,
                [FlavorNames.CloudCaramelCache] = 260,
                [FlavorNames.AicaiBowl] = 90,
                // Toppings: portions (one per serving)
                [ToppingNames.RainbowSprinkles] = 120,
                [ToppingNames.ChocolateChips] = 90,
                [ToppingNames.CrushedCookies] = 75,
                [ToppingNames.GrahamCrackerCrumble] = 40,
                [ToppingNames.AlmondGranola] = 35,
                [ToppingNames.ChoppedPeanuts] = 50,
                [ToppingNames.HotFudge] = 80,
                [ToppingNames.CaramelDrizzle] = 70,
                [ToppingNames.FreshBerries] = 25,
                [ToppingNames.MochiBits] = 0,
                // Waffle cones: units
                [FroyoMenu.WaffleCone] = 60,
            },
            [StoreIds.TheFlats] = new Dictionary<string, int>
            {
                // Flavors: ounces
                [FlavorNames.MintCondition] = 210,
                [FlavorNames.BerryBlockchainBlast] = 0,
                [FlavorNames.CookieContainer] = 350,
                [FlavorNames.RecursiveRaspberry] = 140,
                [FlavorNames.VanillaException] = 300,
                [FlavorNames.NullPointerPistachio] = 160,
                [FlavorNames.JavaJolt] = 8,
                [FlavorNames.PeanutButterProtocol] = 220,
                [FlavorNames.CloudCaramelCache] = 190,
                [FlavorNames.AicaiBowl] = 130,
                // Toppings: portions (one per serving)
                [ToppingNames.RainbowSprinkles] = 100,
                [ToppingNames.ChocolateChips] = 60,
                [ToppingNames.CrushedCookies] = 55,
                [ToppingNames.GrahamCrackerCrumble] = 30,
                [ToppingNames.AlmondGranola] = 45,
                [ToppingNames.ChoppedPeanuts] = 40,
                [ToppingNames.HotFudge] = 65,
                [ToppingNames.CaramelDrizzle] = 50,
                [ToppingNames.FreshBerries] = 20,
                [ToppingNames.MochiBits] = 15,
                // Waffle cones: units
                [FroyoMenu.WaffleCone] = 0,
            },
            [StoreIds.OverTheRhine] = new Dictionary<string, int>
            {
                // Flavors: ounces
                [FlavorNames.MintCondition] = 280,
                [FlavorNames.BerryBlockchainBlast] = 170,
                [FlavorNames.CookieContainer] = 310,
                [FlavorNames.RecursiveRaspberry] = 14,
                [FlavorNames.VanillaException] = 420,
                [FlavorNames.NullPointerPistachio] = 110,
                [FlavorNames.JavaJolt] = 240,
                [FlavorNames.PeanutButterProtocol] = 95,
                [FlavorNames.CloudCaramelCache] = 230,
                [FlavorNames.AicaiBowl] = 150,
                // Toppings: portions (one per serving)
                [ToppingNames.RainbowSprinkles] = 110,
                [ToppingNames.ChocolateChips] = 85,
                [ToppingNames.CrushedCookies] = 70,
                [ToppingNames.GrahamCrackerCrumble] = 35,
                [ToppingNames.AlmondGranola] = 0,
                [ToppingNames.ChoppedPeanuts] = 45,
                [ToppingNames.HotFudge] = 75,
                [ToppingNames.CaramelDrizzle] = 60,
                [ToppingNames.FreshBerries] = 30,
                [ToppingNames.MochiBits] = 25,
                // Waffle cones: units
                [FroyoMenu.WaffleCone] = 45,
            },
        };
}
