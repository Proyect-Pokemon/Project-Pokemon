$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host " PROJECT POKEMON AUTOWEBDEPLOY"
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# --------------------------------------------------
# Comprobacion de herramientas
# --------------------------------------------------

if (!(Get-Command pnpm -ErrorAction SilentlyContinue))
{
    throw "pnpm no esta instalado o no esta en PATH"
}

if (!(Get-Command dotnet -ErrorAction SilentlyContinue))
{
    throw "dotnet no esta instalado o no esta en PATH"
}

if (!(Get-Command msbuild -ErrorAction SilentlyContinue))
{
    throw "msbuild no esta disponible en PATH"
}

# --------------------------------------------------
# Configuracion
# --------------------------------------------------

$Root = Join-Path $env:USERPROFILE "Documents\repos\Project-Pokemon"

$ConfigFile = Join-Path $PSScriptRoot "Config\production.json"

if (!(Test-Path $ConfigFile))
{
    throw "No existe Config\production.json"
}

$config = Get-Content $ConfigFile -Raw | ConvertFrom-Json

$ClientPath = Join-Path $Root "client\project-pokemon"
$ServerPath = Join-Path $Root "server\ProjectPokemon"
$ApiProject = Join-Path $ServerPath "ProjectPokemon"

$LaunchSettings = Join-Path $ApiProject "Properties\launchSettings.json"
$EnvironmentFile = Join-Path $ClientPath "src\app\environments\environment.ts"

$Dist = Join-Path $ClientPath "dist\project-pokemon\browser"
$WwwRoot = Join-Path $ApiProject "wwwroot"

function Step($Text)
{
    Write-Host ""
    Write-Host "------------------------------------------------" -ForegroundColor Yellow
    Write-Host $Text -ForegroundColor Yellow
    Write-Host "------------------------------------------------" -ForegroundColor Yellow
}

# --------------------------------------------------
# launchSettings.json
# --------------------------------------------------

Step "Actualizando launchSettings.json"

$launch = Get-Content $LaunchSettings -Raw

$launch =
$launch -replace '"JWT_KEY"\s*:\s*".*?"',
('"JWT_KEY": "' + $config.JWT_KEY + '"')

$launch =
$launch -replace '"DB_CONNECTION"\s*:\s*".*?"',
('"DB_CONNECTION": "' + $config.DB_CONNECTION + '"')

$launch =
$launch -replace '"GOOGLE_CLIENT_ID"\s*:\s*".*?"',
('"GOOGLE_CLIENT_ID": "' + $config.GOOGLE_CLIENT_ID + '"')

Set-Content $LaunchSettings $launch

# --------------------------------------------------
# environment.ts
# --------------------------------------------------

Step "Actualizando environment.ts"

$envText = Get-Content $EnvironmentFile -Raw

$envText =
$envText -replace "apiUrl:\s*'.*?'",
("apiUrl: '" + $config.API_URL + "'")

$envText =
$envText -replace "wsUrl:\s*'.*?'",
("wsUrl: '" + $config.WS_URL + "'")

$envText =
$envText -replace "GOOGLE_CLIENT_ID:\s*'.*?'",
("GOOGLE_CLIENT_ID: '" + $config.GOOGLE_CLIENT_ID + "'")

Set-Content $EnvironmentFile $envText

# --------------------------------------------------
# Angular
# --------------------------------------------------

Step "Compilando Angular"

Set-Location $ClientPath

pnpm install
if ($LASTEXITCODE -ne 0)
{
    throw "pnpm install ha fallado"
}

pnpm run build
if ($LASTEXITCODE -ne 0)
{
    throw "Angular build ha fallado"
}

if (!(Test-Path "$Dist\index.html"))
{
    throw "No se encontro dist\project-pokemon\browser\index.html"
}

# --------------------------------------------------
# wwwroot
# --------------------------------------------------

Step "Limpiando wwwroot"

if (!(Test-Path $WwwRoot))
{
    New-Item -ItemType Directory -Path $WwwRoot | Out-Null
}

Get-ChildItem $WwwRoot -Force -ErrorAction SilentlyContinue |
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Step "Copiando Angular a wwwroot"

Copy-Item "$Dist\*" $WwwRoot -Recurse -Force

if (!(Test-Path "$WwwRoot\index.html"))
{
    throw "No se copio correctamente el frontend a wwwroot"
}

# --------------------------------------------------
# Backend
# --------------------------------------------------

Step "Compilando Backend"

Set-Location $ServerPath

dotnet clean
if ($LASTEXITCODE -ne 0)
{
    throw "dotnet clean ha fallado"
}

dotnet restore
if ($LASTEXITCODE -ne 0)
{
    throw "dotnet restore ha fallado"
}

# --------------------------------------------------
# Web Deploy
# --------------------------------------------------

Step "Ejecutando Web Deploy"

msbuild $ApiProject\ProjectPokemon.csproj `
/p:Configuration=Release `
/p:DeployOnBuild=true `
/p:PublishProfile=$($config.WEBDEPLOY_PROFILE) `
/p:UserName=$($config.WEBDEPLOY_USER) `
/p:Password=$($config.WEBDEPLOY_PASSWORD)

if ($LASTEXITCODE -ne 0)
{
    throw "Web Deploy ha fallado"
}

# --------------------------------------------------
# Final
# --------------------------------------------------

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host " AUTOWEBDEPLOY COMPLETADO CON EXITO" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""

Write-Host "Frontend copiado a: $WwwRoot" -ForegroundColor Green
Write-Host "Publish generado en: $PublishFolder" -ForegroundColor Green
Write-Host "Perfil utilizado: $($config.WEBDEPLOY_PROFILE)" -ForegroundColor Green
Write-Host ""