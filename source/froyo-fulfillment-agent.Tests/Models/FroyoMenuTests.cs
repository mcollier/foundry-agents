using froyo_fulfillment_agent.Models;

namespace froyo_fulfillment_agent.Tests.Models;

public class FroyoMenuTests
{
    [Theory]
    [InlineData(CupSize.Byte, 6)]
    [InlineData(CupSize.Kilobyte, 10)]
    [InlineData(CupSize.Megabyte, 14)]
    [InlineData(CupSize.Terabyte, 16)]
    public void OuncesFor_EachSize_ReturnsMenuOunces(CupSize size, int expectedOunces)
    {
        Assert.Equal(expectedOunces, FroyoMenu.OuncesFor(size));
    }

    [Fact]
    public void Flavors_MatchesMenu()
    {
        string[] expected =
        [
            "Mint Condition", "Berry Blockchain Blast", "Cookie Container", "Recursive Raspberry",
            "Vanilla Exception", "Null Pointer Pistachio", "Java Jolt", "Peanut Butter Protocol",
            "Cloud Caramel Cache", "Aicai Bowl",
        ];

        Assert.Equal(expected, FroyoMenu.Flavors);
    }

    [Fact]
    public void Toppings_MatchesMenu()
    {
        string[] expected =
        [
            "Rainbow Sprinkles", "Chocolate Chips", "Crushed Cookies", "Graham Cracker Crumble",
            "Almond Granola", "Chopped Peanuts", "Hot Fudge", "Caramel Drizzle", "Fresh Berries", "Mochi Bits",
        ];

        Assert.Equal(expected, FroyoMenu.Toppings);
    }

    [Fact]
    public void Containers_AreCupAndWaffleCone()
    {
        Assert.Equal(["Cup", "Waffle Cone"], FroyoMenu.Containers);
    }

    [Fact]
    public void Stores_AreTheThreeStoreIds()
    {
        Assert.Equal(["cmh-short-north", "cle-the-flats", "cvg-otr"], FroyoMenu.Stores);
    }

    [Theory]
    [InlineData("mint condition")]
    [InlineData("  MINT CONDITION  ")]
    [InlineData("Mint Condition")]
    public void TryResolveFlavor_IgnoresCaseAndWhitespace_ReturnsCanonicalName(string input)
    {
        Assert.True(FroyoMenu.TryResolveFlavor(input, out var name));
        Assert.Equal("Mint Condition", name);
    }

    [Theory]
    [InlineData("hot fudge", "Hot Fudge")]
    [InlineData(" MOCHI BITS ", "Mochi Bits")]
    public void TryResolveTopping_IgnoresCaseAndWhitespace_ReturnsCanonicalName(string input, string expected)
    {
        Assert.True(FroyoMenu.TryResolveTopping(input, out var name));
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData("waffle cone", "Waffle Cone")]
    [InlineData(" CUP ", "Cup")]
    public void TryResolveContainer_IgnoresCaseAndWhitespace_ReturnsCanonicalName(string input, string expected)
    {
        Assert.True(FroyoMenu.TryResolveContainer(input, out var name));
        Assert.Equal(expected, name);
    }

    [Fact]
    public void TryResolveStore_IgnoresCaseAndWhitespace_ReturnsCanonicalId()
    {
        Assert.True(FroyoMenu.TryResolveStore(" CMH-Short-North ", out var id));
        Assert.Equal("cmh-short-north", id);
    }

