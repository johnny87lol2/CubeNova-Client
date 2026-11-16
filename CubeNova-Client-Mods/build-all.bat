@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

REM Builds every CubeNova Client mod (all CubeNova-Client-<version> folders) one after another.
REM Each jar is copied to ..\CubeNova\builtin and to the jars folder next to this file.
REM A failed version does not stop the others.
set PAUSECMD=echo.
set OK=
set FAIL=

for %%V in (1.20.1 1.21.1 1.21.4 26.2) do (
  echo.
  echo ============================================================
  echo   Building CubeNova Client %%V
  echo ============================================================
  if exist "%~dp0CubeNova-Client-%%V\build-mod.bat" (
    call "%~dp0CubeNova-Client-%%V\build-mod.bat"
    if errorlevel 1 (set "FAIL=!FAIL! %%V") else (set "OK=!OK! %%V")
  ) else (
    echo Folder CubeNova-Client-%%V not found, skipped.
  )
)

echo.
echo ============================================================
echo   Finished
echo   Built OK :!OK!
if defined FAIL (echo   FAILED   :!FAIL!) else (echo   FAILED   : none)
echo ============================================================
echo Jars:
if exist "%~dp0jars" dir /b "%~dp0jars\*.jar"
echo.
pause
if defined FAIL exit /b 1
