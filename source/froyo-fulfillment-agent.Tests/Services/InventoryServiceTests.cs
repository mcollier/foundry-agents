using froyo_fulfillment_agent.Models;
using froyo_fulfillment_agent.Services;
using froyo_fulfillment_agent.Tests.Fakes;

namespace froyo_fulfillment_agent.Tests.Services;

public class InventoryServiceTests
{
    private const string ShortNorth = "cmh-short-north";

    [Theory]
    [InlineData("nyc-soho")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAvailableInventoryAsync_UnknownOrBlankStore_ReturnsNotFoundWithEmptyLists(string storeId)
    {
        var service = new InventoryService(new FakeInventoryRepository().WithStore(ShortNorth));

        var inventory = await service.GetAvailableInventoryAsync(storeId);

        Assert.False(inventory.StoreFound);
        Assert.Empty(inventory.Flavors);
        Assert.Empty(inventory.Toppings);
        Assert.Empty(inventory.Cones);
    }

    [Fact]
    public async Task GetAvailableInventoryAsync_CatalogStoreMissingFromRepository_ReturnsNotFound()
    {
        var service = new InventoryService(new FakeInventoryRepository());

        var inventory = await service.GetAvailableInventoryAsync(ShortNorth);

        Assert.False(inventory.StoreFound);
    }

    [Fact]
    public async Task GetAvailableInventoryAsync_StoreIdDifferentCase_ReturnsCanonicalStoreId()
    {
        var service = new InventoryService(new FakeInventoryRepository().WithStore(ShortNorth));

        var inventory = await service.GetAvailableInventoryAsync(" CMH-SHORT-NORTH ");

        Assert.True(inventory.StoreFound);
        Assert.Equal(ShortNorth, inventory.StoreId);
    }

    [Fact]
    public async Task GetAvailableInventoryAsync_KnownStore_ReturnsEveryCatalogFlavorInOrder()
    {
        var service = new InventoryService(new FakeInventoryRepository().WithStore(ShortNorth));

        var inventory = await service.GetAvailableInventoryAsync(ShortNorth);

        Assert.Equal(FroyoCatalog.Flavors, inventory.Flavors.Select(f => f.Name));
    }

    [Fact]
    public async Task GetAvailableInventoryAsync_FlavorInStock_ReportsOuncesOnHand()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, flavorOunces: new() { ["Java Jolt"] = 42 });
        var service = new InventoryService(repository);

        var inventory = await service.GetAvailableInventoryAsync(ShortNorth);

