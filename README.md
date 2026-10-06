# Galent Local 1040

A local-first application for preparing and reviewing 2025 Form 1040 submissions. The repository contains an ASP.NET Core Web API and a React/Vite frontend. Identity, JWT roles, SQLite persistence, submission validation, and local document storage are implemented in the current codebase.

> **Important status:** The API includes PDF generation code configured to use `Api/assets/f1040.pdf`. Confirm the official 2025 template is present and verify all mapped fields and generated output before relying on it. Tax calculations and generated returns require review against official IRS instructions. This software is not tax advice.

## Architecture and design

- [ARCHITECTURE.md](ARCHITECTURE.md) — components, request/data flows, roles, API routes, schema, storage, security, operations, testing, and implementation gaps.
- [DESIGN.md](DESIGN.md) — concise design decisions, PII handling, encryption-at-rest limitation, validation, assumptions, and AI usage.

## Repository layout

- `Api/` — ASP.NET Core Web API, Identity/JWT, EF Core/SQLite, calculation, validation, PDF, and local document storage.
- `Client/` — React + TypeScript frontend built with Vite.
- `tests/Api.Tests/` — API smoke/integration and unit tests (coverage is being expanded; not every method/branch is exhaustively tested).
- `ARCHITECTURE.md`, `DESIGN.md` — design and status documentation.

## Requirements

- .NET 10 SDK and ASP.NET Core 10 runtime.
- Node.js and npm.
- Windows PowerShell examples below; adapt paths for other local environments.

## Configure and run locally

Run the API and frontend in separate PowerShell terminals from the repository root. Use unique local credentials; do not commit secrets.

### 1. API

```powershell
$env:JWT_SECRET = "replace-with-a-random-secret-at-least-32-bytes-long"
$env:ADMIN_EMAIL = "admin@example.test"
$env:ADMIN_PASSWORD = "Admin-Test-123!"
$env:PREPARER_EMAIL = "preparer@example.test"
$env:PREPARER_PASSWORD = "Preparer-Test-123!"
$env:REVIEWER_EMAIL = "reviewer@example.test"
$env:REVIEWER_PASSWORD = "Reviewer-Test-123!"

dotnet restore .\Api\Api.csproj
dotnet run --project .\Api\Api.csproj --launch-profile Api
```

The API profile is documented to listen at `http://127.0.0.1:5000`; verify the active profile in `Api/Properties/launchSettings.json`. Startup applies EF Core migrations and initializes configured local accounts/roles. In Development, Swagger is available at the API's `/swagger` path.

Seed accounts are created when missing. Changing environment variables does not reset passwords for existing users. SQLite defaults to `Api/galent.db`; storage defaults to the configured local storage directory. Keep database and storage files out of source control.

### 2. Frontend

In another PowerShell terminal:

```powershell
cd .\Client
npm install
$env:VITE_API_PROXY_TARGET = "http://127.0.0.1:5000"
npm run dev
```

Open the Vite URL shown in the terminal (typically `http://localhost:5173`). Configure the API CORS origin to match the frontend origin if it differs from the default.

### 3. Build and test

```powershell
dotnet build .\Api\Api.csproj
dotnet test .\tests\Api.Tests\Api.Tests.csproj
```

Frontend production build:

```powershell
cd .\Client
npm run build
```

Tests must use synthetic data only. Test success does not certify IRS tax correctness or the PDF field map.

## Configuration and secrets

- `JWT_SECRET` is required and must be at least 32 bytes.
- Configure `ADMIN_EMAIL` and `ADMIN_PASSWORD` before first startup. Preparer and reviewer seed variables are used where enabled by the seeding implementation.
- Optional overrides include connection string, `Storage:Root`, `Pdf:TemplatePath`, frontend origin, and JWT issuer/audience/lifetime.
- Use environment variables or `dotnet user-secrets` for secrets. Do not put real credentials, tax records, PDFs, SQLite databases, or storage output in Git.

## PDF generation and local storage

The configured template path is `Api/assets/f1040.pdf`. Supply the official fillable 2025 IRS form locally; the application does not fetch it at runtime. Verify the exact AcroForm mapping and visually inspect generated output with synthetic data before use. Files are kept under the configured storage root, outside public static content, and retrieved through authorized API endpoints..

