$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$releaseDirectory = Join-Path $root "dist\ControleViagens"
$apiOutput = Join-Path $releaseDirectory "ControleViagens.Api"
$clientOutput = Join-Path $releaseDirectory "ControleViagens.Client"
$apiProject = Join-Path $root "ControleViagens.Api\ControleViagens.Api.csproj"
$clientProject = Join-Path $root "ControleViagens.Client\ControleViagens.Client.csproj"
$developmentSettings = Join-Path $root "ControleViagens.Api\appsettings.Development.json"

if (-not (Test-Path $developmentSettings)) {
    throw "Configuracao local do PostgreSQL nao encontrada: $developmentSettings"
}

dotnet publish $apiProject --configuration Release --runtime win-x64 --self-contained true --output $apiOutput
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao publicar a API."
}

# dotnet publish never deletes old fingerprinted files; start from a clean client output.
if (Test-Path $clientOutput) {
    Remove-Item $clientOutput -Recurse -Force
}

dotnet publish $clientProject --configuration Release --output $clientOutput
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao publicar a PWA."
}

$clientWebRoot = Join-Path $clientOutput "wwwroot"
$apiWebRoot = Join-Path $apiOutput "wwwroot"
if (-not (Test-Path (Join-Path $clientWebRoot "index.html"))) {
    throw "Arquivos publicados da PWA nao foram encontrados em $clientWebRoot"
}

if (Test-Path $apiWebRoot) {
    Remove-Item $apiWebRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $apiWebRoot -Force | Out-Null
Copy-Item -Path (Join-Path $clientWebRoot "*") -Destination $apiWebRoot -Recurse -Force
Copy-Item -Path $developmentSettings -Destination (Join-Path $apiOutput "appsettings.Development.json") -Force
Copy-Item -Path (Join-Path $root "Executar-ControleDeViagens.bat") -Destination (Join-Path $releaseDirectory "Executar-ControleDeViagens.bat") -Force
Copy-Item -Path (Join-Path $root "Abrir-ControleDeViagens-TelaCheia.bat") -Destination (Join-Path $releaseDirectory "Abrir-ControleDeViagens-TelaCheia.bat") -Force

Write-Output "Publicacao pronta em: $releaseDirectory"