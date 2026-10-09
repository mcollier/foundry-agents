using froyo_fulfillment_agent.Models;

namespace froyo_fulfillment_agent.Tests.Models;

public class FroyoCatalogTests
{
    [Theory]
    [InlineData(CupSize.Byte, 6)]
    [InlineData(CupSize.Kilobyte, 10)]
    [InlineData(CupSize.Megabyte, 14)]
    [InlineData(CupSize.Terabyte, 16)]
    public void OuncesFor_EachSize_ReturnsMenuOunces(CupSize size, int expectedOunces)
    {
        Assert.Equal(expectedOunces, FroyoCatalog.OuncesFor(size));
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

        Assert.Equal(expected, FroyoCatalog.Flavors);
    }

    [Fact]
    public void Toppings_MatchesMenu()
    {
        string[] expected =
        [
            "Rainbow Sprinkles", "Chocolate Chips", "Crushed Cookies", "Graham Cracker Crumble",
            "Almond Granola", "Chopped Peanuts", "Hot Fudge", "Caramel Drizzle", "Fresh Berries", "Mochi Bits",
        ];

        Assert.Equal(expected, FroyoCatalog.Toppings);
    }

    [Fact]
    public void Containers_AreCupAndWaffleCone()
    {
        Assert.Equal(["Cup", "Waffle Cone"], FroyoCatalog.Containers);
    }

    [Fact]
    public void Cones_IsWaffleConeOnly()
    {
        Assert.Equal(["Waffle Cone"], FroyoCatalog.Cones);
    }

    [Fact]
    public void Stores_AreTheThreeStoreIds()
    {
        Assert.Equal(["cmh-short-north", "cle-the-flats", "cvg-otr"], FroyoCatalog.Stores);
    }

    [Theory]
    [InlineData("mint condition")]
    [InlineData("  MINT CONDITION  ")]
    [InlineData("Mint Condition")]
    public void TryResolveFlavor_IgnoresCaseAndWhitespace_ReturnsCanonicalName(string input)
    {
        Assert.True(FroyoCatalog.TryResolveFlavor(input, out var name));
        Assert.Equal("Mint Condition", name);
    }

    [Theory]
    [InlineData("hot fudge", "Hot Fudge")]
    [InlineData(" MOCHI BITS ", "Mochi Bits")]
    public void TryResolveTopping_IgnoresCaseAndWhitespace_ReturnsCanonicalName(string input, string expected)
    {
        Assert.True(FroyoCatalog.TryResolveTopping(input, out var name));
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData("waffle cone", "Waffle Cone")]
    [InlineData(" CUP ", "Cup")]
    public void TryResolveContainer_IgnoresCaseAndWhitespace_ReturnsCanonicalName(string input, string expected)
    {
        Assert.True(FroyoCatalog.TryResolveContainer(input, out var name));
        Assert.Equal(expected, name);
    }

    [Fact]
    public void TryResolveStore_IgnoresCaseAndWhitespace_ReturnsCanonicalId()
    {
        Assert.True(FroyoCatalog.TryResolveStore(" CMH-Short-North ", out var id));
        Assert.Equal("cmh-short-north", id);
    }

    [Theory]
    [InlineData("Bubblegum Bug")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryResolveFlavor_UnknownOrBlank_ReturnsFalse(string? input)
    {
        Assert.False(FroyoCatalog.TryResolveFlavor(input, out _));
    }

    [Theory]
    [InlineData("Gummy Bears")]
    [InlineData(null)]
    public void TryResolveTopping_UnknownOrBlank_ReturnsFalse(string? input)
    {
        Assert.False(FroyoCatalog.TryResolveTopping(input, out _));
    }

    [Theory]
    [InlineData("Sugar Cone")]
    [InlineData(null)]
    public void TryResolveContainer_UnknownOrBlank_ReturnsFalse(string? input)
    {
        Assert.False(FroyoCatalog.TryResolveContainer(input, out _));
    }

    [Theory]
    [InlineData("nyc-soho")]
    [InlineData("")]
    [InlineData(null)]
    public void TryResolveStore_UnknownOrBlank_ReturnsFalse(string? input)
    {
        Assert.False(FroyoCatalog.TryResolveStore(input, out _));
    }
}
