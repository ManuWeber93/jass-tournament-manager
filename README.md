# Jass Tournament Manager

Cross-platform application for managing Schieber Jass tournaments.

The system helps tournament organizers plan, run and evaluate Jass tournaments, including participant registration, pairing generation, score entry and leaderboards.

## Tech Stack

- Frontend: .NET MAUI
- Backend: ASP.NET Core Web API
- Database: PostgreSQL
- ORM/Migrations: Entity Framework Core
- API Documentation: Swagger / OpenAPI

## Main Features

- Tournament management
- Participant registration
- Tournament-code based joining
- Manual and automatic pairings
- Score entry with automatic 157-point calculation
- Optional match bonus
- Leaderboards and reports
- Excel import for historical tournament data

## User Roles

- SYSADMIN: Can access and manage all data for support purposes
- ORGANIZER: Can create and manage own tournaments
- PLAYER: Can join tournaments, enter scores and view allowed results

Users can see tournaments if they are either the organizer of the tournament or registered as a participant in that tournament. SYSADMIN users can see all tournaments.

## Project Structure

```text
JassTournamentManager
├─ src
│  ├─ JassTournamentManager.Api
│  ├─ JassTournamentManager.Application
│  ├─ JassTournamentManager.Domain
│  ├─ JassTournamentManager.Infrastructure
│  ├─ JassTournamentManager.Contracts
│  └─ JassTournamentManager.App
├─ tests
├─ docs
│  ├─ requirements.md
│  ├─ architecture.md
│  ├─ features
│  │  └─ authentication.md
│  ├─ testing
│  │  └─ authentication.md
│  └─ database-schema.md
├─ JassTournamentManager.Backend.slnx
└─ JassTournamentManager.slnx
```

## First-time setup

Follow these steps once for each new clone. Run all commands from the repository root unless stated otherwise. Commands below work in PowerShell and Bash, except where a specific shell is indicated.

### 1. Install prerequisites and clone

- Git and Bash (Git for Windows includes Git Bash).
- .NET 10 SDK, not just the runtime.
- Docker with Docker Compose, running Linux containers, for the local PostgreSQL database and database integration tests.
- Optional: an IDE with the MAUI workloads and platform SDKs required by your target device. Backend development and platform-neutral app tests do not require MAUI workloads.

Clone the repository using GitHub's clone command, then enter the `jass-tournament-manager` directory. Check the installed tools:

```bash
git --version
dotnet --list-sdks
docker compose version
docker info
```

Ensure a `10.0.x` SDK is listed and Docker is reachable. See `docs/architecture.md` and `docs/conventions.md` for project boundaries and coding/testing conventions.

### 2. Enable the pre-commit hook

```bash
git config --local core.hooksPath .githooks
git config --local --get core.hooksPath
```

The second command should print `.githooks`. Check for existing custom hooks before replacing their configuration. On Linux/macOS, run `chmod +x .githooks/pre-commit` if necessary. This configuration is local to the clone and is not transferred by Git.

The hook formats staged backend C# files. If it makes corrections, review and stage them before retrying the commit. Partially staged files are rejected before formatting. See **Pre-commit formatting** below for details.

### 3. Restore backend dependencies and repository tools

```bash
dotnet restore JassTournamentManager.Backend.slnx
dotnet tool restore
```

Use the backend solution rather than the full solution when you do not have MAUI workloads installed. The tool manifest currently restores ReportGenerator; it does not include the EF Core CLI.

### 4. Configure the local database and API secrets

Copy `.env.example` to `.env` without overwriting an existing local file, then replace `change-me` with your own local database password. Never commit `.env` or real secrets.

PowerShell:

```powershell
if (!(Test-Path .env)) { Copy-Item .env.example .env }
```

Bash:

```bash
if [ ! -e .env ]; then cp .env.example .env; fi
```

