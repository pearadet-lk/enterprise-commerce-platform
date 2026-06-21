# Shared deployment naming (root: enterprise-commerce-platform)
$script:DeployRootName = "enterprise-commerce-platform"
$script:ComposeProjectName = "enterprise-commerce-platform"
# Running container names (Compose container_name + K8s pod container name)
$script:ComposeContainerPrefix = "ecp"

$script:NamespaceDockerDesktop = "enterprise-commerce-platform-desktop"
$script:NamespaceMinikube = "enterprise-commerce-platform-minikube"

# Minikube: kubectl port-forward local ports (match overlays/minikube/platform.env)
$script:MinikubePortForwardPidFile = Join-Path $PSScriptRoot ".minikube-port-forward.pids"
$script:MinikubeTunnelPidFile = Join-Path $PSScriptRoot ".minikube-tunnel.pid"
$script:DockerDesktopPortForwardPidFile = Join-Path $PSScriptRoot ".docker-desktop-port-forward.pids"
$script:MinikubePortForwards = @(
    @{ Service = "angular-external"; Deployment = "angular"; LocalPort = 29200; RemotePort = 80; NodePort = 29200; Label = "Angular SPA" },
    @{ Service = "duende-ui-external"; Deployment = "duende-ui"; LocalPort = 25001; RemotePort = 80; NodePort = 25001; Label = "Identity / Duende UI" },
    @{ Service = "gateway-external"; Deployment = "gateway"; LocalPort = 27000; RemotePort = 8080; NodePort = 27000; Label = "API Gateway" },
    @{ Service = "jaeger-external"; Deployment = "jaeger"; LocalPort = 16688; RemotePort = 16686; NodePort = 16688; Label = "Jaeger UI" }
)
$script:DockerDesktopPortForwards = @(
    @{ Service = "angular-external"; Deployment = "angular"; LocalPort = 19200; RemotePort = 80; Label = "Angular SPA" },
    @{ Service = "duende-ui-external"; Deployment = "duende-ui"; LocalPort = 15001; RemotePort = 80; Label = "Identity / Duende UI" },
    @{ Service = "gateway-external"; Deployment = "gateway"; LocalPort = 17000; RemotePort = 8080; Label = "API Gateway" },
    @{ Service = "jaeger-external"; Deployment = "jaeger"; LocalPort = 16687; RemotePort = 16686; Label = "Jaeger UI" }
)

function Clear-MinikubeDockerEnvironment {
    Remove-Item Env:DOCKER_HOST -ErrorAction SilentlyContinue
    Remove-Item Env:DOCKER_TLS_VERIFY -ErrorAction SilentlyContinue
    Remove-Item Env:DOCKER_CERT_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:MINIKUBE_ACTIVE_DOCKERD -ErrorAction SilentlyContinue
}

function Use-DockerDesktopEngine {
    Clear-MinikubeDockerEnvironment

    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        return
    }

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $contexts = @(docker context ls --format "{{.Name}}" 2>$null)
        if ($contexts -contains "desktop-linux") {
            docker context use desktop-linux 2>$null | Out-Null
        }
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

function Test-DockerEngineReady {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        docker info 2>$null | Out-Null
        return $LASTEXITCODE -eq 0
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

function Assert-DockerEngineReady {
    if (Test-DockerEngineReady) {
        return
    }

    if ($env:DOCKER_HOST) {
        throw @"
Docker CLI cannot reach the engine (DOCKER_HOST=$($env:DOCKER_HOST)).
This usually happens after running minikube build in the same PowerShell window.
Open a new terminal, or rerun ./ecp.ps1 desktop deploy (the script now resets Docker context automatically).
"@
    }

    throw "Docker is not running. Start Docker Desktop and wait until the engine is ready, then retry."
}
