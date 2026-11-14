@echo off
setlocal
cd /d "%~dp0"
REM Tools are kept outside the project so they survive replacing the project folder
set TOOLS=%LOCALAPPDATA%\CubeNovaTools
if exist "%~dp0.tools\jdk\bin\javac.exe" if not exist "%TOOLS%\jdk\bin\javac.exe" (
  echo Reusing tools from the old .tools folder ...
  if not exist "%TOOLS%" mkdir "%TOOLS%"
  move "%~dp0.tools\jdk" "%TOOLS%\jdk" >nul
  if exist "%~dp0.tools\gradle" move "%~dp0.tools\gradle" "%TOOLS%\gradle" >nul
)
set JDKDIR=%TOOLS%\jdk
set GRADLEDIR=%TOOLS%\gradle

if not exist "%TOOLS%" mkdir "%TOOLS%"

if not exist "%JDKDIR%\bin\javac.exe" (
  echo Downloading JDK 21 ...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; Invoke-WebRequest 'https://api.adoptium.net/v3/binary/latest/21/ga/windows/x64/jdk/hotspot/normal/eclipse' -OutFile '%TOOLS%\jdk.zip'; if (Test-Path '%TOOLS%\jdk_tmp') { Remove-Item -Recurse -Force '%TOOLS%\jdk_tmp' }; Expand-Archive '%TOOLS%\jdk.zip' '%TOOLS%\jdk_tmp' -Force; $d = Get-ChildItem '%TOOLS%\jdk_tmp' | Select-Object -First 1; if (Test-Path '%JDKDIR%') { Remove-Item -Recurse -Force '%JDKDIR%' }; Move-Item $d.FullName '%JDKDIR%'; Remove-Item -Recurse -Force '%TOOLS%\jdk_tmp'; Remove-Item '%TOOLS%\jdk.zip'"
)
if not exist "%JDKDIR%\bin\javac.exe" (echo JDK download failed & pause & exit /b 1)

if not exist "%GRADLEDIR%\bin\gradle.bat" (
  echo Downloading Gradle 8.11.1 ...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; Invoke-WebRequest 'https://services.gradle.org/distributions/gradle-8.11.1-bin.zip' -OutFile '%TOOLS%\gradle.zip'; if (Test-Path '%TOOLS%\gr_tmp') { Remove-Item -Recurse -Force '%TOOLS%\gr_tmp' }; Expand-Archive '%TOOLS%\gradle.zip' '%TOOLS%\gr_tmp' -Force; $d = Get-ChildItem '%TOOLS%\gr_tmp' | Select-Object -First 1; if (Test-Path '%GRADLEDIR%') { Remove-Item -Recurse -Force '%GRADLEDIR%' }; Move-Item $d.FullName '%GRADLEDIR%'; Remove-Item -Recurse -Force '%TOOLS%\gr_tmp'; Remove-Item '%TOOLS%\gradle.zip'"
)
if not exist "%GRADLEDIR%\bin\gradle.bat" (echo Gradle download failed & pause & exit /b 1)

set JAVA_HOME=%JDKDIR%
set PATH=%JDKDIR%\bin;%GRADLEDIR%\bin;%PATH%

echo Building mod ...
call gradle build --no-daemon
if errorlevel 1 (echo Build failed & pause & exit /b 1)

if not exist "..\CubeNova\builtin" mkdir "..\CubeNova\builtin"
copy /y "build\libs\cubenova-client-*.jar" "..\CubeNova\builtin\" >nul
echo.
echo Done. Jar copied to CubeNova\builtin
pause
