using System.Globalization;
using froyo_fulfillment_agent.Models;

namespace froyo_fulfillment_agent.Tests.Models;

/// <summary>
/// The knowledge base markdown and <see cref="FroyoMenu"/> both carry menu facts. These tests fail when they drift.
/// </summary>
public class MenuDocumentSyncTests
{
    private static readonly string Menu = ReadDoc("froyo-menu.md");
    private static readonly string AllergenSheet = ReadDoc("froyo-allergen-sheet.md");

    [Theory]
    [InlineData(CupSize.Byte, "6 oz cup")]
    [InlineData(CupSize.Kilobyte, "10 oz cup")]
    [InlineData(CupSize.Megabyte, "14 oz cup")]
    [InlineData(CupSize.Terabyte, "Pint to go")]
    public void Menu_SizeRow_MatchesPrice(CupSize size, string amount)
    {
        Assert.Contains($"| {size} | {amount} | {Money(FroyoMenu.PriceFor(size))} |", Menu);
    }

    [Fact]
    public void Menu_ToppingAndConePrices_MatchCode()
    {
        Assert.Contains(
            $"Toppings are {Money(FroyoMenu.ToppingPrice)} each, or any {FroyoMenu.ToppingBundleSize} for {Money(FroyoMenu.ToppingBundlePrice)}.",
            Menu);
        Assert.Contains($"**Waffle Cone** (+{Money(FroyoMenu.WaffleConePrice)})", Menu);
    }

    [Fact]
    public void Docs_ListEveryFlavorAndTopping()
    {
        Assert.All(FroyoMenu.Flavors, flavor =>
        {
            Assert.Contains($"### {flavor}", Menu);
            Assert.Contains($"| {flavor} |", AllergenSheet);
        });
        Assert.All(FroyoMenu.Toppings, topping =>
        {
            Assert.Contains(topping, Menu);
            Assert.Contains($"| {topping} |", AllergenSheet);
        });
    }

    private static string Money(decimal value) => value.ToString("$0.00", CultureInfo.InvariantCulture);

    private static string ReadDoc(string fileName)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "docs", fileName);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        throw new FileNotFoundException($"Could not find docs/{fileName} above {AppContext.BaseDirectory}.");
    }
}
