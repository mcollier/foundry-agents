using System.Text.Json;
using froyo_fulfillment_agent.Models;
using froyo_fulfillment_agent.Services;
using froyo_fulfillment_agent.Tools;
using Microsoft.Extensions.AI;

namespace froyo_fulfillment_agent.Tests.Tools;

public class FroyoToolsTests
{
    private const string Store = StoreIds.ShortNorth;

    // Small explicit stock so tests don't depend on the hardcoded demo data.
    private static FroyoTools Tools(Dictionary<string, int> stock) =>
        new(new InMemoryInventoryRepository(new Dictionary<string, IReadOnlyDictionary<string, int>> { [Store] = stock }));

    private static FroyoTools DefaultTools() => new(new InMemoryInventoryRepository());

    [Fact]
    public void AsAIFunctions_ReturnsThreeToolsWithDescriptions()
    {
        var functions = DefaultTools().AsAIFunctions();

        Assert.Equal(["GetStoreStock", "CheckStock", "PriceOrder"], functions.Select(f => f.Name));
        Assert.All(functions, f => Assert.False(string.IsNullOrWhiteSpace(f.Description)));
    }

    [Fact]
    public void CheckStock_JsonSchema_ExposesSizesAsNames()
    {
        var schema = DefaultTools().AsAIFunctions().Single(f => f.Name == "CheckStock").JsonSchema.GetRawText();

        Assert.Contains("\"Byte\"", schema);
        Assert.Contains("\"Terabyte\"", schema);
    }

    [Fact]
    public async Task GetStoreStock_KnownStore_ReturnsServingsBySizeAndMissingItemsAsZero()
    {
        var report = await Tools(new() { [FlavorNames.MintCondition] = 100, [ToppingNames.HotFudge] = 7, [FroyoMenu.WaffleCone] = 3 }).GetStoreStock(" CMH-Short-North ");

        Assert.True(report.StoreFound);
        Assert.Equal(Store, report.StoreId);
        var mint = report.Flavors.Single(f => f.Name == FlavorNames.MintCondition);
        Assert.Equal(16, mint.ServingsBySize[CupSize.Byte]);
        Assert.Equal(10, mint.ServingsBySize[CupSize.Kilobyte]);
        Assert.Equal(7, mint.ServingsBySize[CupSize.Megabyte]);
        Assert.Equal(6, mint.ServingsBySize[CupSize.Terabyte]);
        Assert.Equal(FroyoMenu.Flavors.Count, report.Flavors.Count);
        Assert.Equal(0, report.Flavors.Single(f => f.Name == FlavorNames.JavaJolt).Ounces);
        Assert.Equal(7, report.ToppingPortions[ToppingNames.HotFudge]);
        Assert.Equal(0, report.ToppingPortions[ToppingNames.MochiBits]);
        Assert.Equal(3, report.WaffleCones);
    }

    [Fact]
    public async Task GetStoreStock_UnknownStore_ReturnsNotFound()
    {
        var report = await DefaultTools().GetStoreStock("nyc-soho");

        Assert.False(report.StoreFound);
        Assert.Empty(report.Flavors);
    }

    [Fact]
    public async Task GetStoreStock_WithoutStoreId_DefaultsToShortNorth()
    {
        var function = DefaultTools().AsAIFunctions().Single(f => f.Name == "GetStoreStock");

        var result = (JsonElement)(await function.InvokeAsync(new()))!;

        Assert.Equal(Store, result.GetProperty("storeId").GetString());
    }

