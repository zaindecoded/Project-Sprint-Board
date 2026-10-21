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
- **Tests:** xUnit

The delay risk assessment uses backend rules and weighted conditions. It is advisory and does not automatically change task status.

## Architecture

The React app communicates with the ASP.NET Core API over HTTP. The API is organized into controllers, business logic services, and data repositories backed by Entity Framework Core.

## Project structure

```text
TaskSprintBoard.slnx
TaskSprintBoard.Backend.Presentation/   API controllers and startup
TaskSprintBoard.Backend.Business Logic Layer/  Services, DTOs, validators, enums
TaskSprintBoard.Backend.Data Layer/     EF Core context, entities, repositories, migrations
TaskSprintBoard.Tests/                  Backend tests
Frontend/ReactApp/                      React application
postman/                                API request collection
```
