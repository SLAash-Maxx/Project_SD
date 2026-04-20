# BlindMatch PAS

**Project Approval System — PUSL2020 Coursework 2026/2027**  
National School of Business Management | In Partnership with Plymouth University  
Module Leaders: Ms. Pavithra Subhashini | Mr. Anton Jayakody

---

## What This System Does

BlindMatch PAS is a secure, web-based Project Approval System that matches student
research proposals with academic supervisors using **Blind Matching** — supervisors
review proposals without seeing the student's identity. Only after a supervisor
confirms their selection does the system reveal both parties' identities to each other.

---

## Technology Stack

| Layer        | Technology                                |
|--------------|-------------------------------------------|
| Framework    | ASP.NET Core 8 MVC                        |
| Language     | C# 12                                     |
| Database     | SQLite via Entity Framework Core 8        |
| Auth / RBAC  | ASP.NET Core Identity                     |
| UI           | Razor Views + Bootstrap 5.3               |
| Testing      | xUnit + Moq + FluentAssertions            |

---

## Prerequisites

Before running this project, install the following:

1. **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)**
   — verify with: `dotnet --version` (should show `8.0.x`)

2. **[Git](https://git-scm.com/download/win)**
   — verify with: `git --version`

3. **EF Core CLI Tools** — install once, globally:
   ```powershell
   dotnet tool install --global dotnet-ef
   ```
   Verify with: `dotnet ef --version`

---

## Project Structure

```
PUSL2020 Final Upload/
├── BlindMatchPAS.sln                    ← Solution file (open this in VS)
│
├── BlindMatchPAS/                       ← Main web application
│   ├── Controllers/
│   │   ├── AccountController.cs         ← Login, Register, Logout
│   │   ├── StudentController.cs         ← Submit, Edit, Withdraw, Track
│   │   ├── SupervisorController.cs      ← Blind dashboard, Interest, Confirm match
│   │   ├── ModuleLeaderController.cs    ← Oversight, User mgmt, Research areas
│   │   └── HomeController.cs            ← Landing page + System Admin
│   ├── Models/
│   │   ├── Domain.cs                    ← All database entity classes
│   │   └── ViewModels.cs               ← Typed view models per role
│   ├── Services/
│   │   └── MatchingService.cs           ← Blind-match business logic
│   ├── Data/
│   │   ├── ApplicationDbContext.cs      ← EF Core DbContext
│   │   └── DbSeeder.cs                  ← Seeds roles + default accounts
│   ├── Migrations/                      ← EF Core migration history
│   ├── Views/                           ← Razor (.cshtml) UI files
│   ├── wwwroot/css/site.css             ← Stylesheet
│   ├── appsettings.json                 ← Connection string + config
│   └── Program.cs                       ← DI setup + middleware pipeline
│
└── BlindMatchPAS.Tests/                 ← Test project
    ├── Unit/MatchingServiceTests.cs      ← 18 unit tests (business logic)
    ├── Integration/DatabaseIntegrationTests.cs  ← 7 integration tests (EF Core)
    ├── Functional/ControllerTests.cs    ← 8 functional tests (Moq)
    └── GlobalUsings.cs
```

---

## Running the Project

### Step 1 — Restore NuGet packages

Open PowerShell, navigate to the project root, and run:

```powershell
cd "C:\path\to\PUSL2020 Final Upload"
dotnet restore BlindMatchPAS.sln
```

You should see: `Restore succeeded`.

---

### Step 2 — Set up the database

Navigate into the main project folder and apply migrations:

```powershell
cd BlindMatchPAS
dotnet ef database update
```

This creates `blindmatch.db` (SQLite database file) and runs the
`20260413194636_InitialCreate` migration, which builds all tables.

You should see: `Done.` at the end.

> **If you see "no such table" errors when running:** Delete `blindmatch.db`
> and run `dotnet ef database update` again to recreate it cleanly.

---

### Step 3 — Run the application

```powershell
dotnet run
```

Or, for **hot reload** (auto-refreshes when you edit files — recommended during development):

```powershell
dotnet watch run
```

The terminal will show the URL the app is running on:

```
Now listening on: http://localhost:5000
```

Open that URL in your browser.

---

### Step 4 — Log in

You can see the demo credentials in log in page

| Role          | Email                             | Password      |
|---------------|-----------------------------------|---------------|
| System Admin  | `admin@blindmatch.ac.lk`          | `Admin@1234!` |
| Module Leader | `moduleleader@blindmatch.ac.lk`   | `Leader@1234!`|

> Students can self-register at `/Account/Register`.  
> Supervisor accounts must be created by the Module Leader or Admin.

---

## Running the Tests

### Run all 34 tests

Navigate to the solution root (not the BlindMatchPAS subfolder):

```powershell
cd "C:\path\to\PUSL2020 Final Upload"
dotnet test
```

Expected output:
```
Passed!  - Failed: 0, Passed: 34, Skipped: 0, Total: 34
```

---

### Run with detailed output

```powershell
dotnet test --verbosity normal
```

This shows each test name and whether it passed or failed.

---

### Run a specific test file only

```powershell
# Unit tests only (MatchingService logic)
dotnet test --filter "FullyQualifiedName~Unit"

# Integration tests only (EF Core database)
dotnet test --filter "FullyQualifiedName~Integration"

# Functional tests only (Controller + Moq)
dotnet test --filter "FullyQualifiedName~Functional"
```

---

### Run with code coverage

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

A coverage report XML file is saved in `BlindMatchPAS.Tests/TestResults/`.
To view it as HTML, install the report generator:

```powershell
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"BlindMatchPAS.Tests/TestResults/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

Then open `coverage-report/index.html` in your browser.

---

### Test summary

| File                            | Type        | What it tests                        | Count |
|---------------------------------|-------------|--------------------------------------|-------|
| `Unit/MatchingServiceTests.cs`  | Unit        | Blind-match business logic           | 18    |
| `Integration/DatabaseIntegrationTests.cs` | Integration | EF Core persistence + lifecycle | 7  |
| `Functional/ControllerTests.cs` | Functional  | Controller actions via Moq           | 8     |
| **Total**                       |             |                                      | **34**|

---

## User Roles and Access

| Role          | What they can do                                               | Access URL              |
|---------------|----------------------------------------------------------------|-------------------------|
| Student       | Submit, edit, withdraw proposals; track status; see reveal     | `/Student/Dashboard`    |
| Supervisor    | Set expertise areas; blind-review proposals; confirm matches   | `/Supervisor/Dashboard` |
| Module Leader | View all allocations; manage areas; create users; reassign     | `/ModuleLeader/Dashboard` |
| System Admin  | Full user management; infrastructure overview                  | `/Admin/Dashboard`      |

> Role-Based Access Control is enforced via `[Authorize(Roles = "...")]` on
> all controllers. Accessing the wrong dashboard redirects to `/Account/AccessDenied`.

---

## The Blind-Match Flow

```
Student submits proposal
        ↓
   Status: PENDING
   (Student identity hidden from all supervisors)
        ↓
Supervisor selects their research expertise areas
        ↓
Supervisor sees proposals — NO student names shown
        ↓
Supervisor clicks "Express Interest"
        ↓
   Status: UNDER REVIEW
        ↓
Supervisor clicks "Confirm Match & Reveal"
        ↓
   Status: MATCHED
   IdentityRevealed = true
   ← Both parties can now see each other's name and email →
```

The identity reveal is controlled by a single boolean field —
`Project.IdentityRevealed` — set to `true` only inside `ConfirmMatchAsync()`
in `Services/MatchingService.cs`, and only after the supervisor's prior
interest is validated.

---

## Database

The project uses **SQLite** for portability. The database file `blindmatch.db`
is created automatically in the `BlindMatchPAS/` folder when you run
`dotnet ef database update`.

### Switching to SQL Server (production)

1. Change `appsettings.json`:
   ```json
   "DefaultConnection": "Server=.;Database=BlindMatchPAS;Trusted_Connection=True;"
   ```

2. Replace the SQLite package in `BlindMatchPAS.csproj`:
   ```xml
   <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
   ```

3. Update `Program.cs` — change `UseSqlite(...)` to `UseSqlServer(...)`.

4. Re-run migrations:
   ```powershell
   dotnet ef database update
   ```

---

## Adding a New EF Core Migration

After changing any model class in `Models/Domain.cs`:

```powershell
cd BlindMatchPAS
dotnet ef migrations add <DescriptiveName>
dotnet ef database update
```

Example:
```powershell
dotnet ef migrations add AddIsUrgentToProjects
dotnet ef database update
```

Always commit migration files to Git:
```powershell
git add Migrations/
git commit -m "feat(migrations): add IsUrgent field to Projects table"
```

---

## Git Workflow (Team)

This project uses a feature-branch strategy:

```
main
 └── develop
      ├── feature/setup-infrastructure      ← Member 1
      ├── feature/authentication            ← Member 2
      ├── feature/student-portal            ← Member 3
      ├── feature/blind-matching            ← Member 4
      ├── feature/admin-moduleleader        ← Member 5
      └── feature/testing-suite             ← Member 6
```

Each member works on their branch and creates a Pull Request to merge into `main`.

**Commit message format used:**
```
type(scope): short description

Examples:
feat(service): implement confirm match with identity reveal
feat(views): add supervisor blind review dashboard
test(unit): add 18 unit tests for MatchingService
fix(auth): correct role redirect after login
```

---

## Common Issues and Fixes

**"no such table: AspNetRoles" on startup**
```powershell
del blindmatch.db            # delete old database
dotnet ef database update    # recreate all tables
dotnet run
```

**"dotnet" is not recognised**
> .NET SDK is not installed. Download from https://dotnet.microsoft.com/download/dotnet/8.0
> After installing, close and reopen PowerShell.

**"dotnet ef" is not recognised**
```powershell
dotnet tool install --global dotnet-ef
```

**Build failed — MigrateAsync not found**
> Add `using Microsoft.EntityFrameworkCore;` at the top of `Data/DbSeeder.cs`.

**Port already in use**
```powershell
dotnet run --urls "http://localhost:5001"
```

---

## Academic Information

- **Module:** PUSL2020 — Software Development Tools and Practices
- **Assessment:** C1 — Group Coursework (50% of module marks)
- **Semester:** Semester 2, 2026/2027
- **Submission:** Via Plymouth Digital Learning Environment (DLE)
- **Deliverable 1:** Source code + Git history (this repository)
- **Deliverable 2:** Technical report (PDF, 2,000 words)
