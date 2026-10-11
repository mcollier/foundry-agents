# Tasks: Simplify Fulfillment Agent

Spec: [docs/specs/SPEC-fulfillment-tools.md](../docs/specs/SPEC-fulfillment-tools.md). Plan: [plan.md](plan.md).

## Done

- [x] `FroyoMenu` has size prices, topping and cone prices, `ToppingsPriceFor`, and `Resolve` for order validation
- [x] `IInventoryRepository` returns raw stock per store. `InMemoryInventoryRepository` holds stock for all three stores, with demo shortages
- [x] `FroyoTools` exposes `GetStoreStock`, `CheckStock`, and `PriceOrder`
- [x] `Program.cs` creates the repository and passes it to `FroyoTools`. `IInventoryService` is removed
- [x] `agent-instructions.md` describes the knowledge base and the three tools
- [x] Tests pass and `dotnet build froyo.slnx` has no warnings

## Task 6: Knowledge base

The knowledge base was created separately in Foundry.

**Acceptance criteria:**

- [x] A Foundry IQ knowledge base contains `docs/froyo-menu.md` and `docs/froyo-allergen-sheet.md`
- [x] The knowledge base is added to the `froyo-foundry` toolbox
- [ ] Asking "Does Mint Condition contain nuts?" returns an answer grounded in the allergen sheet

## Task 7: Manual run

**Acceptance criteria:**

- [ ] `dotnet run --project froyo-fulfillment-agent` starts with the toolbox loaded
- [ ] The shortage request in `froyo-fulfillment-agent.http` gets an in-stock alternative and a price from `PriceOrder`
