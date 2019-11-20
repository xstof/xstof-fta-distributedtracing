[CmdletBinding()]
Param(
  [Parameter(Mandatory=$True)]
  [string]$RG,

  [Parameter(Mandatory=$False)]
  [string]$ResourcesPrefix="aicorr2"
)

$Location = "westeurope"
$StorageAccountNameForNestedTemplates = "$($ResourcesPrefix)storacct"
$NestedTemplatesStorageContainerName = "nestedtemplates"

# create RG
az group create -n $RG -l $Location

# create storage account in RG to deploy nested templates towards
Write-Output "Creating Storage Account for storing nested arm templates"
az storage account create -g $RG -n $StorageAccountNameForNestedTemplates -l $Location --sku Standard_LRS
Write-Output "Storage Account created"

# upload nested templates
Write-Output "Creating storage container for nested templates: '$NestedTemplatesStorageContainerName'"
az storage container create -n $NestedTemplatesStorageContainerName --account-name $StorageAccountNameForNestedTemplates
Write-Output "Storage container created"
Write-Output "Uploading nested templates"
az storage blob upload-batch --account-name $StorageAccountNameForNestedTemplates -d $NestedTemplatesStorageContainerName -s "./nestedTemplates" --pattern "*.json"
Write-Output "Templates uploaded"

# create sas token
$SasTokenForNestedTemplates = az storage container generate-sas --account-name $StorageAccountNameForNestedTemplates -n $NestedTemplatesStorageContainerName  --permissions r --expiry (Get-Date).AddMinutes(180).ToString("yyyy-MM-dTH:mZ")
Write-Output "Sas-token for accessing nested templates: $SasTokenForNestedTemplates"

$NestedTemplatesLocation = "https://$StorageAccountNameForNestedTemplates.blob.core.windows.net"

# deploy template
$templateFile = "deploy.json"

az group deployment create -n "appinsights-corrtest-deployment" -g $RG --template-file "$templateFile" --parameters _artifactsLocation=$NestedTemplatesLocation _artifactsLocationSasToken=$SasTokenForNestedTemplates resourcesPrefix=$ResourcesPrefix
