# Tasks: Inventory Service

Spec: [docs/specs/SPEC-inventory-service.md](../docs/specs/SPEC-inventory-service.md). Plan: [plan.md](plan.md).

Every task follows red, green, refactor: write one failing test, confirm it fails for the expected reason, make it pass with minimal code, then clean up.

Commands (run from `source/`):

- Focused tests: `dotnet test froyo-fulfillment-agent.Tests/froyo-fulfillment-agent.Tests.csproj --filter "FullyQualifiedName~<Name>"`
- All tests: `dotnet test froyo-fulfillment-agent.Tests/froyo-fulfillment-agent.Tests.csproj`
- Build: `dotnet build froyo.slnx`

## Task 1: Catalog and cup sizes

**Description:** Add `CupSize` and the static `FroyoCatalog` with flavors, toppings, containers, store IDs, size ounces, and case-insensitive name resolution to canonical names.

**Acceptance criteria:**

- [x] `FroyoCatalog.OuncesFor` returns Byte 6, Kilobyte 10, Megabyte 14, Terabyte 16
- [x] The flavor and topping lists exactly match `docs/froyo-menu.md` (10 each), and containers are Cup and Waffle Cone
- [x] `TryResolveFlavor`, `TryResolveTopping`, `TryResolveContainer`, and `TryResolveStore` match case-insensitively, trim whitespace, and return canonical names

**Verification:**

- [x] Tests pass: filter `FroyoCatalog`
- [x] Build succeeds

**Dependencies:** None

**Files likely touched:**

- `source/froyo-fulfillment-agent/Models/CupSize.cs`
- `source/froyo-fulfillment-agent/Models/FroyoCatalog.cs`
- `source/froyo-fulfillment-agent.Tests/Models/FroyoCatalogTests.cs`

**Estimated scope:** Small

## Task 2: GetAvailableInventoryAsync with a fake repository

**Description:** Add `IInventoryRepository`, `StoreStock`, `StoreInventory`, `FlavorAvailability`, `StockLevel`, `IInventoryService`, and `InventoryService.GetAvailableInventoryAsync`. Covers spec GetAvailableInventoryAsync rules 1 to 4. `CheckInventoryAsync` stays unimplemented until Task 3.

**Acceptance criteria:**

- [x] An unknown or blank store returns `StoreFound = false` with empty lists, without throwing
- [x] Every catalog flavor, topping, and cone is returned, and items missing from stock report 0
- [x] `ServingsBySize` uses floor division (100 oz gives 16, 10, 7, 6)

**Verification:**

- [x] Tests pass: filter `InventoryService`
- [x] Build succeeds

**Dependencies:** Task 1

**Files likely touched:**

- `source/froyo-fulfillment-agent/Models/InventoryModels.cs`
- `source/froyo-fulfillment-agent/Services/IInventoryRepository.cs`
- `source/froyo-fulfillment-agent/Services/IInventoryService.cs`
- `source/froyo-fulfillment-agent/Services/InventoryService.cs`
- `source/froyo-fulfillment-agent.Tests/Fakes/FakeInventoryRepository.cs`
- `source/froyo-fulfillment-agent.Tests/Services/InventoryServiceTests.cs`

**Estimated scope:** Medium

## Checkpoint: Foundation

- [x] All tests pass
- [x] `dotnet build froyo.slnx` succeeds

## Task 3: CheckInventoryAsync for flavors

**Description:** Add `OrderItem`, `InventoryCheckResult`, `InventoryCheckLine`, `StockStatus`, and `ItemType`. Implement flavor demand across all items. Covers spec CheckInventoryAsync rules 1, 2 (flavors), 3, 4, 7, and 8.

**Acceptance criteria:**

- [x] An unknown store, or an empty or null item list, returns `AllSufficient = false` with no lines
- [x] Items with the same flavor are summed into one line (for example, 2 Megabyte plus 1 Terabyte requires 44 oz)
- [x] Lines are `Sufficient` when available is at least required, otherwise `Insufficient`, and `AllSufficient` reflects all lines

**Verification:**

- [x] Tests pass: filter `InventoryService`

**Dependencies:** Task 2

**Files likely touched:**