    [Fact]
    public async Task CheckStock_EnoughStock_IsAllInStock()
    {
        var result = await Tools(new() { [FlavorNames.JavaJolt] = 28, [ToppingNames.HotFudge] = 2, [FroyoMenu.WaffleCone] = 2 })
            .CheckStock([new OrderItem(FlavorNames.JavaJolt, CupSize.Megabyte, 2, [ToppingNames.HotFudge], FroyoMenu.WaffleCone)], Store);

        Assert.True(result.AllInStock);
        Assert.Empty(result.Shortages);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CheckStock_SumsDemandAcrossItems_ReportsShortages()
    {
        var result = await Tools(new() { [FlavorNames.JavaJolt] = 20, [ToppingNames.HotFudge] = 1, [FroyoMenu.WaffleCone] = 5 }).CheckStock(
            [
                new OrderItem(FlavorNames.JavaJolt, CupSize.Kilobyte, 1, [ToppingNames.HotFudge]),
                new OrderItem("java jolt", CupSize.Byte, 2, [ToppingNames.HotFudge], FroyoMenu.WaffleCone),
            ],
            Store);

        Assert.False(result.AllInStock);
        Assert.Equal(
            [new Shortage(FlavorNames.JavaJolt, 22, 20, "oz"), new Shortage(ToppingNames.HotFudge, 3, 1, "portions")],
            result.Shortages);
    }

    [Fact]
    public async Task CheckStock_NotEnoughWaffleCones_ReportsConeShortage()
    {
        var result = await Tools(new() { [FlavorNames.JavaJolt] = 100, [FroyoMenu.WaffleCone] = 1 }).CheckStock(
            [new OrderItem(FlavorNames.JavaJolt, CupSize.Byte, 2, [], FroyoMenu.WaffleCone)],
            Store);

        Assert.False(result.AllInStock);
        Assert.Equal([new Shortage(FroyoMenu.WaffleCone, 2, 1, "cones")], result.Shortages);
    }

    private static readonly OrderItem[] HugeOrder =
    [
        new(FlavorNames.JavaJolt, CupSize.Terabyte, int.MaxValue, [ToppingNames.HotFudge], FroyoMenu.WaffleCone),
        new(FlavorNames.JavaJolt, CupSize.Terabyte, int.MaxValue),
    ];

    [Fact]
    public async Task CheckStock_HugeQuantities_ReturnsErrorsInsteadOfThrowing()
    {
        var result = await DefaultTools().CheckStock(HugeOrder, Store);

        Assert.False(result.AllInStock);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void PriceOrder_HugeQuantities_ReturnsErrorsInsteadOfThrowing()
    {
        var quote = DefaultTools().PriceOrder(HugeOrder);

        Assert.Null(quote.Subtotal);
        Assert.NotEmpty(quote.Errors);
    }

    [Fact]
    public async Task CheckStock_CupsAreUnlimited()
    {
        var result = await Tools(new() { [FlavorNames.AicaiBowl] = 6 }).CheckStock([new OrderItem(FlavorNames.AicaiBowl, CupSize.Byte, 1)], Store);

        Assert.True(result.AllInStock);
    }

    [Fact]
    public async Task CheckStock_InvalidItem_ReportsErrorAndIsNotInStock()
    {
        var result = await Tools(new() { [FlavorNames.AicaiBowl] = 100 }).CheckStock([new OrderItem("Bubblegum Bug", CupSize.Byte, 1)], Store);

        Assert.False(result.AllInStock);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task CheckStock_UnknownStore_ReportsError()
    {
        var result = await DefaultTools().CheckStock([new OrderItem(FlavorNames.AicaiBowl, CupSize.Byte, 1)], "nyc-soho");

        Assert.False(result.AllInStock);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task CheckStock_JsonArguments_DeserializeSizeNamesAndDefaultStore()
    {
        var function = DefaultTools().AsAIFunctions().Single(f => f.Name == "CheckStock");
        var items = JsonDocument.Parse("""[{ "flavor": "Mint Condition", "size": "Megabyte", "quantity": 1 }]""").RootElement;

        var result = (JsonElement)(await function.InvokeAsync(new() { ["items"] = items }))!;

        Assert.Equal(Store, result.GetProperty("storeId").GetString());
        Assert.True(result.GetProperty("allInStock").GetBoolean());
    }

    [Fact]
    public void PriceOrder_PricesSizeToppingsAndCone()
    {
        var quote = DefaultTools().PriceOrder(
            [
                new OrderItem(FlavorNames.MintCondition, CupSize.Megabyte, 2, [ToppingNames.HotFudge, ToppingNames.RainbowSprinkles, ToppingNames.MochiBits, ToppingNames.FreshBerries], FroyoMenu.WaffleCone),
                new OrderItem(FlavorNames.AicaiBowl, CupSize.Byte, 1),
            ]);

        Assert.Empty(quote.Errors);
        // 7.75 + (2.50 + 0.95) + 1.25 = 12.45 per serving.
        var first = quote.Lines[0];
        Assert.Equal(12.45m, first.UnitPrice);
        Assert.Equal(24.90m, first.LineTotal);
        Assert.Equal(4.50m, quote.Lines[1].LineTotal);
        Assert.Equal(29.40m, quote.Subtotal);
    }

    [Fact]
    public void PriceOrder_InvalidItem_HasNoSubtotal()
    {
        var quote = DefaultTools().PriceOrder(
            [new OrderItem(FlavorNames.AicaiBowl, CupSize.Byte, 1), new OrderItem("Bubblegum Bug", CupSize.Byte, 1)]);

        Assert.Null(quote.Subtotal);
        Assert.Single(quote.Errors);
        Assert.Single(quote.Lines);
    }
}
