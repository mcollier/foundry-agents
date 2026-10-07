targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@maxLength(90)
@description('Name of the resource group to use or create')
param resourceGroupName string = 'rg-${environmentName}'

// Restricted locations to match list from
// https://learn.microsoft.com/en-us/azure/ai-foundry/openai/how-to/responses?tabs=python-key#region-availability
@minLength(1)
@description('Primary location for all resources')
@allowed([
  'australiaeast'
  'brazilsouth'
  'canadacentral'
  'canadaeast'
  'eastus'
  'eastus2'
  'francecentral'
  'germanywestcentral'
  'italynorth'
  'japaneast'
  'koreacentral'
  'northcentralus'
  'norwayeast'
  'polandcentral'
  'southafricanorth'
  'southcentralus'
  'southeastasia'
  'southindia'
  'spaincentral'
  'swedencentral'
  'switzerlandnorth'
  'uaenorth'
  'uksouth'
  'westus'
  'westus2'
  'westus3'
])
param location string

@description('Id of the user or app to assign application roles')
param principalId string

@description('Principal type of user or app')
param principalType string

@description('Resource tags applied to all resources.')
param tags object = {}

// param apimPublisherEmail string

// ─── Microsoft Foundry Parameters ──────────────────────────────────────────────────

// @description('Azure region for the AI Foundry account. Defaults to the main location. Override to use a region with available model quota.')
// param foundryLocation string = location

@description('The name of the OpenAI model you want to deploy')
param modelName string = 'gpt-5.6-sol'

@description('The model format of the model you want to deploy. Example: OpenAI')
param modelFormat string = 'OpenAI'

@description('The version of the model you want to deploy. Example: 2024-11-20')
param modelVersion string = '2026-07-09'

// @description('The SKU name for the model deployment. Example: GlobalStandard')
param modelSkuName string = 'GlobalStandard'

@description('The capacity of the model deployment in TPM.')
param modelCapacity int = 40

@description('Azure region for the Azure AI Search service. Can differ from the main location param based on capacity/availability of the Search service.')
param searchLocation string = 'southcentralus'

// ─── Variables ──────────────────────────────────────────────────────────────

var resourceToken = toLower(uniqueString(subscription().subscriptionId, resourceGroupName, environmentName))

// Load abbreviations
var abbrs = loadJsonContent('abbreviations.json')

// Resource names following Azure naming conventions
var foundryAccountName = '${abbrs.aiFoundryAccounts}-${resourceToken}'
var acrName = '${abbrs.containerRegistryRegistries}${resourceToken}'
var logAnalyticsWorkspaceName = '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
var appInsightsName = '${abbrs.insightsComponents}${resourceToken}'
var searchServiceName = '${abbrs.searchSearchServices}${resourceToken}'

// ═════════════════════════════════════════════════════════════════════════════
// Resource Group
// ═════════════════════════════════════════════════════════════════════════════

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

output AZURE_RESOURCE_GROUP string = rg.name

// ═════════════════════════════════════════════════════════════════════════════
// Observability (Log Analytics + Application Insights)
// ═════════════════════════════════════════════════════════════════════════════

module logAnalytics 'br/public:avm/res/operational-insights/workspace:0.11.2' = {
  scope: rg
  params: {
    name: logAnalyticsWorkspaceName
    location: rg.location
    tags: tags
    skuName: 'PerGB2018'
    dataRetention: 30
  }
}

module appInsights 'br/public:avm/res/insights/component:0.6.0' = {
  scope: rg
  params: {
    name: appInsightsName
    location: rg.location
    tags: tags
    workspaceResourceId: logAnalytics.outputs.resourceId
    applicationType: 'web'
    kind: 'web'
  }
}

// ═════════════════════════════════════════════════════════════════════════════
// Azure AI Search (Foundry IQ knowledge resource)
// ═════════════════════════════════════════════════════════════════════════════