    [Theory]
    [InlineData("Bubblegum Bug")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryResolveFlavor_UnknownOrBlank_ReturnsFalse(string? input)
    {
        Assert.False(FroyoMenu.TryResolveFlavor(input, out _));
    }

    [Theory]
    [InlineData("Gummy Bears")]
    [InlineData(null)]
    public void TryResolveTopping_UnknownOrBlank_ReturnsFalse(string? input)
    {
        Assert.False(FroyoMenu.TryResolveTopping(input, out _));
    }

    [Theory]
    [InlineData("Sugar Cone")]
    [InlineData(null)]
    public void TryResolveContainer_UnknownOrBlank_ReturnsFalse(string? input)
    {
        Assert.False(FroyoMenu.TryResolveContainer(input, out _));
    }

    [Theory]
    [InlineData("nyc-soho")]
    [InlineData("")]
    [InlineData(null)]
    public void TryResolveStore_UnknownOrBlank_ReturnsFalse(string? input)
    {
        Assert.False(FroyoMenu.TryResolveStore(input, out _));
    }

    [Theory]
    [InlineData(CupSize.Byte, "4.50")]
    [InlineData(CupSize.Kilobyte, "6.25")]
    [InlineData(CupSize.Megabyte, "7.75")]
    [InlineData(CupSize.Terabyte, "9.50")]
    public void PriceFor_EachSize_ReturnsMenuPrice(CupSize size, string expected)
    {
        Assert.Equal(decimal.Parse(expected), FroyoMenu.PriceFor(size));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "0.95")]
    [InlineData(2, "1.90")]
    [InlineData(3, "2.50")]
    [InlineData(4, "3.45")]
    [InlineData(6, "5.00")]
    public void ToppingsPriceFor_AppliesThreeForBundle(int count, string expected)
    {
        Assert.Equal(decimal.Parse(expected), FroyoMenu.ToppingsPriceFor(count));
    }

    [Fact]
    public void Resolve_ValidItem_ReturnsCanonicalNamesAndNoErrors()
    {
        var (items, errors) = FroyoMenu.Resolve(
            [new OrderItem(" java jolt ", CupSize.Byte, 2, ["hot fudge"], "waffle cone")]);

        Assert.Empty(errors);
        var item = Assert.Single(items);
        Assert.Equal(new OrderItem("Java Jolt", CupSize.Byte, 2, item.Toppings, "Waffle Cone"), item);
        Assert.Equal(["Hot Fudge"], item.Toppings!);
    }

    [Fact]
    public void Resolve_UnknownNamesAndBadQuantity_ReportsEachErrorAndDropsItem()
    {
        var (items, errors) = FroyoMenu.Resolve(
            [new OrderItem("Bubblegum Bug", CupSize.Byte, 0, ["Gummy Bears"], "Sugar Cone")]);

        Assert.Empty(items);
        Assert.Equal(4, errors.Count);
    }

    [Fact]
    public void Resolve_UndefinedSize_ReportsError()
    {
        var (items, errors) = FroyoMenu.Resolve([new OrderItem("Java Jolt", (CupSize)99, 1)]);

        Assert.Empty(items);
        Assert.Single(errors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Resolve_NullOrEmpty_ReportsNoItems(bool useNull)
    {
        var (items, errors) = FroyoMenu.Resolve(useNull ? null : []);

        Assert.Empty(items);
        Assert.Single(errors);
    }

    [Fact]
    public void Resolve_MoreThanTwentyItems_ReportsLimit()
    {
        var (_, errors) = FroyoMenu.Resolve(
            [new OrderItem("Java Jolt", CupSize.Byte, 15), new OrderItem("Aicai Bowl", CupSize.Byte, 6)]);

        Assert.Contains(errors, e => e.Contains("20"));
    }

    [Fact]
    public void Resolve_ExactlyTwentyItems_IsAllowed()
    {
        var (items, errors) = FroyoMenu.Resolve(
            [new OrderItem("Java Jolt", CupSize.Byte, 14), new OrderItem("Aicai Bowl", CupSize.Byte, 6)]);

        Assert.Empty(errors);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void Resolve_HugeQuantities_ReportsLimitWithoutThrowing()
    {
        var (items, errors) = FroyoMenu.Resolve(
            [new OrderItem("Java Jolt", CupSize.Byte, int.MaxValue), new OrderItem("Aicai Bowl", CupSize.Byte, int.MaxValue)]);

        Assert.Empty(items);
        Assert.Contains(errors, e => e.Contains("20"));
    }

    [Fact]
    public void Resolve_NullItem_ReportsErrorAndKeepsOtherItems()
    {
        var (items, errors) = FroyoMenu.Resolve([null!, new OrderItem("Java Jolt", CupSize.Byte, 1)]);

        Assert.Equal("Item 1 is empty.", Assert.Single(errors));
        Assert.Single(items);
    }
}
