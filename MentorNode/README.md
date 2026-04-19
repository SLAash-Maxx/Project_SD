# BlindMatch PAS — PUSL2020 Coursework

> **Blind-Match Project Approval System** — ASP.NET Core 8 MVC  
> NSBM Green University | In Partnership with Plymouth University  
> Module: PUSL2020 Software Development Tools and Practices

---

## Quick Start (5 minutes)

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

### Run the application

```bash
# 1. Restore packages
dotnet restore BlindMatchPAS.sln

# 2. Apply EF Core migrations and create the SQLite database
cd BlindMatchPAS
dotnet ef database update

# 3. Run
dotnet run

# App starts at https://localhost:5001 / http://localhost:5000
```

### Run the tests

```bash
cd BlindMatchPAS.Tests
dotnet test --verbosity normal
```

---

## Default Accounts (auto-seeded on first run)

| Role          | Email                           | Password      |
|---------------|---------------------------------|---------------|
| System Admin  | admin@blindmatch.ac.lk          | Admin@1234!   |
| Module Leader | moduleleader@blindmatch.ac.lk   | Leader@1234!  |

Use the **Admin** or **Module Leader** dashboards to create Supervisor accounts.  
Students can self-register via the public `/Account/Register` page.

---

## Architecture Overview

```
BlindMatchPAS/
├── Controllers/
│   ├── AccountController.cs       # Login, Register, Logout
│   ├── StudentController.cs       # Submit, Edit, Withdraw, Details
│   ├── SupervisorController.cs    # Blind dashboard, Interest, Confirm match
│   ├── ModuleLeaderController.cs  # Oversight, User mgmt, Areas
│   ├── HomeController.cs          # Landing page + role-based redirect
│   └── AdminController.cs         # System-wide user/infrastructure admin
│
├── Models/
│   ├── Domain.cs                  # ApplicationUser, Project, ResearchArea, …
│   └── ViewModels.cs              # All typed view models
│
├── Services/
│   └── MatchingService.cs         # IMatchingService + implementation
│                                  #   — blind project list
│                                  #   — express interest (state: → UnderReview)
│                                  #   — confirm match  (state: → Matched + reveal)
│                                  #   — withdraw        (state: → Withdrawn)
│                                  #   — reassign (Module Leader intervention)
│
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext + seed data
│   └── DbSeeder.cs                # Roles + default admin accounts
│
├── Migrations/                    # EF Core migration history
├── Views/                         # Razor views per controller
└── wwwroot/css/site.css

BlindMatchPAS.Tests/
├── Unit/MatchingServiceTests.cs        # 18 unit tests — matching logic
├── Integration/DatabaseIntegrationTests.cs  # 8 integration tests — EF Core
└── Functional/ControllerTests.cs       # 8 functional tests — Moq
```

### Blind-Match State Machine

```
[Student submits]
       │
       ▼
   PENDING  ◄──── Student can Edit or Withdraw here
       │
       │  Supervisor expresses interest
       ▼
 UNDER REVIEW
       │
       │  Supervisor confirms match
       ▼
   MATCHED  ── IdentityRevealed = true  ── both parties see each other
```

Key rule enforced in `MatchingService.ConfirmMatchAsync`:
- `IdentityRevealed` is set to `true` **only** at this point — never earlier.
- The supervisor cannot confirm without first expressing interest.
- Once matched, the project is immutable (no withdraw, no second confirm).

---

## Role-Based Access Control (RBAC)

All controllers are decorated with `[Authorize(Roles = "...")]`.

| Controller       | Allowed Role(s)          |
|------------------|--------------------------|
| StudentController | Student                 |
| SupervisorController | Supervisor           |
| ModuleLeaderController | ModuleLeader       |
| AdminController  | SystemAdmin              |
| AccountController | Anonymous (Login/Register) |

Configured in `Program.cs` via ASP.NET Core Identity. Cookie redirects:
- Unauthenticated → `/Account/Login`
- Wrong role → `/Account/AccessDenied`

---

## Database (EF Core + SQLite)

- **Provider:** SQLite (file `blindmatch.db`) — swap to SQL Server by changing the connection string in `appsettings.json`
- **Migrations:** Located in `Migrations/` — run `dotnet ef database update` to apply
- **Key relationships:**
  - `Project → Student` (Restrict delete)
  - `Project → Supervisor` (SetNull — project survives supervisor removal)
  - `SupervisorInterest` has a unique composite index `(SupervisorId, ProjectId)`

To add a new migration after schema changes:
```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

---

## Dependency Injection

All services registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IMatchingService, MatchingService>();
```

`MatchingService` depends only on `ApplicationDbContext`, injected via constructor.  
Controllers depend on `IMatchingService` (not the concrete class) — enabling full Moq mocking in tests.

---

## Testing Strategy

### Unit Tests (`MatchingServiceTests`) — 18 tests
Tests the `MatchingService` business logic in isolation using the **EF Core InMemory provider**.
Covers all state transitions: pending → under review → matched, plus edge cases (already matched, wrong student, non-existent project).

### Integration Tests (`DatabaseIntegrationTests`) — 8 tests
Tests EF Core persistence: CRUD operations, relational queries, full lifecycle end-to-end.

### Functional Tests (`ControllerTests`) — 8 tests
Tests controller action behaviour using **Moq** to mock `IMatchingService` and `UserManager`.
Verifies redirect targets, TempData messages, and that service methods are called with correct arguments.

### Run all tests with coverage (requires `coverlet`):
```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Git Workflow (Professional Commit Strategy)

The recommended branching strategy for the group:

```
main
 └── develop
      ├── feature/student-submission
      ├── feature/blind-supervisor-dashboard
      ├── feature/identity-reveal-logic
      ├── feature/module-leader-oversight
      ├── feature/admin-rbac
      └── feature/testing-suite
```

Each feature branch is merged to `develop` via Pull Request with at least one reviewer.  
Merge to `main` only for release-ready versions.

Example commit message format:
```
feat(matching): implement identity reveal on supervisor confirm

- ConfirmMatchAsync sets IdentityRevealed=true only after interest validated
- Adds MatchedAt timestamp on confirmation
- Unit test: ConfirmMatch_SetsMatchedStatusAndRevealsIdentity
```

---

## Clean Code Principles Applied

- **Separation of concerns:** matching logic lives entirely in `IMatchingService`, never in controllers
- **Dependency Injection:** all dependencies injected via constructor, testable via interfaces
- **Data Annotations:** validation on all model properties (`[Required]`, `[StringLength]`, `[EmailAddress]`)
- **Anti-forgery tokens:** on every POST action (`[ValidateAntiForgeryToken]`)
- **No hardcoded strings:** connection strings in `appsettings.json`, roles as constants
- **Nullable reference types:** enabled project-wide (`<Nullable>enable</Nullable>`)

---

## Switching to SQL Server

1. Change the connection string in `appsettings.json`:
   ```json
   "DefaultConnection": "Server=.;Database=BlindMatchPAS;Trusted_Connection=True;"
   ```
2. Replace the SQLite package in `.csproj`:
   ```xml
   <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
   ```
3. Update `Program.cs`:
   ```csharp
   options.UseSqlServer(...)
   ```
4. Re-run migrations:
   ```bash
   dotnet ef database update
   ```
