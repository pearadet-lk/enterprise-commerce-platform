param(
    [switch]$Stop,
    [switch]$Status,
    [switch]$UsePod,
    [ValidateSet("127.0.0.1", "0.0.0.0", "localhost")]
    [string]$Address = "127.0.0.1"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "deploy-config.ps1")

function Stop-DockerDesktopPortForwards {
    if (-not (Test-Path $DockerDesktopPortForwardPidFile)) {
        Write-Host "No Docker Desktop port-forward processes recorded."
        return
    }

    $pids = @(Get-Content $DockerDesktopPortForwardPidFile | Where-Object { $_ -match '^\d+$' })
    foreach ($procId in $pids) {
        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Host "Stopping port-forward (PID $procId)..."
            Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
        }
    }

    Remove-Item $DockerDesktopPortForwardPidFile -Force -ErrorAction SilentlyContinue
    Write-Host "Docker Desktop port-forwards stopped."
}

function Show-DockerDesktopPortForwardStatus {
    if (-not (Test-Path $DockerDesktopPortForwardPidFile)) {
        Write-Host "Docker Desktop port-forward: not running"
        return
    }

    $pids = @(Get-Content $DockerDesktopPortForwardPidFile | Where-Object { $_ -match '^\d+$' })
    $alive = @($pids | ForEach-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue })
    if ($alive.Count -eq 0) {
        Write-Host "Docker Desktop port-forward: stale PID file (no processes running)"
        return
    }

    Write-Host "Docker Desktop port-forward: running ($($alive.Count) process(es))"
    foreach ($pf in $DockerDesktopPortForwards) {
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
            "-n", $NamespaceDockerDesktop,
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

function Start-DockerDesktopPortForwards {
    if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
        throw "kubectl is not installed or not in PATH."
    }

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $ns = kubectl get namespace $NamespaceDockerDesktop -o name 2>$null
    }
    finally {
        $ErrorActionPreference = $previous
    }
    if (-not $ns) {
        throw "Namespace '$NamespaceDockerDesktop' not found. Run ./deploy/scripts/deploy-docker-desktop.ps1 first."
    }

    Stop-DockerDesktopPortForwards

    $startedPids = @()
    $failed = @()

    foreach ($pf in $DockerDesktopPortForwards) {
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
            if (Test-Path $DockerDesktopPortForwardPidFile) {
                Remove-Item $DockerDesktopPortForwardPidFile -Force -ErrorAction SilentlyContinue
            }
            break
        }

        $startedPids += $proc.Id
    }

    if ($failed.Count -gt 0) {
        throw "Could not forward $($failed[0].Label) (svc/$($failed[0].Service))."
    }

    $startedPids | Set-Content $DockerDesktopPortForwardPidFile

    Write-Host ""
    Write-Host "$DeployRootName - Docker Desktop port-forward active:"
    foreach ($pf in $DockerDesktopPortForwards) {
        Write-Host "  $($pf.Label): http://localhost:$($pf.LocalPort)"
    }
    Write-Host ""
    Write-Host "Stop port-forward:"
    Write-Host "  ./deploy/scripts/port-forward-docker-desktop.ps1 -Stop"
}

if ($Stop) {
    Stop-DockerDesktopPortForwards
    exit 0
}

if ($Status) {
    Show-DockerDesktopPortForwardStatus
    exit 0
}

Start-DockerDesktopPortForwards
