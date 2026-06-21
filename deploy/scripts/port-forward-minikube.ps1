param(
    [switch]$Stop,
    [switch]$Status,
    [switch]$UsePod,
    [ValidateSet("127.0.0.1", "0.0.0.0", "localhost")]
    [string]$Address = "127.0.0.1"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "deploy-config.ps1")

function Stop-MinikubePortForwards {
    if (-not (Test-Path $MinikubePortForwardPidFile)) {
        Write-Host "No Minikube port-forward processes recorded."
        return
    }

    $pids = @(Get-Content $MinikubePortForwardPidFile | Where-Object { $_ -match '^\d+$' })
    foreach ($procId in $pids) {
        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Host "Stopping port-forward (PID $procId)..."
            Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
        }
    }

    Remove-Item $MinikubePortForwardPidFile -Force -ErrorAction SilentlyContinue
    Write-Host "Minikube port-forwards stopped."
}

function Show-MinikubePortForwardStatus {
    if (-not (Test-Path $MinikubePortForwardPidFile)) {
        Write-Host "Minikube port-forward: not running"
        return
    }

    $pids = @(Get-Content $MinikubePortForwardPidFile | Where-Object { $_ -match '^\d+$' })
    $alive = @($pids | ForEach-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue })
    if ($alive.Count -eq 0) {
        Write-Host "Minikube port-forward: stale PID file (no processes running)"
        return
    }

    Write-Host "Minikube port-forward: running ($($alive.Count) process(es))"
    foreach ($pf in $MinikubePortForwards) {
        Write-Host "  $($pf.Label): http://localhost:$($pf.LocalPort)"
    }
}

function Start-SinglePortForward {
    param(
        [hashtable]$Forward,
        [string]$TargetKind,
        [string]$TargetName,
        [string]$BindAddress
    )

    $localTarget = "{0}:{1}" -f $Forward.LocalPort, $Forward.RemotePort
    $target = "{0}/{1}" -f $TargetKind, $TargetName
    Write-Host "Forwarding $($Forward.Label) -> ${BindAddress}:$($Forward.LocalPort) ($target)"

    $proc = Start-Process -FilePath "kubectl" `
        -ArgumentList @(
            "port-forward",
            "-n", $NamespaceMinikube,
            "--address", $BindAddress,
            $target,
            $localTarget
        ) `
        -PassThru `
        -WindowStyle Hidden

    Start-Sleep -Milliseconds 500
    return $proc
}

function Test-PortForwardAlive([System.Diagnostics.Process]$Process) {
    return $Process -and -not $Process.HasExited
}

function Start-MinikubePortForwards {
    if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
        throw "kubectl is not installed or not in PATH."
    }

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $ns = kubectl get namespace $NamespaceMinikube -o name 2>$null
    }
    finally {
        $ErrorActionPreference = $previous
    }
    if (-not $ns) {
        throw "Namespace '$NamespaceMinikube' not found. Run ./deploy/scripts/deploy-minikube.ps1 first."
    }

    Stop-MinikubePortForwards

    $startedPids = @()
    $failed = @()

    foreach ($pf in $MinikubePortForwards) {
        $proc = $null
        if (-not $UsePod) {
            $proc = Start-SinglePortForward -Forward $pf -TargetKind "svc" -TargetName $pf.Service -BindAddress $Address
            if (-not (Test-PortForwardAlive $proc)) {
                Write-Warning "Service forward failed for svc/$($pf.Service); trying deployment/$($pf.Deployment)..."
                $proc = Start-SinglePortForward -Forward $pf -TargetKind "deployment" -TargetName $pf.Deployment -BindAddress $Address
            }
        }
        else {
            $proc = Start-SinglePortForward -Forward $pf -TargetKind "deployment" -TargetName $pf.Deployment -BindAddress $Address
        }

        if (-not (Test-PortForwardAlive $proc)) {
            $failed += $pf
            foreach ($pidToStop in $startedPids) {
                Stop-Process -Id $pidToStop -Force -ErrorAction SilentlyContinue
            }
            if (Test-Path $MinikubePortForwardPidFile) {
                Remove-Item $MinikubePortForwardPidFile -Force -ErrorAction SilentlyContinue
            }
            break
        }

        $startedPids += $proc.Id
    }

    if ($failed.Count -gt 0) {
        Write-Host ""
        Write-Host "port-forward failed. Try these workarounds:" -ForegroundColor Yellow
        Write-Host "  1. ./deploy/scripts/access-minikube.ps1 -Method tunnel"
        Write-Host "  2. ./deploy/scripts/access-minikube.ps1 -Method pod-forward"
        Write-Host "  3. ./deploy/scripts/access-minikube.ps1 -Method diagnose"
        Write-Host "  4. ./deploy/scripts/access-minikube.ps1 -Method manual"
        throw "Could not forward $($failed[0].Label) (svc/$($failed[0].Service))."
    }

    $startedPids | Set-Content $MinikubePortForwardPidFile

    Write-Host ""
    Write-Host "$DeployRootName - Minikube port-forward active:"
    foreach ($pf in $MinikubePortForwards) {
        Write-Host "  $($pf.Label): http://localhost:$($pf.LocalPort)"
    }
    Write-Host ""
    Write-Host "If the browser still cannot connect, run:"
    Write-Host "  ./deploy/scripts/access-minikube.ps1 -Method tunnel"
    Write-Host ""
    Write-Host "Stop port-forward:"
    Write-Host "  ./deploy/scripts/port-forward-minikube.ps1 -Stop"
}

if ($Stop) {
    Stop-MinikubePortForwards
    exit 0
}

if ($Status) {
    Show-MinikubePortForwardStatus
    exit 0
}

Start-MinikubePortForwards
