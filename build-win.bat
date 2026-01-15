@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem ============================================================
rem  KEEP OPEN: wenn per Doppelklick gestartet -> cmd /k Relaunch
rem ============================================================
if /I "%~1" NEQ "__KEEP" (
  echo [INFO] Relaunch in persistent console
  cmd /k ""%~f0" __KEEP"
  exit /b
)
shift

rem ============================================================
rem  FIND PROJECT ROOT (walk up until GatewayIDE.App\GatewayIDE.App.csproj exists)
rem  ROOT wird am Ende immer der Ordner mit der csproj: ...\GatewayIDE.App\
rem ============================================================
set "ROOT="
set "CUR=%~dp0"

:ROOT_LOOP
rem Case A: Script liegt direkt im Projektordner (GatewayIDE.App\)
if exist "%CUR%GatewayIDE.App.csproj" (
  set "ROOT=%CUR%"
  goto ROOT_FOUND
)

rem Case B: Script liegt im Repo-Root (IDE\) -> Projekt liegt in Unterordner GatewayIDE.App\
if exist "%CUR%GatewayIDE.App\GatewayIDE.App.csproj" (
  set "ROOT=%CUR%GatewayIDE.App\"
  goto ROOT_FOUND
)

set "PREV=%CUR%"
for %%P in ("%CUR%..") do set "CUR=%%~fP\"

rem Stop if we can't go higher (drive root reached)
if /I "%CUR%"=="%PREV%" goto ROOT_NOT_FOUND
goto ROOT_LOOP

:ROOT_NOT_FOUND
echo [ABORT] Konnte GatewayIDE.App.csproj nicht finden (vom Startpfad aus).
echo [INFO] Startpfad war: %~dp0
pause
exit /b 1

:ROOT_FOUND
pushd "%ROOT%" || (echo [ABORT] pushd ROOT failed & exit /b 1)

:START
cls
title GatewayIDE Build Script

rem ============================================================
rem  CONFIG
rem  ROOT ist der Ordner, in dem GatewayIDE.App.csproj liegt.
rem ============================================================
set "APP=%ROOT%"
set "CSPROJ=%ROOT%\GatewayIDE.App.csproj"

set "RUNTIME=win-x64"
set "OUTDIR=%APP%\bin\Release"
set "OUTEXE=%OUTDIR%\GatewayIDE.App.exe"

rem 0 = do nothing, 1 = stop known containers, 2 = stop ALL running containers
set "DOCKER_CLEAN_MODE=0"

rem 0 = keep bin (only obj), 1 = clean bin\Debug+Release only, 2 = nuke bin
set "CLEAN_MODE=1"

rem ============================================================
rem  LOGS + SAFE TEMP (stabil relativ zum Projektordner)
rem ============================================================
set "CRASHLOG=%ROOT%\build-win-crash.log"

set "LOGROOT=%ROOT%\bin\_buildlogs"
if not exist "%LOGROOT%" mkdir "%LOGROOT%" >nul 2>&1

set "SAFE_TEMP=%ROOT%\_msbuildtmp"
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
echo [ROOT] %ROOT%
echo [CSPROJ] %CSPROJ%
echo [LOG] %LOG%
echo [TMP] %SAFE_TEMP%
echo.

call :DOTNET_CHECK
if errorlevel 1 goto :FAIL_HARD

call :GIT_CHECK
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
  docker rm -f leona-container   >nul 2>&1
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
if /I "%MODE%"=="Q" goto :QUIT
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
if /I "%MODE%"=="Q" goto :QUIT
if /I "%MODE%"=="X" goto :RUN_DIAG
goto :RETRY


:RUN_RELEASE
echo.
echo [RUN] Release: %OUTEXE%
echo.
if not exist "%OUTEXE%" (
  echo [ABORT] Release EXE existiert nicht: %OUTEXE%
  goto :RETRY
)

pushd "%OUTDIR%" || (
  echo [ABORT] Konnte OUTDIR nicht betreten: %OUTDIR%
  goto :RETRY
)

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
rem  SUBROUTINES (PASS 3/3 - cleaned)
rem ============================================================

:DOTNET_CHECK
echo [RUN] .NET SDK check
set "DOTNET_VER="

rem 1) PATH
where dotnet >nul 2>&1
if errorlevel 1 (
  rem 2) Systemweit Standard
  if exist "C:\Program Files\dotnet\dotnet.exe" (
    set "PATH=C:\Program Files\dotnet;%PATH%"
  ) else (
    rem 3) User-local (Admin-frei)
    if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
      set "DOTNET_ROOT=%LOCALAPPDATA%\Microsoft\dotnet"
      set "PATH=%DOTNET_ROOT%;%DOTNET_ROOT%\tools;%PATH%"
    )
  )
)

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [WARN] dotnet.exe nicht gefunden
  call :DOTNET_ASK_INSTALL
  exit /b %ERRORLEVEL%
)

