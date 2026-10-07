param foundryAccountName string
param foundryProjectName string
param location string
param tags object
param modelName string
param modelFormat string
param modelVersion string
param modelSkuName string
param modelCapacity int
param principalId string
param principalType string

@description('Resource ID of the Log Analytics workspace to route Foundry account diagnostic logs and metrics to.')
param logAnalyticsWorkspaceResourceId string

@description('Name of the Application Insights component to connect to the Foundry account for tracing/observability.')
param appInsightsName string

@description('Resource ID of the Azure AI Search service to connect to the Foundry project (Foundry IQ knowledge resource).')
param searchServiceResourceId string

@description('Endpoint URL of the Azure AI Search service to connect to the Foundry project.')
param searchServiceEndpoint string

@description('Principal ID of the Azure AI Search service system-assigned managed identity, granted Cognitive Services User on this Foundry account.')
param searchServicePrincipalId string

// Existing reference to the App Insights component so the Monitoring Reader
// role can be assigned directly on it (required for the Foundry portal's
// trace viewer, separate from the ApiKey-based connection below).
resource existingAppInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource foundryResource 'Microsoft.CognitiveServices/accounts@2026-05-15-preview' = {
  name: foundryAccountName
  location: location
  tags: tags
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'S0'
  }
  properties:{
    allowProjectManagement: true
    customSubDomainName: foundryAccountName
    networkAcls: {
      defaultAction: 'Allow'
    }
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
  
  resource project 'projects' = {
    name: foundryProjectName
    location: location
    identity: {
      type: 'SystemAssigned'
    }
    properties:{
      description: '${foundryProjectName} Project'
      displayName: '${foundryProjectName}Project'
    }

    resource searchConnection 'connections' = {
      name: 'search-connection'
      properties: {
        category: 'CognitiveSearch'
        target: searchServiceEndpoint
        authType: 'AAD'
        isSharedToAll: true
        metadata: {
          ApiType: 'Azure'
          ResourceId: searchServiceResourceId
        }
      }
    }
  }

  resource modelDeployment 'deployments' = {
    name: modelName
    sku: {
      name: modelSkuName
      capacity: modelCapacity
    }
    properties:{
      model: {
        name: modelName
        format: modelFormat
        version: modelVersion
      }
    }
  }

  resource appInsightsConnection 'connections' = {
    name: 'appinsights-connection'
    properties: {
      category: 'AppInsights'
      target: existingAppInsights.id
      authType: 'ApiKey'
      isSharedToAll: true
      credentials: {
        key: existingAppInsights.properties.ConnectionString
      }
      metadata: {
        ApiType: 'Azure'
        ResourceId: existingAppInsights.id
      }
    }
  }
}

// Route Foundry account logs and metrics to the Log Analytics workspace
resource foundryDiagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-to-law'
  scope: foundryResource
  properties: {
    workspaceId: logAnalyticsWorkspaceResourceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

// Assign Foundry User Role to the specified principal (user or app) at the
// project scope, for calling models via the Foundry project endpoint.
resource foundryUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryResource.id, principalId, 'foundry-user-role-assignment')
  scope: foundryResource::project
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '53ca6127-db72-4b80-b1b0-d745d6d5456d') // Foundry User Role Definition ID
    principalId: principalId
    principalType: principalType
  }
}

// Also assign Foundry User Role at the account scope, so Entra ID auth works
// for calls made directly against the account-level endpoint (not just the
// project endpoint) — RBAC does not inherit upward from project to account.
resource foundryUserAccountRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryResource.id, principalId, 'foundry-user-account-role-assignment')
  scope: foundryResource
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '53ca6127-db72-4b80-b1b0-d745d6d5456d') // Foundry User Role Definition ID
    principalId: principalId
    principalType: principalType
  }
}

// Assign Foundry User Role at the account scope to the project's own
// managed identity. This is a documented minimum requirement (Microsoft
// Learn: "Assign the Foundry User role on your Foundry resource to your
// project's managed identity") — without it, the project isn't authorized
// to broker Entra ID authentication for model calls, surfacing as
// "You don't have permission to use the Entra ID authentication" in the
// Foundry portal.
resource foundryUserForProjectIdentityRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryResource.id, foundryResource::project.id, 'foundry-user-for-project-identity')
  scope: foundryResource
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '53ca6127-db72-4b80-b1b0-d745d6d5456d') // Foundry User Role Definition ID
    principalId: foundryResource::project.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Grant the Foundry project's managed identity Monitoring Reader on App
// Insights so the Foundry portal's trace viewer can read trace data.
resource appInsightsMonitoringReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(existingAppInsights.id, foundryResource::project.id, 'monitoring-reader')
  scope: existingAppInsights
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '43d0d8ad-25c7-4714-9337-8ba259a9fe05') // Monitoring Reader
    principalId: foundryResource::project.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Grant the Azure AI Search service's system-assigned managed identity
// Cognitive Services User on this Foundry account, so Search can call back
// into Foundry (e.g. for vectorization/embeddings during indexing).
resource cognitiveServicesUserForSearchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryResource.id, searchServicePrincipalId, 'cognitive-services-user-for-search')
  scope: foundryResource
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908') // Cognitive Services User
    principalId: searchServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

@description('The principal ID of the Foundry account system-assigned managed identity.')
output foundryPrincipalId string = foundryResource.identity.principalId

@description('The principal ID of the Foundry project system-assigned managed identity.')
output foundryProjectPrincipalId string = foundryResource::project.identity.principalId

@description('The resource ID of the Foundry account.')
output foundryAccountId string = foundryResource.id

@description('The resource ID of the Foundry project.')
output foundryProjectId string = foundryResource::project.id

@description('The name of the Foundry account.')
output foundryAccountName string = foundryResource.name

@description('The name of the Foundry project.')
output foundryProjectName string = foundryResource::project.name

@description('The Foundry project endpoint used by Foundry SDKs and azd agent commands.')
output foundryProjectEndpoint string = foundryResource::project.properties.endpoints['AI Foundry API']

@description('The OpenAI-compatible endpoint for the Foundry account.')
output openAiEndpoint string = foundryResource.properties.endpoints['OpenAI Language Model Instance API']
