# Implementation Plan: Inventory Service

Spec: [docs/specs/SPEC-inventory-service.md](../docs/specs/SPEC-inventory-service.md)

## Overview

Add an `IInventoryService` to the Froyo Fulfillment Agent that reports per-store stock and checks whether a store can fulfill a list of order items. Raw stock comes from an `IInventoryRepository`, starting with a hardcoded in-memory implementation. Two agent tools, `GetAvailableInventory` and `CheckInventory`, expose the service to the agent in Program.cs. Every task follows a red, green, refactor loop.

## Architecture Decisions

- **Three parts.** A static `FroyoCatalog` holds menu facts. `IInventoryRepository` holds raw stock. `InventoryService` holds the rules. Swapping the data source touches only the repository.
- **Ounces for flavors.** All sizes draw from one store-level quantity per flavor. Servings per size are derived, never stored.
- **Never throw to the agent.** Unknown stores and names, and invalid quantities, are reported as statuses in the result.
- **Service tests use a fake repository.** Rule tests never depend on hardcoded seed data.
- **No DI yet.** Program.cs creates the repository, service, and tools directly before building the agent.
- **Enums as strings.** `CupSize` and `StockStatus` use `JsonStringEnumConverter`, so tool schemas and results show names such as `"Megabyte"` instead of numbers.

## Dependency Graph

```text
FroyoCatalog, CupSize
    |
    +-- Inventory models, IInventoryRepository
            |
            +-- InventoryService.GetAvailableInventoryAsync
            |       |
            |       +-- InventoryService.CheckInventoryAsync (flavors, then toppings, cones, invalid)
            |
            +-- InMemoryInventoryRepository (seed data)
                    |
InventoryService ---+-- InventoryTools
                            |
                            +-- Program.cs wiring
```

## Task List

Detailed tasks with acceptance criteria are in [todo.md](todo.md).

### Phase 1: Foundation

- [x] Task 1: Catalog and cup sizes
- [x] Task 2: GetAvailableInventoryAsync with a fake repository

### Checkpoint: Foundation

- [x] All tests pass and the solution builds

### Phase 2: Order checking

- [x] Task 3: CheckInventoryAsync for flavors
- [x] Task 4: CheckInventoryAsync for toppings, cones, unknown names, and invalid quantities

### Checkpoint: Service complete

- [x] All service behavior rules in the spec have tests and pass
- [ ] Review with human before wiring

### Phase 3: Data and agent integration

- [ ] Task 5: InMemoryInventoryRepository seed data
- [ ] Task 6: InventoryTools agent adapter
- [ ] Task 7: Wire tools into the agent in Program.cs

### Checkpoint: Complete

- [ ] All spec success criteria met
- [ ] Ready for review

## Parallelization

Task 5 depends only on Task 2, so it could run alongside Tasks 3 and 4. Task 6 needs the order models from Tasks 3 and 4. Tasks 3 and 4 change the same method and must run in order. Because this is a small feature, the plan runs all tasks one after another.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| `AIFunctionFactory` serializes enums as numbers, so the model sends invalid sizes | Medium | Add a string enum converter. Assert the tool JSON schema contains `"Byte"` in Task 6 |
| A manual agent run needs Foundry endpoint and toolbox configuration | Medium | Automated success relies on unit tests and build. The manual run is a best-effort check using existing appsettings |
| Catalog drifts from `docs/froyo-menu.md` | Low | Task 1 tests assert the exact flavor and topping lists |
| The test project references a web SDK project | Low | Confirm it builds during the Task 1 red step |

## Open Questions

None. Spec decisions are recorded in the spec.