rem --- SDK >= 8 vorhanden?
set "HAS_SDK8PLUS=0"
for /f "usebackq tokens=1 delims= " %%s in (`dotnet --list-sdks 2^>nul`) do (
  for /f "tokens=1 delims=." %%m in ("%%s") do (
    if %%m GEQ 8 set "HAS_SDK8PLUS=1"
  )
)

if "%HAS_SDK8PLUS%" NEQ "1" (
  echo [WARN] Kein .NET SDK >= 8 gefunden
  call :DOTNET_ASK_INSTALL
  exit /b %ERRORLEVEL%
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
echo GatewayIDE.App braucht .NET SDK >= 8
echo.

set /p INSTALLDOTNET="Soll .NET 8 SDK jetzt USER-LOCAL installiert werden? (Y/N): "
if /I "%INSTALLDOTNET%" NEQ "Y" exit /b 1

set "DOTNET_ARCH=x64"
if /I "%PROCESSOR_ARCHITECTURE%"=="ARM64" set "DOTNET_ARCH=arm64"

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
  exit /b 1
)

rem Admin-frei: user-local InstallDir
set "DOTNET_INSTALL_DIR=%LOCALAPPDATA%\Microsoft\dotnet"

echo [RUN] Installiere .NET SDK 8.0 (Arch=%DOTNET_ARCH%) nach "%DOTNET_INSTALL_DIR%"
powershell -NoProfile -ExecutionPolicy Bypass -File "%INSTALLPS1%" ^
  -Channel 8.0 ^
  -Architecture %DOTNET_ARCH% ^
  -InstallDir "%DOTNET_INSTALL_DIR%" ^
  -NoPath

if errorlevel 1 (
  echo [ABORT] Installation fehlgeschlagen
  exit /b 1
)

set "DOTNET_ROOT=%DOTNET_INSTALL_DIR%"
set "PATH=%DOTNET_ROOT%;%DOTNET_ROOT%\tools;%PATH%"

dotnet --list-sdks >nul 2>&1
if errorlevel 1 (
  echo [ABORT] dotnet laeuft nach Installation nicht
  exit /b 1
)

echo [OK] .NET SDK installiert (user-local)
exit /b 0


:GIT_CHECK
echo [RUN] Git check

where git >nul 2>&1
if errorlevel 1 (
  if exist "C:\Program Files\Git\cmd\git.exe" (
    set "PATH=C:\Program Files\Git\cmd;C:\Program Files\Git\bin;%PATH%"
  ) else if exist "C:\Program Files (x86)\Git\cmd\git.exe" (
    set "PATH=C:\Program Files (x86)\Git\cmd;C:\Program Files (x86)\Git\bin;%PATH%"
  )
)

where git >nul 2>&1
if errorlevel 1 (
  echo [WARN] git.exe nicht gefunden
  call :GIT_ASK_INSTALL
  exit /b %ERRORLEVEL%
)

for /f "usebackq delims=" %%v in (`git --version 2^>^&1`) do (
  echo [OK] %%v
  exit /b 0
)

echo [ABORT] git --version gab keine Ausgabe
exit /b 1


:GIT_ASK_INSTALL
echo.
echo ============================================
echo   Git wird benoetigt
echo ============================================
echo.

set /p INSTALLGIT="Soll Git jetzt installiert werden? (Y/N): "
if /I "%INSTALLGIT%" NEQ "Y" exit /b 1

where winget >nul 2>&1
if errorlevel 1 (
  echo [ABORT] winget nicht verfuegbar. Bitte Git manuell installieren.
  exit /b 1
)

echo [RUN] winget install Git.Git
winget install --id Git.Git -e --source winget
if errorlevel 1 (
  echo [ABORT] winget Installation fehlgeschlagen oder abgebrochen
  exit /b 1
)

rem Re-check + PATH-Fallback
where git >nul 2>&1
if errorlevel 1 (
  if exist "C:\Program Files\Git\cmd\git.exe" (
    set "PATH=C:\Program Files\Git\cmd;C:\Program Files\Git\bin;%PATH%"
  )
)

where git >nul 2>&1
if errorlevel 1 (
  echo [ABORT] Git ist nach Installation nicht im PATH
  exit /b 1
)

echo [OK] Git installiert
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
echo   BUILD ABGEBROCHEN (HARD FAIL)
echo   Crashlog: %CRASHLOG%
echo   Letzter LOG: %LOG%
echo ============================================
>>"%CRASHLOG%" echo %date% %time% - HARD FAIL

if defined LOG if exist "%LOG%" (
  call :SHOW_FAIL "%LOG%"
)
goto :RETRY


:RETRY
echo.
echo ============================================
echo   Taste druecken zum Neustart
echo   (oder Fenster schliessen)
echo ============================================
pause >nul

rem Ensure we are back in project root for the next loop (no dir-stack growth)
cd /d "%ROOT%" >nul 2>&1

goto :START


:QUIT
rem Cleanly unwind directory stack
popd >nul 2>&1
endlocal
exit /b 0
