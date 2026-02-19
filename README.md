# Extensible Task-Management Platform

A full-stack task management system built with ASP.NET Core and React that cleanly separates general workflow rules from task-type-specific rules using the Strategy Pattern.

https://refactoring.guru/design-patterns/strategy
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
Used clean architecture - as explained [here](https://youtu.be/yF9SwL0p0Y0?si=luSwumnYRpOG9lth)
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


## Project Dependencies

```
                    +---------------------+
                    |  TaskManagement.Api  |
                    |  Controllers         |
                    |  Middleware           |
                    |  Program.cs (DI)     |
                    +---------------------+
                       /              \
                      /                \
                     v                  v
  +-------------------------+    +------------------------------+
  | TaskManagement.         |    | TaskManagement.              |
  | Application             |    | Infrastructure               |
  |                         |    |                              |
  | Services (TaskService,  |    | AppDbContext : IAppDbContext  |
  |   UserService)          |    | EF Configurations            |
  | Interfaces (ITaskService|    | Strategy implementations     |
  |   IAppDbContext,        |    |   (ProcurementTaskStrategy,  |
  |   ITaskTypeStrategy)    |    |    DevelopmentTaskStrategy)  |
  | DTOs, Errors            |    | Migrations                   |
  +-------------------------+    +------------------------------+
              |                        /          |
              |                       /           |
              v                      v            |
        +------------------------+                |
        | TaskManagement.Domain  |  <-------------+
        |                        |
        | Entities:              |
        |   TaskItem             |
        |   User                 |
        |   StatusChange         |
        | (zero dependencies)    |
        +------------------------+
```

Reference direction:

```
Api             --> Application, Infrastructure
Infrastructure  --> Application, Domain
Application     --> Domain
Domain          --> (nothing)
```

Arrows always point inward. Inner layers never reference outer layers.

### Layers

| Layer | NuGet Packages | What lives here | When does it change |
|-------|---------------|-----------------|---------------------|
| Domain | None | Entities | When business rules change |
| Application | EF Core (abstractions only) | Services, interfaces, DTOs | When workflows or API contracts change |
| Infrastructure | Npgsql | DbContext, EF configs, strategies | When we swap tech (DB, providers) |
| Api | Swashbuckle, EF InMemory | Controllers, middleware, DI wiring | When HTTP/presentation concerns change |

### Bridging Application and Infrastructure

Services need database access, but `AppDbContext` sits in Infrastructure. Application can't reference Infrastructure (that would be an outward dependency). So we use an interface:

```
Application defines:     IAppDbContext (Tasks, Users, StatusChanges, SaveChangesAsync)
Infrastructure provides: AppDbContext : DbContext, IAppDbContext
Program.cs wires them:   AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>())
```

Services inject `IAppDbContext`. They never see PostgreSQL, Npgsql, or the concrete `AppDbContext`.

---

## Adding a New Task Type (HR Example)

The whole point of the strategy pattern here: adding a task type means adding files, not touching existing ones.

### The HR task

3 statuses:
- 1: Created (no fields)
- 2: Interview scheduled (needs `candidateName`, `interviewDate`)
- 3: Decision made (needs `decision`)

### What to do

**1. Create `Infrastructure/Strategies/HrTaskStrategy.cs`:**

```csharp
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Strategies;

public class HrTaskStrategy : TaskStrategyBase
{
    public override string TaskType => "HR";
    public override int MaxStatus => 3;

    public override IReadOnlyList<StatusDefinition> GetStatusDefinitions() => new List<StatusDefinition>
    {
        new() { Status = 1, Label = "Created" },
        new() { Status = 2, Label = "Interview scheduled" },
        new() { Status = 3, Label = "Decision made" }
    };

    public override IReadOnlyList<FieldDefinition> GetRequiredFields(int targetStatus) => targetStatus switch
    {
        2 => new List<FieldDefinition>
        {
            new() { FieldName = "candidateName", Label = "Candidate Name", FieldType = "string", Required = true },
            new() { FieldName = "interviewDate", Label = "Interview Date", FieldType = "string", Required = true }
        },
        3 => new List<FieldDefinition>
        {
            new() { FieldName = "decision", Label = "Decision", FieldType = "string", Required = true }
        },
        _ => new List<FieldDefinition>()
    };

    public override List<string> ValidateStatusData(int targetStatus, Dictionary<string, object> customData)
    {
        var errors = new List<string>();

        switch (targetStatus)
        {
            case 2:
                ValidateRequiredString(customData, "candidateName", "Candidate Name", errors);
                ValidateRequiredString(customData, "interviewDate", "Interview Date", errors);
                break;
            case 3:
                ValidateRequiredString(customData, "decision", "Decision", errors);
                break;
        }

        return errors;
    }
}
```

**2. One line in Program.cs:**

```csharp
builder.Services.AddScoped<ITaskTypeStrategy, HrTaskStrategy>();
```

Done. No migration, no schema change, no edits to TaskService, AppDbContext, entities, or other strategies.

Why it works: `TaskService` gets `IEnumerable<ITaskTypeStrategy>` from DI, builds a dictionary by `TaskType` at construction time, and looks up the right strategy when a request comes in. Custom data goes into the same `jsonb` column. The frontend discovers the new type via `GET /api/task-types`.

### Proof: existing files know nothing about HR

| File | Touched? |
|------|----------|
| TaskService.cs | No |
| UserService.cs | No |
| AppDbContext.cs | No |
| TaskItem.cs | No |
| ProcurementTaskStrategy.cs | No |
| DevelopmentTaskStrategy.cs | No |

---

## EF Core Configuration Files

### The problem

Two ways to configure entity-to-table mapping, both bad for clean architecture:

**Data Annotations** put database concerns on domain entities, violates the clean architecture dependencies:

```csharp
// Domain entity now knows about table names and PostgreSQL
[Table("tasks")]
public class TaskItem
{
    [Column("id")]
    public int Id { get; set; }

    [Column("custom_data", TypeName = "jsonb")]   // PostgreSQL-specific in Domain!
    public Dictionary<string, object> CustomData { get; set; }
}
```

Domain shouldn't know what database it's stored in.

**Dumping everything in OnModelCreating** creates one massive method:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // 60 lines for TaskItem...
    // 30 lines for StatusChange...
    // 15 lines for User...
    // seed data...
    // grows forever
}
```

### What we do instead
Each entity gets its own configuration class
Each configuration file tells EF Core how to map a C# class to a database table:


```
Infrastructure/Data/Configurations/
    TaskItemConfiguration.cs        -- table, columns, jsonb, relationships, indexes
    StatusChangeConfiguration.cs    -- table, columns, FK to Task and User
    UserConfiguration.cs            -- table, columns
