using froyo_fulfillment_agent.Models;
using froyo_fulfillment_agent.Services;

namespace froyo_fulfillment_agent.Tests.Services;

public class InMemoryInventoryRepositoryTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> Stock =
        InMemoryInventoryRepository.DefaultStock;

    [Fact]
    public void DefaultStock_CoversEveryStore()
    {
        Assert.Equal(FroyoMenu.Stores.Order(), Stock.Keys.Order());
    }

    [Fact]
    public void DefaultStock_EveryStoreHasEveryMenuItem()
    {
        string[] items = [.. FroyoMenu.Flavors, .. FroyoMenu.Toppings, FroyoMenu.WaffleCone];

        Assert.All(Stock.Values, stock => Assert.All(items, item => Assert.True(stock.ContainsKey(item), item)));
    }

    [Fact]
    public void DefaultStock_EveryStoreHasAtLeastOneShortageForDemos()
    {
        Assert.All(Stock.Values, stock => Assert.Contains(stock.Values, quantity => quantity < 16));
    }

    [Fact]
    public async Task GetStoreStockAsync_KnownStore_ReturnsStock()
    {
        var stock = await new InMemoryInventoryRepository().GetStoreStockAsync(StoreIds.ShortNorth);

        Assert.NotNull(stock);
        Assert.Equal(320, stock[FlavorNames.MintCondition]);
    }

    [Fact]
    public async Task GetStoreStockAsync_UnknownStore_ReturnsNull()
    {
        Assert.Null(await new InMemoryInventoryRepository().GetStoreStockAsync("nyc-soho"));
    }

    [Fact]
    public async Task GetStoreStockAsync_SuppliedStock_UsesIt()
    {
        var repository = new InMemoryInventoryRepository(
            new Dictionary<string, IReadOnlyDictionary<string, int>> { [StoreIds.OverTheRhine] = new Dictionary<string, int> { [FlavorNames.JavaJolt] = 5 } });

        var stock = await repository.GetStoreStockAsync(StoreIds.OverTheRhine);

        Assert.Equal(5, stock![FlavorNames.JavaJolt]);
    }
}
