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