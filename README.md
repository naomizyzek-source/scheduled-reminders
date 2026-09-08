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

The UI listens on **http://localhost:4200** and calls the API at `http://localhost:5288`. CORS allows `http://localhost:4200` and `http://127.0.0.1:4200`.

### In-Memory database

The API uses **EF Core In-Memory**. Data lives only in the running process:

- Seeded users are created on startup.
- Reminders and execution history exist only while the process is running.
- Restarting the API resets reminders and history (users are re-seeded).

No SQL Server, Docker, or connection string is required.

### How to run / test

1. Start the backend, then the Angular app.
2. Sign in as Admin or Viewer.
3. As Admin, create a reminder scheduled a few seconds in the past (`IsActive = true`, `FutureRunsCount >= 1`). Status starts as **Pending**.
4. Within about one second it should move to **Running** for ~10 seconds, then **Success** or **Failed**.
5. Open the reminder detail page (or `GET /api/reminders/{id}/executions`) for history, most recent first.

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

`ReminderProcessor` settings:

- `PollIntervalSeconds` (default `1`) — how often the worker looks for due reminders.
- `SimulationDelaySeconds` (default `10`) — simulated send duration.

The Angular API base URL is `frontend/src/environments/environment.ts` (`http://localhost:5288`).

CORS is enabled for `http://localhost:4200` and `http://127.0.0.1:4200`.

## Architecture & Technology Decisions

**Minimal API** was selected because the assignment specifies it, and the surface area is small enough that endpoint mapping stays readable without controllers.

**EF Core In-Memory** was selected so evaluators can clone and run the API without installing a database.

**ReminderService** holds create/update/read rules (including `Status = Pending` on create) and execution-history queries. Endpoint files bind, validate, authorize, and map HTTP results.

**JWT** authenticates the Angular client. The backend issues and validates issuer, audience, lifetime, and signing key. A role claim on the token is not trusted from the client body — it is issued at login from the seeded user record.

**Role-based authorization on the backend** is the security boundary. GET (including execution history) is allowed for Admin and Viewer. POST and PUT require Admin. A Viewer token receives **403 Forbidden** on Admin-only operations. Angular route guards hide create/edit routes as a UX layer only.

**Angular** is a standalone client: login, reminder list/detail/form, JWT interceptor, `authGuard` / `adminGuard`.

## Background processing

A `BackgroundService` (`ReminderProcessorHostedService`) runs for the lifetime of the API process. It is the right fit here: one in-process worker, no extra infrastructure, and built-in `CancellationToken` support for shutdown.

**Polling.** About every second it opens a DI scope and asks `ReminderExecutionService` for eligible reminders:

- `IsActive == true`
- `Status == Pending`
- `ScheduledAt <= DateTime.UtcNow`
- `FutureRunsCount > 0`

**Claim before wait.** Each eligible reminder is claimed **sequentially** in that cycle: `Pending` → `Running`, a `ReminderExecution` row is created with `Status = Running` and `StartedAt` (UTC), and `SaveChangesAsync` runs **before** the next reminder is claimed and **before** any 10-second wait. Because only one writer polls, that persisted claim is enough to stop the same reminder from being picked again while it is `Running`. No extra locks, tokens, or brokers are used.

**Concurrent simulation.** After all claims in the cycle are saved, each claimed reminder’s 10-second simulated send runs **concurrently** (`Task.WhenAll`). Each task creates a **new DI scope** via `IServiceScopeFactory` and therefore a **new `DbContext`**. `DbContext` is not shared across threads.

**State transitions.**

- Claim: reminder `Pending` → `Running`; execution `Running` with `StartedAt`.
- After the 10-second wait, the simulated result is randomly `Success` or `Failed`.
- The execution gets `CompletedAt` and that final status.
- **Once:** `FutureRunsCount = 0`, `IsActive = false`, reminder status stays `Success` or `Failed` (not reset to `Pending`). It will not run again.
- **Daily / Weekly / Monthly:** `FutureRunsCount` decreases by 1 (including on failure). If it remains `> 0`, `ScheduledAt` moves +1 day / +7 days / +1 calendar month and status returns to `Pending`. If it reaches `0`, there is no next run; status stays `Success` or `Failed` and **`IsActive` is set to `false`** so the UI active flag matches “no remaining runs”.

**FutureRunsCount** is only consumed when an execution actually runs (claim + simulated send). It is not derived from frequency.

**Cancellation.** `Task.Delay` uses the hosted-service stopping token. `OperationCanceledException` from shutdown is not caught and rewritten as `Failed`. An in-flight execution may remain `Running` with a null `CompletedAt` if the process stops during the wait.

**Isolation.** Claiming a reminder and completing a simulated send are each wrapped so one reminder’s unexpected exception is logged and does not stop the poll loop or other concurrent simulations.

## Time handling

The backend uses **UTC** for scheduling and persistence. Incoming `ScheduledAt` values are normalized to UTC. Eligibility compares `ScheduledAt` to `DateTime.UtcNow`. Execution `StartedAt` / `CompletedAt` and reminder `CreatedAt` / `UpdatedAt` are UTC.

## Execution history

Every actual execution attempt creates one `ReminderExecution` (including Failed attempts). `GET /api/reminders/{id}/executions` is available to Admin and Viewer, returns 404 if the reminder does not exist, and orders by `StartedAt` descending (most recent first). The API returns DTOs (`Id`, `ReminderId`, `StartedAt`, `CompletedAt`, `Status`), not EF entities.

## Failure behavior

A simulated **Failed** result still counts as an execution: it is recorded, `FutureRunsCount` is decremented, and a recurring reminder is rescheduled if runs remain. There is **no automatic retry** of the same attempt.

## Assumptions

- **`FutureRunsCount` is user-supplied.** It is **not** calculated from `Frequency` or `ScheduledAt`. Example: `Frequency = Daily` and `FutureRunsCount = 5` means five future executions are planned.
- Failed executions consume a run the same way successful ones do; there is no retry of that attempt.
- When `FutureRunsCount` reaches 0 (Once after its single run, or recurring after the last run), **`IsActive` is set to `false`**.
- Once reminders do not reschedule; remaining `FutureRunsCount` is forced to 0 after the execution.
- Supported **Frequency** values are `Once`, `Daily`, `Weekly`, and `Monthly`.
- Reminder sending is **simulated only** (`Task.Delay` of 10 seconds, then a random Success/Failed). There is no email, SMS, or external notification provider.
- New reminders always start with **`Status = Pending`**. Clients cannot set status on create or update.
- `ScheduledAt` must be a valid `DateTime`; it is **not** required to be in the future. A past time is useful for local evaluation of the processor.
- Name max length is 200 characters; message max length is 2000 characters; `FutureRunsCount` must be a non-negative integer.
- Invalid credentials on login return **401 Unauthorized** without distinguishing unknown user vs wrong password.
- In-Memory data is lost on process restart.
- Angular UI restrictions are not a substitute for API authorization.
- Shutdown during the simulated wait does not mark the execution Failed; it may stay Running.

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
