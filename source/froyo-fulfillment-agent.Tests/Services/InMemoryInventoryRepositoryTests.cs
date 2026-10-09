using froyo_fulfillment_agent.Models;
using froyo_fulfillment_agent.Services;

namespace froyo_fulfillment_agent.Tests.Services;

public class InMemoryInventoryRepositoryTests
{
    public static TheoryData<string> StoreIds => new(FroyoCatalog.Stores);

    [Theory]
    [MemberData(nameof(StoreIds))]
    public async Task GetStoreStockAsync_EachStore_HasEveryCatalogItem(string storeId)
    {
        var stock = await new InMemoryInventoryRepository().GetStoreStockAsync(storeId);

        Assert.NotNull(stock);
        Assert.Equal(storeId, stock.StoreId);
        Assert.Equal(FroyoCatalog.Flavors.Order(), stock.FlavorOunces.Keys.Order());
        Assert.Equal(FroyoCatalog.Toppings.Order(), stock.ToppingPortions.Keys.Order());
        Assert.Equal(FroyoCatalog.Cones.Order(), stock.ConeUnits.Keys.Order());
    }

    [Theory]
    [MemberData(nameof(StoreIds))]
    public async Task GetStoreStockAsync_EachStore_HasNoNegativeQuantities(string storeId)
    {
        var stock = await new InMemoryInventoryRepository().GetStoreStockAsync(storeId);

        Assert.NotNull(stock);
        Assert.All(stock.FlavorOunces.Values.Concat(stock.ToppingPortions.Values).Concat(stock.ConeUnits.Values),
            quantity => Assert.True(quantity >= 0));
    }

    [Theory]
    [MemberData(nameof(StoreIds))]
    public async Task GetStoreStockAsync_EachStore_HasAtLeastOneShortageForDemos(string storeId)
    {
        var stock = await new InMemoryInventoryRepository().GetStoreStockAsync(storeId);

        Assert.NotNull(stock);
        var hasShortage =
            stock.FlavorOunces.Values.Any(ounces => ounces < FroyoCatalog.OuncesFor(CupSize.Terabyte))
            || stock.ToppingPortions.Values.Any(portions => portions == 0)
            || stock.ConeUnits.Values.Any(units => units == 0);
        Assert.True(hasShortage);
    }

    [Fact]
    public async Task GetStoreStockAsync_UnknownStore_ReturnsNull()
    {
        Assert.Null(await new InMemoryInventoryRepository().GetStoreStockAsync("nyc-soho"));
    }

    [Fact]
    public async Task GetStoreStockAsync_StoreIdDifferentCase_ReturnsStock()
    {
        Assert.NotNull(await new InMemoryInventoryRepository().GetStoreStockAsync("CLE-THE-FLATS"));
    }
}
