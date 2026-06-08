# SafeEye API

Guardian-side backend for tracking IoT devices and receiving SOS alerts.

## Quick Start (Docker)

```bash
# 1. Start all services
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
| `FIREBASE_CREDENTIALS_PATH` | `/app/firebase-credentials.json` | Path to Firebase service account JSON |
| `FIREBASE_RTDB_URL` | *(optional)* | Firebase Realtime Database URL |

## API Endpoints

### Auth — `api/auth`

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | No | Register a new guardian account |
| `POST` | `/api/auth/login` | No | Authenticate and receive tokens |
| `POST` | `/api/auth/refresh` | No | Exchange a refresh token for a new access token |
| `POST` | `/api/auth/logout` | `[Authorize]` | Invalidate refresh token(s) |

### Users — `api/users`

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/users/me` | `[Authorize]` | Get current user profile |
| `PUT` | `/api/users/me` | `[Authorize]` | Update name and/or password |
| `PUT` | `/api/users/me/fcm-token` | `[Authorize]` | Register/refresh FCM push token |

### Guardian Devices — `api/guardian-devices`

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/api/guardian-devices` | `[Authorize]` | Add a device to watch list using device key |
| `GET` | `/api/guardian-devices` | `[Authorize]` | List all watched devices with latest location & SOS status |
| `GET` | `/api/guardian-devices/{id:guid}` | `[Authorize]` | Get a single watched device |
| `PUT` | `/api/guardian-devices/{id:guid}` | `[Authorize]` | Update display label |
| `DELETE` | `/api/guardian-devices/{id:guid}` | `[Authorize]` | Stop watching a device |

### IoT Devices — `api/iot`

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/iot` | No | List all registered IoT devices |
| `POST` | `/api/iot/register` | No | Register a new IoT device |
| `POST` | `/api/iot/sos` | `[DeviceAuth]` | REST fallback SOS trigger (normally via Firebase RTDB) |

### SOS Events — `api/sos`

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/sos` | `[Authorize]` | List SOS events (optional `?status=Active\|Resolved`) |
| `GET` | `/api/sos/{id:guid}` | `[Authorize]` | Get a single SOS event |
| `PUT` | `/api/sos/{id:guid}/resolve` | `[Authorize]` | Mark SOS as resolved |

### Real-time

| Type | Route | Auth | Description |
|---|---|---|---|
| WebSocket | `/hubs/tracking` | `[Authorize]` | SignalR hub for real-time SOS alerts and device tracking |

### Health

| Method | Route | Description |
|---|---|---|
| `GET` | `/health` | Health check (DB connectivity) |


## Tech Stack

- **.NET 8** – ASP.NET Core Web API
- **Entity Framework Core** – ORM with Npgsql (PostgreSQL 16)
- **MediatR** – CQRS command/query handling
- **FluentValidation** – Request validation (via MediatR pipeline behaviour)
- **BCrypt.Net** – Password hashing (work factor 12)
- **JWT Bearer** – Authentication with refresh token rotation
- **SignalR** – Real-time WebSocket communication
- **Swagger / OpenAPI** – API documentation
- **Firebase Admin SDK** – Push notifications (FCM, optional)
- **Firebase RTDB SSE** – Background listener for device SOS triggers
- **Docker** – Multi-stage build, Compose with PostgreSQL 16 (Alpine)
