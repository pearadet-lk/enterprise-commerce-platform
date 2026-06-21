# Enterprise Commerce Platform

A B2B ordering platform built with **Angular 22**, **Duende IdentityServer**, and **.NET 10**, designed for local development with Docker Compose.

## Architecture

```
Angular SPA (9200)
    │ OIDC Authorization Code + PKCE
    ▼
Duende UI (5001) ──► IdentityServer (internal)
    │ JWT access tokens
    ▼
API Gateway (7000)
    ├── Catalog API (5101)
    ├── Orders API (5102)
    └── Users API (5103)
            │
            ▼
      SQL Server (1433)
            │
            ▼
      Jaeger (16686) ◄── OpenTelemetry OTLP
```

## Features

- Login via Duende IdentityServer (OIDC + PKCE + refresh tokens)
- **OpenTelemetry** tracing and metrics exported to **Jaeger**
- User management and roles (`Admin`, `Manager`, `User`)
- Product catalog
- Orders
- Audit logs
- Admin dashboard

## Deployment

This platform supports **three mutually exclusive** deployment modes. The deploy root name is **`enterprise-commerce-platform`** (Compose project name, container prefix, and K8s `app.kubernetes.io/name` label).

**→ Full prerequisites, ports, verification, and troubleshooting:** [deploy/README.md](deploy/README.md)

> Run **only one mode at a time**. Each mode uses different host ports and namespaces so they do not overlap, but SQL and CPU/RAM are still shared on one machine.

### Command summary (`ecp.ps1`)

Run from the **repository root**. Help: `./ecp.ps1 help` · Windows cmd: `ecp.cmd <mode> <action>`

| Mode | Build | Deploy | Stop |
|------|-------|--------|------|
| **Compose** | `compose build` | `compose up` | `compose down` |
| **Docker Desktop K8s** | `desktop build` | `desktop deploy` | `desktop undeploy` |
| **Minikube** | `minikube build` | `minikube deploy` | `minikube undeploy` |

Full commands (prefix with `./ecp.ps1 `):

```powershell
./ecp.ps1 help

# Mode 1 — Docker Compose
./ecp.ps1 compose build
./ecp.ps1 compose up
./ecp.ps1 compose down

# Mode 2 — Docker Desktop Kubernetes
./ecp.ps1 desktop build
./ecp.ps1 desktop deploy
./ecp.ps1 desktop undeploy

# Mode 3 — Minikube
./ecp.ps1 minikube build
./ecp.ps1 minikube deploy              # deploy + auto port-forward
./ecp.ps1 minikube access              # restart port-forward manually
./ecp.ps1 minikube access -Method tunnel
./ecp.ps1 minikube undeploy            # stops port-forward + removes overlay
```

| Mode | Build | Deploy / start | Stop |
|------|-------|----------------|------|
| Compose | `./ecp.ps1 compose build` | `./ecp.ps1 compose up` | `./ecp.ps1 compose down` |
| Docker Desktop K8s | `./ecp.ps1 desktop build` | `./ecp.ps1 desktop deploy` | `./ecp.ps1 desktop undeploy` |
| Minikube | `./ecp.ps1 minikube build` | `./ecp.ps1 minikube deploy` | `./ecp.ps1 minikube undeploy` |

**Minikube:** `deploy` starts port-forward automatically (localhost 29200, 25001, 27000, 16688). `undeploy` stops it. If port-forward fails, use `minikube access` or `-Method tunnel`.

Scripts live under `deploy/scripts/`; `ecp.ps1` calls them for you.

### Build details (all modes)

Build **Docker images only** — no containers/pods started:

| Mode | Command | Image tag | Where images are built |
|------|---------|-----------|------------------------|
| **Compose** | `./ecp.ps1 compose build` | `ecp/*:compose` | Local Docker (Docker Desktop) |
| **Docker Desktop K8s** | `./ecp.ps1 desktop build` | `ecp/*:local` | Local Docker (Docker Desktop) |
| **Minikube** | `./ecp.ps1 minikube build` | `ecp/*:local` | Minikube Docker daemon |

