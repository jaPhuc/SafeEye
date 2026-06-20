# SafeEye API

Guardian-side backend for tracking IoT devices and receiving SOS alerts via Firebase Realtime Database.

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
| `GOOGLE_CLIENT_ID` | *(required for Google login)* | Google OAuth 2.0 client ID |
| `FIREBASE_CREDENTIALS_PATH` | `/app/firebase-credentials.json` | Path to Firebase service account JSON |
| `FIREBASE_RTDB_URL` | *(required for SSE listener)* | Firebase Realtime Database URL |

### `appsettings.json`

```json
"Firebase": {
    "CredentialsPath": "path/to/firebase-adminsdk.json",
    "RealtimeDatabaseUrl": "https://your-project-default-rtdb.region.firebasedatabase.app"
}
```

> Both `CredentialsPath` and `RealtimeDatabaseUrl` must be set for the Firebase background listener to start.

## Firebase Realtime Database Schema

### `/users/{userId}`

Created by the backend on registration or Google login. Updated by the mobile app when SOS is triggered.

```json
{
  "name": "string",
  "phone": "string | null",
  "latitude": "number | null",
  "longitude": "number | null",
  "sos": false
}
```

When the user presses the SOS button, the mobile app writes `sos: true` together with `latitude` and `longitude`.

### `/sos_requests/{requestId}`

Created by the mobile app (key format `req_{timestamp}`, status `pending`) and independently by the backend SSE listener (Firebase push ID `-N...`, status `active`).

```json
{
  "userId": "string",
  "latitude": "number | null",
  "longitude": "number | null",
  "status": "active | pending | resolved | cancelled",
  "timestamp": 1718000000000,
  "resolvedAt": "number | null",
  "resolvedBy": "string | null"
}
```

### `/device_status/{deviceKey}`

Written by IoT devices directly. Listened to by the backend for heartbeat, battery, and uptime updates.

```json
{
  "battery_percent": 85.5,
  "uptime_seconds": 3600
}
```

## Firebase Background Listener

`FirebaseRealtimeListenerService` is a `BackgroundService` that connects to Firebase RTDB via SSE (Server-Sent Events) and monitors two paths:

### 1. `/users.json` — SOS detection

- Records initial state of all users on connection (idempotency).
- Detects `sos` field transitions from `false → true`.
- On false→true: creates a new SOS request at `/sos_requests/{pushId}` with `status: "active"`.
- On true→false: does nothing (resolution is done exclusively by guardians via the API).
- After creating the SOS request, sends FCM push notifications to all guardians of the associated IoT device.
- Payload includes `{ type: "sos", userId, lat, lng }`.

### 2. `/device_status.json` — Heartbeat & battery

- Receives device status updates with `battery_percent` and `uptime_seconds`.
- Looks up the IoT device by `device_key` (Firebase key) in PostgreSQL.
- Updates `LastSeenAt`, `BatteryPercent`, and `UptimeSeconds` fields.

### Idempotency

`ConcurrentDictionary<string, bool>` tracks the last known SOS state per user. Only `false → true` transitions trigger actions. Repeated `true` values are ignored.

## API Endpoints

### Auth — `api/auth`

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | No | Register a new guardian account (name, email, password, phoneNumber). Creates user node in Firebase RTDB. |
| `POST` | `/api/auth/login` | No | Authenticate with email & password |
| `POST` | `/api/auth/google-login` | No | Authenticate with Google ID token. Creates user node in Firebase RTDB. |
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
| `POST` | `/api/iot/register` | No | Register a new IoT device (`label`, optional `firebaseDeviceKey`, optional `firebaseUserId`) |
| `POST` | `/api/iot/sos` | `[Authorize]` | REST acknowledge endpoint — actual SOS handling is done via Firebase RTDB listener |

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

## Push Notifications (FCM)

Firebase Admin SDK is initialized at startup from the credentials file. Push notifications are sent in two scenarios:

1. **Firebase SSE listener** — when a user's `sos` transitions to `true`, the backend creates a SOS request and sends FCM to all guardians of the linked IoT device via `SendToFirebaseUserAsync`.
2. **REST API** — the legacy `HandleSosTriggerCommand` handler sends FCM to guardians of the IoT device via `SendToGuardiansOfDeviceAsync`.

FCM tokens are registered by users via `PUT /api/users/me/fcm-token` and stored in the `Users.FcmToken` column.

## Database Entities

| Entity | Key fields | Notes |
|---|---|---|
| `User` | Id, Name, Email, PasswordHash, FcmToken, FirebaseUid | FirebaseUid maps to the node key in `/users/{firebaseUid}` |
| `IoTDevice` | Id, DeviceKey, Label, FirebaseDeviceKey, FirebaseUserId, LastSeenAt, BatteryPercent, UptimeSeconds | FirebaseUserId links to `/users/{userId}` for SOS notification routing |
| `GuardianDevice` | Id, GuardianId, DeviceId, Label | Join table: a guardian watches a device |
| `SosEvent` | Id, DeviceId, Latitude, Longitude, Status, ResolvedAt, ResolvedById | Created by legacy REST trigger; Firebase SOS requests are stored directly in RTDB |
| `RefreshToken` | Id, UserId, Token, ExpiresAt, CreatedAt, RevokedAt | Rotation-based refresh token |

## Tech Stack

- **.NET 8** — ASP.NET Core Web API
- **Entity Framework Core** — ORM with Npgsql (PostgreSQL 16)
- **MediatR** — CQRS command/query handling
- **FluentValidation** — Request validation (via MediatR pipeline behaviour)
- **BCrypt.Net** — Password hashing (work factor 12)
- **JWT Bearer** — Authentication with refresh token rotation
- **SignalR** — Real-time WebSocket communication
- **Swagger / OpenAPI** — API documentation
- **Firebase Admin SDK** — Push notifications (FCM, optional)
- **Firebase RTDB SSE** — Background listener for SOS triggers and device status
- **Docker** — Multi-stage build, Compose with PostgreSQL 16 (Alpine)
