using System.Text.Json;
using froyo_fulfillment_agent.Models;
using froyo_fulfillment_agent.Tests.Fakes;
using froyo_fulfillment_agent.Tools;
using Microsoft.Extensions.AI;

namespace froyo_fulfillment_agent.Tests.Tools;

public class InventoryToolsTests
{
    [Fact]
    public void AsAIFunctions_ReturnsBothToolsWithExpectedNamesAndDescriptions()
    {
        var functions = new InventoryTools(new FakeInventoryService()).AsAIFunctions();

        Assert.Equal(["GetAvailableInventory", "CheckInventory"], functions.Select(f => f.Name));
        Assert.All(functions, f => Assert.False(string.IsNullOrWhiteSpace(f.Description)));
    }

    [Fact]
    public async Task GetAvailableInventory_WithStoreId_DelegatesToService()
    {
        var service = new FakeInventoryService();

        await Function(service, "GetAvailableInventory").InvokeAsync(new() { ["storeId"] = "cvg-otr" });

        Assert.Equal("cvg-otr", service.LastStoreId);
    }

    [Fact]
    public async Task GetAvailableInventory_WithoutStoreId_DefaultsToShortNorth()
    {
        var service = new FakeInventoryService();

        await Function(service, "GetAvailableInventory").InvokeAsync(new());

        Assert.Equal("cmh-short-north", service.LastStoreId);
    }

    [Fact]
    public async Task CheckInventory_JsonArgumentsWithSizeNames_DelegatesTypedItemsToService()
    {
        var service = new FakeInventoryService();
        var items = JsonDocument.Parse("""
            [{ "flavor": "Java Jolt", "size": "Megabyte", "quantity": 2, "toppings": ["Hot Fudge"], "container": "Waffle Cone" }]
            """).RootElement;

        await Function(service, "CheckInventory").InvokeAsync(new() { ["items"] = items, ["storeId"] = "cle-the-flats" });

        Assert.Equal("cle-the-flats", service.LastStoreId);
        var item = Assert.Single(service.LastItems!);
        Assert.Equal("Java Jolt", item.Flavor);
        Assert.Equal(CupSize.Megabyte, item.Size);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(["Hot Fudge"], item.Toppings!);
        Assert.Equal("Waffle Cone", item.Container);
    }

    [Fact]
    public async Task CheckInventory_WithoutStoreId_DefaultsToShortNorth()
    {
        var service = new FakeInventoryService();
        var items = JsonDocument.Parse("""[{ "flavor": "Java Jolt", "size": "Byte", "quantity": 1 }]""").RootElement;

        await Function(service, "CheckInventory").InvokeAsync(new() { ["items"] = items });

        Assert.Equal("cmh-short-north", service.LastStoreId);
    }

    [Fact]
    public void CheckInventory_JsonSchema_ExposesSizesAsNames()
    {
        var schema = Function(new FakeInventoryService(), "CheckInventory").JsonSchema.GetRawText();

        Assert.Contains("\"Byte\"", schema);
        Assert.Contains("\"Terabyte\"", schema);
    }

    private static AIFunction Function(FakeInventoryService service, string name) =>
        new InventoryTools(service).AsAIFunctions().Single(f => f.Name == name);
}