Windows cmd equivalents: `ecp.cmd compose build`, `ecp.cmd desktop build`, `ecp.cmd minikube build`.

**Images built (app services):**

| Image | Services |
|-------|----------|
| `ecp/identity-server` | Duende IdentityServer |
| `ecp/catalog-api` | Catalog API |
| `ecp/orders-api` | Orders API |
| `ecp/users-api` | Users API |
| `ecp/gateway` | API Gateway |
| `ecp/angular-spa` | Angular SPA |

Compose `build` also builds `ecp/duende-ui:compose` (nginx proxy). SQL Server and Jaeger use public images (not built locally).

**Per mode:**

```powershell
# Mode 1 — Compose (runs conflict check + docker compose build)
./ecp.ps1 compose build
# equivalent: docker compose build

# Mode 2 — Docker Desktop K8s (runs conflict check + deploy/scripts/build-images.ps1)
./ecp.ps1 desktop build
# equivalent: ./deploy/scripts/build-images.ps1 -Target local

# Mode 3 — Minikube (requires minikube running; builds inside cluster Docker)
minikube start --cpus=4 --memory=8192
./ecp.ps1 minikube build
# equivalent: ./deploy/scripts/build-images.ps1 -Target minikube
```

After `build`, start the stack with `up` / `deploy` for that mode (build is included in deploy, so `build` is optional unless you want images ready first).

### Mode 1 — Docker Compose (recommended for local dev)

**Prerequisites:** Docker Desktop with Compose V2. .NET and Node.js are not required.

```powershell
# Build images only
./ecp.ps1 compose build

# Build + start (from repository root)
./ecp.ps1 compose up
# or
docker compose up --build
```

| Item | Value |
|------|-------|
| Project name | `enterprise-commerce-platform` |
| Container names | `ecp-*` (e.g. `ecp-users-api`) |
| Docker images | `ecp/*:compose` |
| SPA | http://localhost:9200 |
| Identity / login | http://localhost:5001 |
| Gateway | http://localhost:7000 |
| Jaeger | http://localhost:16686 |

**Stop:**

```powershell
docker compose down          # keep data
docker compose down -v       # reset databases
```

### Mode 2 — Docker Desktop Kubernetes

**Prerequisites:** Docker Desktop with **Kubernetes enabled**, `kubectl`, PowerShell.

```powershell
docker compose down   # stop Compose if running

# Build images only
./ecp.ps1 desktop build

# Build + deploy to cluster
./ecp.ps1 desktop deploy
# or
./deploy/scripts/deploy-docker-desktop.ps1
```

| Item | Value |
|------|-------|
| Namespace | `enterprise-commerce-platform-desktop` |
| Container names | `ecp-*` (e.g. `ecp-users-api`) |
| Docker images | `ecp/*:local` (built by `build-images.ps1`) |
| SPA | http://localhost:19200 |
| Identity / login | http://localhost:15001 |
| Gateway | http://localhost:17000 |
| Jaeger | http://localhost:16687 |

**Undeploy:**

```powershell
./deploy/scripts/undeploy.ps1 -Mode docker-desktop
```

### Mode 3 — Minikube

**Prerequisites:** Minikube, `kubectl`, Docker (or another Minikube driver), PowerShell.

On Windows, Minikube **NodePort** often does not work in the browser. This project **starts port-forward automatically** on deploy and **stops it on undeploy**.

#### Build (images only)

```powershell
minikube start --cpus=4 --memory=8192
./ecp.ps1 minikube build
# equivalent: ./deploy/scripts/build-images.ps1 -Target minikube
```

Builds `ecp/*:local` images inside the **Minikube Docker daemon** (required before deploy).

#### Deploy (auto port-forward)

