#Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process

#eg: .\update-version-number.ps1 1.3.1 debug
#this will update all versions to 1.3.1 and enable debug output

$argsCount = $args.Count
if($args.Count -lt 1 -or $args.Count -gt 2){
    Write-Error "Script got called with $argsCount arguments, required: 1"
    exit
}

if($args.Count -eq 2){
    $debug = $args[1]
    if($debug -ne "debug") {
        Write-Error "Unknown second argument: $debug. If you want to enable debug output use debug"
        exit
    }
    $DebugPreference = "Continue"
    Write-Debug "Debug output enabled"
}

$version = $args[0]
Write-Debug "Version: $version"

$startLocation = Get-Location

Set-Location ..
 
$items = Get-ChildItem -Depth 1 -Filter extension.yaml -Name | ForEach-Object {$_}
$itemsCount = $items.Length
Write-Host "Found $itemsCount extensions.yaml files"
Write-Debug "Items: $items"

foreach ($item in $items) {
    Write-Host "Reading content of $item"
    $content = Get-Content -Path $item
    $versionLine = $content[2]
    Write-Debug "Current version: $versionLine"
    $content[2] = "Version: $version"
    $versionLine = $content[2]
    Write-Debug "New version: $versionLine"
    Out-File -FilePath $item -InputObject $content
    Write-Host "Wrote content to $item"
}

Set-Location $startLocation