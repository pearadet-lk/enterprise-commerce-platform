param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("docker-desktop", "minikube", "all")]
    [string]$Mode
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "deploy-config.ps1")
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")

function Remove-Overlay([string]$Name) {
    $overlay = Join-Path $RepoRoot "deploy/kubernetes/overlays/$Name"
    if (Test-Path $overlay) {
        Write-Host "Deleting overlay: $Name"
        kubectl delete -k $overlay --ignore-not-found
    }
}

switch ($Mode) {
    "docker-desktop" {
        Write-Host "Stopping Docker Desktop port-forward..."
        & (Join-Path $PSScriptRoot "port-forward-docker-desktop.ps1") -Stop
        Remove-Overlay "docker-desktop"
    }
    "minikube" {
        Write-Host "Stopping Minikube port-forward and tunnel..."
        & (Join-Path $PSScriptRoot "access-minikube.ps1") -Stop
        Remove-Overlay "minikube"
    }
    "all" {
        Write-Host "Stopping Docker Desktop port-forward..."
        & (Join-Path $PSScriptRoot "port-forward-docker-desktop.ps1") -Stop
        Write-Host "Stopping Minikube port-forward and tunnel..."
        & (Join-Path $PSScriptRoot "access-minikube.ps1") -Stop
        Remove-Overlay "docker-desktop"
        Remove-Overlay "minikube"
    }
}

Write-Host "Undeploy complete for mode: $Mode"
