using froyo_fulfillment_agent.Models;

namespace froyo_fulfillment_agent.Services;

/// <summary>
/// Hardcoded stock for each Froyo Foundry store. Replace with a database-backed
/// <see cref="IInventoryRepository"/> when one is available.
/// </summary>
/// <remarks>
/// Each store has a few deliberate shortages so the agent's alternative-finding flow can be demoed.
/// </remarks>
public sealed class InMemoryInventoryRepository : IInventoryRepository
{
    private static readonly Dictionary<string, StoreStock> Stores = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cmh-short-north"] = new StoreStock(
            "cmh-short-north",
            FlavorOunces: new Dictionary<string, int>
            {
                ["Mint Condition"] = 320,
                ["Berry Blockchain Blast"] = 240,
                ["Cookie Container"] = 400,
                ["Recursive Raspberry"] = 180,
                ["Vanilla Exception"] = 480,
                ["Null Pointer Pistachio"] = 12,
                ["Java Jolt"] = 200,
                ["Peanut Butter Protocol"] = 150,
                ["Cloud Caramel Cache"] = 260,
                ["Aicai Bowl"] = 90,
            },
            ToppingPortions: new Dictionary<string, int>
            {
                ["Rainbow Sprinkles"] = 120,
                ["Chocolate Chips"] = 90,
                ["Crushed Cookies"] = 75,
                ["Graham Cracker Crumble"] = 40,
                ["Almond Granola"] = 35,
                ["Chopped Peanuts"] = 50,
                ["Hot Fudge"] = 80,
                ["Caramel Drizzle"] = 70,
                ["Fresh Berries"] = 25,
                ["Mochi Bits"] = 0,
            },
            ConeUnits: new Dictionary<string, int> { ["Waffle Cone"] = 60 }),

        ["cle-the-flats"] = new StoreStock(
            "cle-the-flats",
            FlavorOunces: new Dictionary<string, int>
            {
                ["Mint Condition"] = 210,
                ["Berry Blockchain Blast"] = 0,
                ["Cookie Container"] = 350,
                ["Recursive Raspberry"] = 140,
                ["Vanilla Exception"] = 300,
                ["Null Pointer Pistachio"] = 160,
                ["Java Jolt"] = 8,
                ["Peanut Butter Protocol"] = 220,
                ["Cloud Caramel Cache"] = 190,
                ["Aicai Bowl"] = 130,
            },
            ToppingPortions: new Dictionary<string, int>
            {
                ["Rainbow Sprinkles"] = 100,
                ["Chocolate Chips"] = 60,
                ["Crushed Cookies"] = 55,
                ["Graham Cracker Crumble"] = 30,
                ["Almond Granola"] = 45,
                ["Chopped Peanuts"] = 40,
                ["Hot Fudge"] = 65,
                ["Caramel Drizzle"] = 50,
                ["Fresh Berries"] = 20,
                ["Mochi Bits"] = 15,
            },
            ConeUnits: new Dictionary<string, int> { ["Waffle Cone"] = 0 }),

        ["cvg-otr"] = new StoreStock(
            "cvg-otr",
            FlavorOunces: new Dictionary<string, int>
            {
                ["Mint Condition"] = 280,
                ["Berry Blockchain Blast"] = 170,
                ["Cookie Container"] = 310,
                ["Recursive Raspberry"] = 14,
                ["Vanilla Exception"] = 420,
                ["Null Pointer Pistachio"] = 110,
                ["Java Jolt"] = 240,
                ["Peanut Butter Protocol"] = 95,
                ["Cloud Caramel Cache"] = 230,
                ["Aicai Bowl"] = 150,
            },
            ToppingPortions: new Dictionary<string, int>
            {
                ["Rainbow Sprinkles"] = 110,
                ["Chocolate Chips"] = 85,
                ["Crushed Cookies"] = 70,
                ["Graham Cracker Crumble"] = 35,
                ["Almond Granola"] = 0,
                ["Chopped Peanuts"] = 45,
                ["Hot Fudge"] = 75,
                ["Caramel Drizzle"] = 60,
                ["Fresh Berries"] = 30,
                ["Mochi Bits"] = 25,
            },
            ConeUnits: new Dictionary<string, int> { ["Waffle Cone"] = 45 }),
    };

    public Task<StoreStock?> GetStoreStockAsync(string storeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Stores.GetValueOrDefault(storeId));
}
