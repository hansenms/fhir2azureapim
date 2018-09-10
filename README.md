Azure API Management for FHIR Service
-------------------------------------

This project contains utilities and services for deploying [Azure API Management](https://azure.microsoft.com/en-us/services/api-management/) for a FHIR Server. A live deployment of the service can be found at [https://fhir2apim.azurewebsites.net](https://fhir2apim.azurewebsites.net). 

You can also use the templates included in the repository to deploy an API Management instance for a FHIR server. If you would like to use OAuth2 security, you should create Azure Active Directory (AAD) application registrations first by following the instructions below. You will need two application registrations, one for the API itself and an application registration for any applications that will be consuming the API. The instructions below will walk through creating both.


Creating Azure Active Directory Application Registrations
---------------------------------------------------------

**Azure CLI**

```bash
#Start by defining a few variables we will need
apimInstanceName="myFhirApimInstance"
fhirApiAudience="https://myfhirapi.local"
apimInstanceReplyUrl="https://${apimInstanceName}.portal.azure-api.net/docs/services/oauthServer/console/oauth2/authorizationcode/callback"
postmanReplyUrl="https://www.getpostman.com/oauth2/callback"

#Create the app registration for the API itself:
apiAppReg=$(az ad app create --display-name ${fhirApiAudience} --identifier-uris ${fhirApiAudience})

#Grab a few details we will need
apiAppId=$(echo $apiAppReg | jq -r .appId)
apiAppScopeId=$(echo $apiAppReg | jq -r .additionalProperties.oauth2Permissions[0].id)
aadPermissions="{ \"resourceAppId\": \"00000002-0000-0000-c000-000000000000\", \"resourceAccess\": [{ \"id\": \"311a71cc-e848-46a1-bdf8-97ff7156d8e6\", \"type\": \"Scope\"}]}"
apiPermissions="{ \"resourceAppId\": \"${apiAppId}\", \"resourceAccess\": [{ \"id\": \"${apiAppScopeId}\", \"type\": \"Scope\"}]}"

apiPermissionsManifest="[${aadPermissions},${apiPermissions}]"

#Create Service Principal for API app registration
az ad sp create --id ${apiAppId}

#Create app registration for API management instance (will be used in Developer Portal)
oauthClientSecret=$(uuidgen | base64)
apiManagementAppReg=$(az ad app create --display-name ${apimInstanceName} --password ${oauthClientSecret} --identifier-uris "https://${apimInstanceName}" --required-resource-access "${apiPermissionsManifest}" --reply-urls ${apimInstanceReplyUrl} ${postmanReplyUrl})

apiManagementAppId=$(echo $apiManagementAppReg | jq -r .appId)

#Service principal for API 
az ad sp create --id ${apiManagementAppId}

#Gather some information for deployment:
oauthClientId=${apiManagementAppId}
oauthAudience=${apiAppId}
oauthTenantId=$(az account show | jq -r .tenantId)
oauthAuthority="https://login.microsoftonline.com/${oauthTenantId}"
```

**PowerShell**


Deploy Template
---------------

**Azure CLI**

This example walks through deploying with OAuth using details from Azure AD registration made above. If you would like to deploy without OAuth, just leave out arguments related to OAuth.

```bash
resourceGroupName="myfhirApim"
publisherName="My Publisher"
publisherEmail="myname@contoso.com"
fhirServerUrl="http://hapi.fhir.org/baseDstu3/"

rg=$(az group create --name ${resourceGroupName} --location eastus)

az group deployment create --resource-group ${resourceGroupName} --name myfhirapidep --template-uri https://fhir2apim.azurewebsites.net/azuredeploy.json --parameters apimServiceName="${apimInstanceName}" fhirServerUrl="${fhirServerUrl}" publisherEmail="${publisherEmail}" publisherName="${publisherName}" oauthAuthority="${oauthAuthority}" oauthClientId="${oauthClientId}" oauthClientSecret="${oauthClientSecret}" oauthAudience="${oauthAudience}" oauthAADTenantId="${oauthTenantId}"
```


**PowerShell**
