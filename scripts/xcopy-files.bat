Rem ./xcopy-files.bat "M:\Projects\Playnite.Extensions\DLSiteMetadata\bin\Debug" "M:\Games\Playnite\Extensions\DLSiteMetadata"

set inputFolder=%1
set outputFolder=%2

echo %inputFolder%
echo %outputFolder%

xcopy %inputFolder%\*.dll %outputFolder% /Y
xcopy %inputFolder%\*.pdb %outputFolder% /Y
xcopy %inputFolder%\extension.yaml %outputFolder% /Y
xcopy %inputFolder%\icon.png %outputFolder% /Y