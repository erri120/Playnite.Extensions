$A = "DLSiteMetadata", "ExtensionUpdater","F95ZoneMetadata","JastusaMetadata","VNDBMetadata", "ExtensionUpdater"
      
$configuration = "$(buildConfiguration)"
$output = "$(Build.ArtifactStagingDirectory)"

foreach($element in $A) {
    $currentPath = "$(Build.SourcesDirectory)"
    $inputPath = $currentPath+"\"+$element+"\bin\"+$configuration
    xcopy.exe $inputPath\*.dll $output\$element\ /Y /c
    xcopy.exe $inputPath\*.pdb $output\$element\ /Y /c
    xcopy.exe $inputPath\extension.yaml $output\$element\ /Y /c
    xcopy.exe $inputPath\icon.png $output\$element\ /Y /c
}

exit 0