# Spec: Inventory Service for the Froyo Fulfillment Agent

Status: Approved

## Objective

Give the Froyo Fulfillment Agent a reliable, swappable source of per-store stock data so it never claims an item is in stock without checking. `agent-instructions.md` already requires this: "Never say an item is in stock unless CheckInventory or FindInStockAlternatives says so."

### Problem statement

How might we let the agent answer "what's available?" and "can this store fulfill this order?" from one service whose data source can later move from hardcoded values to a database without changing business rules?

### Users

| User | Need |
|---|---|
| Fulfillment agent (LLM) | Tools that report stock and check an order's needs for a given store |
| Developer (repository owner) | A clean seam to replace hardcoded stock with a database or external service |

### User stories

1. As the agent, I can get the available inventory for a store so I can describe what is in stock.
2. As the agent, I can check whether a store has enough stock for a list of order items so I can find shortages before confirming an order.
3. As a developer, I can replace the hardcoded stock by implementing one repository interface, with no changes to the service or tools.

### Recommended direction

The service is split into three parts:

| Part | Responsibility | Changes when |
|---|---|---|
| `FroyoCatalog` (static) | Menu facts: flavor names, sizes and their ounces, topping names, cone names, store IDs | The menu changes |
| `IInventoryRepository` | Raw stock per store: flavor ounces, topping portions, cone units | The data source changes |
| `IInventoryService` / `InventoryService` | Rules: convert ounces to servings per size, add up how much an order needs, compare it with stock | Business rules change |

`InMemoryInventoryRepository` is the first repository implementation and holds the hardcoded stock.

## Domain rules

### Sizes

| Size | Ounces |
|---|---|
| Byte | 6 |
| Kilobyte | 10 |
| Megabyte | 14 |
| Terabyte (pint) | 16 |

### Stock units

| Item type | Unit | Notes |
|---|---|---|
| Flavor | Ounces (bulk) | One store-level quantity per flavor, shared by all sizes |
| Topping | Portions | Each listed topping on one serving uses one portion |
| Waffle Cone | Units | One per serving served in a waffle cone |
| Cup | Not tracked | Treated as unlimited, never produces a check line |

### Catalog contents (from `docs/froyo-menu.md`)

- **Flavors (10):** Mint Condition, Berry Blockchain Blast, Cookie Container, Recursive Raspberry, Vanilla Exception, Null Pointer Pistachio, Java Jolt, Peanut Butter Protocol, Cloud Caramel Cache, Aicai Bowl
- **Toppings (10):** Rainbow Sprinkles, Chocolate Chips, Crushed Cookies, Graham Cracker Crumble, Almond Granola, Chopped Peanuts, Hot Fudge, Caramel Drizzle, Fresh Berries, Mochi Bits
- **Containers:** Cup, Waffle Cone
- **Stores:** `cmh-short-north`, `cle-the-flats`, `cvg-otr`

Allergen data and prices are out of scope for this service. Menu search covers allergens, and ValidateOrder will cover prices.

### Name matching

- Store IDs, flavor names, topping names, and container names match case-insensitively and ignore surrounding whitespace.
- Results always use the canonical catalog name.

## Public contract

```csharp
public interface IInventoryService
{
    Task<StoreInventory> GetAvailableInventoryAsync(
        string storeId, CancellationToken cancellationToken = default);

    Task<InventoryCheckResult> CheckInventoryAsync(
        string storeId, IReadOnlyList<OrderItem> items, CancellationToken cancellationToken = default);
}

public interface IInventoryRepository
{
    // Returns null when the store is unknown.
    Task<StoreStock?> GetStoreStockAsync(string storeId, CancellationToken cancellationToken = default);
}
```

### Models (records)

