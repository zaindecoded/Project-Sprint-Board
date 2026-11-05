# TaskSprintBoard

TaskSprintBoard is a project and sprint planning app for organizing team work, tracking task progress, and spotting delivery risks.

## Features

- Organize work into projects and time-boxed sprints.
- Create, assign, prioritize, and track tasks through their lifecycle.
- Add task comments and review status change history.
- View project and task progress in a dashboard.
- Get an advisory task delay risk assessment based on task details and progress.
- Use role-based access for administrators, project managers, and team members.

## Technology

- **Frontend:** React 18, React Router, Vite, Bootstrap, Chart.js
- **Backend:** ASP.NET Core 8 Web API, C#
- **Data:** Entity Framework Core 8 and SQL Server
- **Authentication:** JWT bearer tokens

The delay risk assessment uses backend rules and weighted conditions. It is advisory and does not automatically change task status.

The project chat widget is available to admins and project managers. It uses local intent matching against current project, sprint, task, and assignment data; it needs no API key and sends no project data to an AI provider.

## Architecture

The React app communicates with the ASP.NET Core API over HTTP. The API is organized into controllers, business logic services, and data repositories backed by Entity Framework Core.

## Run locally

Prerequisites: .NET 8 SDK, Node.js, and SQL Server Express (`.\\SQLEXPRESS`).

1. Start the API from the repository root:

   ```powershell
   dotnet run --project Backend/TaskSprintBoard.Backend.Presentation --launch-profile http
   ```

   The API applies database migrations on startup and is available at `http://localhost:5183` (Swagger: `/swagger`). Configure `ConnectionStrings:DefaultConnection` in `appsettings.Development.json` if your SQL Server differs.

2. In another terminal, start the React app:

   ```powershell
   cd Frontend/ReactApp
   npm install
   npm run dev
   ```

   Open `http://localhost:5173`. Vite proxies `/api` requests to the local API.

The project chat widget is shown to admins and project managers. It answers questions about stored project, sprint, task, and assignment fields using local intent matching; no OpenAI key is needed. For active projects, it follows the project list display rule: projects with past end dates are shown as overdue, not active.

## Project structure

```text
Backend/TaskSprintBoard.slnx
Backend/TaskSprintBoard.Backend.Presentation/   API controllers and startup
Backend/TaskSprintBoard.Backend.Business Logic Layer/  Services, DTOs, validators, enums
Backend/TaskSprintBoard.Backend.Data Layer/     EF Core context, entities, repositories, migrations
TaskSprintBoard.Tests/                  Backend tests
Frontend/ReactApp/                      React application
