@echo off
setlocal EnableExtensions EnableDelayedExpansion

title GatewayIDE Build Script

rem ============================================================
rem  NOTE (TEMP / WILL BE REMOVED)
rem  This script currently contains a "stop all running containers"
rem  safety-clean step for local dev convenience.
rem  This is intentionally marked for future removal.
rem ============================================================

rem === Pfade (ohne "src") ===
set "ROOT=%~dp0"
set "APP=%ROOT%GatewayIDE.App"
set "SLN=%ROOT%GatewayIDE.sln"
set "CSPROJ=%APP%\GatewayIDE.App.csproj"

rem === Build-Parameter ===
set "RUNTIME=win-x64"
set "OUTDIR=%APP%\bin\Release"
set "OUTEXE=%OUTDIR%\GatewayIDE.App.exe"
set "ERR=0"

rem ============================================================
rem  CONFIG (TEMP / WILL BE REMOVED)
rem  1 = stop+remove ALL running docker containers on this machine
rem  0 = do not touch unrelated containers; only clean leona/network
rem ============================================================
set "STOP_ALL_RUNNING_CONTAINERS=1"

echo ============================================
echo   PRECHECKS
echo ============================================

rem --- 0) .NET SDK vorhanden? ---
dotnet --version >nul 2>&1 || (
  echo [ABORT] .NET SDK nicht gefunden. Bitte .NET 8+ installieren.
  set ERR=1
  goto :ABORT
)

rem --- 1) Docker erreichbar? ---
docker info >NUL 2>&1 || (
  echo [ABORT] Docker ist nicht erreichbar. Bitte Docker Desktop starten.
  pause
  exit /b 1
)

rem --- 2) TEMP: Alle laufenden Container stoppen+entfernen (WILL BE REMOVED) ---
if "%STOP_ALL_RUNNING_CONTAINERS%"=="1" (
  echo ============================================
  echo   DOCKER CLEAN (TEMP / WILL BE REMOVED)
  echo   Stoppe + Entferne ALLE laufenden Container
  echo ============================================
  call :STOP_REMOVE_ALL_RUNNING || (
    echo [FEHLER] Konnte nicht alle laufenden Container bereinigen.
    pause
    exit /b 2
  )
) else (
  echo [INFO] STOP_ALL_RUNNING_CONTAINERS=0 (nur Projekt-Container werden bereinigt)
)

rem --- 3) Projekt-Container gezielt bereinigen (leona + network) ---
echo ============================================
echo   DOCKER CLEAN (PROJECT)
echo ============================================
call :REMOVE_CONTAINER leona-container "LEONA" || (
  echo [FEHLER] LEONA-Container cleanup fehlgeschlagen
  pause
  exit /b 2
)
call :REMOVE_CONTAINER network-container "NETWORK" || (
  echo [FEHLER] NETWORK-Container cleanup fehlgeschlagen
  pause
  exit /b 2
)

echo ============================================
echo   CLEAN
echo ============================================
if exist "%APP%\obj" (
  echo [INFO] Loesche obj ...
  rmdir /s /q "%APP%\obj"
)
if exist "%APP%\bin" (
  echo [INFO] Loesche bin ...
  rmdir /s /q "%APP%\bin"
)

echo ============================================
echo   RESTORE + PUBLISH
echo ============================================

if not exist "%SLN%" (
  echo [INFO] Erzeuge Solution-Datei ...
  dotnet new sln -n GatewayIDE || goto :ABORT
)

dotnet sln "%SLN%" list | findstr /i "GatewayIDE.App" >nul
if errorlevel 1 (
  echo [INFO] Fuege Projekt zur Solution hinzu ...
  dotnet sln "%SLN%" add "%CSPROJ%" || goto :ABORT
)

dotnet restore "%CSPROJ%" || goto :ABORT

rem SingleFile, self-contained, Zielordner = %OUTDIR%
dotnet publish "%CSPROJ%" -c Release -r %RUNTIME% --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o "%OUTDIR%" || goto :ABORT

echo ============================================
echo   BUILD ERFOLGREICH
echo   Ausgabe: "%OUTEXE%"
echo ============================================

rem === Nach erfolgreichem Build: EXE starten ===
if exist "%OUTEXE%" (
  echo [RUN] Starte GatewayIDE ...
  pushd "%OUTDIR%"
  start "" "GatewayIDE.App.exe"
  popd
  exit /b 0
) else (
  echo [WARN] Konnte EXE nicht finden: "%OUTEXE%"
  goto :END
)

:ABORT
echo ============================================
echo   BUILD ABGEBROCHEN (ERR=%ERR%)
echo ============================================
echo [HINWEIS] Druecke ENTER, um das Fenster zu schliessen...
pause >nul
goto :EOF

:END
echo [HINWEIS] Druecke ENTER, um das Fenster zu schliessen...
pause >nul
endlocal
goto :EOF


rem ============================================================
rem  FUNCTIONS
rem ============================================================

:STOP_REMOVE_ALL_RUNNING
set "ANY=0"
for /f "usebackq delims=" %%i in (`docker ps -q 2^>NUL`) do (
  set "ANY=1"
  call :STOP_REMOVE_BY_ID %%i || exit /b 1
)
if "%ANY%"=="0" (
  echo [OK] Keine laufenden Container gefunden
)
exit /b 0

:STOP_REMOVE_BY_ID
set "CID=%~1"
set "CNAME="
for /f "usebackq delims=" %%n in (`docker inspect -f "{{.Name}}" %CID% 2^>NUL`) do set "CNAME=%%n"
if not defined CNAME set "CNAME=(unknown)"

rem Name kommt i.d.R. mit führendem Slash
set "CNAME=!CNAME:/=!"

echo [INFO] Stoppe Container: !CNAME!  (ID !CID!)
docker stop %CID% >NUL 2>&1

echo [INFO] Entferne Container: !CNAME!  (ID !CID!)
docker rm -f %CID% >NUL 2>&1

exit /b 0

:REMOVE_CONTAINER
set "CNAME=%~1"
set "CLABEL=%~2"
set "CID="

for /f "usebackq delims=" %%i in (`docker inspect -f "{{.Id}}" %CNAME% 2^>NUL`) do set "CID=%%i"

if not defined CID (
  echo [OK] Kein %CLABEL%-Container vorhanden (%CNAME%)
  goto :eof
)

echo [INFO] Container '%CNAME%' gefunden (ID %CID%)
echo [ACTION] Entferne Container automatisch ...
docker rm -f %CNAME% >NUL 2>&1
if errorlevel 1 (
  echo [FEHLER] Container konnte nicht entfernt werden: %CNAME%
  echo Bitte manuell mit: docker rm -f %CNAME%
  exit /b 2
)
echo [OK] Container erfolgreich entfernt: %CNAME%
goto :eof