        Assert.Equal(42, inventory.Flavors.Single(f => f.Name == "Java Jolt").OuncesOnHand);
    }

    [Fact]
    public async Task GetAvailableInventoryAsync_FlavorMissingFromStock_ReportsZero()
    {
        var service = new InventoryService(new FakeInventoryRepository().WithStore(ShortNorth));

        var inventory = await service.GetAvailableInventoryAsync(ShortNorth);

        var mint = inventory.Flavors.Single(f => f.Name == "Mint Condition");
        Assert.Equal(0, mint.OuncesOnHand);
        Assert.All(mint.ServingsBySize.Values, servings => Assert.Equal(0, servings));
    }

    [Fact]
    public async Task GetAvailableInventoryAsync_100Ounces_DerivesServingsBySizeWithFloorDivision()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, flavorOunces: new() { ["Vanilla Exception"] = 100 });
        var service = new InventoryService(repository);

        var inventory = await service.GetAvailableInventoryAsync(ShortNorth);

        var servings = inventory.Flavors.Single(f => f.Name == "Vanilla Exception").ServingsBySize;
        Assert.Equal(16, servings[CupSize.Byte]);
        Assert.Equal(10, servings[CupSize.Kilobyte]);
        Assert.Equal(7, servings[CupSize.Megabyte]);
        Assert.Equal(6, servings[CupSize.Terabyte]);
    }

    [Fact]
    public async Task GetAvailableInventoryAsync_KnownStore_ReturnsEveryToppingInPortions()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, toppingPortions: new() { ["Hot Fudge"] = 30 });
        var service = new InventoryService(repository);

        var inventory = await service.GetAvailableInventoryAsync(ShortNorth);

        Assert.Equal(FroyoCatalog.Toppings, inventory.Toppings.Select(t => t.Name));
        Assert.All(inventory.Toppings, t => Assert.Equal("portions", t.Unit));
        Assert.Equal(30, inventory.Toppings.Single(t => t.Name == "Hot Fudge").Quantity);
        Assert.Equal(0, inventory.Toppings.Single(t => t.Name == "Mochi Bits").Quantity);
    }

    [Fact]
    public async Task GetAvailableInventoryAsync_KnownStore_ReturnsWaffleConesInUnits()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, coneUnits: new() { ["Waffle Cone"] = 12 });
        var service = new InventoryService(repository);

        var inventory = await service.GetAvailableInventoryAsync(ShortNorth);

        var cone = Assert.Single(inventory.Cones);
        Assert.Equal("Waffle Cone", cone.Name);
        Assert.Equal(12, cone.Quantity);
        Assert.Equal("units", cone.Unit);
    }

    [Theory]
    [InlineData("nyc-soho")]
    [InlineData("")]
    public async Task CheckInventoryAsync_UnknownOrBlankStore_ReturnsNotFoundWithNoLines(string storeId)
    {
        var service = new InventoryService(new FakeInventoryRepository().WithStore(ShortNorth));

        var result = await service.CheckInventoryAsync(storeId, [new OrderItem("Java Jolt", CupSize.Byte, 1)]);

        Assert.False(result.StoreFound);
        Assert.False(result.AllSufficient);
        Assert.Empty(result.Lines);
    }

    [Fact]
    public async Task CheckInventoryAsync_EmptyItems_ReturnsNotSufficientWithNoLines()
    {
        var service = new InventoryService(new FakeInventoryRepository().WithStore(ShortNorth));

        var result = await service.CheckInventoryAsync(ShortNorth, []);

        Assert.True(result.StoreFound);
        Assert.False(result.AllSufficient);
        Assert.Empty(result.Lines);
    }

    [Fact]
    public async Task CheckInventoryAsync_NullItems_ReturnsNotSufficientWithNoLines()
    {
        var service = new InventoryService(new FakeInventoryRepository().WithStore(ShortNorth));

        var result = await service.CheckInventoryAsync(ShortNorth, null);

        Assert.False(result.AllSufficient);
        Assert.Empty(result.Lines);
    }

    [Fact]
    public async Task CheckInventoryAsync_FlavorWithEnoughOunces_ReturnsSufficientLine()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, flavorOunces: new() { ["Mint Condition"] = 100 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth, [new OrderItem("Mint Condition", CupSize.Megabyte, 2)]);

        var line = Assert.Single(result.Lines);
        Assert.Equal(new InventoryCheckLine(ItemType.Flavor, "Mint Condition", 28, 100, "oz", StockStatus.Sufficient), line);
        Assert.True(result.AllSufficient);
    }

    [Fact]
    public async Task CheckInventoryAsync_SameFlavorAcrossItems_SumsOuncesIntoOneLine()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, flavorOunces: new() { ["Mint Condition"] = 100 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth,
        [
            new OrderItem("Mint Condition", CupSize.Megabyte, 2),
            new OrderItem("mint condition", CupSize.Terabyte, 1),
        ]);

        var line = Assert.Single(result.Lines);
        Assert.Equal("Mint Condition", line.Name);
        Assert.Equal(44, line.Required);
    }

    [Fact]
    public async Task CheckInventoryAsync_FlavorShortByTwoOunces_ReturnsInsufficientLine()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, flavorOunces: new() { ["Java Jolt"] = 20 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth,
        [
            new OrderItem("Java Jolt", CupSize.Terabyte, 1),
            new OrderItem("Java Jolt", CupSize.Byte, 1),
        ]);

        var line = Assert.Single(result.Lines);
        Assert.Equal(22, line.Required);
        Assert.Equal(20, line.Available);
        Assert.Equal(StockStatus.Insufficient, line.Status);
        Assert.False(result.AllSufficient);
    }

    [Fact]
    public async Task CheckInventoryAsync_FlavorExactlyMatchingStock_IsSufficient()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, flavorOunces: new() { ["Aicai Bowl"] = 16 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth, [new OrderItem("Aicai Bowl", CupSize.Terabyte, 1)]);

        Assert.Equal(StockStatus.Sufficient, Assert.Single(result.Lines).Status);
    }

    [Fact]
    public async Task CheckInventoryAsync_OneOfTwoFlavorsShort_FlagsOnlyThatFlavor()
    {
        var repository = new FakeInventoryRepository().WithStore(ShortNorth,
            flavorOunces: new() { ["Vanilla Exception"] = 50, ["Java Jolt"] = 5 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth,
        [
            new OrderItem("Vanilla Exception", CupSize.Byte, 1),
            new OrderItem("Java Jolt", CupSize.Byte, 1),
        ]);

        Assert.Equal(["Vanilla Exception", "Java Jolt"], result.Lines.Select(l => l.Name));
        Assert.Equal([StockStatus.Sufficient, StockStatus.Insufficient], result.Lines.Select(l => l.Status));
        Assert.False(result.AllSufficient);
    }

    [Fact]
    public async Task CheckInventoryAsync_ToppingsAcrossItemsIncludingRepeats_SumsPortionsIntoOneLine()
    {
        var repository = new FakeInventoryRepository().WithStore(ShortNorth,
            flavorOunces: new() { ["Vanilla Exception"] = 100 },
            toppingPortions: new() { ["Hot Fudge"] = 10 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth,
        [
            new OrderItem("Vanilla Exception", CupSize.Byte, 2, ["Hot Fudge", "Hot Fudge"]),
            new OrderItem("Vanilla Exception", CupSize.Byte, 1, ["hot fudge"]),
        ]);

        var line = Assert.Single(result.Lines, l => l.ItemType == ItemType.Topping);
        Assert.Equal(new InventoryCheckLine(ItemType.Topping, "Hot Fudge", 5, 10, "portions", StockStatus.Sufficient), line);
        Assert.True(result.AllSufficient);
    }

    [Fact]
    public async Task CheckInventoryAsync_ToppingShort_ReturnsInsufficientToppingLine()
    {
        var repository = new FakeInventoryRepository().WithStore(ShortNorth,
            flavorOunces: new() { ["Vanilla Exception"] = 100 },
            toppingPortions: new() { ["Mochi Bits"] = 1 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth,
            [new OrderItem("Vanilla Exception", CupSize.Byte, 2, ["Mochi Bits"])]);

        var line = Assert.Single(result.Lines, l => l.ItemType == ItemType.Topping);
        Assert.Equal(StockStatus.Insufficient, line.Status);
        Assert.False(result.AllSufficient);
    }

    [Fact]
    public async Task CheckInventoryAsync_WaffleConesAcrossItems_SumsUnitsIntoOneLine()
    {
        var repository = new FakeInventoryRepository().WithStore(ShortNorth,
            flavorOunces: new() { ["Cookie Container"] = 100 },
            coneUnits: new() { ["Waffle Cone"] = 4 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth,
        [
            new OrderItem("Cookie Container", CupSize.Byte, 2, Container: "Waffle Cone"),
            new OrderItem("Cookie Container", CupSize.Byte, 3, Container: "waffle cone"),
        ]);

        var line = Assert.Single(result.Lines, l => l.ItemType == ItemType.Cone);
        Assert.Equal(new InventoryCheckLine(ItemType.Cone, "Waffle Cone", 5, 4, "units", StockStatus.Insufficient), line);
    }

    [Fact]
    public async Task CheckInventoryAsync_Cup_ProducesNoConeLine()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, flavorOunces: new() { ["Cookie Container"] = 100 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth,
            [new OrderItem("Cookie Container", CupSize.Byte, 1, Container: "Cup")]);

        Assert.DoesNotContain(result.Lines, l => l.ItemType == ItemType.Cone);
        Assert.True(result.AllSufficient);
    }

    [Fact]
    public async Task CheckInventoryAsync_UnknownFlavor_ReturnsUnknownLineWithoutThrowing()
    {
        var service = new InventoryService(new FakeInventoryRepository().WithStore(ShortNorth));

        var result = await service.CheckInventoryAsync(ShortNorth, [new OrderItem(" Bubblegum Bug ", CupSize.Byte, 1)]);

        var line = Assert.Single(result.Lines);
        Assert.Equal(new InventoryCheckLine(ItemType.Flavor, "Bubblegum Bug", 6, 0, "oz", StockStatus.Unknown), line);
        Assert.False(result.AllSufficient);
    }

    [Fact]
    public async Task CheckInventoryAsync_UnknownTopping_ReturnsUnknownToppingLine()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, flavorOunces: new() { ["Java Jolt"] = 100 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth,
            [new OrderItem("Java Jolt", CupSize.Byte, 2, ["Gummy Bears"])]);

        var line = Assert.Single(result.Lines, l => l.ItemType == ItemType.Topping);
        Assert.Equal(new InventoryCheckLine(ItemType.Topping, "Gummy Bears", 2, 0, "portions", StockStatus.Unknown), line);
        Assert.False(result.AllSufficient);
    }

    [Fact]
    public async Task CheckInventoryAsync_UnknownContainer_ReturnsUnknownConeLine()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, flavorOunces: new() { ["Java Jolt"] = 100 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth,
            [new OrderItem("Java Jolt", CupSize.Byte, 1, Container: "Sugar Cone")]);

        var line = Assert.Single(result.Lines, l => l.ItemType == ItemType.Cone);
        Assert.Equal(new InventoryCheckLine(ItemType.Cone, "Sugar Cone", 1, 0, "units", StockStatus.Unknown), line);
        Assert.False(result.AllSufficient);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task CheckInventoryAsync_QuantityBelowOne_ReturnsInvalidLineAndAddsNoDemand(int quantity)
    {
        var repository = new FakeInventoryRepository().WithStore(ShortNorth,
            flavorOunces: new() { ["Java Jolt"] = 100 },
            toppingPortions: new() { ["Hot Fudge"] = 10 },
            coneUnits: new() { ["Waffle Cone"] = 10 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth,
            [new OrderItem("Java Jolt", CupSize.Byte, quantity, ["Hot Fudge"], "Waffle Cone")]);

        var line = Assert.Single(result.Lines);
        Assert.Equal(new InventoryCheckLine(ItemType.Flavor, "Java Jolt", 0, 100, "oz", StockStatus.Invalid), line);
        Assert.False(result.AllSufficient);
    }

    [Fact]
    public async Task CheckInventoryAsync_MixedItems_OrdersLinesFlavorsThenToppingsThenCones()
    {
        var repository = new FakeInventoryRepository().WithStore(ShortNorth,
            flavorOunces: new() { ["Java Jolt"] = 100, ["Aicai Bowl"] = 100 },
            toppingPortions: new() { ["Fresh Berries"] = 10, ["Almond Granola"] = 10 },
            coneUnits: new() { ["Waffle Cone"] = 10 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth,
        [
            new OrderItem("Java Jolt", CupSize.Byte, 1, ["Fresh Berries"], "Waffle Cone"),
            new OrderItem("Aicai Bowl", CupSize.Kilobyte, 1, ["Almond Granola"]),
        ]);

        Assert.Equal(
            ["Java Jolt", "Aicai Bowl", "Fresh Berries", "Almond Granola", "Waffle Cone"],
            result.Lines.Select(l => l.Name));
        Assert.True(result.AllSufficient);
    }

    [Fact]
    public async Task CheckInventoryAsync_UndefinedSize_ReturnsInvalidLineWithoutThrowing()
    {
        var repository = new FakeInventoryRepository()
            .WithStore(ShortNorth, flavorOunces: new() { ["Java Jolt"] = 100 });
        var service = new InventoryService(repository);

        var result = await service.CheckInventoryAsync(ShortNorth, [new OrderItem("Java Jolt", (CupSize)99, 1)]);

        Assert.Equal(StockStatus.Invalid, Assert.Single(result.Lines).Status);
        Assert.False(result.AllSufficient);
    }
}
