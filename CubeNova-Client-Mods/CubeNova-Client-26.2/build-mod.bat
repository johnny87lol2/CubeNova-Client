@echo off
setlocal
cd /d "%~dp0"
REM Tools are kept outside the project so they survive replacing the project folder
set TOOLS=%LOCALAPPDATA%\CubeNovaTools
set JDKDIR=%TOOLS%\jdk25
set GRADLEDIR=%TOOLS%\gradle95

if not exist "%TOOLS%" mkdir "%TOOLS%"

if not exist "%JDKDIR%\bin\javac.exe" (
  echo Downloading JDK 25 ...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; Invoke-WebRequest 'https://api.adoptium.net/v3/binary/latest/25/ga/windows/x64/jdk/hotspot/normal/eclipse' -OutFile '%TOOLS%\jdk25.zip'; if (Test-Path '%TOOLS%\jdk25_tmp') { Remove-Item -Recurse -Force '%TOOLS%\jdk25_tmp' }; Expand-Archive '%TOOLS%\jdk25.zip' '%TOOLS%\jdk25_tmp' -Force; $d = Get-ChildItem '%TOOLS%\jdk25_tmp' | Select-Object -First 1; if (Test-Path '%JDKDIR%') { Remove-Item -Recurse -Force '%JDKDIR%' }; Move-Item $d.FullName '%JDKDIR%'; Remove-Item -Recurse -Force '%TOOLS%\jdk25_tmp'; Remove-Item '%TOOLS%\jdk25.zip'"
)
if not exist "%JDKDIR%\bin\javac.exe" (echo JDK download failed & pause & exit /b 1)

if not exist "%GRADLEDIR%\bin\gradle.bat" (
  echo Downloading Gradle 9.5.1 ...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; Invoke-WebRequest 'https://services.gradle.org/distributions/gradle-9.5.1-bin.zip' -OutFile '%TOOLS%\gradle95.zip'; if (Test-Path '%TOOLS%\gr_tmp') { Remove-Item -Recurse -Force '%TOOLS%\gr_tmp' }; Expand-Archive '%TOOLS%\gradle95.zip' '%TOOLS%\gr_tmp' -Force; $d = Get-ChildItem '%TOOLS%\gr_tmp' | Select-Object -First 1; if (Test-Path '%GRADLEDIR%') { Remove-Item -Recurse -Force '%GRADLEDIR%' }; Move-Item $d.FullName '%GRADLEDIR%'; Remove-Item -Recurse -Force '%TOOLS%\gr_tmp'; Remove-Item '%TOOLS%\gradle95.zip'"
)
if not exist "%GRADLEDIR%\bin\gradle.bat" (echo Gradle download failed & pause & exit /b 1)

set JAVA_HOME=%JDKDIR%
set PATH=%JDKDIR%\bin;%GRADLEDIR%\bin;%PATH%

echo Building mod ...
call gradle build --no-daemon
if errorlevel 1 (echo Build failed & pause & exit /b 1)

if not exist "..\..\CubeNova\builtin" mkdir "..\..\CubeNova\builtin"
copy /y "build\libs\cubenova-client-*.jar" "..\..\CubeNova\builtin\" >nul
if not exist "..\jars" mkdir "..\jars"
copy /y "build\libs\cubenova-client-*.jar" "..\jars\" >nul
echo.
echo Done. Jar copied to CubeNova\builtin and CubeNova-Client-Mods\jars
pause
