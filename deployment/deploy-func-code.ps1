[CmdletBinding()]
Param(
  [Parameter(Mandatory=$True)]
  [string]$RG,

  [Parameter(Mandatory=$False)]
  [string]$ResourcesPrefix="aicorr2"
)

#az login


## build, publish, zip and deploy functions via the cli - function name hard coded here and in the function nested ARM deploy template
dotnet publish "..\src\functionAppA\FunctionAppA.csproj"
$compress = @{
Path= "..\src\FunctionAppA\bin\Debug\netcoreapp2.2\publish\*"
CompressionLevel = "Fastest"
DestinationPath = "FunctionAppA.zip"
}
Compress-Archive @compress -Force
$funcAname = "$ResourcesPrefix" + "-fn-a"
az functionapp deployment source config-zip  -g $RG -n $funcAname --src "FunctionAppA.zip"

write-host "published function app A" -ForegroundColor Green

dotnet publish "..\src\functionAppB\FunctionAppB.csproj"
$compress = @{
Path= "..\src\FunctionAppB\bin\Debug\netcoreapp2.2\publish\*"
CompressionLevel = "Fastest"
DestinationPath = "FunctionAppB.zip"
}
Compress-Archive @compress -Force
$funcBname = $ResourcesPrefix + "-fn-b"
az functionapp deployment source config-zip  -g $RG -n $funcBname --src "FunctionAppB.zip"

write-host "published function app B" -ForegroundColor Green

## create a subscription for the demo event grid and functionA => ConsunmeEventGridEvent function
##get a key
$topicName = $ResourcesPrefix + "-egtopic"
#$key=az eventgrid topic key list --name $topicName -g $RG --query "key1" --output tsv 
#$endpoint=az eventgrid topic show --name $topicName -g $RG --query "endpoint" --output tsv
$azsubscription= az account show | ConvertFrom-Json
# get master key to get sys key
$masterKey=""
$sysKey=""
$funcEndpoint = "https://$funcAname.azurewebsites.net/runtime/webhooks/eventgrid?functionName=ConsunmeEventGridEvent&code=$sysKey"
az eventgrid event-subscription create --name "funcAppAegSub" --source-resource-id "/subscriptions/$azsubscription.id/resourceGroups/$RG/providers/Microsoft.EventGrid/topics/$topicName" --endpoint $funcEndpoint