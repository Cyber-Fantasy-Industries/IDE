@echo off
setlocal EnableExtensions

rem ============================================================
rem  KEEP OPEN: wenn per Doppelklick gestartet -> cmd /k Relaunch
rem ============================================================
if /I "%~1" NEQ "__KEEP" (
  echo [INFO] Relaunch in persistent console
  cmd /k ""%~f0" __KEEP"
  exit /b
)
shift

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

rem 0 = do nothing, 1 = stop known containers, 2 = stop ALL running containers
set "DOCKER_CLEAN_MODE=0"

rem 0 = keep bin (only obj), 1 = clean bin\Debug+Release only, 2 = nuke bin
set "CLEAN_MODE=1"

rem ============================================================
rem  LOGS + SAFE TEMP
rem ============================================================
set "CRASHLOG=%ROOT%build-win-crash.log"

set "LOGROOT=%APP%\bin\_buildlogs"
if not exist "%LOGROOT%" mkdir "%LOGROOT%" >nul 2>&1

set "SAFE_TEMP=%ROOT%_msbuildtmp"
if not exist "%SAFE_TEMP%" mkdir "%SAFE_TEMP%" >nul 2>&1

set "TEMP=%SAFE_TEMP%"
set "TMP=%SAFE_TEMP%"

rem Timestamp (PowerShell optional; fallback to unknown)
set "TS=unknown"
for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd_HH-mm-ss" 2^>^&1`) do (
  set "TS=%%i"
  goto TS_OK
)
:TS_OK

set "LOG=%LOGROOT%\build-%TS%.log"
set "DIAGLOG=%LOGROOT%\build-%TS%-diag.log"

echo ============================================
echo   GATEWAY IDE BUILD
echo ============================================
echo.
echo [LOG] %LOG%
echo [TMP] %SAFE_TEMP%
echo.

call :DOTNET_CHECK
if errorlevel 1 goto :FAIL_HARD

if not "%DOCKER_CLEAN_MODE%"=="0" (
  call :DOCKER_CHECK
  if errorlevel 1 goto :FAIL_HARD
)

rem ============================================================
rem  DOCKER CLEAN (optional)
rem ============================================================
if "%DOCKER_CLEAN_MODE%"=="1" (
  echo [INFO] Stoppe bekannte Container (leona/network)
  docker rm -f leona-container  >nul 2>&1
  docker rm -f network-container >nul 2>&1
)

if "%DOCKER_CLEAN_MODE%"=="2" (
  echo [WARN] Stoppe ALLE laufenden Container
  for /f %%i in ('docker ps -q') do (
    docker stop %%i >nul 2>&1
    docker rm -f %%i >nul 2>&1
  )
)

rem ============================================================
rem  CLEAN
rem ============================================================
echo [INFO] Clean
if exist "%APP%\obj" rmdir /s /q "%APP%\obj" >nul 2>&1

if "%CLEAN_MODE%"=="1" (
  if exist "%APP%\bin\Debug"   rmdir /s /q "%APP%\bin\Debug"   >nul 2>&1
  if exist "%APP%\bin\Release" rmdir /s /q "%APP%\bin\Release" >nul 2>&1
) else if "%CLEAN_MODE%"=="2" (
  if exist "%APP%\bin" rmdir /s /q "%APP%\bin" >nul 2>&1
)

if not exist "%LOGROOT%" mkdir "%LOGROOT%" >nul 2>&1
if not exist "%SAFE_TEMP%" mkdir "%SAFE_TEMP%" >nul 2>&1

rem ============================================================
rem  RESTORE
rem ============================================================
echo [STEP] dotnet restore
dotnet restore "%CSPROJ%" > "%LOG%" 2>&1
if errorlevel 1 (
  echo.
  echo [FAIL] Restore fehlgeschlagen
  call :SHOW_FAIL "%LOG%"
  goto :MODE_FAIL
)

rem ============================================================
rem  PUBLISH
rem ============================================================
echo [STEP] dotnet publish (Release)
dotnet publish "%CSPROJ%" ^
  -c Release ^
  -r %RUNTIME% ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o "%OUTDIR%" >> "%LOG%" 2>&1

if errorlevel 1 (
  echo.
  echo [FAIL] Publish fehlgeschlagen
  call :SHOW_FAIL "%LOG%"
  goto :MODE_FAIL
)

echo.
echo ============================================
echo   BUILD ERFOLGREICH
echo ============================================
echo.

if not exist "%OUTEXE%" (
  echo [WARN] EXE nicht gefunden: %OUTEXE%
  echo [INFO] OUTDIR Inhalt:
  dir /b "%OUTDIR%" 2>nul
)

