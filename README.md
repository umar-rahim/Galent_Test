# Galent Local 1040

A local-first web application for preparing and reviewing 2025 Form 1040 submissions. The repository contains an ASP.NET Core API and a React/Vite frontend. Data is stored in SQLite and generated documents are stored on local disk.

> **Status:** Authentication, role-based access, submission editing, validation, local document storage, and PDF-generation code are present. The IRS PDF template must be available at `Api/assets/f1040.pdf` for PDF generation. Confirm the template exists and test the field mapping before relying on generated documents. This software is not tax advice and generated returns must be reviewed.

## Requirements

- .NET 10 SDK and ASP.NET Core 10 runtime
- Node.js and npm
- Windows PowerShell (commands below use PowerShell syntax)

## Repository layout

- `Api/` — ASP.NET Core Web API, Identity/JWT, EF Core/SQLite, validation, PDF, and local document storage
- `Client/` — React + TypeScript frontend built with Vite
- `tests/Api.Tests/` — API test project
- `ARCHITECTURE.md`, `DESIGN.md` — design notes

## Configure and run locally

Open two PowerShell terminals from `C:\Galent_Project` (or your repository checkout path). These are local development credentials only; use unique secrets and passwords outside local testing.

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

The API profile listens at `http://127.0.0.1:5000`. Startup applies EF Core migrations and seeds the configured Admin, Preparer, and Reviewer accounts. In Development, Swagger is at `http://127.0.0.1:5000/swagger`.

Seed account variables are required every time a new database is seeded. Existing accounts are not reset when the seed passwords change. SQLite defaults to `Api/galent.db`; document files default under the configured local storage root (`Api/storage`). Keep these local data files out of source control.

### 2. Frontend

In another PowerShell terminal:

```powershell
cd .\Client
npm install
$env:VITE_API_PROXY_TARGET = "http://127.0.0.1:5000"
npm run dev
```

Open the Vite URL shown in the terminal (normally `http://localhost:5173`). Use one of the local accounts configured above. The API's default CORS origin is `http://localhost:5173`.

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

## PDF generation

The API expects the official fillable 2025 Form 1040 template at `Api/assets/f1040.pdf` (configured in `Api/appsettings.json`). The PDF code maps form data into PDF fields, validates before generation, and stores output locally. Verify the template is present and exercise PDF generation/download with synthetic test data before treating the output as verified. Do not commit taxpayer information or generated returns.

## Configuration and security notes

- `JWT_SECRET` must be at least 32 bytes.
- Set `ADMIN_EMAIL`/`ADMIN_PASSWORD`, `PREPARER_EMAIL`/`PREPARER_PASSWORD`, and `REVIEWER_EMAIL`/`REVIEWER_PASSWORD` before first startup.
- Do not commit secrets, database files, local storage, build output, or real taxpayer data.
- Use only synthetic data while developing and testing.

## Push this project to GitHub

From the repository root in PowerShell, inspect the current repository and working tree first:

```powershell
git status
git remote -v
```

If this local project is not already a Git repository, initialize it. If it has no `origin` remote, add the requested repository. Then stage, commit, and push the current branch:

```powershell
git init
git branch -M main
git remote add origin https://github.com/umar-rahim/Galent_Test.git
git add .
git status
git commit -m "Add local Form 1040 preparation application"
git push -u origin main
```

If `git remote -v` already shows `origin`, do **not** add it again. If the repository already has commits or a branch, review `git status` and `git branch --show-current`; commit and push that branch rather than reinitializing. GitHub may prompt you to authenticate. Never put a GitHub password or token in the remote URL or commit it to the repository.
