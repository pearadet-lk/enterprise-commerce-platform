param(
    [ValidateSet("port-forward", "pod-forward", "tunnel", "diagnose", "urls", "manual")]
    [string]$Method = "port-forward",
    [switch]$Stop
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "deploy-config.ps1")

function Stop-MinikubeTunnel {
    if (Test-Path $MinikubeTunnelPidFile) {
        $tunnelPid = Get-Content $MinikubeTunnelPidFile -Raw
        if ($tunnelPid -match '^\d+$') {
            $proc = Get-Process -Id ([int]$tunnelPid) -ErrorAction SilentlyContinue
            if ($proc) {
                Write-Host "Stopping minikube tunnel (PID $tunnelPid)..."
                Stop-Process -Id ([int]$tunnelPid) -Force -ErrorAction SilentlyContinue
            }
        }
        Remove-Item $MinikubeTunnelPidFile -Force -ErrorAction SilentlyContinue
    }

    Get-Process -Name "minikube" -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "Note: minikube process still running (PID $($_.Id)). Stop tunnel window manually if needed."
    }
}

function Stop-AllMinikubeAccess {
    & (Join-Path $PSScriptRoot "port-forward-minikube.ps1") -Stop
    Stop-MinikubeTunnel
    Write-Host "All Minikube access helpers stopped."
}

function Get-MinikubeNodeIp {
    $ip = (& minikube ip 2>$null).Trim()
    if (-not $ip -or $ip -match "does not exist|not found") {
        return $null
    }
    return $ip
}

function Show-MinikubeUrls {
    Write-Host ""
    Write-Host "Minikube access URLs (same ports as OIDC config when using localhost):"
    Write-Host ""

    Write-Host "Option A - port-forward (recommended):"
    foreach ($pf in $MinikubePortForwards) {
        Write-Host "  $($pf.Label): http://localhost:$($pf.LocalPort)"
    }

    Write-Host ""
    Write-Host "Option B - minikube tunnel (then use localhost NodePorts):"
    foreach ($pf in $MinikubePortForwards) {
        Write-Host "  $($pf.Label): http://localhost:$($pf.NodePort)"
    }

    $nodeIp = Get-MinikubeNodeIp
    if ($nodeIp) {
        Write-Host ""
        Write-Host "Option C - Minikube node IP (may break OIDC login; localhost preferred):"
        foreach ($pf in $MinikubePortForwards) {
            Write-Host "  $($pf.Label): http://${nodeIp}:$($pf.NodePort)"
        }
    }

    Write-Host ""
    Write-Host "Option D - minikube service (opens/proxy via minikube):"
    foreach ($pf in $MinikubePortForwards) {
        Write-Host "  minikube service $($pf.Service) -n $NamespaceMinikube --url"
    }
}

function Invoke-MinikubeDiagnose {
    Write-Host "=== Minikube diagnose ($NamespaceMinikube) ==="
    Write-Host ""

    if (Get-Command minikube -ErrorAction SilentlyContinue) {
        Write-Host "--- minikube status ---"
        & minikube status
        $nodeIp = Get-MinikubeNodeIp
        if ($nodeIp) { Write-Host "Minikube IP: $nodeIp" }
    }
    else {
        Write-Warning "minikube not found in PATH"
    }

    Write-Host ""
    Write-Host "--- kubectl context ---"
    & kubectl config current-context

    Write-Host ""
    Write-Host "--- pods ---"
    & kubectl get pods -n $NamespaceMinikube

    Write-Host ""
    Write-Host "--- services ---"
    & kubectl get svc -n $NamespaceMinikube

    Write-Host ""
    Write-Host "--- local port checks ---"
    foreach ($pf in $MinikubePortForwards) {
        $inUse = Get-NetTCPConnection -LocalPort $pf.LocalPort -State Listen -ErrorAction SilentlyContinue
        if ($inUse) {
            Write-Host "  Port $($pf.LocalPort) ($($pf.Label)): IN USE (port-forward or another app may be bound)"
        }
        else {
            Write-Host "  Port $($pf.LocalPort) ($($pf.Label)): free - start port-forward or tunnel"
        }
    }

    Write-Host ""
    Write-Host "--- suggested next steps ---"
    Write-Host "  1. ./deploy/scripts/port-forward-minikube.ps1"
    Write-Host "  2. ./deploy/scripts/access-minikube.ps1 -Method tunnel"
    Write-Host "  3. ./deploy/scripts/access-minikube.ps1 -Method pod-forward"
    Write-Host "  4. Run PowerShell as Administrator (tunnel / firewall)"
    Write-Host "  5. ./deploy/scripts/access-minikube.ps1 -Method manual"
}