```

Each implements `IEntityTypeConfiguration<T>` with a single `Configure` method.

### How EF Core loads them

1. App starts, DI registers `AppDbContext`
2. First time the context is used, EF Core calls `OnModelCreating`
3. Inside, one line does everything:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```

This scans the Infrastructure DLL with reflection, finds every class implementing `IEntityTypeConfiguration<T>`, instantiates each one, and calls `Configure`. Under the hood it's doing:

```csharp
new TaskItemConfiguration().Configure(modelBuilder.Entity<TaskItem>());
new StatusChangeConfiguration().Configure(modelBuilder.Entity<StatusChange>());
new UserConfiguration().Configure(modelBuilder.Entity<User>());
```

But we never write those lines. Drop a new config class in the folder and it gets picked up.

4. The model is built once and cached for the app lifetime. Not per-request -- just on first use.

### What TaskItemConfiguration actually configures

```
ToTable("tasks")                              table name
Property(...).HasColumnName("...")            C# props -> snake_case columns
Property(CustomData).HasColumnType("jsonb")   PostgreSQL-specific storage
    .HasConversion(...)                       serialize/deserialize Dictionary<>
    .SetValueComparer(...)                    tell EF how to detect changes in the dict
HasOne/HasMany                                FK relationships
HasIndex(...)                                 DB indexes for query performance
```

All PostgreSQL-specific details stay here in Infrastructure. Domain entities are plain C# classes with no attributes, no annotations, no idea what database they live in.

