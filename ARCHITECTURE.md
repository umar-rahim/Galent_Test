# Galent AI — Architecture (Phase 1)

## Overview
This document describes the planned architecture for the local-only Galent 1040 (2025) application. The app provides authenticated, role-based web access for filling IRS Form 1040 (2025), validating it, generating a filled PDF from the official provided form, and storing generated PDFs on the local file system.

Tech stack (planned)
- Backend: ASP.NET Core Web API (net8.0), EF Core (SQLite), Identity + JWT
- Frontend: React + TypeScript (Vite)
- PDF: PdfSharpCore (preferred) or iText7 (AGPL — choose explicitly)
- Storage: Local file system via IDocumentStore abstraction
- Hosting: Docker Compose (local)

## Components
- Api (ASP.NET Core Web API)
  - Authentication & Authorization (Identity + JWT)
  - Submission API (create/save/draft/submit)
  - Validation engine (server authoritative)
  - PDF generation service (IForm1040PdfService)
  - Document storage (IDocumentStore -> LocalFileDocumentStore)
  - Admin endpoints (user management, role assignment)
- Client (React)
  - Login, dashboard, 1040 form UI, My Submissions, Reviewer views, Admin user management
- assets/
  - Official f1040.pdf (must be placed here: ./assets/f1040.pdf)

## Repository status (inspection results)
- `Api/` and `Client/` scaffolds exist in the workspace. `Api/Api.csproj` targets .NET 8; the frontend uses React, TypeScript, and Vite.
- The configured solution path `C:\Galent_Project\Galent_Project.slnx` was not found during inspection. The only solution file discovered was `11.0/PlatformIntegration/Passkeys/Passkeys.sln`, unrelated to this app.
- No official IRS Form 1040 PDF was found anywhere in the workspace. PHASE 2 cannot inspect or map PDF fields until the official 2025 PDF is supplied. The planned location is `assets/f1040.pdf`; do not fetch it at runtime.
- No Dockerfile or docker-compose.yml were found.
- Current `Api/appsettings.json` and `Api/Program.cs` contain development credentials and a fallback JWT key. These are existing scaffold values, not an acceptable final configuration; PHASE 3 must remove them and require configured secrets.
- Git status/commit could not be verified because `git` is not available in this environment.

## Database model (first-cut)
- AspNetUsers / AspNetRoles (Identity)
- Form1040Submission
  - Id (GUID)
  - UserId (FK to AspNetUsers)
  - Status (Draft | Submitted | Validated)
  - CreatedAt, UpdatedAt, SubmittedAt
  - Data (normalized fields mapped to domain model; stored as JSON in a column or normalized columns depending on design)
  - CalculatedValues (server-calculated totals stored for audit)
  - LatestDocumentId (FK to generated document metadata)
- Document
  - Id (GUID)
  - SubmissionId
  - Path (relative to storage root)
  - CreatedAt
  - Hash/Checksum (optional)
- ValidationFinding
  - Id
  - SubmissionId
  - Code
  - Severity (Error | Warning)
  - Field (domain path)
  - Message

Note: This is a proposed model, not the existing database schema. SSNs must be masked in API responses; encryption-at-rest must either be implemented or explicitly documented as a limitation.

## API surface (first-cut)
- POST /api/auth/login -> { token }
- POST /api/auth/register -> create user (Admin only for role assignment)
- GET /api/submissions -> list submissions (Preparer: own; Reviewer/Admin: scoped)
- POST /api/submissions -> create draft
- GET /api/submissions/{id} -> get submission (authorization enforced)
- PUT /api/submissions/{id} -> update (save draft or submit)
- POST /api/submissions/{id}/validate -> server-side validation; returns findings
- POST /api/submissions/{id}/generate-pdf -> validate + generate PDF; stores via IDocumentStore
- GET /api/submissions/{id}/documents -> list documents for submission
- GET /api/submissions/{id}/documents/{documentId} -> authorized download
- GET /api/admin/users -> (Admin) list users
- POST /api/admin/users -> (Admin) create user
- PUT /api/admin/users/{id}/roles -> (Admin) change roles

Use appropriate HTTP status codes and never return raw SSNs in GET/list responses.

## Authentication & Authorization
- ASP.NET Core Identity for user management and password hashing.
- JWT access tokens for bearer authentication. JWT secret must be provided via environment variable or user-secrets (JWT_SECRET).
- Role-based authorization using [Authorize(Roles = "Admin")] and policies for endpoints.
- Token expiry: tokens short-lived (e.g., 8 hours) with refresh handled by re-login (no refresh tokens in initial MVP).

## PDF mapping strategy
- PHASE 2 will enumerate AcroForm fields from the bundled PDF.
- Implement IForm1040PdfService and Form1040PdfMapper.
- Maintain a single mapping table (C# map/dictionary) that maps domain fields -> PDF field names and transformations (money formatting, checkboxes, radio groups).
- The server recalculates computed fields and uses server-calculated values when populating the PDF.

## Storage strategy
- IDocumentStore interface with LocalFileDocumentStore implementation.
- Storage path template: {storageRoot}/{environment}/{userId}/{submissionId}/{timestamp}.pdf
- storageRoot must be configurable via appsettings/environment variable.
- Documents are not served from a public static folder; downloads go through authorized API endpoints.

## Security & PII
- Do not log SSNs or other PII.
- Mask SSNs in API responses (e.g., ***-**-1234) and UI.
- Keep secrets out of Git; use environment variables or dotnet user-secrets for JWT secret and admin credentials.
- Validate paths to prevent traversal when reading documents.
- Document encryption-at-rest limitations in DESIGN.md if not implementing file or DB encryption.

## Testing plan (high level)
- Unit tests: calculation engine, validators, SSN/format validators, PDF mapping transforms.
- Integration tests: Auth flows, authorization, submission lifecycle, document download authorization.

## Next steps (PHASE 2+)
1. Add official f1040.pdf at ./assets/f1040.pdf and perform AcroForm field discovery.
