# GEMINI.md

This file provides guidance to the Gemini CLI when working with code in this repository.

## Project Overview

QES KUKA AMR (Autonomous Mobile Robot) Management System is a .NET 8.0 solution for managing KUKA AMR fleet operations. The system consists of three main applications:

-   **QES-KUKA-AMR-API**: The main backend API that manages mission queues, robot operations, analytics, and workflow orchestration.
-   **QES-KUKA-AMR-WEB**: An ASP.NET Razor Pages frontend for user interaction.
-   **QES-KUKA-AMR-API-Simulator**: A test simulator that mimics the external AMR control system API.

The core of the application is a sophisticated mission queue system that uses a combination of a database and in-memory semaphores to manage concurrency and process missions in the background.

## Building and Running

### Build

```bash
# Build the entire solution
dotnet build QES-KUKA-AMR.sln

# Build a specific project
dotnet build QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj
```

### Run

```bash
# Run the main API (default port: 5109)
dotnet run --project QES-KUKA-AMR-API/QES-KUKA-AMR-API.csproj

# Run the web frontend
dotnet run --project QES-KUKA-AMR-WEB/QES-KUKA-AMR-WEB.csproj

# Run the API simulator (port: 5235)
dotnet run --project QES-KUKA-AMR-API-Simulator/QES-KUKA-AMR-API-Simulator.csproj
```

### Testing

```bash
# Run all tests
dotnet test
```

## Development Conventions

### Architecture

-   **Service Layer**: The application follows a service-oriented architecture with services organized by domain.
-   **Dependency Injection**: Services are registered with the built-in dependency injection container in `Program.cs`. The `QueueService` is registered as a singleton, while most other services are scoped.
-   **Background Services**: Long-running tasks such as mission processing, submission, and status polling are handled by background services that implement `IHostedService`.
-   **Mission Queue**: The `QueueService` manages the mission queue. It uses a `SemaphoreSlim` to control global concurrency and a database to persist the queue state. The `InitializeAsync` method in `QueueService` is critical for synchronizing the semaphore state with the database on application startup.
-   **Configuration**: Application settings are stored in `appsettings.json` and accessed through the `IOptions` pattern.

### Database

-   **Entity Framework Core**: The application uses EF Core for data access.
-   **Migrations**: Database migrations are managed using the `dotnet ef` command-line tools.
-   **Connection String**: The database connection string is configured in `appsettings.json`.

### Logging

-   **log4net**: The application uses log4net for logging. The configuration is in `log4net.config`.
