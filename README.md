# Pulse: Uptime Monitoring API

**Pulse** is a backend application for monitoring the availability and health of websites and HTTP APIs. Built with **ASP.NET Core Minimal APIs and PostgreSQL**, it periodically checks configured endpoints, records their responses, and calculates the current health status of monitored services.

The project is developed as a personal backend engineering project, with an emphasis on API design, background processing, database persistence, security, and automated testing.

**English** | [Русский](README.ru.md)

## Features

- **Service management** — Create, view, update, and delete services through a REST API.
- **Configurable HTTP monitors** — Assign multiple monitors to a service, each with its own endpoint path, HTTP method (GET or HEAD), and check interval.
- **Background monitoring** — Automatically execute scheduled checks using an ASP.NET Core hosted service.
- **Check history** — Store timestamps, HTTP status codes, response times, and check outcomes in PostgreSQL.
- **Health status aggregation** — Calculate service status (`Up`, `Down`, or `Unknown`) from the latest results of its enabled monitors.
- **Authentication and authorization** — Use ASP.NET Core Identity and bearer-token authentication to protect user data.
- **SSRF protection** — Apply the Microsoft AntiSSRF library to outgoing requests to restrict access to unsafe network destinations.
- **Interactive API documentation** — Explore and test endpoints using Swagger UI in development.
- **Automated testing** — Unit and integration tests covering monitoring logic, HTTP failures, API endpoints, and database behavior.

## Tech Stack

| Category | Technology |
|---|---|
| Language | C# |
| Framework | .NET 10, ASP.NET Core Minimal API |
| Database | PostgreSQL |
| ORM | Entity Framework Core 10, Npgsql |
| Authentication | ASP.NET Core Identity, bearer tokens |
| Background processing | `BackgroundService`, dependency injection |
| HTTP monitoring | `HttpClient`, `IHttpClientFactory` |
| Security | Microsoft.Security.AntiSSRF |
| Testing | xUnit v3, Testcontainers, WebApplicationFactory |
| API documentation | Swagger / OpenAPI |

## Architecture

Pulse separates API operations, HTTP checking, result persistence, and service-status calculation.

```text
                         REST API
                            |
                   Authentication
                            |
                     Service / Monitor
                       Management
                            |
                        PostgreSQL
                            ^
                            |
                     MonitorWorker
                            |
              +-------------+-------------+
              |                           |
       DueMonitorProcessor         ServiceStatusHandler
              |
       +------+------+
       |             |
HttpMonitorChecker  CheckResultWriter
       |             |
 External APIs    PostgreSQL
```

### Monitoring workflow

1. An authenticated user registers a service and configures one or more monitors.
2. `MonitorWorker` periodically starts a monitoring iteration.
3. `DueMonitorProcessor` selects enabled monitors whose next scheduled check is due.
4. `HttpMonitorChecker` sends the configured HTTP request and measures its duration.
5. `CheckResultWriter` records the outcome, while the processor schedules the next check.
6. `ServiceStatusHandler` evaluates the latest results of enabled monitors and updates the service's overall health status.

### Check outcomes

Each HTTP check produces one of the following outcomes:

| Outcome | Description |
|---|---|
| `Success` | The endpoint returned a successful HTTP response. |
| `HttpError` | The endpoint responded with a non-success HTTP status. |
| `Timeout` | The request exceeded its configured timeout. |
| `NetworkError` | The request failed because of a network or connection error. |

Service health is calculated using the latest check result for each enabled monitor. A service is `Up` when all enabled monitors report success, `Down` when a failure is present and all enabled monitors have results, and `Unknown` when there are no enabled monitors or at least one enabled monitor has not yet been checked.

## API Endpoints

All service, monitor, and check-result operations require authentication.

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/register` | Register a user |
| `POST` | `/login` | Authenticate and obtain a token |
| `GET`, `POST` | `/services` | List or create services |
| `GET`, `PUT`, `DELETE` | `/services/{serviceId}` | Manage an individual service |
| `GET`, `POST` | `/services/{serviceId}/monitors` | List or create monitors |
| `GET`, `PUT`, `DELETE` | `/services/{serviceId}/monitors/{monitorId}` | Manage an individual monitor |
| `GET` | `/check-results/` | List the authenticated user's check results |
| `GET` | `/services/{serviceId}/monitors/{monitorId}/check-results/` | View a monitor's check history |
| `GET`, `DELETE` | `/services/{serviceId}/monitors/{monitorId}/check-results/{checkResultId}` | View or delete a check result |

Users can access only services, monitors, and results associated with their accounts. HTTP check results are generated by the monitoring system rather than created through public POST endpoints.

## Getting Started

### Prerequisites

- .NET 10 SDK
- PostgreSQL
- Git
- `dotnet-ef` CLI tool for database migrations
- Docker, for integration tests using Testcontainers

### 1. Clone the repository

```bash
git clone https://github.com/fptitsyn/pulse.git
cd pulse
dotnet restore
```

### 2. Configure PostgreSQL

Create a PostgreSQL database and configure the application's `ConnStr` connection string.

For example, on Linux:

```bash
export ConnectionStrings__ConnStr="Host=localhost;Port=5432;Database=pulse;Username=YOUR_USER;Password=YOUR_PASSWORD"
```

Replace the placeholders with your PostgreSQL credentials. The database user must have the permissions required to apply migrations.

### 3. Apply database migrations

```bash
dotnet ef database update --project app --startup-project app
```

### 4. Run the application

```bash
dotnet run --project app
```

The API address is shown in the application output. When running in the Development environment, Swagger UI is available at:

```text
<application-url>/swagger
```

Register a user, log in to obtain a bearer token, and use the authenticated API endpoints to create services and monitors.

## Testing

Pulse uses automated tests at multiple levels:

- **Unit tests** verify HTTP-checking logic, timeout handling, request failures, and validation without making real network requests.
- **Integration tests** verify API behavior, authentication, database persistence, and monitor processing using a real PostgreSQL instance.
- **Testcontainers** provides disposable PostgreSQL containers for integration tests.
- **WebApplicationFactory** hosts the ASP.NET Core application in a test environment.

Run the full test suite:

```bash
dotnet test
```

Make sure Docker is running before executing integration tests.

## Project Structure

```text
app/
├── Models/           # Database entities and EF Core context
├── Monitoring/       # Background worker, HTTP checks, status calculation
├── RouteHandlers/    # Minimal API endpoints
├── Migrations/       # EF Core database migrations
└── Program.cs        # Application setup and DI configuration

tests/
├── Pulse.UnitTests/
└── Pulse.IntegrationTests/
```

## Project Status

Pulse is an **ongoing personal project** focused on backend development.

The current implementation provides authenticated service management, scheduled HTTP monitoring, persisted check history, and service-health calculations.

The application is currently backend-focused; a user-facing dashboard, alert notifications, and advanced availability analytics are outside the implemented scope.

## Motivation

I built Pulse to gain practical experience designing a backend system beyond basic CRUD operations.

The project explores scheduled background work, dependency injection and service lifetimes, reliable database operations, HTTP failure handling, secure outbound requests, and automated testing with real infrastructure.
