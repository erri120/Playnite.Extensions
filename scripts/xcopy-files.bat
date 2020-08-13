@echo off
Rem ./xcopy-files.bat "M:\Games\Playnite\Extensions"

set outputFolder=%1

echo %outputFolder%

set len=6
set obj[0]=DLSiteMetadata
set obj[1]=ExtensionsUpdater
set obj[2]=F95ZoneMetadata
set obj[3]=JastusaMetadata
set obj[4]=VNDBMetadata
set obj[5]=VNDBMetadata

set i=0
:loop
if %i% equ %len% goto :eof
for /f "usebackq delims== tokens=2" %%j in (`set obj[%i%]`) do (
    set extensionPath=%cd%\%%j\bin\Debug
    set outPath=%outputFolder%\%%j
    echo %i%: %extensionPath% to %outPath%
    xcopy %extensionPath%\*.dll %outPath% /Y
    xcopy %extensionPath%\*.pdb %outPath% /Y
    xcopy %extensionPath%\extension.yaml %outPath% /Y
    xcopy %extensionPath%\icon.png %outPath% /Y
)
set /a i=%i%+1
goto loop