:MODE_OK
echo ============================================
echo   RUN MODUS
echo   [R] Release EXE starten
echo   [D] Debug Run (dotnet run -c Debug)
echo   [X] Diagnostik (dotnet build -v:diag)
echo   [Q] Quit
echo ============================================
set /p MODE="Bitte waehlen (R/D/X/Q): "
if /I "%MODE%"=="Q" goto :EOF
if /I "%MODE%"=="D" goto :RUN_DEBUG
if /I "%MODE%"=="X" goto :RUN_DIAG
goto :RUN_RELEASE

:MODE_FAIL
echo ============================================
echo   RUN MODUS (FAIL)
echo   [X] Diagnostik (dotnet build -v:diag)
echo   [R] Retry
echo   [Q] Quit
echo ============================================
set /p MODE="Bitte waehlen (X/R/Q): "
if /I "%MODE%"=="Q" goto :EOF
if /I "%MODE%"=="X" goto :RUN_DIAG
goto :RETRY

:RUN_RELEASE
echo.
echo [RUN] Release: %OUTEXE%
echo.
if not exist "%OUTEXE%" (
  echo [ABORT] Release EXE existiert nicht.
  goto :RETRY
)
pushd "%OUTDIR%"
GatewayIDE.App.exe
set "EC=%ERRORLEVEL%"
popd
echo.
echo [EXITCODE] %EC%
goto :RETRY

:RUN_DEBUG
echo.
echo [RUN] Debug: dotnet run -c Debug
echo.
dotnet run -c Debug --project "%CSPROJ%"
echo.
echo [EXITCODE] %ERRORLEVEL%
goto :RETRY

:RUN_DIAG
echo.
echo [DIAG] dotnet build -v:diag -> %DIAGLOG%
echo.
dotnet build "%CSPROJ%" -c Debug -v:diag > "%DIAGLOG%" 2>&1
if errorlevel 1 (
  echo [DIAG] Build fehlgeschlagen
  call :SHOW_FAIL "%DIAGLOG%"
) else (
  echo [DIAG] Build OK
)
goto :RETRY

rem ============================================================
rem  SUBROUTINES
rem ============================================================

:DOTNET_CHECK
echo [RUN] .NET SDK check

rem 1) Normaler PATH-Check
where dotnet >nul 2>&1
if errorlevel 1 (
  rem 2) Fallback A: User-local Install
  if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
    set "PATH=%LOCALAPPDATA%\Microsoft\dotnet;%LOCALAPPDATA%\Microsoft\dotnet\tools;%PATH%"
  ) else (
    rem 3) Fallback B: Systemweit (typisch)
    if exist "%ProgramFiles%\dotnet\dotnet.exe" (
      set "PATH=%ProgramFiles%\dotnet;%PATH%"
    )
  )
)

rem 3) Nochmal pruefen
where dotnet >nul 2>&1
if errorlevel 1 (
  echo [ABORT] dotnet.exe nicht im PATH gefunden
  echo [HINT] Installiere .NET 8+ SDK: https://aka.ms/dotnet/download
  exit /b 1
)

rem 4) Nochmal pruefen
where dotnet >nul 2>&1
if errorlevel 1 (
  echo [ABORT] dotnet.exe nicht im PATH gefunden
  echo [HINT] Installiere .NET 8+ SDK: https://aka.ms/dotnet/download
  exit /b 1
)

rem --- Check: ist ein SDK mit Major >= 8 installiert?
set "HAS_SDK8PLUS=0"
for /f "usebackq tokens=1 delims= " %%s in (`dotnet --list-sdks 2^>nul`) do (
  for /f "tokens=1 delims=." %%m in ("%%s") do (
    if %%m GEQ 8 set "HAS_SDK8PLUS=1"
  )
)

if "%HAS_SDK8PLUS%" NEQ "1" (
  echo [WARN] Kein .NET SDK >= 8 gefunden
  call :DOTNET_ASK_INSTALL
  if errorlevel 1 (
    echo [ABORT] .NET SDK >= 8 fehlt (Installation abgebrochen)
    exit /b 1
  )

  rem --- nach Installation nochmal pruefen
  set "HAS_SDK8PLUS=0"
  for /f "usebackq tokens=1 delims= " %%s in (`dotnet --list-sdks 2^>nul`) do (
    for /f "tokens=1 delims=." %%m in ("%%s") do (
      if %%m GEQ 8 set "HAS_SDK8PLUS=1"
    )
  )
  if "%HAS_SDK8PLUS%" NEQ "1" (
    echo [ABORT] .NET SDK >= 8 ist immer noch nicht verfuegbar
    echo [HINT] Bitte Installer abschliessen und Script erneut starten
    exit /b 1
  )
)

for /f "usebackq delims=" %%v in (`dotnet --version 2^>^&1`) do (
  set "DOTNET_VER=%%v"
  goto DOTNET_GOT
)
:DOTNET_GOT
if not defined DOTNET_VER (
  echo [ABORT] dotnet --version gab keine Ausgabe
  exit /b 1
)

echo [OK] dotnet %DOTNET_VER%
exit /b 0


