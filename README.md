# Scheduled Reminders

Take-home full-stack assignment: scheduled reminders with JWT authentication, role-based access, a simulated execution worker, and an Angular UI.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (for the Angular app)

### Run the backend

```bash
cd backend
dotnet restore
dotnet run
```

The API listens on **http://localhost:5288**.

### Run the Angular app

In a second terminal:

```bash
cd frontend
npm install
npm start
```

The UI listens on **http://localhost:4200** and calls the API at `http://localhost:5288`. CORS is already enabled for that origin.

### In-Memory database

The API uses **EF Core In-Memory**. Data lives only in the running process:

- Seeded users are created on startup.
- Reminders and execution history exist only while the process is running.
- Restarting the API resets reminders and history (users are re-seeded).

No SQL Server, Docker, or connection string is required.

### How to run / test

1. Start the backend, then the Angular app.
2. Sign in as Admin or Viewer.
3. As Admin, create a reminder. Use a scheduled time in the past (or wait until it is due) to see the hosted service simulate a send.
4. Open the reminder detail page for execution history. The list refreshes every 5 seconds.

API-only testing: http://localhost:5288/swagger — log in, click **Authorize**, paste the JWT.

### Seeded credentials

| Role   | Username | Password    |
|--------|----------|-------------|
| Admin  | `admin`  | `Admin123!` |
| Viewer | `viewer` | `Viewer123!` |

### Swagger URL

http://localhost:5288/swagger

### Configuration

JWT settings live under the `Jwt` section in `backend/appsettings.json`:

- `Issuer`
- `Audience`
- `ExpiryHours` (8)
- `Key` — signing key

Do not commit production secrets. `appsettings.Development.json` contains a **development-only** signing key. If `Jwt:Key` is empty, the application falls back to the same development key so the project runs immediately after cloning. Override `Jwt__Key` via environment variables for anything other than local evaluation.

`ReminderProcessor:PollIntervalSeconds` (default `5`) controls how often due reminders are processed.

The Angular API base URL is `frontend/src/environments/environment.ts` (`http://localhost:5288`).

CORS is enabled for `http://localhost:4200`.

## Architecture & Technology Decisions

**Minimal API** was selected because the assignment specifies it, and the surface area is small enough that endpoint mapping stays readable without controllers.

**EF Core In-Memory** was selected so evaluators can clone and run the API without installing a database.

**ReminderService** holds create/update/read rules (including `Status = Pending` on create). Endpoint files bind, validate, authorize, and map HTTP results.

**JWT** authenticates the Angular client. The backend issues and validates issuer, audience, lifetime, and signing key. A role claim on the token is not trusted from the client body — it is issued at login from the seeded user record.

**Role-based authorization on the backend** is the security boundary. GET (including execution history) is allowed for Admin and Viewer. POST and PUT require Admin. A Viewer token receives **403 Forbidden** on Admin-only operations. Angular route guards hide create/edit routes as a UX layer only.

**Hosted Service** polls due reminders, simulates a send (no email/SMS provider), writes execution history, decrements `FutureRunsCount`, and reschedules `ScheduledAt` for Daily/Weekly/Monthly while remaining runs exist.

**Angular** is a standalone client: login, reminder list/detail/form, JWT interceptor, `authGuard` / `adminGuard`.

## Assumptions

- **`FutureRunsCount` is user-supplied.** It is **not** calculated from `Frequency` or `ScheduledAt`. Example: `Frequency = Daily` and `FutureRunsCount = 5` means five future executions are planned.
- After a successful simulated run, `FutureRunsCount` is decremented by 1. Daily/Weekly/Monthly reminders with remaining runs stay active, return to `Pending`, and `ScheduledAt` moves forward by 1 day, 7 days, or 1 month.
- **Once** reminders complete after a single execution: remaining `FutureRunsCount` is set to 0, `IsActive` is false, and `Status` is `Success`.
- Supported **Frequency** values are `Once`, `Daily`, `Weekly`, and `Monthly`.
- Reminder sending is **simulated only**. Every processed run currently records `Success`. There is no email, SMS, or external notification provider.
- New reminders always start with **`Status = Pending`**. Clients cannot set status on create or update.
- `ScheduledAt` must be a valid `DateTime`; it is **not** required to be in the future. A past time is useful for local evaluation of the processor.
- Name max length is 200 characters; message max length is 2000 characters; `FutureRunsCount` must be a non-negative integer.
- Invalid credentials on login return **401 Unauthorized** without distinguishing unknown user vs wrong password.
- In-Memory data is lost on process restart.
- Angular UI restrictions are not a substitute for API authorization.

## AI Usage Disclosure

AI development tools were used during implementation, including **Cursor**. They assisted with planning, scaffolding, code generation, refactoring, and debugging. The submitted solution was reviewed and understood by the developer; AI assistance did not replace ownership of the design or the code.

## API endpoints

| Method | Path | Roles |
|--------|------|--------|
| POST | `/api/auth/login` | Anonymous |
| GET | `/api/reminders` | Admin, Viewer |
| GET | `/api/reminders/{id}` | Admin, Viewer |
| GET | `/api/reminders/{id}/executions` | Admin, Viewer |
| POST | `/api/reminders` | Admin |
| PUT | `/api/reminders/{id}` | Admin |