| Type | Fields |
|---|---|
| `CupSize` (enum, serialized as string) | `Byte`, `Kilobyte`, `Megabyte`, `Terabyte` |
| `StoreStock` | `StoreId`, `FlavorOunces` (name to int), `ToppingPortions` (name to int), `ConeUnits` (name to int) |
| `StoreInventory` | `StoreId`, `StoreFound`, `Flavors` (list of `FlavorAvailability`), `Toppings` (list of `StockLevel`), `Cones` (list of `StockLevel`) |
| `FlavorAvailability` | `Name`, `OuncesOnHand`, `ServingsBySize` (`CupSize` to int) |
| `StockLevel` | `Name`, `Quantity`, `Unit` |
| `OrderItem` | `Flavor`, `Size` (`CupSize`), `Quantity`, `Toppings` (list of string), `Container` (default "Cup") |
| `InventoryCheckResult` | `StoreId`, `StoreFound`, `AllSufficient`, `Lines` (list of `InventoryCheckLine`) |
| `InventoryCheckLine` | `ItemType` (Flavor, Topping, Cone), `Name`, `Required`, `Available`, `Unit`, `Status` |
| `StockStatus` (enum, serialized as string) | `Sufficient`, `Insufficient`, `Unknown`, `Invalid` |

### Behavior

**GetAvailableInventoryAsync**

1. For an unknown or blank store, return `StoreFound = false` with empty lists. Do not throw.
2. Return every catalog flavor, topping, and cone, including items with zero stock.
3. `ServingsBySize[size] = floor(OuncesOnHand / size ounces)`. For example, 100 oz gives Byte 16, Kilobyte 10, Megabyte 7, Terabyte 6.
4. A catalog item missing from the repository data reports quantity 0.

**CheckInventoryAsync**

1. For an unknown or blank store, return `StoreFound = false`, `AllSufficient = false`, and no lines.
2. Add up demand per resource across all items:
   - Flavor ounces: `size ounces * quantity`, summed across items with the same flavor
   - Topping portions: `quantity` for each listed topping occurrence, summed across items
   - Waffle cones: `quantity` for each item with container Waffle Cone
3. Emit one line per distinct resource, ordered flavors first, then toppings, then cones.
4. A line is `Sufficient` when `Available >= Required` and `Insufficient` otherwise.
5. A flavor, topping, or container name that is not in the catalog produces an `Unknown` line with `Available = 0`. Do not throw.
6. An item with `Quantity < 1` or an undefined size produces an `Invalid` flavor line and adds no demand.
7. `AllSufficient` is true only when the store is found, at least one line exists, and every line is `Sufficient`.
8. An empty or null item list returns `AllSufficient = false` with no lines.

### Agent tools

`InventoryTools` is a thin adapter over `IInventoryService`. It has `[Description]` attributes and is registered with `AIFunctionFactory.Create`.

| Tool name | Maps to | Notes |
|---|---|---|
| `GetAvailableInventory` | `GetAvailableInventoryAsync` | `storeId` defaults to `cmh-short-north` |
| `CheckInventory` | `CheckInventoryAsync` | Name matches `agent-instructions.md` |

Both tools are exposed because they answer different questions:

- `GetAvailableInventory` answers browsing questions such as "what's in stock?" or "which vegan flavors do you have?"
- `CheckInventory` answers "can this store fulfill this exact order?" The service adds up demand across items (shared flavor tubs, toppings, cones), so the model never does stock arithmetic itself.

## Tech Stack

- .NET 10 (`net10.0`), C# with nullable reference types and implicit usings enabled
- Microsoft.Agents.AI 1.24.0, Microsoft.Extensions.AI (`AIFunctionFactory`)
- xUnit 2.9.3 for tests (existing `froyo-fulfillment-agent.Tests` project)
- No new NuGet dependencies

## Commands

Run from `/workspaces/foundry-agents/source`:

| Purpose | Command |
|---|---|
| Build | `dotnet build froyo.slnx` |
| Test (all) | `dotnet test froyo-fulfillment-agent.Tests/froyo-fulfillment-agent.Tests.csproj` |
| Test (focused) | `dotnet test froyo-fulfillment-agent.Tests/froyo-fulfillment-agent.Tests.csproj --filter "FullyQualifiedName~InventoryService"` |
| Run locally | `dotnet run --project froyo-fulfillment-agent` |

## Project Structure