Set the API connection string using the same password. Replace both example secret values below before running the commands; use a randomly generated JWT secret of at least 32 ASCII characters, not the example text:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=jass_tournament_manager;Username=jass_app;Password=your_password" --project src/JassTournamentManager.Api
dotnet user-secrets set "Jwt:Secret" "replace-with-a-random-local-secret-of-at-least-32-characters" --project src/JassTournamentManager.Api
docker compose up postgres -d
```

User secrets are stored outside the repository for the API's existing `UserSecretsId`; they may already be configured on your machine and are not automatically supplied by a clone. The `.env` file configures Docker Compose, not an API started with `dotnet run`. The API requires `Jwt:Secret` at startup.

### 5. Apply database migrations and start the API

Install the EF Core CLI if it is not already available. Version `10.0.8` matches the API's current EF Core design package:

```bash
dotnet tool install --global dotnet-ef --version 10.0.8
dotnet ef --version
```

If it is already installed, check its version rather than installing it again. If needed, align it with `dotnet tool update --global dotnet-ef --version 10.0.8`.

Once PostgreSQL is ready, apply migrations explicitly; API startup does not apply them automatically:

```bash
dotnet ef database update --project src/JassTournamentManager.Infrastructure/JassTournamentManager.Infrastructure.csproj --startup-project src/JassTournamentManager.Api/JassTournamentManager.Api.csproj --context JtmDbContext
dotnet run --project src/JassTournamentManager.Api --launch-profile http
```

Open Swagger UI at `http://localhost:5272/swagger`. This setup runs only PostgreSQL in Docker and the API on the host; user secrets are not automatically passed into the API container. See **Docker** and **Database Migrations** below for further commands.

### 6. Verify the clone

In another terminal at the repository root:

```bash
dotnet format JassTournamentManager.Backend.slnx --verify-no-changes --no-restore
dotnet test JassTournamentManager.Backend.slnx
```

Keep Docker running for PostgreSQL integration tests and review skipped tests, not just the exit code: database tests may be skipped when no Docker endpoint is available. Tests use a separate PostgreSQL test container, not your local development database. The test command compiles the backend before running tests.

Optionally run the platform-neutral app tests using the command in **App unit tests** below. After pulling changes that update dependencies, run the restore commands again; apply new database migrations before using the updated API.

## Backend Commands

All `dotnet` commands target the backend solution file:

```bash
# Restore dependencies
dotnet restore JassTournamentManager.Backend.slnx

# Build
dotnet build JassTournamentManager.Backend.slnx --configuration Release

# Run all tests
dotnet test JassTournamentManager.Backend.slnx

# Run tests for a single project
dotnet test tests/JassTournamentManager.Domain.Tests

# Run a single test by name filter
dotnet test tests/JassTournamentManager.Domain.Tests --filter "FullyQualifiedName~Tournament.Constructor"

# Start the API locally (requires a running PostgreSQL on port 5433)
dotnet run --project src/JassTournamentManager.Api
```

Swagger UI (local): `http://localhost:5272/swagger`

### Backend linting

CI checks backend formatting and code style in a separate job using the repository's `.editorconfig`. Generated EF Core migrations are excluded from formatting.

Run these commands from the repository root:

```bash
dotnet restore JassTournamentManager.Backend.slnx
dotnet format JassTournamentManager.Backend.slnx --verify-no-changes --no-restore
```

To apply formatting fixes locally, omit `--verify-no-changes`. Review formatting changes separately from functional changes.

Backend test runs publish TRX results and raw coverage files as the `backend-test-results` artifact, including when tests fail. The generated coverage report remains a separate artifact on successful runs.

### Pre-commit formatting

Enable the versioned hook once per clone, from the repository root:

```bash
git config --local core.hooksPath .githooks
```

On Linux/macOS, also run `chmod +x .githooks/pre-commit` if the executable bit was not preserved. The hook requires Bash (provided by Git for Windows), the .NET SDK, and a previously restored backend solution. It does not restore dependencies or run a build.

The hook formats only staged C# files belonging to projects in the backend solution, excluding generated migrations and build output. App files are not included. If any selected file also has unstaged changes, it stops before formatting anything; stage or set aside those changes first.

When formatting changes files, the commit is stopped. Review the diff, stage the corrections, and commit again. The hook never stages files or stashes changes automatically. Formatter failures also stop the commit; review any partial formatting changes before retrying.

Existing custom hooks must be integrated manually before changing `core.hooksPath`. To disable this setup, run `git config --local --unset core.hooksPath`. Hooks can be bypassed with `git commit --no-verify`, so CI remains the authoritative formatting check.