:DOTNET_ASK_INSTALL
echo.
echo ============================================
echo   .NET SDK wird benoetigt
echo ============================================
echo GatewayIDE.App targetet .NET 8 (SDK >= 8 erforderlich)
echo Auf diesem System wurde kein SDK >= 8 gefunden
echo.

set /p INSTALLDOTNET="Soll .NET 8 SDK jetzt installiert werden? (Y/N): "
if /I "%INSTALLDOTNET%" NEQ "Y" exit /b 1

rem --------------------------------------------
rem Architektur bestimmen (x64 vs arm64)
rem --------------------------------------------
set "DOTNET_ARCH=x64"
if /I "%PROCESSOR_ARCHITECTURE%"=="ARM64" set "DOTNET_ARCH=arm64"

rem --------------------------------------------
rem Download dotnet-install.ps1 (stabiler Microsoft-Source)
rem --------------------------------------------
set "INSTALLPS1=%SAFE_TEMP%\dotnet-install.ps1"

echo [DL] Lade dotnet-install.ps1 nach: "%INSTALLPS1%"
where curl >nul 2>&1
if not errorlevel 1 (
  curl -L --fail -o "%INSTALLPS1%" "https://dot.net/v1/dotnet-install.ps1"
) else (
  powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $u='https://dot.net/v1/dotnet-install.ps1'; Invoke-WebRequest -Uri $u -OutFile '%INSTALLPS1%' -UseBasicParsing; exit 0 } catch { Write-Host $_; exit 1 }"
)
if errorlevel 1 (
  echo [ABORT] Download dotnet-install.ps1 fehlgeschlagen
  echo [HINT] Bitte manuell installieren: https://aka.ms/dotnet/download
  exit /b 1
)



rem --------------------------------------------
rem Installiere SDK Channel 8.0 fuer aktuelle Architektur
rem -InstallDir: User-local, keine Adminrechte notwendig
rem Danach PATH fuer diese Session anpassen
rem --------------------------------------------
set "DOTNET_INSTALL_DIR=%LOCALAPPDATA%\Microsoft\dotnet"

echo [RUN] Installiere .NET SDK 8.0 (Arch=%DOTNET_ARCH%) nach "%DOTNET_INSTALL_DIR%"
powershell -NoProfile -ExecutionPolicy Bypass -File "%INSTALLPS1%" ^
  -Channel 8.0 ^
  -Architecture %DOTNET_ARCH% ^
  -InstallDir "%DOTNET_INSTALL_DIR%" ^
  -NoPath

if errorlevel 1 (
  echo [ABORT] dotnet-install.ps1 Installation fehlgeschlagen
  echo [HINT] Bitte manuell installieren: https://aka.ms/dotnet/download
  exit /b 1
)

rem PATH fuer diese Session (damit dotnet sofort gefunden wird)
set "PATH=%DOTNET_INSTALL_DIR%;%DOTNET_INSTALL_DIR%\tools;%PATH%"

echo.
echo [OK] Installation durchgefuehrt.
echo [INFO] Re-Check: dotnet --info
dotnet --info
echo.
pause
exit /b 0



:DOCKER_CHECK
echo [RUN] Docker check
docker info >nul 2>&1
if errorlevel 1 (
  echo [ABORT] Docker nicht erreichbar (docker info failed)
  exit /b 1
)
echo [OK] Docker erreichbar
exit /b 0


:SHOW_FAIL
set "INLOG=%~1"
echo [LOG] %INLOG%
echo.
echo ------------------------------------------------------------
echo [ERRORS] Erste 80 Fehlerzeilen:
powershell -NoProfile -Command "if(Test-Path '%INLOG%'){ (Get-Content -LiteralPath '%INLOG%') | Select-String -Pattern ':\s*error\s|^\s*error\s' | Select-Object -First 80 | ForEach-Object { $_.Line } }"
echo ------------------------------------------------------------
echo [AXAML] XAML/Avalonia Hinweise (erste 60 Treffer):
powershell -NoProfile -Command "if(Test-Path '%INLOG%'){ (Get-Content -LiteralPath '%INLOG%') | Select-String -Pattern 'AXN0002|AVLN|XAML|axaml' | Select-Object -First 60 | ForEach-Object { $_.Line } }"
echo ------------------------------------------------------------
echo [TAIL] Letzte 160 Zeilen:
powershell -NoProfile -Command "if(Test-Path '%INLOG%'){ Get-Content -LiteralPath '%INLOG%' -Tail 160 }"
echo ------------------------------------------------------------
exit /b 0


:FAIL_HARD
echo.
echo ============================================
echo   BUILD ABGEBROCHEN
echo   Crashlog: %CRASHLOG%
echo ============================================
>>"%CRASHLOG%" echo %date% %time% - HARD FAIL
goto :RETRY


:RETRY
echo.
echo ============================================
echo   Taste druecken zum Neustart
echo   (oder Fenster schliessen)
echo ============================================
pause >nul
goto :START
