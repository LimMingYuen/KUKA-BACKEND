# QES-KUKA-AMR-Penang-Renesas Project

## Project Overview

The QES-KUKA-AMR-Penang-Renesas project is a comprehensive Automated Mobile Robot (AMR) management system that provides a web-based interface for managing KUKA AMRs in a warehouse or factory environment. The system allows operators to assign missions to robots, monitor their status, and track mission execution.

### Architecture

The system is built using a .NET 8 microservices architecture with three main components:

1. **QES-KUKA-AMR-API** - The main API backend that handles all business logic, database operations, and communication with the actual AMR system
2. **QES-KUKA-AMR-WEB** - A web frontend application that provides the user interface for operators to interact with the system
3. **QES-KUKA-AMR-API-Simulator** - A simulation environment that mimics the real AMR system for testing and development purposes

### Key Features

- **Mission Management**: Create, schedule, and track custom mission execution for AMRs
- **Robot Monitoring**: Real-time tracking of robot status, battery levels, and locations
- **Manual Waypoint Handling**: Support for manual mission interventions with waypoints
- **Mission Scheduling**: Automated scheduling of recurring missions using cron expressions
- **Workflow Management**: Define and execute complex robot workflows
- **Analytics**: Track mission history and robot performance
- **Authentication**: JWT-based authentication with automated token refresh

### Database Schema

The system uses Entity Framework Core with SQL Server. Key database entities include:

- `MissionQueues`: Manages the queue of missions to be processed
- `MissionHistories`: Tracks completed mission history
- `MobileRobots`: Contains information about active robots
- `RobotManualPauses`: Handles manual pause/resume operations
- `SavedCustomMissions`: Stores predefined mission templates
- `SavedMissionSchedules`: Manages scheduled missions
- `WorkflowDiagrams`: Defines robot workflows
- `QrCodes`: Tracks QR code positions in the facility
- `MapZones`: Defines zones and areas in the facility

## Building and Running

### Prerequisites

- .NET 8 SDK
- SQL Server (or SQL Server Express)
- Node.js (if using any front-end build tools)

### Setup Instructions

1. **Database Setup**:
   - The `migration.sql` file contains all the necessary database schema changes
   - The application uses Entity Framework Core migrations
   - Update the connection string in `QES-KUKA-AMR-API/appsettings.json` to point to your database server

2. **Running the API Simulator (Development)**:
   - Update connection strings in `QES-KUKA-AMR-API/appsettings.json`:
     ```
     "DefaultConnection": "Server=(local);Database=QES_KUKA_AMR_Penang;User Id=mingyuen;Password=123456;TrustServerCertificate=true;"
     ```
   - The API simulator can be used to test without a real AMR system
   - Configure the API to point to the simulator in `appsettings.json`:
     ```
     "LoginService": {
       "LoginUrl": "http://localhost:5235/api/v1/data/sys-user/login"
     }
     ```

3. **Running the Applications**:
   - **API**: Run `dotnet run` in the `QES-KUKA-AMR-API` directory
   - **Web**: Run `dotnet run` in the `QES-KUKA-AMR-WEB` directory  
   - **API Simulator**: Run `dotnet run` in the `QES-KUKA-AMR-API-Simulator` directory

4. **Ports**:
   - API (QES-KUKA-AMR-API): Typically runs on port 5109
   - Web (QES-KUKA-AMR-WEB): Configure to run on a different port (e.g., 5000)
   - API Simulator: Typically runs on port 5235

### Configuration

The system is configured through appsettings.json files in each project:

- **QES-KUKA-AMR-API**: Contains the main business logic and database configuration
- **QES-KUKA-AMR-WEB**: Contains web frontend settings and API base URL
- **QES-KUKA-AMR-API-Simulator**: Contains simulation settings

## Development Conventions

### Code Structure

- **API Project**: Follows standard ASP.NET Core Web API patterns with Entity Framework Core for data access
- **Background Services**: Uses IHostedService for continuous operations like mission queue processing, job status polling, and scheduling
- **Dependency Injection**: Services are registered in Program.cs following SOLID principles
- **Logging**: Uses log4net for structured logging
- **Authentication**: JWT-based authentication with refresh token functionality

### Key Services

- **MissionQueueService**: Handles queuing and processing of missions
- **MissionSubmitterBackgroundService**: Submits missions to the AMR system
- **JobStatusPollerBackgroundService**: Monitors mission progress
- **SavedMissionSchedulerBackgroundService**: Manages scheduled missions
- **QueueService**: Manages the mission queue with configurable concurrency limits

### Testing

- The `tests` directory contains unit and integration tests
- The API simulator allows for testing without a real AMR system

## Security

- JWT authentication with configurable token lifetimes
- Session management with configurable idle timeout (default 8 hours)
- Token refresh mechanism to maintain user sessions
- CORS configured to allow any origin (may need to be restricted in production)

## Production Deployment Notes

- Update connection strings to point to production database
- Configure SSL certificates for HTTPS
- Set up proper logging and monitoring
- Configure reverse proxy if needed (e.g., nginx, IIS)
- Consider using a more robust message queue for production instead of in-memory queue
- Review and strengthen security settings, especially CORS policies