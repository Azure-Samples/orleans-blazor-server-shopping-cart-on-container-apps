param appName string
param location string = resourceGroup().location

resource acr 'Microsoft.ContainerRegistry/registries@2021-09-01' = {
  name: toLower('${uniqueString(resourceGroup().id)}acr')
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

module env 'environment.bicep' = {
  name: 'containerAppEnvironment'
  params: {
    location: location
    operationalInsightsName: '${appName}-logs'
    appInsightsName: '${appName}-insights'
  }
}

var envVars = [
  {
    name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
    value: env.outputs.appInsightsInstrumentationKey
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: env.outputs.appInsightsConnectionString
  }
  {
    name: 'ORLEANS_AZURE_STORAGE_CONNECTION_STRING'
    value: storageModule.outputs.connectionString
  }
  {
    name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
    value: 'true'
  }
]

module storageModule 'storage.bicep' = {
  name: 'orleansStorageModule'
  params: {
    name: '${appName}storage'
    location: location
  }
}

module siloModule 'container-app.bicep' = {
  name: 'orleansSiloModule'
  params: {
    appName: appName
    location: location
    containerAppEnvironmentId: env.outputs.id
    registry: acr.name
    registryPassword: acr.listCredentials().passwords[0].value
    registryUsername: acr.listCredentials().username
    envVars: envVars
  }
}

output acrLoginServer string = acr.properties.loginServer
