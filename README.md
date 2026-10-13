# TaskSprintBoard

TaskSprintBoard is a sprint planning and task tracking application. Users can organize projects and sprints, manage tasks and assignments, record comments and status changes, review a dashboard, and request an advisory task delay risk evaluation.

## Technology

- React 18, React Router, and Vite
- ASP.NET Core 8 Web API and C#
- Entity Framework Core 8 and SQL Server
- JWT bearer authentication
- Chart.js with `react-chartjs-2`
- xUnit backend tests

## Architecture

```text
React frontend
    ↓ HTTP/JSON
ASP.NET Core controllers
    ↓
Business logic services
    ↓
Data repositories
    ↓
Entity Framework Core
    ↓
SQL Server
```

Controllers expose HTTP routes and translate requests and responses. Services contain application rules and validation. Repositories perform database queries and updates through the EF Core `TaskSprintBoardDbContext`. The React frontend calls the API through one authenticated Axios client.

## Project structure

```text
TaskSprintBoard.slnx
TaskSprintBoard.Backend.Presentation/   API startup, controllers, middleware, settings
TaskSprintBoard.Backend.BusinessLogic/  Services, DTOs, validators, enums
TaskSprintBoard.Backend.Data/           EF Core context, entities, repositories, migrations, seed helper
TaskSprintBoard.Tests/                  xUnit tests
Frontend/ReactApp/                      React application and Vite configuration
Frontend/ReactApp/src/pages/            Application pages
Frontend/ReactApp/src/services/         API service functions and shared apiClient.js
Frontend/ReactApp/src/routes/           Authentication route guards
Frontend/ReactApp/src/styles/           Global responsive styles
postman/TaskSprintBoard.postman_collection.json
```

## Requirements

- .NET 8 SDK
- Node.js and npm
- SQL Server. The default configuration uses SQL Server LocalDB on Windows.
- `dotnet-ef` 8.0.12 for applying migrations. If the command is missing, install it with `dotnet tool install --global dotnet-ef --version 8.0.12`.

## Database and local configuration

The connection string is `ConnectionStrings:DefaultConnection` in `TaskSprintBoard.Backend.Presentation/appsettings.json` and `appsettings.Development.json`. It points to SQL Server Express instance `localhost\SQLEXPRESS` and database `TaskSprintBoard`. Override it with `ConnectionStrings__DefaultConnection` when using another SQL Server. Do not commit credentials in a connection string.

The API requires a signing key at startup. Supply `TASKSPRINTBOARD_JWT_KEY` in the current shell before running the API or EF commands. Use a private random value of at least 32 characters; keep it out of source control and do not reuse a production secret for development.

PowerShell example (replace the placeholder in your own shell):

```powershell
$env:TASKSPRINTBOARD_JWT_KEY = "<your-private-random-key-of-at-least-32-characters>"
```

The API applies checked-in migrations automatically on startup. On the existing integer-keyed `TaskSprintBoard` schema, startup moves the original tables into the `Legacy` schema, creates the current GUID-keyed tables, and copies supported data while preserving the original rows. Legacy-only fields and tables remain available under `Legacy`.

## Run the backend

From the repository root, set `TASKSPRINTBOARD_JWT_KEY` as above, then run:

```powershell
dotnet restore
dotnet build TaskSprintBoard.slnx
dotnet run --project TaskSprintBoard.Backend.Presentation --launch-profile https
```

The configured HTTPS API URL is `https://localhost:7078`; the same profile also listens on `http://localhost:5183`. In Development, Swagger UI is at `https://localhost:7078/swagger` (or `http://localhost:5183/swagger`). Trust the ASP.NET Core development certificate if your browser requests it.

## Run the frontend

In another terminal:

```powershell
cd Frontend/ReactApp
npm.cmd install
npm.cmd run dev
```

Vite serves the frontend at `http://localhost:5173`. Its `/api` proxy targets `https://localhost:7078` by default; set `VITE_BACKEND_PROXY_TARGET` if the backend is running at another address. The shared API client uses `/api` as its default base URL. `VITE_API_BASE_URL` can override the client base URL and should include `/api`; leave it at `/api` for the normal local setup so the Vite proxy handles requests. The API does not currently configure cross-origin requests, so pointing the browser directly at a different API origin requires CORS configuration.

## Accounts and authentication

`POST /api/auth/login` is the only public authentication route; there is no public registration endpoint. `GET /api/users` requires an Admin or Project Manager token. In Development, startup creates the local demo administrator account `alice@company.com` / `Admin123!` only if that email is absent. Existing account credentials and roles are preserved. Other environments do not seed accounts.

Swagger uses HTTP bearer authentication. To call a protected route:

