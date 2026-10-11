# My Demo Notes

1. Show the Foundry portal

## Prompt Agent

### Knowledge base

- Already created the knowledge base. Two files:
  - docs/froyo-allergen-sheet.md
  - docs/froyo-menu.md

### Portal

- Create a new agent in the portal
  - Set the model
  - Set the instructions from agent-instructions.md
  - Set the knowledge base

### Existing agent

**froyo-sundae-support**

### Client console app

.NET console app to connect to the **froyo-sundae-support** agent.

1. show the code (`/source/sundae-support-client`)
1. run interactively (`dotnet run`)
    - starts with a "Hi froyo-sundae-support" message
    - "Which flavors are vegan?"
    - "What are the hours of the Columbus location?"
1. show observability features in Foundry portal
    - Go to **Traces**, then **Conversation**

## Hosted Agent

1. Walk through the code (`source/froyo-fulfillment-agent`)
1. Deploy through AZD (`azd deploy`).
1. Run locally via `azd ai agent run`
    - Will launch a local playground
    - "I would like 2 Megabyte pistachios, with hot fudge from your short north location"
    - Notice the tool calls
        - local tools
        - knowledge base via Foundry Toolbox
        - notice there is a shortage and agent recommends alternatives
    - "What are the ingredients?"
1. Open seperate terminal
    - Interact with agent via `azd ai agent invoke --local "What are the hours of the short north location"`
1. Deploy through GitHub workflow
