[CmdletBinding()]
Param(
  [Parameter(Mandatory=$True)]
  [string]$RG,

  [Parameter(Mandatory=$False)]
  [string]$ResourcesPrefix="aicorr2"
)

#az login

## function App SA

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

## function App B

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
$egsubname = "funcAppAegSub"
$azsubscription= az account show | ConvertFrom-Json
$SubId = $azsubscription.Id
$checkExistingSub = az eventgrid event-subscription show --name $egsubname --source-resource-id /subscriptions/$SubId/resourceGroups/$RG/providers/Microsoft.EventGrid/topics/$topicName | ConvertFrom-Json
if ( $checkExistingSub.name -eq $egsubname ) {
    Write-Host "creating EG subscription for function app A exists" -ForegroundColor Yellow

} else {
    Write-Host "creating EG subscription for function app A"



    $EgfuncName= "ConsunmeEventGridEvent"
    # get sys key
    $resourceId  = "/subscriptions/$SubId/resourceGroups/$RG/providers/Microsoft.Web/sites/$funcAname"
    $keys=az rest --method post --uri "$resourceId/host/default/listKeys?api-version=2018-11-01" | ConvertFrom-Json
    $sysKey=$keys.systemKeys[0].eventgrid_extension
    $funcEndpoint = "https://$funcAname.azurewebsites.net/runtime/webhooks/eventgrid?functionName=$EgfuncName^^^&code=$sysKey"
    az eventgrid event-subscription create --name $egsubname --source-resource-id "/subscriptions/$SubId/resourceGroups/$RG/providers/Microsoft.EventGrid/topics/$topicName" --endpoint $funcEndpoint

}

## for testing using VS Code .http the following are required to post an event
##    az eventgrid topic key list --name $topicName -g $RG --query "key1" --output tsv 
##    az eventgrid topic show --name $topicName -g $RG --query "endpoint" --output tsv