- `source/froyo-fulfillment-agent/Models/InventoryModels.cs`
- `source/froyo-fulfillment-agent/Services/InventoryService.cs`
- `source/froyo-fulfillment-agent.Tests/Services/InventoryServiceTests.cs`

**Estimated scope:** Small

## Task 4: CheckInventoryAsync for toppings, cones, unknown names, and invalid quantities

**Description:** Extend the check with topping portions and waffle cones, Cup as untracked, `Unknown` lines for names not in the catalog, and `Invalid` lines for `Quantity < 1`. Covers spec CheckInventoryAsync rules 2 (toppings and cones), 3 (ordering), 5, and 6.

**Acceptance criteria:**

- [x] Toppings (including repeats) and waffle cones are summed across items, and Cup produces no line
- [x] Unknown flavor, topping, or container names produce `Unknown` lines with `Available = 0`, without throwing
- [x] `Quantity < 1` produces an `Invalid` flavor line, adds no demand, and makes `AllSufficient` false
- [x] Lines are ordered flavors, then toppings, then cones

**Verification:**

- [x] Tests pass: filter `InventoryService`

**Dependencies:** Task 3

**Files likely touched:**

- `source/froyo-fulfillment-agent/Services/InventoryService.cs`
- `source/froyo-fulfillment-agent.Tests/Services/InventoryServiceTests.cs`

**Estimated scope:** Small

## Checkpoint: Service complete

- [x] Every service behavior rule in the spec has a test, and all tests pass
- [x] Review with human before integration

## Task 5: InMemoryInventoryRepository seed data

**Description:** Add hardcoded stock for all three stores, including at least one shortage per store for demos.

**Acceptance criteria:**

- [x] Every store has an entry for every catalog flavor, topping, and Waffle Cone
- [x] An unknown store returns null, and store IDs match case-insensitively
- [x] At least one item per store is below one Terabyte (16 oz) or at 0 portions

**Verification:**

- [x] Tests pass: filter `InMemoryInventoryRepository`

**Dependencies:** Task 2

**Files likely touched:**

- `source/froyo-fulfillment-agent/Services/InMemoryInventoryRepository.cs`
- `source/froyo-fulfillment-agent.Tests/Services/InMemoryInventoryRepositoryTests.cs`

**Estimated scope:** Small

## Task 6: InventoryTools agent adapter

**Description:** Add `InventoryTools`, a thin adapter over `IInventoryService` with `[Description]` attributes and a method that returns the two `AIFunction`s named `GetAvailableInventory` and `CheckInventory`.

**Acceptance criteria:**

- [x] The tools delegate to the service, and `storeId` defaults to `cmh-short-north`
- [x] The `AIFunction` names are exactly `GetAvailableInventory` and `CheckInventory`, and each has a description
- [x] The `CheckInventory` JSON schema exposes `CupSize` as string names (contains `"Byte"`)

**Verification:**

- [x] Tests pass: filter `InventoryTools`

**Dependencies:** Task 4

**Files likely touched:**

- `source/froyo-fulfillment-agent/Tools/InventoryTools.cs`
- `source/froyo-fulfillment-agent.Tests/Tools/InventoryToolsTests.cs`
- `source/froyo-fulfillment-agent.Tests/Fakes/FakeInventoryService.cs`

**Estimated scope:** Small

## Task 7: Wire tools into the agent in Program.cs

**Description:** In Program.cs, create `InMemoryInventoryRepository`, `InventoryService`, and `InventoryTools`, and pass the two tools to `AsAIAgent`.

**Acceptance criteria:**

- [x] The agent's `tools` list includes `GetAvailableInventory` and `CheckInventory`
- [x] No other Program.cs behavior changes

**Verification:**

- [x] All tests pass
- [x] `dotnet build froyo.slnx` succeeds with no new warnings
- [ ] Manual check (best effort): `dotnet run --project froyo-fulfillment-agent`, then ask "What's in stock at Short North?" from `froyo-fulfillment-agent.http`. Partial: app startup verified with placeholder config; a live model call needs Foundry credentials

**Dependencies:** Tasks 5 and 6

**Files likely touched:**

- `source/froyo-fulfillment-agent/Program.cs`

**Estimated scope:** Extra small

## Checkpoint: Complete

- [x] All spec success criteria met
- [x] Ready for review
