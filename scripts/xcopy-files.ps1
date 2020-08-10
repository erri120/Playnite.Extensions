#Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process

#eg: .\xcopy-files.ps1 "M:\Projects\Playnite.Extensions\DLSiteMetadata\bin\Debug" "M:\Games\Playnite\Extensions\DLSiteMetadata"

$argsCount = $args.Count
if($args.Count -lt 2 -or $args.Count -gt 3){
    Write-Error "Script got called with $argsCount arguments, required: 3"
    exit
}

if($args.Count -eq 3){
    $debug = $args[2]
    if($debug -ne "debug") {
        Write-Error "Unknown second argument: $debug. If you want to enable debug output use debug"
        exit
    }
    $DebugPreference = "Continue"
    Write-Debug "Debug output enabled"
}

$inputFolder = $args[0]
$outputFolder = $args[1]
Write-Debug "Input: $inputFolder"
Write-Debug "Output: $outputFolder"

xcopy.exe $inputFolder\*.dll $outputFolder /Y
xcopy.exe $inputFolder\*.pdb $outputFolder /Y
xcopy.exe $inputFolder\extension.yaml $outputFolder /Y
xcopy.exe $inputFolder\icon.png $outputFolder /Y