```powershell
minikube start --cpus=4 --memory=8192
docker compose down
./deploy/scripts/undeploy.ps1 -Mode docker-desktop   # if that overlay was used

# Recommended — from repository root
./ecp.ps1 minikube deploy
```

Equivalent:

```powershell
./deploy/scripts/deploy-minikube.ps1
```

After deploy completes, port-forward runs in the background. Open:

| Service | URL |
|---------|-----|
| Angular SPA | http://localhost:29200 |
| Identity / login | http://localhost:25001 |
| API Gateway | http://localhost:27000 |
| Jaeger UI | http://localhost:16688 |

| Item | Value |
|------|-------|
| Namespace | `enterprise-commerce-platform-minikube` |
| Container names | `ecp-*` (e.g. `ecp-users-api`) |
| Docker images | `ecp/*:local` (built inside Minikube Docker) |
| Port-forward PIDs | `deploy/scripts/.minikube-port-forward.pids` |

Skip auto port-forward (not recommended on Windows):

```powershell
./deploy/scripts/deploy-minikube.ps1 -SkipPortForward
./ecp.ps1 minikube deploy -SkipPortForward
```

Restart port-forward manually:

```powershell
./ecp.ps1 minikube access
# or
./deploy/scripts/port-forward-minikube.ps1
```

Check status / stop port-forward only:

```powershell
./deploy/scripts/port-forward-minikube.ps1 -Status
./deploy/scripts/port-forward-minikube.ps1 -Stop
./ecp.ps1 minikube access -StopAccess
```

#### Undeploy (auto stop port-forward)

Removes port-forward processes first, then deletes the Kubernetes overlay:

```powershell
./ecp.ps1 minikube undeploy
```

Or:

```powershell
./deploy/scripts/undeploy.ps1 -Mode minikube
minikube stop   # optional — stop the whole cluster
```

#### If auto port-forward fails

```powershell
./ecp.ps1 minikube access -Method pod-forward
./ecp.ps1 minikube access -Method tunnel      # Admin PowerShell + minikube tunnel
./ecp.ps1 minikube access -Method diagnose
./ecp.ps1 minikube access -Method manual
```

See [deploy/README.md](deploy/README.md) for full Minikube troubleshooting.

### Port matrix (all modes)

| Service | Compose | Docker Desktop K8s | Minikube |
|---------|---------|-------------------|----------|
| Angular SPA | 9200 | 19200 | 29200 |
| Identity UI | 5001 | 15001 | 25001 |
| API Gateway | 7000 | 17000 | 27000 |
| Jaeger UI | 16686 | 16687 | 16688 |

### Seed users (all modes)

| Username | Password | Role |
|----------|----------|------|
| admin | Admin123! | Admin |
| manager | Manager123! | Manager |
| user | User123! | User |

Sign in from the SPA URL for your mode (see tables above).

### Minimum requirements (Compose)

| Tool | Required |
|------|----------|
| Docker Desktop | Yes |
| Docker Compose V2 | Yes (included with Docker Desktop) |
| .NET / Node.js | No (images build inside Docker) |

See [deploy/README.md](deploy/README.md) for Kubernetes prerequisites, hardware sizing, and verification steps.

## Quick Start (Docker Compose)

If you only need the fastest path, use Compose from the repository root:

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| Angular SPA | http://localhost:9200 |
| Duende UI (login / OIDC) | http://localhost:5001 |
| Identity Server (internal) | http://identity:8080 |
| API Gateway | http://localhost:7000 |
| Catalog API | http://localhost:5101 |
| Orders API | http://localhost:5102 |
| Users API | http://localhost:5103 |
| SQL Server | localhost:1433 |
| Jaeger UI | http://localhost:16686 |

Sign in at http://localhost:9200/login with `admin` / `Admin123!`.

## Duende UI container

Duende login pages must share the same public origin as the IdentityServer endpoints (cookie + redirect requirements). Docker Compose therefore runs:

