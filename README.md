# Scheduled Reminders

Take-home full-stack assignment: scheduled reminders with JWT authentication and role-based access. This repository currently contains the **backend foundation only**. The Angular frontend, reminder Hosted Service, and execution history will be added in later steps.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run the backend

From the repository root:

```bash
cd backend
dotnet restore
dotnet run
```

The API listens on **http://localhost:5288** (see `backend/Properties/launchSettings.json`).

### In-Memory database

The API uses **EF Core In-Memory**. Data lives only in the running process:

- Seeded users are created on startup.
- Reminders exist only while the process is running.
- Restarting the API resets all reminders (users are re-seeded).

No SQL Server, Docker, or connection string is required.

### How to run / test the API

1. Start the backend with `dotnet run` from `backend/`.
2. Open Swagger UI (URL below).
3. Call `POST /api/auth/login` with a seeded user.
4. Click **Authorize**, paste the JWT (Swagger prepends `Bearer `), and test protected endpoints.

You can also call the API with any HTTP client. Send `Authorization: Bearer <token>` on reminder endpoints.

### Seeded credentials

| Role   | Username | Password    |
|--------|----------|-------------|
| Admin  | `admin`  | `Admin123!` |
| Viewer | `viewer` | `Viewer123!` |

### Swagger URL

http://localhost:5288/swagger

Swagger includes an **Authorize** button for JWT Bearer authentication.

### Configuration

JWT settings live under the `Jwt` section in `backend/appsettings.json`:

- `Issuer`
- `Audience`
- `ExpiryHours` (8)
- `Key` — signing key

Do not commit production secrets. `appsettings.Development.json` contains a **development-only** signing key. If `Jwt:Key` is empty, the application falls back to the same development key so the project runs immediately after cloning. Override `Jwt__Key` via environment variables for anything other than local evaluation.

CORS is enabled for the future Angular dev server: `http://localhost:4200`.

## Architecture & Technology Decisions

**Minimal API** was selected because the assignment specifies it, and the surface area (login plus reminder CRUD) is small enough that endpoint mapping stays readable without controllers.

**EF Core In-Memory** was selected so evaluators can clone and run the API without installing a database. Persistence is sufficient for local demonstration; it is not a production store.

**ReminderService** exists so create/update rules (including `Status = Pending` on create) live in one place instead of inline in `Program.cs`. Endpoint files stay thin: bind, validate, authorize, call the service, map HTTP results.

**JWT** is used for stateless API authentication. The Angular client will send the token; the backend issues and validates it (issuer, audience, lifetime, signing key).

**Role-based authorization on the backend** is the actual security boundary. GET is allowed for Admin and Viewer. POST and PUT require Admin. A Viewer token receives **403 Forbidden** on Admin-only operations even if a future UI hides those actions.

**Hosted Service (planned, not implemented):** a background worker will pick due reminders, simulate sending, update status, and decrement `FutureRunsCount` / reschedule according to `Frequency`. That work is intentionally out of scope for this step.

## Assumptions

- **`FutureRunsCount` is user-supplied.** It is **not** calculated from `Frequency` or `ScheduledAt`. Example: `Frequency = Daily` and `FutureRunsCount = 5` means five future executions are planned. Decrement/reschedule behavior belongs to the Hosted Service in the next step.
- Supported **Frequency** values are `Once`, `Daily`, `Weekly`, and `Monthly`.
- Reminder sending is **simulated only**. This step does not send email, SMS, or any external notification.
- New reminders always start with **`Status = Pending`**. Clients cannot set status on create or update; the Hosted Service will own status transitions later.
- `ScheduledAt` must be a valid `DateTime`; it is **not** required to be in the future.
- Name max length is 200 characters; message max length is 2000 characters; `FutureRunsCount` must be a non-negative integer.
- Invalid credentials on login return **401 Unauthorized** without distinguishing unknown user vs wrong password.
- In-Memory data is lost on process restart.
- `frontend/` is a placeholder directory until the Angular app is added.

## AI Usage Disclosure

AI development tools were used during implementation, including **Cursor**. They assisted with planning, scaffolding, code generation, refactoring, and debugging. The submitted solution was reviewed and understood by the developer; AI assistance did not replace ownership of the design or the code.

## API endpoints (this step)

| Method | Path | Roles |
|--------|------|--------|
| POST | `/api/auth/login` | Anonymous |
| GET | `/api/reminders` | Admin, Viewer |
| GET | `/api/reminders/{id}` | Admin, Viewer |
| POST | `/api/reminders` | Admin |
| PUT | `/api/reminders/{id}` | Admin |

## Out of scope (next steps)

- Angular application
- Hosted Service / simulated execution
- Execution history
