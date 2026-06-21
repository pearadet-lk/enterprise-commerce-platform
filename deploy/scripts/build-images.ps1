param(
    [ValidateSet("local", "minikube")]
    [string]$Target = "local"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "deploy-config.ps1")
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
Set-Location $RepoRoot

$images = @(
    @{ Name = "ecp/identity-server:local"; Dockerfile = "src/backend/Identity/IdentityServer.Host/Dockerfile" },
    @{ Name = "ecp/catalog-api:local"; Dockerfile = "src/backend/Services/Catalog.API/Dockerfile" },
    @{ Name = "ecp/orders-api:local"; Dockerfile = "src/backend/Services/Orders.API/Dockerfile" },
    @{ Name = "ecp/users-api:local"; Dockerfile = "src/backend/Services/Users.API/Dockerfile" },
    @{ Name = "ecp/gateway:local"; Dockerfile = "src/backend/ApiGateway/Dockerfile" },
    @{ Name = "ecp/angular-spa:local"; Dockerfile = "src/frontend/angular-spa/Dockerfile"; Context = "src/frontend/angular-spa" }
)

if ($Target -eq "local") {
    Use-DockerDesktopEngine
    Assert-DockerEngineReady
}
else {
    if (-not (Get-Command minikube -ErrorAction SilentlyContinue)) {
        throw "minikube is not installed or not in PATH."
    }

    & minikube status | Out-Null
    & minikube docker-env | Invoke-Expression
    Assert-DockerEngineReady
}

$failed = @()
foreach ($image in $images) {
    $context = if ($image.Context) { $image.Context } else { "." }
    Write-Host "Building $($image.Name) ..."
    docker build -t $image.Name -f $image.Dockerfile $context
    if ($LASTEXITCODE -ne 0) {
        $failed += $image.Name
    }
}

if ($failed.Count -gt 0) {
    throw "Docker build failed for: $($failed -join ', '). Ensure Docker Desktop is running and retry."
}

Write-Host "Images built."

if ($Target -eq "minikube") {
    Write-Host "Images are in Minikube Docker daemon (same as cluster)."
}