1. Start the API and open `/swagger`.
2. Call `POST /api/auth/login` with a provisioned account's `email` and `password`.
3. Copy the `token` from the response.
4. Select **Authorize** in Swagger and enter the token as a bearer token.
5. Call the protected API routes.

Projects, sprints, tasks, assignments, comments, and status history require one of the `Admin`, `ProjectManager`, or `TeamMember` roles. Dashboard and Intelligence require an authenticated user. The frontend stores and sends the JWT through its shared Axios client; protected frontend routes redirect unauthenticated users to `/login`.

## API modules

All routes below are relative to `/api`. Collection routes that accept `page` and `pageSize` use paginated responses; supported search and filter parameters are noted in the controller implementation.

| Module | Available routes |
| --- | --- |
| Authentication | `POST /auth/login` |
| Projects | `GET /projects`, `GET /projects/{id}`, `POST /projects`, `PUT /projects/{id}`, `DELETE /projects/{id}` |
| Sprints | `GET /sprints`, `GET /sprints/{id}`, `POST /sprints`, `PUT /sprints/{id}`, `DELETE /sprints/{id}` |
| Tasks | `GET /tasks`, `GET /tasks/{id}`, `POST /tasks`, `PUT /tasks/{id}`, `DELETE /tasks/{id}` |
| Assignments | `GET /assignments`, `GET /assignments/{id}`, `POST /assignments`, `PUT /assignments/{id}`, `DELETE /assignments/{id}` |
| Comments | `GET /comments`, `GET /comments/{id}`, `POST /comments`, `PUT /comments/{id}`, `DELETE /comments/{id}` |
| Status history | `GET /status-history`, `GET /status-history/{id}`, `GET /tasks/{taskId}/status-history` |
| Dashboard | `GET /dashboard/summary` |
| Intelligence | `POST /intelligence/evaluate` |

The functional task history endpoint is `GET /api/tasks/{taskId}/status-history`. It returns an array ordered newest first. Each item contains `id`, `taskId`, `oldStatus`, `newStatus`, `changedAt`, `changedBy`, `createdAt`, `updatedAt`, `reason`, and `isActive`. Status changes made through task updates are recorded when the status changes. The separate `/tasks/{id}/history` action is an empty stub and is not the history API used by the application.

The dashboard summary returns `totalProjects`, `activeProjects`, `activeSprints`, `totalTasks`, `completedTasks`, `overdueTasks`, `blockedTasks`, and `highRiskTasks`.

## Task delay risk

`POST /api/intelligence/evaluate` accepts `taskId`, `estimateHours`, `taskAge`, `daysRemaining`, `progressPercentage`, `isBlocked`, `priority`, `currentStatus`, and `numberOfStatusChanges`. The backend calculates the risk using rules and weighted conditions; it does not use ML.NET or an external AI service. The response contains `riskLevel`, `riskScore`, `confidence`, `riskFactors`, `explanation`, and `timestamp` (as well as `taskId`). Confidence is returned as a fraction of the score (for example, `0.62` represents 62%). The frontend displays the result as advisory information and does not automatically change task status. No evaluation history endpoint is exposed.

Tasks without a due date cannot currently be evaluated through the frontend because the API requires the caller to provide `daysRemaining`; the UI disables evaluation for those tasks.

## Validation and deletion behavior

Implemented checks include required project, sprint, task, relationship, and comment fields; unique project names and sprint names within a project; sprint date ordering; positive task estimates; task progress from 0 to 100; valid task status and priority values; and allowed task status transitions. Assignment and comment creation validate referenced tasks and users, and an active user cannot be assigned to the same task twice. Backend validation remains authoritative.

Project, sprint, task, assignment, and comment delete operations mark records deleted and inactive rather than physically removing them. The data model has `IsDeleted` and `IsActive` flags; query behavior varies by repository and endpoint.

## Postman collection

Import `postman/TaskSprintBoard.postman_collection.json` into Postman. Start the API with the `https` launch profile; the collection uses its HTTP endpoint (`http://localhost:5183/api`) to avoid local development-certificate errors. It is prefilled with the Development demo Admin credentials; change them if using another account. Run the collection in order. Login saves the JWT, the Users request selects a Team Member ID, and create requests save their IDs for subsequent requests. Cleanup is last and soft-deletes created records.

## Verification

Run the frontend production build from `Frontend/ReactApp`:

```powershell
npm.cmd run build
```

Run backend tests from the repository root:

```powershell
dotnet test --nologo
```

Latest verification for this delivery: frontend build passed; backend tests passed (42 passed, 0 failed). No frontend automated test suite is configured. A successful build/test run does not confirm live browser/API behavior; that also requires a running SQL Server/API and a provisioned user account.
