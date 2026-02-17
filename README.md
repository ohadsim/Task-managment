# Extensible Task-Management Platform

A full-stack task management system built with ASP.NET Core and React that cleanly separates general workflow rules from task-type-specific rules using the Strategy Pattern.

## Quick Start

### Prerequisites
- .NET 10 SDK
- Node.js 18+
- PostgreSQL (running locally)

### Backend

```bash
cd backend

# Create and migrate the database (PostgreSQL must be running)
# Connection string in appsettings.Development.json uses postgres/postgres
dotnet ef database update --project TaskManagement.Infrastructure --startup-project TaskManagement.Api

# Run the API (starts on http://localhost:5062)
dotnet run --project TaskManagement.Api
```

### Frontend

```bash
cd frontend
npm install
npm run dev
# Opens on http://localhost:5173, proxies API calls to :5062
```

### Run Tests

```bash
# Backend integration tests (23 tests)
dotnet test backend/TaskManagement.Tests

# Frontend build check
cd frontend && npm run build
```

## Architecture

### Backend Structure

```
backend/
  TaskManagement.Api/            # Controllers, middleware, Program.cs
  TaskManagement.Application/    # Services, DTOs, interfaces, errors
  TaskManagement.Domain/         # Entities (TaskItem, StatusChange, User)
  TaskManagement.Infrastructure/ # DbContext, EF configurations, strategies
  TaskManagement.Tests/          # xUnit integration tests
```
### Task Types

| Type | Statuses | Required Data |
|------|----------|--------------|
| **Procurement** | 1: Created, 2: Supplier offers received, 3: Purchase completed | Status 2: 2 price quotes. Status 3: Receipt |
| **Development** | 1: Created, 2: Spec completed, 3: Dev completed, 4: Distribution completed | Status 2: Spec text. Status 3: Branch name. Status 4: Version number |

### API Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/users` | List all users |
| GET | `/api/users/{id}/tasks` | Get tasks assigned to a user |
| GET | `/api/task-types` | Get all task type configurations |
| POST | `/api/tasks` | Create a new task |
| PUT | `/api/tasks/{id}/status` | Change task status (forward/backward) |
| PUT | `/api/tasks/{id}/close` | Close a task |

### Database

- PostgreSQL with EF Core migrations
- 3 tables: `users`, `tasks`, `status_changes`
- 4 seeded users: Alice Johnson, Bob Smith, Charlie Brown, Diana Prince


## Extensibility: How to Add a New Task Type

**Step 1**: Create a new strategy class in `TaskManagement.Infrastructure/Strategies/`:

```csharp
public class SupportTaskStrategy : ITaskTypeStrategy
{
    public string TaskType => "Support";
    public int MaxStatus => 3;
    // Define statuses, required fields, and validation...
}
```

**Step 2**: Register it in `Program.cs`:

```csharp
builder.Services.AddScoped<ITaskTypeStrategy, SupportTaskStrategy>();
```

**Step 3**: Done. No other code changes needed.

- The database schema doesn't change (jsonb handles any custom fields)
- The `TaskService` workflow engine works with any strategy
- The frontend automatically picks up the new type from `GET /api/task-types`
- The `ChangeStatusDialog` dynamically renders the required fields


