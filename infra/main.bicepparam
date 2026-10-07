using 'main.bicep'

param environmentName =  'froyofoundryagents'

param location =  'eastus'

param principalId = readEnvironmentVariable('AZURE_PRINCIPAL_ID', '')

param principalType = 'User'

param searchLocation = 'eastus'

param tags = {
  Owner: 'mcollier'
}
