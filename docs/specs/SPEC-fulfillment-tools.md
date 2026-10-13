# Spec: Fulfillment Agent Knowledge and Tools

Status: Approved. Replaces the earlier inventory service spec.

## Objective

Keep the Froyo Fulfillment Agent simple. Menu facts live in a Foundry knowledge base, the model does the reasoning, and code handles only the math the model must not guess: stock and prices.

| Concern | Owner |
|---|---|
| Menu descriptions, prices for display, allergens, dietary tags | Foundry knowledge base built from `docs/froyo-menu.md` and `docs/froyo-allergen-sheet.md` |
| Matching descriptions to items, recommending alternatives, allergen reasoning | The model, guided by `agent-instructions.md` |
| Stock levels per store, shortage detection | `GetStoreStock` and `CheckStock` tools |
| Order pricing | `PriceOrder` tool |

## Knowledge base

1. Upload `docs/froyo-menu.md` and `docs/froyo-allergen-sheet.md` to a Foundry IQ knowledge base. It uses the Azure AI Search service in `infra/main.bicep`.
2. Add the knowledge base to the `froyo-foundry` toolbox (`FOUNDRY_TOOLBOX_NAME`).
3. `AddFoundryToolboxes` in `Program.cs` adds the toolbox tools to every agent run. No code changes are needed.

## Code layout

```text
source/froyo-fulfillment-agent/
  Models/CupSize.cs        Sizes, serialized as names
  Models/MenuNames.cs      FlavorNames, ToppingNames, and StoreIds constants
  Models/FroyoMenu.cs      Flavors, toppings, containers, stores, ounces, prices, name resolution, order validation
  Models/OrderModels.cs    OrderItem and tool result records
  Services/IInventoryRepository.cs          Stock source abstraction
  Services/InMemoryInventoryRepository.cs   Hardcoded demo stock, or stock supplied by tests
  Tools/FroyoTools.cs                       GetStoreStock, CheckStock, PriceOrder
```

`FroyoTools` depends on `IInventoryRepository`, not on a concrete data source. `Program.cs` creates the repository and passes it to `FroyoTools`. The agent is built before the service container, so neither is registered for dependency injection. Moving to a database means adding one `IInventoryRepository` implementation and changing one line in `Program.cs`.

```csharp
public interface IInventoryRepository
{
    // Stock keyed by menu item name, or null when the store is unknown.
    Task<IReadOnlyDictionary<string, int>?> GetStoreStockAsync(string storeId, CancellationToken cancellationToken = default);
}
```

The repository returns raw quantities only. Rules such as servings per size, totals across an order, and pricing stay in `FroyoTools` and `FroyoMenu`, so they don't change when the data source does.

## Domain rules

| Size | Ounces | Price |
|---|---|---|
| Byte | 6 | $4.50 |
| Kilobyte | 10 | $6.25 |
| Megabyte | 14 | $7.75 |
| Terabyte (pint) | 16 | $9.50 |

| Item | Stock unit | Price |
|---|---|---|
| Flavor | Ounces, shared by all sizes | By size (table above) |
| Topping | Portions, one per serving | $0.95 each, or each full group of 3 on one serving for $2.50 |
| Waffle Cone | Units, one per serving | +$1.25 per serving |
| Cup | Not tracked (unlimited) | Included |

- Names match case-insensitively, ignoring surrounding whitespace. Results use canonical menu names.
- An order must have at least one item, every quantity must be at least 1, and the total quantity is at most 20.
- Prices are before tax.

## Tools

| Tool | Input | Output |
|---|---|---|
| `GetStoreStock` | `storeId` (default `cmh-short-north`) | Ounces and servings per size for each flavor, topping portions, waffle cones. `StoreFound = false` for unknown stores |
| `CheckStock` | `items`, `storeId` (default `cmh-short-north`) | `AllInStock`, `Shortages` (item, needed, available, unit) totaled across the order, and `Errors` for invalid items or stores |
| `PriceOrder` | `items` | `Lines` (base, toppings, container, unit price, line total), `Subtotal`, and `Errors`. `Subtotal` is null when there are errors |

Tools never throw for bad input. Problems are returned as plain-language errors so the model can explain them.

## Commands

Run from `source/`:

| Purpose | Command |
|---|---|
| Build | `dotnet build froyo.slnx` |
| Test | `dotnet test froyo-fulfillment-agent.Tests/froyo-fulfillment-agent.Tests.csproj` |
| Run locally | `dotnet run --project froyo-fulfillment-agent` |

## Testing

| Test class | Covers |
|---|---|
| `FroyoMenuTests` | Ounces, prices, topping bundle math, name resolution, order validation |
| `MenuDocumentSyncTests` | Sizes, prices, flavors, and toppings in code match the knowledge base markdown |
| `InMemoryInventoryRepositoryTests` | Lookup by store, unknown stores, supplied stock, and demo data covering every item with at least one shortage per store |
| `FroyoToolsTests` | Tool names, JSON schema, stock reports, shortage totals, pricing, errors. Uses `InMemoryInventoryRepository` with small explicit stock |

## Not doing

- **Placing orders or reserving stock:** the agent ends with a confirmed, priced summary.
- **Allergen logic in code:** the model reads the allergen sheet from the knowledge base.
- **Alternative-ranking logic in code:** the model chooses alternatives using `GetStoreStock` and the knowledge base.
- **Tax:** quotes are subtotals before tax.
