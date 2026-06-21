<#
.SYNOPSIS
  Enterprise Commerce Platform — root deploy/build CLI (all 3 modes).

.EXAMPLE
  ./ecp.ps1 compose build
  ./ecp.ps1 compose up
  ./ecp.ps1 desktop build
  ./ecp.ps1 desktop deploy
  ./ecp.ps1 minikube build
  ./ecp.ps1 minikube deploy
  ./ecp.ps1 minikube access
  ./ecp.ps1 minikube access -Method tunnel
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet("compose", "desktop", "minikube", "help", "")]
    [string]$Mode = "",

    [Parameter(Position = 1)]
    [ValidateSet("build", "up", "deploy", "down", "undeploy", "access", "help", "")]
    [string]$Action = "",

    [Alias("Method")]
    [ValidateSet("port-forward", "pod-forward", "tunnel", "diagnose", "urls", "manual")]
    [string]$AccessMethod = "port-forward",
    [switch]$StopAccess,
    [switch]$SkipPortForward
)

$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$Scripts = Join-Path $RepoRoot "deploy/scripts"
Set-Location $RepoRoot

function Show-Help {
    Write-Host @"

Enterprise Commerce Platform — deploy & build (run from repository root)

Usage:
  ./ecp.ps1 <mode> <action> [options]

Modes:
  compose     Docker Compose (ports 9200, 5001, 7000)
  desktop     Docker Desktop Kubernetes (ports 19200, 15001, 17000)
  minikube    Minikube Kubernetes (ports 29200, 25001, 27000)

Actions:
  build       Build Docker images only
  up          Build + start (compose) / alias for deploy
  deploy      Full deploy for the mode (minikube: auto port-forward)
  down        Stop compose stack
  undeploy    Remove Kubernetes overlay (minikube: stops port-forward)
  access      Minikube only — port-forward / tunnel workarounds

Examples:
  ./ecp.ps1 compose build
  ./ecp.ps1 compose up
  ./ecp.ps1 compose down

  ./ecp.ps1 desktop build
  ./ecp.ps1 desktop deploy          # auto port-forward after deploy
  ./ecp.ps1 desktop deploy -SkipPortForward
  ./ecp.ps1 desktop access
  ./ecp.ps1 desktop undeploy

  ./ecp.ps1 minikube build
  ./ecp.ps1 minikube deploy          # auto port-forward after deploy
  ./ecp.ps1 minikube deploy -SkipPortForward
  ./ecp.ps1 minikube access
  ./ecp.ps1 minikube access -Method tunnel
  ./ecp.ps1 minikube access -StopAccess
  ./ecp.ps1 minikube undeploy

Details: deploy/README.md

"@
}

function Invoke-Compose {
    param([string]$Command)

    switch ($Command) {
        "build" {
            & (Join-Path $Scripts "check-conflicts.ps1") -TargetMode compose
            . (Join-Path $Scripts "deploy-config.ps1")
            Use-DockerDesktopEngine
            Assert-DockerEngineReady
            docker compose build
        }
        "up" {
            & (Join-Path $Scripts "check-conflicts.ps1") -TargetMode compose
            . (Join-Path $Scripts "deploy-config.ps1")
            Use-DockerDesktopEngine
            Assert-DockerEngineReady
            docker compose up --build
        }
        "deploy" {
            & (Join-Path $Scripts "check-conflicts.ps1") -TargetMode compose
            . (Join-Path $Scripts "deploy-config.ps1")
            Use-DockerDesktopEngine
            Assert-DockerEngineReady
            docker compose up --build
        }
        "down" {
            docker compose down
        }
        default { throw "Unknown compose action: $Command. Use: build, up, deploy, down" }
    }
}

function Invoke-Desktop {
    param([string]$Command)

    switch ($Command) {
        "build" {
            & (Join-Path $Scripts "check-conflicts.ps1") -TargetMode docker-desktop
            & (Join-Path $Scripts "build-images.ps1") -Target local
        }
        "deploy" {
            if ($SkipPortForward) {
                & (Join-Path $Scripts "deploy-docker-desktop.ps1") -SkipPortForward
            }
            else {
                & (Join-Path $Scripts "deploy-docker-desktop.ps1")
            }
        }
        "access" {
            & (Join-Path $Scripts "port-forward-docker-desktop.ps1")
        }
        "undeploy" {
            & (Join-Path $Scripts "undeploy.ps1") -Mode docker-desktop
        }
        default { throw "Unknown desktop action: $Command. Use: build, deploy, access, undeploy" }
    }
}

function Invoke-Minikube {
    param([string]$Command)

    switch ($Command) {
        "build" {
            & (Join-Path $Scripts "check-conflicts.ps1") -TargetMode minikube
            & (Join-Path $Scripts "build-images.ps1") -Target minikube
        }
        "deploy" {
            if ($SkipPortForward) {
                & (Join-Path $Scripts "deploy-minikube.ps1") -SkipPortForward
            }
            else {
                & (Join-Path $Scripts "deploy-minikube.ps1")
            }
        }
        "access" {
            if ($StopAccess) {
                & (Join-Path $Scripts "access-minikube.ps1") -Stop
            }
            elseif ($AccessMethod -eq "port-forward") {
                & (Join-Path $Scripts "port-forward-minikube.ps1")
            }
            else {
                & (Join-Path $Scripts "access-minikube.ps1") -Method $AccessMethod
            }
        }
        "undeploy" {
            & (Join-Path $Scripts "undeploy.ps1") -Mode minikube
        }
        default { throw "Unknown minikube action: $Command. Use: build, deploy, access, undeploy" }
    }
}

if ($Mode -eq "" -or $Mode -eq "help" -or $Action -eq "" -or $Action -eq "help") {
    Show-Help
    exit 0
}

switch ($Mode) {
    "compose" {
        $act = if ($Action -eq "up") { "up" } else { $Action }
        Invoke-Compose -Command $act
    }
    "desktop" { Invoke-Desktop -Command $Action }
    "minikube" { Invoke-Minikube -Command $Action }
}