```text
source/froyo-fulfillment-agent/
  Models/       Catalog, CupSize, inventory records
  Services/     IInventoryService, InventoryService, IInventoryRepository, InMemoryInventoryRepository
  Tools/        InventoryTools (agent tool adapter)
  Program.cs    Agent tool wiring
source/froyo-fulfillment-agent.Tests/
  Services/     InventoryServiceTests, InMemoryInventoryRepositoryTests
  Tools/        InventoryToolsTests
  Fakes/        FakeInventoryRepository
```

Namespaces follow the root namespace `froyo_fulfillment_agent` (for example, `froyo_fulfillment_agent.Services`).

## Code Style

```csharp
namespace froyo_fulfillment_agent.Services;

/// <summary>
/// Applies inventory rules on top of raw stock from an <see cref="IInventoryRepository"/>.
/// </summary>
public sealed class InventoryService(IInventoryRepository repository) : IInventoryService
{
    public async Task<StoreInventory> GetAvailableInventoryAsync(
        string storeId, CancellationToken cancellationToken = default)
    {
        var stock = await repository.GetStoreStockAsync(storeId, cancellationToken);
        if (stock is null)
        {
            return StoreInventory.NotFound(storeId);
        }
        // ...
    }
}
```

- Use file-scoped namespaces, primary constructors, `sealed` classes, and immutable `record` types.
- Make every service method async and accept a `CancellationToken`.
- Add XML doc comments on public types. Add inline comments only where a rule is not obvious.
- Name tests `Method_Scenario_ExpectedResult`.

## Testing Strategy

Development follows TDD in a strict red, green, refactor loop:

1. **Red:** write one failing test for the next behavior. Run it and confirm it fails for the expected reason.
2. **Green:** write the least code that passes it.
3. **Refactor:** clean up with all tests green.

| Level | Target | Data source |
|---|---|---|
| Unit | `InventoryService` rules (behavior list above) | `FakeInventoryRepository` with small, explicit stock per test |
| Unit | `InMemoryInventoryRepository` | Real hardcoded data. Asserts every store has every catalog item, and at least one item is short for demo purposes |
| Unit | `InventoryTools` | Fake service. Asserts delegation, default store, and tool names and descriptions via `AIFunctionFactory.Create` |
| Manual | Agent run in Program.cs | `dotnet run`, then send a request from `froyo-fulfillment-agent.http` |

Service tests never depend on the hardcoded stock values, so changing seed data cannot break rule tests.

## Boundaries

- **Always:** write a failing test before production code. Keep tests green before moving to the next behavior. Keep the catalog consistent with `docs/froyo-menu.md`.
- **Ask first:** adding NuGet packages, changing `agent-instructions.md`, changing the `AddFoundryResponses` hosting approach, adding reservation or decrement behavior.
- **Never:** put pricing or allergen logic in this service, throw exceptions to the agent for bad names, or edit unrelated projects (`froyo-agent`, `sundae-support-agent`).

## Success Criteria

- [x] `IInventoryService` and `IInventoryRepository` exist with the contract above.
- [x] Every behavior rule above has at least one unit test, and all tests pass.
- [x] `InMemoryInventoryRepository` provides stock for all three stores and every catalog item.
- [x] Program.cs creates `InMemoryInventoryRepository`, `InventoryService`, and `InventoryTools` once and passes the tools to the agent.
- [x] The agent has the `GetAvailableInventory` and `CheckInventory` tools.
- [x] `dotnet build froyo.slnx` succeeds with no new warnings.
- [x] Replacing the data source requires only a new `IInventoryRepository` implementation and a one-line change in Program.cs.

## Not Doing (and Why)

- **Reservations or stock decrement:** PlaceOrder owns this, and it is a separate feature.
- **FindInStockAlternatives:** this is a separate tool and depends on this service.
- **Allergens and prices:** these belong to menu search and ValidateOrder.
- **Config or JSON seeded stock:** hardcoded data is enough until a real data source exists.
- **Database implementation:** the seam is in place; the implementation is future work.

## Decisions

1. **Agent construction.** The agent is built before `builder.Build()`, so Program.cs creates the repository, service, and `InventoryTools` directly and passes the tools to the agent. DI registration is deferred.
2. **`Quantity < 1`.** It is reported as an `Invalid` line instead of being ignored, so a bad item never reads as sufficient.
