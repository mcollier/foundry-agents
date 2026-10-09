# Implementation Plan: Simplify Fulfillment Agent

Spec: [docs/specs/SPEC-fulfillment-tools.md](../docs/specs/SPEC-fulfillment-tools.md)

## Overview

Replace the inventory service layers with a small set of tools. Menu and allergen facts come from a Foundry knowledge base, the model recommends alternatives, and code handles stock and pricing.

## Architecture decisions

- **Knowledge in markdown.** `docs/froyo-menu.md` and `docs/froyo-allergen-sheet.md` are the knowledge base. Code repeats only the facts it needs for math, and `MenuDocumentSyncTests` catches drift.
- **Inventory behind an interface.** `FroyoTools` depends on `IInventoryRepository`. `InMemoryInventoryRepository` holds demo stock, and tests pass their own small stock to it.
- **Separate tools.** `CheckStock` and `PriceOrder` answer different questions. `GetStoreStock` gives the model what it needs to pick alternatives.
- **Topping bundle per serving.** Each serving's toppings are priced in groups of 3 for $2.50, with the rest at $0.95.

## Task list

Details are in [todo.md](todo.md).

- [x] Task 1: Add prices and order validation to `FroyoMenu`
- [x] Task 2: Simplify `IInventoryRepository` to return raw stock, with `InMemoryInventoryRepository` as the implementation
- [x] Task 3: Add `FroyoTools` with `GetStoreStock`, `CheckStock`, and `PriceOrder`
- [x] Task 4: Wire `FroyoTools` in `Program.cs` and remove the service layer
- [x] Task 5: Rewrite `agent-instructions.md` for the new tools
- [ ] Task 6: Create the knowledge base and add it to the `froyo-foundry` toolbox
- [ ] Task 7: Manual run with `froyo-fulfillment-agent.http`