function Show-ManualWorkarounds {
    Write-Host ""
    Write-Host "=== Minikube manual workarounds (if scripts fail) ==="
    Write-Host ""
    Write-Host "Workaround 1 - minikube tunnel (Admin PowerShell, keep open):"
    Write-Host "  minikube tunnel"
    Write-Host "  Then open http://localhost:29200 (NodePort on localhost)"
    Write-Host ""
    Write-Host "Workaround 2 - kubectl port-forward per service (keep each terminal open):"
    foreach ($pf in $MinikubePortForwards) {
        Write-Host "  kubectl port-forward -n $NamespaceMinikube svc/$($pf.Service) $($pf.LocalPort):$($pf.RemotePort)"
    }
    Write-Host ""
    Write-Host "Workaround 3 - port-forward to deployment (if service forward fails):"
    foreach ($pf in $MinikubePortForwards) {
        Write-Host "  kubectl port-forward -n $NamespaceMinikube deployment/$($pf.Deployment) $($pf.LocalPort):$($pf.RemotePort)"
    }
    Write-Host ""
    Write-Host "Workaround 4 - bind all interfaces (firewall / VPN issues):"
    Write-Host "  ./deploy/scripts/port-forward-minikube.ps1 -Address 0.0.0.0"
    Write-Host ""
    Write-Host "Workaround 5 - minikube service proxy:"
    foreach ($pf in $MinikubePortForwards) {
        Write-Host "  minikube service $($pf.Service) -n $NamespaceMinikube --url"
    }
    Write-Host ""
    Write-Host "Workaround 6 - restart cluster and redeploy:"
    Write-Host "  minikube delete"
    Write-Host "  minikube start --cpus=4 --memory=8192"
    Write-Host "  ./deploy/scripts/deploy-minikube.ps1"
    Write-Host ""
    Write-Host "Note: OIDC/CORS expect http://localhost:29200 etc. Prefer tunnel or port-forward on those ports."
}

function Start-MinikubeTunnelAccess {
    Stop-MinikubeTunnel

    Write-Host "Starting minikube tunnel in a new window..."
    Write-Host "On Windows this often requires Administrator - accept the UAC prompt if shown."
    Write-Host "Keep the tunnel window open while using the app."
    Write-Host ""
    Write-Host "After tunnel is running, open:"
    foreach ($pf in $MinikubePortForwards) {
        Write-Host "  $($pf.Label): http://localhost:$($pf.NodePort)"
    }

    $tunnelCmd = "minikube tunnel"
    $proc = Start-Process powershell `
        -ArgumentList @("-NoExit", "-Command", $tunnelCmd) `
        -PassThru `
        -Verb RunAs

    if ($proc) {
        $proc.Id | Set-Content $MinikubeTunnelPidFile
    }

    Write-Host ""
    Write-Host "If UAC/tunnel fails, open an Admin PowerShell manually and run: minikube tunnel"
}

if ($Stop) {
    Stop-AllMinikubeAccess
    exit 0
}

switch ($Method) {
    "port-forward" {
        & (Join-Path $PSScriptRoot "port-forward-minikube.ps1")
    }
    "pod-forward" {
        & (Join-Path $PSScriptRoot "port-forward-minikube.ps1") -UsePod
    }
    "tunnel" {
        Start-MinikubeTunnelAccess
    }
    "diagnose" {
        Invoke-MinikubeDiagnose
    }
    "urls" {
        Show-MinikubeUrls
    }
    "manual" {
        Show-ManualWorkarounds
    }
}
