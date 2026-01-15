@echo off
setlocal EnableExtensions EnableDelayedExpansion

:START
cls
title GatewayIDE Build Script

rem ============================================================
rem  CONFIG
rem ============================================================

set "ROOT=%~dp0"
set "APP=%ROOT%GatewayIDE.App"
set "CSPROJ=%APP%\GatewayIDE.App.csproj"

set "RUNTIME=win-x64"
set "OUTDIR=%APP%\bin\Release"
set "OUTEXE=%OUTDIR%\GatewayIDE.App.exe"

set "STOP_ALL_RUNNING_CONTAINERS=1"

echo ============================================
echo   GATEWAY IDE BUILD
echo ============================================
echo.

rem ============================================================
rem  PRECHECKS
rem ============================================================

dotnet --version >nul 2>&1 || (
  echo [ABORT] .NET SDK nicht gefunden.
  goto :RETRY
)

docker info >NUL 2>&1 || (
  echo [ABORT] Docker nicht erreichbar.
  goto :RETRY
)

rem ============================================================
rem  DOCKER CLEAN (TEMP)
rem ============================================================

if "%STOP_ALL_RUNNING_CONTAINERS%"=="1" (
  echo [INFO] Stoppe laufende Docker-Container (TEMP)
  for /f %%i in ('docker ps -q') do (
    docker stop %%i >NUL 2>&1
    docker rm -f %%i >NUL 2>&1
  )
)

docker rm -f leona-container >NUL 2>&1
docker rm -f network-container >NUL 2>&1

rem ============================================================
rem  CLEAN
rem ============================================================

if exist "%APP%\obj" rmdir /s /q "%APP%\obj"
if exist "%APP%\bin" rmdir /s /q "%APP%\bin"

rem ============================================================
rem  RESTORE + PUBLISH (Release)
rem ============================================================

dotnet restore "%CSPROJ%" || goto :RETRY

dotnet publish "%CSPROJ%" ^
  -c Release ^
  -r %RUNTIME% ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o "%OUTDIR%" || goto :RETRY

echo.
echo ============================================
echo   BUILD ERFOLGREICH
echo ============================================
echo.

if not exist "%OUTEXE%" (
  echo [FEHLER] EXE nicht gefunden:
  echo %OUTEXE%
  goto :RETRY
)

echo ============================================
echo   RUN MODUS
echo   [R] Release EXE starten
echo   [D] Debug Run (zeigt Exception/Stacktrace)
echo ============================================
set "MODE="
set /p MODE="Bitte waehlen (R/D): "

if /I "%MODE%"=="D" goto :RUN_DEBUG
goto :RUN_RELEASE

:RUN_RELEASE
echo.
echo [RUN] Release: %OUTEXE%
echo.
pushd "%OUTDIR%"
GatewayIDE.App.exe
echo.
echo [EXITCODE] %ERRORLEVEL%
popd
goto :RETRY

:RUN_DEBUG
echo.
echo [RUN] Debug: dotnet run -c Debug
echo.
dotnet run -c Debug --project "%CSPROJ%"
echo.
echo [EXITCODE] %ERRORLEVEL%
goto :RETRY

:RETRY
echo.
echo ============================================
echo   Druecken Sie eine beliebige Taste,
echo   um den Build-Prozess zu wiederholen
echo ============================================
pause >nul
goto :START
