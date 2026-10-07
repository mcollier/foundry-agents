# Microsoft Foundry Hosted Agent Demo

A demonstration project for using the Microsoft Foundry Hosted Agent service
with an agent built on the Microsoft Agent Framework.

## What is included

- Azure Developer CLI configuration in `azure.yaml`
- Bicep infrastructure for Microsoft Foundry and supporting Azure services
- Development container configuration
- Azure deployment planning artifacts in `.azure/`

The repository includes a .NET hosted agent in `source/froyo-agent/`. The
infrastructure and application can be developed independently while the
deployment wiring is completed.

## Prerequisites

- Azure CLI
- Azure Developer CLI (`azd`)
- Bicep CLI or the Bicep extension for Azure CLI
- An Azure subscription with permission to create the resources in `infra/`

## Provision the infrastructure

```sh
az login
azd auth login
azd env new foundrymafdemo
azd env set AZURE_LOCATION eastus
azd provision
```

Review the parameters in `infra/main.bicepparam` and the deployment plan in
`.azure/deployment-plan.md` before provisioning.

## Repository layout

| Path | Purpose |
| --- | --- |
| `infra/` | Bicep templates and deployment parameters |
| `source/froyo-agent/` | .NET hosted agent implementation |
| `azure.yaml` | Azure Developer CLI project configuration |
| `.devcontainer/` | Development container configuration |
| `.azure/` | Local Azure Developer CLI and deployment planning files |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidance.

## License

This project is licensed under the [MIT License](LICENSE).
