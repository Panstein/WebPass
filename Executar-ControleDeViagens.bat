@echo off
setlocal
set "APP_DIR=%~dp0ControleViagens.Api"

if not exist "%APP_DIR%\ControleViagens.Api.exe" (
    echo Publicacao nao encontrada. Execute Publicar-ControleDeViagens.bat primeiro.
    pause
    exit /b 1
)

set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5102"
pushd "%APP_DIR%"
echo Controle de Viagens sera iniciado em http://localhost:5102
echo Pressione Ctrl+C nesta janela para encerrar.
echo.
"%APP_DIR%\ControleViagens.Api.exe"
set "APP_EXIT_CODE=%ERRORLEVEL%"
popd
if not "%APP_EXIT_CODE%"=="0" pause
exit /b %APP_EXIT_CODE%