module searchService 'br/public:avm/res/search/search-service:0.13.0' = {
  scope: rg
  params: {
    name: searchServiceName
    location: searchLocation
    tags: tags
    sku: 'standard'
    disableLocalAuth: true
    replicaCount: 1
    partitionCount: 1
    managedIdentities: {
      systemAssigned: true
    }
  }
}

// ═════════════════════════════════════════════════════════════════════════════
// Foundry Account + Project
// ═════════════════════════════════════════════════════════════════════════════

module foundryResource 'module/foundry-resource.bicep' = {
  scope: rg
  params:{
    foundryAccountName: foundryAccountName
    foundryProjectName: 'foundry-project-${resourceToken}'
    location: rg.location
    tags: tags
    modelFormat: modelFormat
    modelName: modelName
    modelVersion: modelVersion
    modelSkuName: modelSkuName
    modelCapacity: modelCapacity
    principalId: principalId
    principalType: principalType
    logAnalyticsWorkspaceResourceId: logAnalytics.outputs.resourceId
    appInsightsName: appInsightsName
    searchServiceResourceId: searchService.outputs.resourceId
    searchServiceEndpoint: searchService.outputs.endpoint
    searchServicePrincipalId: searchService.outputs.systemAssignedMIPrincipalId!
  }
  dependsOn: [
    appInsights
  ]
}

// Grant the Foundry project's managed identity access to the Azure AI Search
// service so it can be used as a Foundry IQ knowledge resource. Declared as a
// separate module (rather than the search module's roleAssignments param) to
// avoid a circular dependency: the Foundry module needs the search service's
// endpoint/resourceId as inputs, while these role assignments need the
// Foundry project's principal ID as an input.
module searchRbac 'module/search-rbac.bicep' = {
  scope: rg
  params: {
    searchServiceName: searchServiceName
    searchServiceResourceId: searchService.outputs.resourceId
    foundryProjectPrincipalId: foundryResource.outputs.foundryProjectPrincipalId
  }
}

// ═════════════════════════════════════════════════════════════════════════════
// Container Registry
// ═════════════════════════════════════════════════════════════════════════════

module containerRegistry 'br/public:avm/res/container-registry/registry:0.13.0' = {
  scope: rg
  params: {
    name: acrName
    location: rg.location
    tags: tags
    acrSku: 'Basic'
    acrAdminUserEnabled: false
    roleAssignments: [
      {
        principalId: principalId
        principalType: principalType
        roleDefinitionIdOrName: 'AcrPull'
      }
      {
        principalId: foundryResource.outputs.foundryPrincipalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: 'AcrPull'
      }
    ]
  }
}

output AZURE_AI_ACCOUNT_ID string = foundryResource.outputs.foundryAccountId
output AZURE_AI_PROJECT_ID string = foundryResource.outputs.foundryProjectId
output AZURE_AI_FOUNDRY_PROJECT_ID string = foundryResource.outputs.foundryProjectId
output AZURE_AI_ACCOUNT_NAME string = foundryResource.outputs.foundryAccountName
output AZURE_AI_PROJECT_NAME string = foundryResource.outputs.foundryProjectName
output AZURE_AI_PROJECT_ENDPOINT string = foundryResource.outputs.foundryProjectEndpoint
output FOUNDRY_PROJECT_ENDPOINT string = foundryResource.outputs.foundryProjectEndpoint
output AZURE_OPENAI_ENDPOINT string = foundryResource.outputs.openAiEndpoint
output APPLICATIONINSIGHTS_CONNECTION_STRING string = appInsights.outputs.connectionString
output APPLICATIONINSIGHTS_RESOURCE_ID string = appInsights.outputs.resourceId
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.outputs.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = logAnalytics.outputs.logAnalyticsWorkspaceId
output AZURE_AI_SEARCH_SERVICE_NAME string = searchService.outputs.name

// module apim 'module/apim.bicep' = {
//   scope: rg
//   params:{
//     apimName: 'apim-${resourceToken}'
//     location: rg.location
//     publisherEmail: apimPublisherEmail
//     publisherName: '${foundryAccountName} AI Gateway'
//   }
// }
