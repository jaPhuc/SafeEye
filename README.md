# SafeEye API

Guardian-side backend for tracking IoT devices and receiving SOS alerts.

## Prerequisites

- [Docker](https://docs.docker.com/engine/install/) (with Compose v2)
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (for local development only)

## Quick Start (Docker)

```bash
# 1. Clone and enter the project
cd safeeye

# 2. Start all services
docker compose up --build
```

The API will be available at `http://localhost:8080`. Swagger UI opens at `/swagger`.

### Stop

```bash
docker compose down
# To also delete the database volume:
docker compose down -v
```

## Local Development

Run the API directly on your machine while Docker provides PostgreSQL.

### 1. Start PostgreSQL only

```bash
docker compose up -d postgres
```

### 2. Run the API

```bash
dotnet run --project src/SafeEye.API
```

The API will be available at `http://localhost:5000`. Swagger UI opens at `/swagger`.

> Port `5000` is used for local development to avoid conflict with the Docker container on port `8080`.
> Set via `Properties/launchSettings.json`.

## Configuration

### Environment variables (`.env`)

| Variable | Default | Description |
|---|---|---|
| `POSTGRES_DB` | `safe_eye` | PostgreSQL database name |
| `POSTGRES_USER` | `sa` | PostgreSQL user |
| `POSTGRES_PASSWORD` | `12345Abc` | PostgreSQL password |
| `JWT_SECRET` | *(required)* | JWT signing key (min 32 characters) |
| `JWT_ISSUER` | `SafeEyeAPI` | JWT issuer claim |
| `JWT_AUDIENCE` | `SafeEyeClients` | JWT audience claim |
| `FIREBASE_CREDENTIALS_PATH` | *(optional)* | Path to Firebase service account JSON |
| `FIREBASE_RTDB_URL` | *(optional)* | Firebase Realtime Database URL |

### `appsettings.json` (local dev)

The `DefaultConnection` in `appsettings.json` must match the credentials of your running PostgreSQL instance:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=safe_eye;Username=sa;Password=12345Abc;"
```

The `Jwt:Secret` in `appsettings.Development.json` provides a fallback for local development:

```json
"Jwt:Secret": "dev_only_secret_replace_in_production_min_32_chars!!"
```

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/auth/register` | Register a new user |
| `POST` | `/api/auth/login` | Login |
| `POST` | `/api/auth/refresh` | Refresh access token |
| `POST` | `/api/auth/logout` | Logout |
| `GET` | `/api/users/me` | Get current user |
| `PUT` | `/api/users/me` | Update current user |
| WebSocket | `/hubs/tracking` | SignalR hub for real-time tracking |
| `GET` | `/health` | Health check (DB connectivity) |

## Project Structure

```
src/
├── SafeEye.API/            # ASP.NET Core Web API entry point
│   ├── Controllers/        # API controllers
│   ├── Filters/            # Action filters (e.g. DeviceAuthFilter)
│   ├── Middleware/         # ExceptionMiddleware
│   └── Properties/         # launchSettings.json
├── SafeEye.Application/    # CQRS commands, queries, DTOs, interfaces
├── SafeEye.Domain/         # Entities, repository interfaces
└── SafeEye.Infrastructure/ # EF Core DbContext, migrations, services
    ├── Persistence/
    │   ├── Configurations/ # Entity type configurations
    │   ├── Migrations/     # EF Core migrations
    │   └── Repositories/   # Repository implementations
    ├── Realtime/           # SignalR hub
    └── Services/           # JwtService, PasswordHasher, etc.
```

## Tech Stack

- **.NET 8** – ASP.NET Core Web API
- **Entity Framework Core** – ORM with Npgsql (PostgreSQL)
- **MediatR** – CQRS command/query handling
- **FluentValidation** – Request validation
- **BCrypt.Net** – Password hashing (work factor 12)
- **JWT Bearer** – Authentication
- **SignalR** – Real-time WebSocket communication
- **Swagger / OpenAPI** – API documentation
- **Firebase Admin SDK** – Push notifications (optional)
