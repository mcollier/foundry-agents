# Microsoft Foundry Agent Demo

This repository contains .NET examples for building agents with Microsoft Foundry and the Microsoft Agent Framework. The main deployment path is the Froyo Fulfillment hosted agent, supported by Bicep infrastructure and Azure Developer CLI configuration.

## Agents and infrastructure

- **Froyo Fulfillment** (`source/froyo-fulfillment-agent/`) is the hosted agent configured in `azure.yaml`. It answers order questions using Foundry knowledge and tools for stock checks and pricing. Its demo inventory is in memory.
- **Froyo recommendation** (`source/froyo-agent/`) is a separate hosted-agent example that recommends flavors from a small in-code flavor list.
- **Sundae Support** (`source/sundae-support-agent/` and `source/sundae-support-client/`) shows creating a Foundry agent version and connecting to it from a console client.
- **Azure infrastructure** (`infra/`) uses Bicep to configure a Foundry account and project, model deployment, Azure AI Search, monitoring, a container registry, and related access assignments.

The fulfillment agent uses the [menu](docs/froyo-menu.md) and [allergen sheet](docs/froyo-allergen-sheet.md) as knowledge sources. Its responsibilities, tools, and domain rules are described in [the fulfillment tools specification](docs/specs/SPEC-fulfillment-tools.md). The [Sundae Support instructions](docs/sundae-support-instructions.md) describe that sample's behavior, and the [Froyo recommendation README](source/froyo-agent/README.md) has its local run instructions.

## Prerequisites

- .NET 10 SDK
- Azure CLI and Azure Developer CLI (`azd`)
- Bicep CLI, available through Azure CLI with `az bicep`
- An Azure subscription with permission to create and configure the resources in `infra/`
- For running or deploying the hosted agents, access to a Foundry project and a configured model deployment; the fulfillment agent also requires its Foundry toolbox

## Build and test

Run these commands from the repository root:

```sh
dotnet build source/froyo-fulfillment-agent/froyo-fulfillment-agent.csproj --configuration Release
dotnet test source/froyo-fulfillment-agent.Tests/froyo-fulfillment-agent.Tests.csproj --configuration Release
```

The test project covers fulfillment menu rules, inventory behavior, and agent tools. The repository CI workflow is defined in [`.github/workflows/ci.yml`](.github/workflows/ci.yml).

## Provision and deploy

Authenticate to Azure and create an Azure Developer CLI environment:

```sh
az login
azd auth login
azd env new <environment-name>
```

Before provisioning, review [`infra/main.bicepparam`](infra/main.bicepparam) and [`azure.yaml`](azure.yaml), and verify that Azure CLI is using the intended subscription. The parameter file sets the resource environment name and location and reads `AZURE_PRINCIPAL_ID` from the process environment for access assignments. Set it to the object ID of the user or service principal being granted access, and adjust `principalType` when using a service principal:

```sh
export AZURE_PRINCIPAL_ID="<principal-object-id>"
```

Update the parameter values for your environment and identity. The hosted agent also needs its Foundry project endpoint, model deployment name, and toolbox name configured for its environment.

Provision the infrastructure and deploy the service configured in `azure.yaml`:

```sh
azd provision
azd deploy
```

The hosted-agent workflow in [`.github/workflows/hosted-agent-cd.yml`](.github/workflows/hosted-agent-cd.yml) shows the GitHub Actions deployment and smoke-test configuration. Provisioning and deployment create Azure resources and may incur charges.

## Run the fulfillment agent locally

Configure `FOUNDRY_PROJECT_ENDPOINT`, `AZURE_AI_MODEL_DEPLOYMENT_NAME`, and `FOUNDRY_TOOLBOX_NAME` for a Foundry project that you can access. Authenticate locally with Azure CLI, Visual Studio, or Visual Studio Code credentials.

```sh
dotnet run --project source/froyo-fulfillment-agent
```

## Observability

The fulfillment agent emits OpenTelemetry traces, metrics, and logs through the [Agent Framework observability](https://learn.microsoft.com/agent-framework/agents/observability?pivots=programming-language-csharp) instrumentation. `Program.cs` wraps the agent with `UseOpenTelemetry` and adds its telemetry to the pipeline that `AgentHost` already registers.

| Item | Value |
| ---- | ----- |
| Activity source and meter | `froyo-fulfillment-agent` |
| Spans | `invoke_agent`, `chat`, and `execute_tool` following the GenAI semantic conventions |
| Metrics | `gen_ai.client.operation.duration` and `gen_ai.client.token.usage` |

Exporters depend on the environment:

| Environment | Exporter | How it's enabled |
| ----------- | -------- | ---------------- |
| Foundry hosted agent | Azure Monitor (Application Insights) | Foundry injects `APPLICATIONINSIGHTS_CONNECTION_STRING` from the project's Application Insights connection. No configuration is needed. |
| Any | OTLP | Set `OTEL_EXPORTER_OTLP_ENDPOINT`. |
| Local `Development` | Console | Set `ASPNETCORE_ENVIRONMENT=Development`. Traces, metrics, and logs are written to standard output. |

To find traces for the hosted agent:

- In the Foundry portal, open the agent and select **Traces**.
- In Application Insights, query the `dependencies` table where `cloud_RoleName == "froyo-fulfillment-agent"` and `name startswith "invoke_agent"`.

> [!WARNING]
> Sensitive data capture is turned on (`EnableSensitiveData = true`). Prompts, responses, tool arguments, and tool results, including customer and order details, are stored in Application Insights. Restrict access to the Application Insights resource, or set `EnableSensitiveData` to `false` in `Program.cs` for production.

## Repository layout

| Path | Purpose |
| --- | --- |
| `infra/` | Bicep infrastructure modules and deployment parameters |
| `source/froyo-fulfillment-agent/` | Hosted order and fulfillment agent |
| `source/froyo-fulfillment-agent.Tests/` | Unit tests for fulfillment menu, inventory, and tools |
| `source/froyo-agent/` | Flavor recommendation agent sample |
| `source/sundae-support-agent/` | Foundry support-agent creation sample |
| `source/sundae-support-client/` | Console client for the support agent |
| `docs/` | Menu, allergen, agent guidance, and specifications |
| `azure.yaml` | Azure Developer CLI project and service configuration |
| `.github/workflows/` | Build, infrastructure, and hosted-agent workflows |
| `.devcontainer/` | Development container configuration |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution and validation guidance.

## License

This project is licensed under the [MIT License](LICENSE).
