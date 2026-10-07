# Contributing to Microsoft Foundry Agent Demo

This is an internal demonstration project. Contributions should preserve the
clarity of the Azure deployment path and keep infrastructure changes
reviewable.

## Getting started

1. Create a feature branch:
   ```sh
   git checkout -b feat/my-feature
   ```
2. Review `.azure/deployment-plan.md`.
3. Make focused changes to the Bicep templates, parameters, or documentation.
4. Validate Bicep syntax and Azure Developer CLI configuration:
   ```sh
   az bicep build --file infra/main.bicep
   azd provision --preview
   ```

Do not commit local Azure credentials, environment files, deployment outputs,
or secrets.

## Pull request process

1. Explain the change and its effect on deployed Azure resources.
2. Update the README or deployment plan when behavior or prerequisites change.
3. Include validation results and note any checks that require an Azure
   subscription.
4. Keep commits focused and use a Conventional Commit prefix.

## Commit convention

- `feat:` new functionality
- `fix:` correction
- `docs:` documentation only
- `chore:` maintenance
- `refactor:` restructuring without behavior change

## Reporting issues

Use the [bug report template](.github/ISSUE_TEMPLATE/bug_report.yml) for
reproducible problems. Use the
[feature request template](.github/ISSUE_TEMPLATE/feature_request.yml) for
proposed improvements.
