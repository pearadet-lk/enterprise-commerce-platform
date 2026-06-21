# Enterprise Commerce Platform — Deployment Guide

Complete reference for deploying the platform in one of **three mutually exclusive modes**. Read this before your first deploy.

> **Important:** Run only **one mode at a time**. Compose, Docker Desktop Kubernetes, and Minikube use different ports, namespaces, and container names by design, but sharing the same machine resources (SQL, host ports) will still cause failures if multiple modes run together.

---

## Quick reference

| Mode | Container names | Docker images | Project / namespace | Deploy command | SPA URL |
|------|-----------------|---------------|---------------------|----------------|---------|
| **Docker Compose** | `ecp-*` | `ecp/*:compose` | `enterprise-commerce-platform` | `docker compose up --build` | http://localhost:9200 |
| **Docker Desktop K8s** | `ecp-*` (pod spec) | `ecp/*:local` | `enterprise-commerce-platform-desktop` | `./deploy/scripts/deploy-docker-desktop.ps1` | http://localhost:19200 |
| **Minikube** | `ecp-*` (pod spec) | `ecp/*:local` | `enterprise-commerce-platform-minikube` | `./deploy/scripts/deploy-minikube.ps1` | http://localhost:29200 |

Shared naming constants live in `deploy/scripts/deploy-config.ps1` (`DeployRootName`, container prefix, namespaces).

**Root CLI (recommended):** run from repository root with `./ecp.ps1`:

| Mode | Build | Deploy | Stop |
|------|-------|--------|------|
| Compose | `./ecp.ps1 compose build` | `./ecp.ps1 compose up` | `./ecp.ps1 compose down` |
| Docker Desktop K8s | `./ecp.ps1 desktop build` | `./ecp.ps1 desktop deploy` | `./ecp.ps1 desktop undeploy` |
| Minikube | `./ecp.ps1 minikube build` | `./ecp.ps1 minikube deploy` (auto port-forward) | `./ecp.ps1 minikube undeploy` (stops port-forward) |

Minikube browser: `./ecp.ps1 minikube access` · Help: `./ecp.ps1 help`

Full documentation sections:

1. [Common requirements (all modes)](#common-requirements-all-modes)
2. [Hardware & resources](#hardware--resources)
3. [Mode 1 — Docker Compose](#mode-1--docker-compose)
4. [Mode 2 — Docker Desktop Kubernetes](#mode-2--docker-desktop-kubernetes)
5. [Mode 3 — Minikube](#mode-3--minikube)
6. [Port matrix](#port-matrix-no-overlap)
7. [Environment variables & secrets](#environment-variables--secrets)
8. [What gets deployed](#what-gets-deployed)
9. [Conflict prevention](#conflict-prevention)
10. [Verification checklist](#verification-checklist)
11. [Undeploy & cleanup](#undeploy--cleanup)
12. [Troubleshooting](#troubleshooting)
13. [Repository layout](#repository-layout)

---

## Common requirements (all modes)

These apply regardless of which deployment mode you choose.

### Operating system

| OS | Supported |
|----|-----------|
| Windows 10/11 | Yes (PowerShell scripts provided) |
| macOS | Yes (use `docker compose` / `kubectl`; adapt `.ps1` to manual steps or run via PowerShell Core) |
| Linux | Yes (same as macOS) |

### Source code

Clone the repository and work from the **repository root**:

```powershell
git clone <your-repo-url> enterprise-commerce-platform
cd enterprise-commerce-platform
```

### Docker images built by this repo

| Image | Purpose |
|-------|---------|
| `ecp/identity-server` | Duende IdentityServer + login UI |
| `ecp/catalog-api` | Product catalog API |
| `ecp/orders-api` | Orders API |
| `ecp/users-api` | Users & audit API |
| `ecp/gateway` | YARP API gateway |
| `ecp/angular-spa` | Angular 22 frontend (nginx) |
| `ecp/duende-ui` | nginx proxy to Identity (Compose only) |

Third-party images: **SQL Server 2025**, **Jaeger 2.x**, **nginx** (K8s duende-ui).

### Default credentials (local dev only)

| Item | Value |
|------|-------|
| SQL SA password | `Your_strong_Password123` |
| Seed user `admin` | `Admin123!` |
| Seed user `manager` | `Manager123!` |
| Seed user `user` | `User123!` |

Change these before any non-local deployment.

### Seed users (all modes)

| Username | Password | Role |
|----------|----------|------|
| admin | Admin123! | Admin |
| manager | Manager123! | Manager |
| user | User123! | User |

Sign in via the SPA **Login** page for your mode (see port matrix below).

---

## Hardware & resources

Minimum recommended for a smooth local experience:

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| CPU | 4 cores | 8 cores |
| RAM | 8 GB | 16 GB |
| Disk | 20 GB free | 40 GB free |

SQL Server alone needs ~2 GB RAM inside its container. Kubernetes modes add further overhead for the control plane (especially Minikube).

---

## Mode 1 — Docker Compose

**Best for:** fastest local startup, default development workflow.

### Required software

| Tool | Version | Verify |
|------|---------|--------|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest stable | `docker --version` |
| Docker Compose V2 | Bundled with Docker Desktop | `docker compose version` |

**.NET SDK and Node.js are not required** for Compose deploy — images are built inside Docker.

### One-time setup

1. Install **Docker Desktop** and ensure it is running.
2. Enable sufficient resources in Docker Desktop → **Settings → Resources** (≥ 8 GB RAM).
3. *(Optional)* Copy environment defaults:
   ```powershell
   copy deploy\compose\.env.example .env
   ```

### Deploy

From repository root:

```powershell
docker compose up --build
```

- Compose file: `deploy/compose/docker-compose.yml` (included by root `docker-compose.yml`)
- Project name: **`enterprise-commerce-platform`**
- Container prefix: **`ecp-*`** (e.g. `ecp-users-api`, `ecp-sqlserver`)

First run builds all images and may take **10–20 minutes** depending on network and CPU.

### URLs (Compose)

| Service | URL |
|---------|-----|
| Angular SPA | http://localhost:9200 |
| Duende UI / OIDC | http://localhost:5001 |
| API Gateway | http://localhost:7000 |
| Catalog API | http://localhost:5101 |
| Orders API | http://localhost:5102 |
| Users API | http://localhost:5103 |
| SQL Server | `localhost:1433` |
| Jaeger UI | http://localhost:16686 |

### Stop

```powershell
docker compose down
```

Remove volumes (resets databases):

```powershell
docker compose down -v
```

---

## Mode 2 — Docker Desktop Kubernetes

**Best for:** testing Kubernetes manifests locally with Docker Desktop’s built-in cluster.

### Required software

| Tool | Version | Verify |
|------|---------|--------|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest stable | `docker --version` |
| Kubernetes (Docker Desktop) | Enabled in settings | `kubectl cluster-info` |
| [kubectl](https://kubernetes.io/docs/tasks/tools/) | Matches cluster (1.28+) | `kubectl version --client` |
| PowerShell | 5.1+ or PowerShell 7+ | `$PSVersionTable.PSVersion` |

Kustomize is **embedded** in kubectl (`kubectl apply -k` / `kubectl kustomize`) — no separate install needed.

### One-time setup

1. Install **Docker Desktop**.
2. **Settings → Kubernetes → Enable Kubernetes** → Apply & restart.
3. Wait until Docker Desktop shows Kubernetes as **running**.
4. Confirm context:
   ```powershell
   kubectl config current-context
   # Expected: docker-desktop
   ```
5. Ensure no Compose stack is running:
   ```powershell
   docker compose down
   ```

### Deploy

```powershell
./deploy/scripts/deploy-docker-desktop.ps1
```

The script will:

1. Run **conflict checks** (`check-conflicts.ps1`)
2. **Build** all `ecp/*:local` images (`build-images.ps1`)
3. **Apply** Kustomize overlay `deploy/kubernetes/overlays/docker-desktop`
4. Wait for **db-init** job and core deployments

Namespace: **`enterprise-commerce-platform-desktop`**

### URLs (Docker Desktop K8s)

| Service | URL |
|---------|-----|
| Angular SPA | http://localhost:19200 |
| Duende UI / OIDC | http://localhost:15001 |
| API Gateway | http://localhost:17000 |
| Jaeger UI | http://localhost:16687 |

Backend APIs and SQL are **cluster-internal** (not exposed on host ports).

### Undeploy

```powershell
./deploy/scripts/undeploy.ps1 -Mode docker-desktop
```

---

## Mode 3 — Minikube

**Best for:** Kubernetes development without Docker Desktop K8s, or CI-like cluster behavior.

### Required software

| Tool | Version | Verify |
|------|---------|--------|
| [Minikube](https://minikube.sigs.k8s.io/docs/start/) | Latest stable | `minikube version` |
| [kubectl](https://kubernetes.io/docs/tasks/tools/) | 1.28+ | `kubectl version --client` |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or other driver) | Latest | `docker --version` |
| PowerShell | 5.1+ or 7+ | `$PSVersionTable.PSVersion` |

Minikube uses the **Minikube Docker daemon** for images when you run `deploy-minikube.ps1` (via `minikube docker-env`).

### One-time setup

1. Install **Minikube** and **kubectl**.
2. Start the cluster (adjust memory/CPU as needed):
   ```powershell
   minikube start --cpus=4 --memory=8192
   ```
3. Confirm context:
   ```powershell
   kubectl config current-context
   # Expected: minikube
   ```
4. Stop Compose and other K8s overlay if running:
   ```powershell
   docker compose down
   ./deploy/scripts/undeploy.ps1 -Mode docker-desktop
   ```

### Deploy

```powershell
./deploy/scripts/deploy-minikube.ps1
# Skip auto port-forward (not recommended on Windows):
./deploy/scripts/deploy-minikube.ps1 -SkipPortForward
```

The script will:

1. Verify Minikube is running
2. Run conflict checks
3. Build images **inside Minikube’s Docker** (`build-images.ps1 -Target minikube`)
4. Apply overlay `deploy/kubernetes/overlays/minikube`
5. Wait for db-init and rollouts
6. **Start port-forward automatically** (localhost 29200, 25001, 27000, 16688)

Namespace: **`enterprise-commerce-platform-minikube`**

### Browser access — port-forward (automatic)

Deploy **starts port-forward for you**. On Windows, NodePort often does not work in the browser; port-forward binds localhost ports used by OIDC/CORS.

Manual commands (if you skipped auto or need to restart):

| Action | Command |
|--------|---------|
| Start port-forward | `./deploy/scripts/port-forward-minikube.ps1` |
| Stop port-forward | `./deploy/scripts/port-forward-minikube.ps1 -Stop` |
| Check status | `./deploy/scripts/port-forward-minikube.ps1 -Status` |

**Undeploy stops port-forward automatically:**

```powershell
./deploy/scripts/undeploy.ps1 -Mode minikube
```

### URLs (Minikube — after port-forward)

| Service | URL |
|---------|-----|
| Angular SPA | http://localhost:29200 |
| Duende UI / OIDC | http://localhost:25001 |
| API Gateway | http://localhost:27000 |
| Jaeger UI | http://localhost:16688 |

NodePorts (29200, 25001, etc.) are still defined in the overlay as a fallback on Linux/macOS, but **port-forward is the supported path on Windows**.

### If port-forward does not work

Try these in order (all keep **localhost** URLs so login/OIDC still works):

| Step | Command | Notes |
|------|---------|-------|
| 1 | `./deploy/scripts/access-minikube.ps1 -Method pod-forward` | Forwards via deployment instead of service |
| 2 | `./deploy/scripts/port-forward-minikube.ps1 -Address 0.0.0.0` | Bind all interfaces (VPN/firewall issues) |
| 3 | `./deploy/scripts/access-minikube.ps1 -Method tunnel` | Opens **Admin** PowerShell with `minikube tunnel`; then use localhost NodePorts |
| 4 | `./deploy/scripts/access-minikube.ps1 -Method diagnose` | Checks pods, services, and local ports |
| 5 | `./deploy/scripts/access-minikube.ps1 -Method manual` | Copy/paste manual `kubectl` / `minikube` commands |

**minikube tunnel** (workaround 3): after the tunnel window shows `Tunnel successfully started`, open the same URLs as above (29200, 25001, 27000, 16688).

Stop port-forward and tunnel:

```powershell
./deploy/scripts/access-minikube.ps1 -Stop
```

Show all URL options (including Minikube node IP — may break OIDC):

```powershell
./deploy/scripts/access-minikube.ps1 -Method urls
```

### Undeploy

```powershell
./deploy/scripts/undeploy.ps1 -Mode minikube
```

Stops port-forward/tunnel, then removes the Kubernetes overlay.

Stop Minikube entirely:

```powershell
minikube stop
```

---

## Port matrix (no overlap)

Host ports are **intentionally different** per mode so documentation and scripts stay unambiguous. Only one mode’s ports should be active at a time.

| Service | Docker Compose | Docker Desktop K8s | Minikube |
|---------|----------------|--------------------|----------|
| Angular SPA | **9200** | **19200** | **29200** |
| Duende / Identity UI | **5001** | **15001** | **25001** |
| API Gateway | **7000** | **17000** | **27000** |
| Catalog API | 5101 | internal | internal |
| Orders API | 5102 | internal | internal |
| Users API | 5103 | internal | internal |
| SQL Server | 1433 | internal | internal |
| Jaeger UI | **16686** | **16687** | **16688** |
| OTLP gRPC | 4317 | internal | internal |
| OTLP HTTP | 4318 | internal | internal |

---

## Environment variables & secrets

### Docker Compose

Optional `.env` at repo root (see `deploy/compose/.env.example`):

| Variable | Default | Description |
|----------|---------|-------------|
| `ECP_DEPLOY_MODE` | `compose` | Deployment mode marker |
| `MSSQL_SA_PASSWORD` | `Your_strong_Password123` | SQL Server SA password |

Services use `ASPNETCORE_ENVIRONMENT=Docker` and `appsettings.Docker.json`.

### Kubernetes (both overlays)

Configuration is injected via **ConfigMaps** in each overlay:

| ConfigMap | Purpose |
|-----------|---------|
| `platform-env` | Public URLs, CORS, OIDC redirect URIs, deploy mode |
| `angular-config` | Runtime SPA config (`app-config.json`) |
| `db-init-sql` | SQL initialization script |
| `duende-ui-nginx` | nginx proxy config |

Secrets:

| Secret | Key | Value (default) |
|--------|-----|-----------------|
| `sql-credentials` | `SA_PASSWORD` | `Your_strong_Password123` |

Backend pods use `ASPNETCORE_ENVIRONMENT=Kubernetes` and `appsettings.Kubernetes.json`.

Overlay-specific public URLs:

| Setting | Docker Desktop | Minikube |
|---------|----------------|----------|
| SPA | http://localhost:19200 | http://localhost:29200 |
| Identity issuer | http://localhost:15001 | http://localhost:25001 |
| Gateway | http://localhost:17000 | http://localhost:27000 |

Files: `deploy/kubernetes/overlays/<mode>/platform.env` and `angular-config.json`.

---

## What gets deployed

### All modes — application components

| Component | Description |
|-----------|-------------|
| SQL Server | `IdentityDb`, `CatalogDb`, `OrdersDb`, `UsersDb` |
| db-init | Creates databases (Compose: one-shot container; K8s: Job) |
| Jaeger | OpenTelemetry trace UI + OTLP collector |
| Identity Server | Duende + ASP.NET Identity + login UI |
| Duende UI | nginx → Identity (Compose container; K8s Deployment) |
| Catalog API | Products |
| Orders API | Orders (calls Catalog) |
| Users API | Users, audit logs, dashboard summary |
| API Gateway | YARP reverse proxy |
| Angular SPA | B2B frontend |

### Kubernetes-only resources

| Resource | Purpose |
|----------|---------|
| Namespace | `enterprise-commerce-platform-desktop` or `enterprise-commerce-platform-minikube` |
| PVC `sqlserver-data` | SQL persistent storage |
| PVC `identity-dpkeys` | Data Protection keys |
| NodePort services | External access on mode-specific ports |

---

## Conflict prevention

Deploy scripts invoke `deploy/scripts/check-conflicts.ps1`, which warns or fails when:

- Compose containers `ecp-*` are running and you deploy Kubernetes
- Both `enterprise-commerce-platform-desktop` and `enterprise-commerce-platform-minikube` namespaces are in use
- Known ports for the target mode are already bound on the host

**Before switching modes**, tear down the active one:

```powershell
# Stop Compose
docker compose down

# Or stop Kubernetes overlays
./deploy/scripts/undeploy.ps1 -Mode docker-desktop
./deploy/scripts/undeploy.ps1 -Mode minikube
```

---

## Verification checklist

After deploy, confirm the stack is healthy:

### 1. Containers / pods running

**Compose:**

```powershell
docker compose ps
```

**Kubernetes:**

```powershell
kubectl get pods -n enterprise-commerce-platform-desktop
# or
kubectl get pods -n enterprise-commerce-platform-minikube
```

All pods should be `Running`; `db-init` job should be `Complete`.

### 2. Identity Server discovery

Replace `<identity-port>` with `5001`, `15001`, or `25001`:

```powershell
curl http://localhost:<identity-port>/.well-known/openid-configuration
```

### 3. Gateway health

Replace `<gateway-port>` with `7000`, `17000`, or `27000`:

```powershell
curl http://localhost:<gateway-port>/health
```

### 4. End-to-end UI

1. Open SPA URL for your mode
2. Click **Sign in** → redirect to Duende login
3. Log in as `admin` / `Admin123!`
4. Browse **Products** and **Orders**
5. Open Jaeger UI → search service `api-gateway` → **Find Traces**

---

## Undeploy & cleanup

| Mode | Command |
|------|---------|
| Compose | `docker compose down` (+ `-v` to drop volumes) |
| Docker Desktop K8s | `./deploy/scripts/undeploy.ps1 -Mode docker-desktop` |
| Minikube | `./deploy/scripts/undeploy.ps1 -Mode minikube` |
| All K8s | `./deploy/scripts/undeploy.ps1 -Mode all` |

---

## Troubleshooting

### Port already in use

Another mode (or another app) is bound to the same port. Stop the other stack or free the port.

```powershell
# Windows — find process on port 9200
netstat -ano | findstr :9200
```

### Compose: SQL Server not healthy

- Ensure ≥ 8 GB RAM allocated to Docker
- Wait up to 60s on first start
- Check logs: `docker logs ecp-sqlserver`

### Kubernetes: `db-init` job failed

```powershell
kubectl logs job/db-init -n enterprise-commerce-platform-desktop
kubectl describe job db-init -n enterprise-commerce-platform-desktop
```

Ensure SQL pod is ready before the job runs; re-apply if needed.

### Kubernetes: ImagePullBackOff

Images must exist **locally** as `ecp/*:local`. Re-run:

```powershell
./deploy/scripts/build-images.ps1
# Minikube:
./deploy/scripts/build-images.ps1 -Target minikube
```

### OIDC / login redirect errors

Redirect URIs must match the SPA URL for your mode. Kubernetes modes set these via `platform.env` and `angular-config.json` in the overlay. If you change NodePorts, update both files and redeploy.

### Minikube: browser cannot reach services

**First try port-forward:**

```powershell
./deploy/scripts/port-forward-minikube.ps1
```

**If that fails**, use the workaround helper:

```powershell
./deploy/scripts/access-minikube.ps1 -Method diagnose
./deploy/scripts/access-minikube.ps1 -Method pod-forward
./deploy/scripts/access-minikube.ps1 -Method tunnel      # Admin PowerShell + minikube tunnel
./deploy/scripts/access-minikube.ps1 -Method manual       # manual kubectl commands
```

Common fixes:

- Run **PowerShell as Administrator** for `minikube tunnel`
- Port already in use — stop other modes: `docker compose down`, `./deploy/scripts/access-minikube.ps1 -Stop`
- Pods not ready — `kubectl get pods -n enterprise-commerce-platform-minikube`
- Rebuild/redeploy — `./deploy/scripts/deploy-minikube.ps1`

Stop all access helpers manually: `./deploy/scripts/access-minikube.ps1 -Stop` (also runs automatically on `undeploy.ps1 -Mode minikube`).

---

## Repository layout

```
deploy/
├── README.md                          # This file
├── compose/
│   ├── docker-compose.yml             # Compose stack (enterprise-commerce-platform)
│   └── .env.example                   # Optional Compose env vars
├── kubernetes/
│   ├── base/                          # Shared K8s manifests
│   │   ├── kustomization.yaml
│   │   ├── sqlserver.yaml
│   │   ├── job-db-init.yaml
│   │   ├── identity.yaml
│   │   ├── apps.yaml
│   │   └── ...
│   └── overlays/
│       ├── docker-desktop/            # NodePorts 15xxx / 19xxx
│       │   ├── platform.env
│       │   ├── angular-config.json
│       │   └── patches/nodeports.yaml
│       └── minikube/                  # NodePorts 25xxx / 29xxx
│           ├── platform.env
│           ├── angular-config.json
│           └── patches/nodeports.yaml
└── scripts/
    ├── deploy-config.ps1              # Shared root name & namespace constants
    ├── build-images.ps1               # Build ecp/*:local images
    ├── check-conflicts.ps1            # Pre-deploy conflict guard
    ├── deploy-docker-desktop.ps1
    ├── deploy-minikube.ps1
    ├── port-forward-minikube.ps1      # kubectl port-forward for Minikube (Windows)
    ├── access-minikube.ps1            # tunnel / pod-forward / diagnose workarounds
    └── undeploy.ps1
```

Root wrapper: `docker-compose.yml` → includes `deploy/compose/docker-compose.yml`.

Root CLI: `ecp.ps1` / `ecp.cmd` at repository root → calls `deploy/scripts/*.ps1`.

---

## Optional: local development without Docker

If you run services directly with `dotnet run` / `npm start`, see the main [README.md](../README.md) **Local Development** section. That path is separate from these three deployment modes and uses different ports/configuration.

---

## Related documentation

- [Main README](../README.md) — architecture, OpenTelemetry, correlation ID, API routes
- [OpenTelemetry / Jaeger](../README.md#opentelemetry-and-jaeger) — tracing setup
- [Duende UI (Compose)](../README.md#duende-ui-container) — why nginx sits in front of Identity
