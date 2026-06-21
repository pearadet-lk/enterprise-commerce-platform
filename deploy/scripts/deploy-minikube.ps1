param(
    [switch]$SkipPortForward
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "deploy-config.ps1")
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
Set-Location $RepoRoot

if (-not (Get-Command minikube -ErrorAction SilentlyContinue)) {
    throw "minikube is not installed or not in PATH."
}

& minikube status | Out-Null

& (Join-Path $PSScriptRoot "check-conflicts.ps1") -TargetMode minikube
& (Join-Path $PSScriptRoot "build-images.ps1") -Target minikube

$overlay = Join-Path $RepoRoot "deploy/kubernetes/overlays/minikube"
Write-Host "Applying Kubernetes overlay: minikube (namespace $NamespaceMinikube)"
kubectl apply -k $overlay

Write-Host "Waiting for database initialization job..."
kubectl wait --for=condition=complete job/db-init -n $NamespaceMinikube --timeout=600s

Write-Host "Waiting for core deployments..."
kubectl rollout status deployment/sqlserver -n $NamespaceMinikube --timeout=300s
kubectl rollout status deployment/identity -n $NamespaceMinikube --timeout=300s
kubectl rollout status deployment/gateway -n $NamespaceMinikube --timeout=300s
kubectl rollout status deployment/angular -n $NamespaceMinikube --timeout=300s

Write-Host ""
Write-Host "$DeployRootName - Minikube deployment complete."
Write-Host "  Namespace: $NamespaceMinikube"

if (-not $SkipPortForward) {
    Write-Host ""
    Write-Host "Starting port-forward automatically..."
    try {
        & (Join-Path $PSScriptRoot "port-forward-minikube.ps1")
    }
    catch {
        Write-Warning "Auto port-forward failed: $_"
        Write-Host ""
        Write-Host "Try workarounds:"
        Write-Host "  ./deploy/scripts/access-minikube.ps1 -Method pod-forward"
        Write-Host "  ./deploy/scripts/access-minikube.ps1 -Method tunnel"
        Write-Host "  ./deploy/scripts/access-minikube.ps1 -Method diagnose"
    }
}
else {
    Write-Host ""
    Write-Host "Port-forward skipped (-SkipPortForward). Start manually:"
    Write-Host "  ./deploy/scripts/port-forward-minikube.ps1"
}

Write-Host ""
Write-Host "  Angular:   http://localhost:29200"
Write-Host "  Identity:  http://localhost:25001"
Write-Host "  Gateway:   http://localhost:27000"
Write-Host "  Jaeger:    http://localhost:16688"
Write-Host ""
Write-Host "Port-forward stops automatically on: ./deploy/scripts/undeploy.ps1 -Mode minikube"