- **`identity`** — Duende IdentityServer + Razor login UI (internal network only)
- **`duende-ui`** — nginx reverse proxy exposed on port **5001** (browser-facing Duende frontend)

The Angular SPA and all OIDC redirects continue to use `http://localhost:5001`.

## OpenTelemetry and Jaeger

All .NET backend services export traces and metrics via **OTLP/HTTP** to Jaeger:

| Service | OpenTelemetry service name |
|---------|----------------------------|
| Identity Server | `identity-server` |
| API Gateway | `api-gateway` |
| Catalog API | `catalog-api` |
| Orders API | `orders-api` |
| Users API | `users-api` |

Instrumentation includes ASP.NET Core requests, `HttpClient` calls, SQL Client, and runtime metrics.

### Correlation ID

Every request receives an **`X-Correlation-Id`** header:

- Reuses the incoming header when the client sends one
- Otherwise defaults to the OpenTelemetry **trace id**
- Returned on the response and included in error payloads
- Added as the `correlation.id` span tag in Jaeger
- Propagated on outbound `HttpClient` calls (gateway → APIs → SQL)

Search Jaeger traces by tag: `correlation.id=<your-id>`.

### Global exception handling

Unhandled exceptions are caught by shared middleware that:

- Logs the error with correlation id, HTTP method, and path
- Returns RFC 7807 `ProblemDetails` JSON with a `correlationId` extension
- Includes exception details in Development / Docker only

### View traces

1. Start the stack: `docker compose up --build`
2. Use the app (login, browse products/orders)
3. Open **Jaeger UI**: http://localhost:16686
4. Select a service (e.g. `api-gateway`) and click **Find Traces**

Distributed traces follow requests from the gateway through downstream APIs and SQL calls.

Configuration (per service `appsettings.json`):

```json
"OpenTelemetry": {
  "Enabled": true,
  "OtlpEndpoint": "http://localhost:4318"
}
```

In Docker, the endpoint is `http://jaeger:4317` with `OtlpProtocol` set to `grpc` (Jaeger v2). Set `OpenTelemetry:Enabled` to `false` to disable export.

## Solution Structure

```
src/
├── frontend/
│   └── angular-spa/
└── backend/
    ├── Identity/IdentityServer.Host/
    ├── ApiGateway/
    ├── Services/
    │   ├── Catalog.API/
    │   ├── Orders.API/
    │   └── Users.API/
    ├── BuildingBlocks/
    │   ├── SharedKernel/
    │   ├── Contracts/
    │   └── Common/
    └── Database/
```

## Local Development (without Docker)

Requires **.NET 10 SDK**, **Node.js 22+**, and a local **SQL Server** instance. This is separate from the three Docker/Kubernetes deploy modes — see [deploy/README.md](deploy/README.md).

1. Start SQL Server and create databases using `src/backend/Database/init.sql`.
2. Run backend services:

```bash
dotnet run --project src/backend/Identity/IdentityServer.Host
dotnet run --project src/backend/Services/Catalog.API
dotnet run --project src/backend/Services/Orders.API
dotnet run --project src/backend/Services/Users.API
dotnet run --project src/backend/ApiGateway
```

3. Run the Angular SPA:

```bash
cd src/frontend/angular-spa
npm start
```

## Security Notes

- JWT bearer authentication is configured on all APIs with audience validation per API resource.
- Role-based policies: `AdminOnly`, `ManagerOrAdmin`.
- The Angular SPA uses `angular-auth-oidc-client` with silent renew and refresh tokens.
- Default SQL SA password is for local development only. Change it before any non-local deployment.

## API Gateway Routes

| Gateway path | Backend service |
|--------------|-----------------|
| `/api/catalog/*` | Catalog API |
| `/api/orders/*` | Orders API |
| `/api/users/*` | Users API |
| `/api/auditlogs/*` | Users API |
| `/api/dashboard/*` | Users API |

## License

See [LICENSE](LICENSE).
