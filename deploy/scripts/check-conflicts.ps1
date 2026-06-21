param(
    [ValidateSet("compose", "docker-desktop", "minikube")]
    [string]$TargetMode
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "deploy-config.ps1")
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")

if ($TargetMode -in @("compose", "docker-desktop")) {
    Use-DockerDesktopEngine
}

function Test-PortInUse([int]$Port) {
    $conn = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    return $null -ne $conn
}

function Test-ComposeRunning {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $names = docker ps --format "{{.Names}}" 2>$null
        if ($LASTEXITCODE -ne 0) {
            return @()
        }

        return @($names | Where-Object { $_ -like "$ComposeContainerPrefix-*" })
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

function Test-K8sNamespace([string]$Namespace) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $output = kubectl get namespace $Namespace -o name 2>$null
        if ($LASTEXITCODE -ne 0) {
            return $false
        }

        return -not [string]::IsNullOrWhiteSpace($output)
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

Write-Host "Checking deployment conflicts for mode: $TargetMode (root: $DeployRootName)"

$composeRunning = Test-ComposeRunning
if ($composeRunning -and $TargetMode -ne "compose") {
    throw "Docker Compose mode is running ($($composeRunning -join ', ')). Run 'docker compose down' first."
}

if ($TargetMode -eq "compose") {
    $k8sDesktop = Test-K8sNamespace $NamespaceDockerDesktop
    $k8sMinikube = Test-K8sNamespace $NamespaceMinikube
    if ($k8sDesktop -or $k8sMinikube) {
        throw "Kubernetes namespaces $NamespaceDockerDesktop / $NamespaceMinikube exist. Run deploy/scripts/undeploy.ps1 first."
    }
}

if ($TargetMode -eq "docker-desktop") {
    if (Test-K8sNamespace $NamespaceMinikube) {
        throw "Minikube namespace $NamespaceMinikube exists. Run ./deploy/scripts/undeploy.ps1 -Mode minikube first."
    }
}

if ($TargetMode -eq "minikube") {
    if (Test-K8sNamespace $NamespaceDockerDesktop) {
        throw "Docker Desktop namespace $NamespaceDockerDesktop exists. Run ./deploy/scripts/undeploy.ps1 -Mode docker-desktop first."
    }
}

$portMap = @{
    "compose" = @(9200, 5001, 7000, 1433, 16686)
    "docker-desktop" = @(19200, 15001, 17000, 16687)
    "minikube" = @(29200, 25001, 27000, 16688)
}

foreach ($port in $portMap[$TargetMode]) {
    if (Test-PortInUse $port) {
        Write-Warning "Port $port is already in use. Another deployment mode may be active."
    }
}

Write-Host "Conflict check passed."
