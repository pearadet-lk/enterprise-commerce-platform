param(
    [switch]$SkipPortForward
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "deploy-config.ps1")
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
Set-Location $RepoRoot

function Assert-DockerDesktopPrerequisites {
    Use-DockerDesktopEngine
    Assert-DockerEngineReady

    if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
        throw "kubectl is not installed or not in PATH."
    }

    $contexts = @(kubectl config get-contexts -o name 2>$null)
    if ($contexts -notcontains "docker-desktop") {
        throw "kubectl context 'docker-desktop' not found. Enable Kubernetes in Docker Desktop (Settings -> Kubernetes -> Enable Kubernetes)."
    }

    $current = kubectl config current-context 2>$null
    if ($current -ne "docker-desktop") {
        Write-Host "Switching kubectl context to docker-desktop..."
        kubectl config use-context docker-desktop | Out-Null
    }

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        kubectl cluster-info 2>$null | Out-Null
    }
    finally {
        $ErrorActionPreference = $previous
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Desktop Kubernetes is not running. Enable it in Settings -> Kubernetes and wait until it shows Running."
    }
}

Assert-DockerDesktopPrerequisites

& (Join-Path $PSScriptRoot "check-conflicts.ps1") -TargetMode docker-desktop
& (Join-Path $PSScriptRoot "build-images.ps1") -Target local

$overlay = Join-Path $RepoRoot "deploy/kubernetes/overlays/docker-desktop"
Write-Host "Applying Kubernetes overlay: docker-desktop (namespace $NamespaceDockerDesktop)"
$applyOutput = kubectl apply -k $overlay 2>&1 | Out-String
Write-Host $applyOutput.TrimEnd()
if ($applyOutput -match "Error from server") {
    throw "kubectl apply failed. Fix the errors above and retry."
}

Write-Host "Waiting for database initialization job..."
kubectl wait --for=condition=complete job/db-init -n $NamespaceDockerDesktop --timeout=600s

Write-Host "Waiting for core deployments..."
kubectl rollout status deployment/sqlserver -n $NamespaceDockerDesktop --timeout=300s
kubectl rollout status deployment/identity -n $NamespaceDockerDesktop --timeout=300s
kubectl rollout status deployment/gateway -n $NamespaceDockerDesktop --timeout=300s
kubectl rollout status deployment/angular -n $NamespaceDockerDesktop --timeout=300s

if (-not $SkipPortForward) {
    Write-Host ""
    Write-Host "Starting port-forward automatically..."
    try {
        & (Join-Path $PSScriptRoot "port-forward-docker-desktop.ps1")
    }
    catch {
        Write-Warning "Auto port-forward failed: $_"
        Write-Host ""
        Write-Host "Start port-forward manually:"
        Write-Host "  ./deploy/scripts/port-forward-docker-desktop.ps1"
    }
}
else {
    Write-Host ""
    Write-Host "Port-forward skipped (-SkipPortForward). Start manually:"
    Write-Host "  ./deploy/scripts/port-forward-docker-desktop.ps1"
}

Write-Host ""
Write-Host "$DeployRootName - Docker Desktop Kubernetes deployment complete."
Write-Host "  Namespace: $NamespaceDockerDesktop"
Write-Host "  Angular:   http://localhost:19200"
Write-Host "  Identity:  http://localhost:15001"
Write-Host "  Gateway:   http://localhost:17000"
Write-Host "  Jaeger:    http://localhost:16687"
Write-Host ""
Write-Host "Port-forward stops automatically on: ./deploy/scripts/undeploy.ps1 -Mode docker-desktop"
