@echo off
setlocal

curl.exe --fail --silent --show-error http://localhost:5102/ --output NUL
if errorlevel 1 (
    echo O Controle de Viagens nao esta respondendo em http://localhost:5102.
    echo Inicie Executar-ControleDeViagens.bat primeiro.
    pause
    exit /b 1
)

set "EDGE_EXE="
if exist "%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe" set "EDGE_EXE=%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe"
if not defined EDGE_EXE if exist "%ProgramFiles%\Microsoft\Edge\Application\msedge.exe" set "EDGE_EXE=%ProgramFiles%\Microsoft\Edge\Application\msedge.exe"
if not defined EDGE_EXE for /f "delims=" %%I in ('where msedge.exe 2^>nul') do if not defined EDGE_EXE set "EDGE_EXE=%%I"

if not defined EDGE_EXE (
    echo Microsoft Edge nao foi encontrado neste computador.
    pause
    exit /b 1
)

start "" "%EDGE_EXE%" --kiosk "http://localhost:5102/" --edge-kiosk-type=fullscreen --no-first-run --user-data-dir="%TEMP%\ControleViagensKiosk"