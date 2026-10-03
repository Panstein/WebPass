@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Publicar-ControleDeViagens.ps1"
if errorlevel 1 (
    echo.
    echo A publicacao falhou.
    pause
    exit /b 1
)
echo.
echo Pacote pronto em dist\ControleViagens
