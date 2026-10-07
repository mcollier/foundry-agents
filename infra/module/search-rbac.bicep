@description('Name of the existing Azure AI Search service to assign roles on.')
param searchServiceName string

@description('Resource ID of the Azure AI Search service (used only to make the role assignment names deterministic).')
param searchServiceResourceId string

@description('Principal ID of the Foundry project system-assigned managed identity to grant access to.')
param foundryProjectPrincipalId string

// Existing reference to the search service resource so role assignments can
// be scoped narrowly to it (rather than the whole resource group).
resource existingSearchService 'Microsoft.Search/searchServices@2025-05-01' existing = {
  name: searchServiceName
}

// Grant the Foundry project's managed identity access to the Azure AI Search
// service so it can be used as a Foundry IQ knowledge resource. Declared here
// (rather than the search AVM module's roleAssignments param) to avoid a
// circular module dependency: the Foundry module needs the search service's
// endpoint/resourceId as inputs, while these role assignments need the
// Foundry project's principal ID as an input.
resource searchIndexDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchServiceResourceId, foundryProjectPrincipalId, 'search-index-data-contributor')
  scope: existingSearchService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7') // Search Index Data Contributor
    principalId: foundryProjectPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource searchServiceContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchServiceResourceId, foundryProjectPrincipalId, 'search-service-contributor')
  scope: existingSearchService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0') // Search Service Contributor
    principalId: foundryProjectPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource searchIndexDataReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchServiceResourceId, foundryProjectPrincipalId, 'search-index-data-reader')
  scope: existingSearchService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f') // Search Index Data Reader
    principalId: foundryProjectPrincipalId
    principalType: 'ServicePrincipal'
  }
}
