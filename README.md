# Enterprise Commerce Platform

A B2B ordering platform built with **Angular 22**, **Duende IdentityServer**, and **.NET 10**, designed for local development with Docker Compose.

## Architecture

```
Angular SPA (4200)
    │ OIDC Authorization Code + PKCE
    ▼
Duende IdentityServer (5001)
    │ JWT access tokens
    ▼
API Gateway (7000)
    ├── Catalog API (5101)
    ├── Orders API (5102)
    └── Users API (5103)
            │
            ▼
      SQL Server (1433)
```

## Features

- Login via Duende IdentityServer (OIDC + PKCE + refresh tokens)
- User management and roles (`Admin`, `Manager`, `User`)
- Product catalog
- Orders
- Audit logs
- Admin dashboard

## Prerequisites

- Docker Desktop
- .NET 10 SDK (optional, for local backend development)
- Node.js 22+ (optional, for local frontend development)

## Quick Start (Docker)

From the repository root:

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| Angular SPA | http://localhost:4200 |
| Identity Server | http://localhost:5001 |
| API Gateway | http://localhost:7000 |
| Catalog API | http://localhost:5101 |
| Orders API | http://localhost:5102 |
| Users API | http://localhost:5103 |
| SQL Server | localhost:1433 |

### Seed Users

| Username | Password | Role |
|----------|----------|------|
| admin | Admin123! | Admin |
| manager | Manager123! | Manager |
| user | User123! | User |

Sign in at http://localhost:4200/login.

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