Run the hook regression tests with `bash .githooks/tests/pre-commit-tests.sh` (or from Git Bash on Windows). These tests use an isolated temporary repository and a fake formatter; they do not change your index or working files.

## Docker

`docker-compose.yml` starts PostgreSQL (host port 5433) and the API (host port 8080). `POSTGRES_PASSWORD` must be provided via a `.env` file:

```bash
# One-time setup
echo "POSTGRES_PASSWORD=your_password" > .env

# Start all services
docker compose up -d

# Stop all services
docker compose down

# Stop and remove volumes (resets the database)
docker compose down -v
```

To run only the database in Docker and the API locally:

```bash
docker compose up postgres -d
dotnet run --project src/JassTournamentManager.Api
```

The connection string is managed via [dotnet user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) and is not committed to the repository. Set it once per developer:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5433;Database=jass_tournament_manager;Username=jass_app;Password=your_password" \
  --project src/JassTournamentManager.Api
```

## Database Migrations

Create a new EF Core migration from the repository root in PowerShell:

```powershell
dotnet ef migrations add NameOfMigration `
  --project src\JassTournamentManager.Infrastructure\JassTournamentManager.Infrastructure.csproj `
  --startup-project src\JassTournamentManager.Api\JassTournamentManager.Api.csproj `
  --context JtmDbContext `
  --output-dir Persistence/Migrations
```

Apply pending migrations to the local database:

```powershell
docker compose up postgres -d

dotnet ef database update `
  --project src\JassTournamentManager.Infrastructure\JassTournamentManager.Infrastructure.csproj `
  --startup-project src\JassTournamentManager.Api\JassTournamentManager.Api.csproj `
  --context JtmDbContext
```

Remove the latest migration if it has not been applied to the database yet:

```powershell
dotnet ef migrations remove `
  --project src\JassTournamentManager.Infrastructure\JassTournamentManager.Infrastructure.csproj `
  --startup-project src\JassTournamentManager.Api\JassTournamentManager.Api.csproj `
  --context JtmDbContext
```

Downgrade the local database to an earlier migration. `TargetMigrationName` is the migration that should remain applied after the downgrade:

```powershell
dotnet ef database update TargetMigrationName `
  --project src\JassTournamentManager.Infrastructure\JassTournamentManager.Infrastructure.csproj `
  --startup-project src\JassTournamentManager.Api\JassTournamentManager.Api.csproj `
  --context JtmDbContext
```

## Run Frontend

Open the solution in Visual Studio, set `JassTournamentManager.App` as startup project and run it with the desired target platform.

### Registration, login and logout

The app supports registration with automatic sign-in, login with an existing account, and logout. See the [authentication feature documentation](docs/features/authentication.md) for business rules and current limitations, the [architecture documentation](docs/architecture.md#app-authentication-and-session-handling) for technical details and local API configuration, and the [manual authentication tests](docs/testing/authentication.md) for smoke-test instructions.

### Running app unit tests

Run the platform-neutral app tests without installing MAUI workloads or starting an emulator:

```bash
dotnet test tests/JassTournamentManager.App.Tests/JassTournamentManager.App.Tests.csproj -p:AppTestsOnly=true
```

`AppTestsOnly` restricts the app project to its `net10.0` library target. The tests reference the app directly and exercise authentication view models, HTTP authentication, and token persistence with fake HTTP and secure-storage implementations. XAML views, platform entry points, and actual device storage are not covered by these unit tests.

The app test project is included in the full solution, not the backend solution. CI runs it in a separate Linux job and publishes test results and a coverage report independently of the backend tests.

## Documentation

Detailed documentation is maintained in:

- [Requirements](docs/requirements.md) — functional requirements and business rules
- [Authentication](docs/features/authentication.md) — business rules, user flows and implementation status
- [Architecture](docs/architecture.md) — technical architecture and deployment concepts
- [Manual authentication tests](docs/testing/authentication.md) — prerequisites and smoke-test scenarios
- [Database schema](docs/database-schema.md) — data model, entities and constraints

## Development Principles

- Layered architecture
- Domain-driven design concepts
- Clear separation of business logic and infrastructure
- REST-based communication
- Dependency Injection
- Testable domain and application logic

## License

MIT
