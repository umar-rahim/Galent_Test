# Galent 1040 (2025) — System Architecture

## 1. Purpose and scope

Galent is a local-first application for authorized preparers to create, save, validate, and submit 2025 Form 1040 drafts; reviewers and administrators can inspect permitted submissions; administrators manage team accounts and roles. Submitted returns can be rendered to PDF and stored on the local filesystem for authorized download.

This document describes the repository's current architecture and distinguishes implemented behavior from items requiring confirmation. It is not a tax-preparation specification or assurance that a return is IRS-compliant. The server is authoritative for calculation, validation, access control, and document generation.

## 2. Technology and deployment boundaries

- **API:** ASP.NET Core Web API, .NET 10, C#; built-in structured `ILogger` logging.
- **Persistence:** EF Core 10, SQLite; ASP.NET Core Identity stores users and roles in the same database.
- **Authentication:** Identity password verification and short-lived signed JWT bearer access tokens; token revocation records support logout.
- **Client:** React, TypeScript, and Vite.
- **PDF:** PDFsharp 6.2.4 with a configured local AcroForm template path (`Pdf:TemplatePath`, default `assets/f1040.pdf`). Field mapping code exists; it must be tested against the actual official 2025 PDF before use.
- **Documents:** `IDocumentStore` abstraction with `LocalFileDocumentStore`; files are not served as static/public content.
- **Hosting:** Local development is documented as separate API and Vite processes. Docker Compose is not currently present in the inspected repository.
- **External/cloud dependencies:** No application cloud service is intended. Packages are restored from configured NuGet/npm feeds during development/build; the running app uses local SQLite and local files.

## 3. Logical component architecture

```text
React/Vite client
    │ HTTP JSON / bearer JWT; PDF streamed from authorized endpoint
    ▼
ASP.NET Core middleware
    ├─ request metadata logging (method, path, status, duration; no body/headers)
    ├─ HTTPS redirect, CORS, JWT authentication, role authorization
    └─ exception handling (generic production error response)
         ▼
Controllers (HTTP adapters; claims and HTTP status/serialization)
         ▼
Application services (use cases, validation/calculation orchestration, access rules)
    ├─ AuthService / AdminUserService
    ├─ SubmissionService / DocumentService
    ├─ Form1040Calculator / Form1040Validator / Form1040PdfService
    └─ IDocumentStore → LocalFileDocumentStore
         ▼
Repositories (Identity/EF persistence boundaries)
    ├─ IUserRepository → ASP.NET Core Identity managers
    ├─ ISubmissionRepository → ApplicationDbContext
    ├─ IDocumentRepository → ApplicationDbContext
    └─ IRevokedTokenRepository → ApplicationDbContext
         ▼
SQLite database                 Local private document directory
```

### Responsibilities

- **Controllers** bind HTTP input, build actor identity from authenticated claims, delegate to services, and map service results to status codes and file responses. They do not contain database queries or tax calculations.
- **Services** implement use cases and domain orchestration. They enforce ownership/access rules in addition to endpoint role attributes; recompute derived values server-side; validate data; and return typed results/findings.
- **Repositories** isolate database and Identity persistence. Identity entities/managers should remain behind `IUserRepository`; EF queries and persistence operations remain behind repository interfaces.
- **Domain/PDF services** isolate calculations, validation, PDF mapping, and physical file I/O.
- **Client** improves usability and may calculate values for immediate display, but client calculations and validation are never trusted by the API.

## 4. Runtime and data flows

### Login and logout

1. Client posts credentials to `POST /api/auth/login`.
2. `AuthService` delegates credential verification to `IUserRepository`/Identity. On success, `ITokenService` issues a JWT with user identifier, name, role claims, issuer, audience, expiry, and `jti`.
3. The client stores and sends the bearer token for protected calls. Do not log or expose the token in URLs.
4. JWT middleware validates signature, issuer, audience, and lifetime. Its token-validated event rejects missing `jti` values and asks `IAuthService` whether the token is revoked.
5. `POST /api/auth/logout` records the current token id and expiry in the revoked-token table. Since JWT is otherwise stateless, revocation checks require a local database lookup.

Tokens are short-lived (configured under `Jwt:AccessTokenMinutes`, bounded by the token service); refresh tokens are not implemented. The user must log in again after expiry.

### Submission lifecycle

1. A Preparer creates a Draft; the API associates it with the authenticated user.
2. Draft reads and edits require ownership and Preparer role. The service recalculates computed lines before persistence; a draft save replaces dependent records and clears stale validation findings.
3. Validation recalculates and persists the latest findings, then returns structured findings and calculated lines. Reviewers/Admins may inspect submissions under the configured access rules; reviewers/admins may validate submitted submissions.
4. Submission is allowed to its owning Preparer only while Draft. Server validation runs before status becomes Submitted; errors produce `422` plus findings. Successful submission records status and timestamps.
5. PDF generation is permitted only for a Submitted, accessible submission. Server validation is rerun; errors prevent generation. The PDF service fills the local template, document bytes are stored by `IDocumentStore`, metadata/hash are persisted, and bytes are streamed in the response.
6. Document listing and downloads check submission access before returning metadata or opening a file. Files are never exposed via a public static directory.

### Authorization matrix (current intent)

| Operation | Preparer | Reviewer | Admin |
|---|---|---|---|
| List submissions | Own submissions | All submissions | All submissions |
| Create submission | Yes | No | Depends on Admin also having Preparer role (create action requires Preparer) |
| Read submission | Own | All | All |
| Edit/save draft | Own Draft | No | Only if also owner and Preparer |
| Validate | Accessible Draft; submitted validation not allowed | Accessible submitted returns | Accessible submitted returns |
| Submit | Own Draft | No | Only if also Preparer and owner |
| List/download/generate documents | Accessible submission | Accessible submission | Accessible submission |
| Manage users/roles | No | No | Yes |

Role combinations are allowed. Endpoint attributes provide coarse role checks; service ownership checks are still required to prevent IDOR. A 404 is used for missing or inaccessible submission resources where the service intentionally avoids disclosing existence.

## 5. API contract

All routes are under `/api`; protected routes require `Authorization: Bearer <JWT>`. Responses use JSON except successful PDF downloads. Exact request/response DTOs are defined in `Api/Models` and `Api/Services/Contracts`; avoid changing wire contracts without coordinating the client.

| Method and route | Access | Purpose / result |
|---|---|---|
| `GET /api/health` | Anonymous | Health status. |
| `POST /api/auth/login` | Anonymous | Login; returns `accessToken`, `expiresAtUtc`, and user id/email/roles. Invalid credentials return 401. |
| `POST /api/auth/logout` | Authenticated | Revoke current JWT; returns 204. |
| `GET /api/submissions` | Preparer, Reviewer, Admin | List; preparers see own records, reviewer/admin see all. SSN is masked in list data. |
| `POST /api/submissions` | Preparer | Create Draft; returns 201 and id/status. |
| `GET /api/submissions/{id}` | Preparer, Reviewer, Admin | Read accessible submission. Sensitive fields are masked for non-owner reviewer/admin reads according to service response mapping. |
| `PUT /api/submissions/{id}` | Preparer | Save owner's Draft; body contains `form`. Server recalculates. |
| `POST /api/submissions/{id}/validate` | Preparer, Reviewer, Admin | Validate accessible submission and return findings/calculated lines. |
| `POST /api/submissions/{id}/submit` | Preparer | Validate and submit owner's Draft; 422 returns findings. |
| `POST /api/submissions/{submissionId}/documents` | Preparer, Reviewer, Admin | Validate and generate/store PDF for an accessible Submitted return; success streams PDF. |
| `GET /api/submissions/{submissionId}/documents` | Preparer, Reviewer, Admin | List authorized document metadata. |
| `GET /api/submissions/{submissionId}/documents/{documentId}` | Preparer, Reviewer, Admin | Authorized PDF download. |
| `GET /api/admin/users` | Admin | List users, roles, lockout information. |
| `POST /api/admin/users` | Admin | Create user with initial allowed role; returns 201. |
| `PUT /api/admin/users/{userId}/roles` | Admin | Replace roles; last-admin demotion is rejected with 409. |

Common outcomes include 400 (invalid request), 401 (unauthenticated), 403 (role restriction), 404 (missing/inaccessible), 409 (invalid state/last admin), 422 (validation errors), and 500 (unexpected server failure). Do not expose exception details to clients in production.

## 6. Domain and persistence model

Database schema is managed through EF Core migrations in `Api/Data/Migrations`; startup applies migrations and initializes configured seed accounts/roles.

- **Identity:** `AspNetUsers`, `AspNetRoles`, and Identity relationship/claim/token tables. Role names: `Preparer`, `Reviewer`, `Admin`.
- **Form1040Submission:** GUID id, owner `UserId`, Draft/Submitted status, create/update/submit timestamps; one related form; validation findings and generated-document metadata.
- **Form1040Data:** one-to-one form values, form identity/address/status flags, dependents, input and calculated 1040 lines, banking, designee, signature, and preparer blocks. Money is represented as decimal/nullable decimal; calculated lines are recalculated by the server.
- **Form1040Dependent:** up to four listed dependents per return, identity/relationship and credit flags; separate relational records.
- **ValidationFinding:** code, severity, field key, human-readable message, timestamp, associated submission. Current validator primarily returns errors; severity supports warnings.
- **GeneratedDocument:** id, submission id, relative storage path, SHA-256, creation timestamp. Do not place absolute paths or file bytes in API responses.
- **RevokedToken:** JWT id and expiry, used to reject logged-out tokens; expired entries may be cleaned up in a future maintenance process.

Sensitive tax and identity data is stored in SQLite. Database-level encryption at rest is not implemented by the application; rely only on OS/disk protection in this local MVP and see `DESIGN.md` for the limitation.

## 7. Calculation and validation

`Form1040Calculator` computes the lines currently wired in the service: 1z, 9, 11a, 14, 15, 24, 25d, 33, 34, 37, and 35a. Filing-status standard deduction values are centralized. The client may mirror calculations for live UX, but the API overwrites computed values on save/validation/submit/PDF generation.

`Form1040Validator` checks required taxpayer name/SSN/filing status, spouse SSN for Married Filing Separately, ZIP, dependent count and required data/credit conflict, bank details/routing checksum/account format, monetary magnitude/precision, third-party designee fields, and refund/amount-owed/carry-forward consistency. Structured findings carry stable code, severity, field path, and message.

The requested acceptance criteria include reconciliation of all line totals and refund XOR amount owed. Review the implementation and add/adjust rules against official IRS 2025 instructions before production; a software test passing is not tax-law certification.

## 8. PDF rendering and document storage

- The template is local and configured through `Pdf:TemplatePath`; there is no runtime download.
- `Form1040PdfService` opens the template with PDFsharp, discovers fields, maps form values, sets text/check values, and returns a stream.
- The mapping must be verified against the official 2025 IRS fillable PDF, including all pages/checkboxes/calculated fields, by fixture-based tests and visual review. Do not infer field names from a different tax year.
- `LocalFileDocumentStore` creates a private root and validates paths against traversal. Save uses a temporary file, atomic move, and SHA-256. Environment/user/submission segments are part of the relative path; storage root is configurable (`Storage:Root`, default `storage`).
- Downloads are streamed only after repository and ownership/role checks. Apply filesystem permissions to keep storage accessible only to the application account and authorized administrators.
- Keep SQLite and storage directories out of Git and back them up together with suitable access controls.

## 9. Configuration and operations

Required first-start configuration is provided by environment variables or .NET user-secrets (never source-controlled):

- `JWT_SECRET`: random secret of at least 32 bytes.
- `ADMIN_EMAIL`, `ADMIN_PASSWORD`: initial administrator.
- `PREPARER_EMAIL`, `PREPARER_PASSWORD`, `REVIEWER_EMAIL`, `REVIEWER_PASSWORD`: optional local test identities where configured/seeded.
- `ConnectionStrings__DefaultConnection`: optional SQLite connection override.
- `Storage__Root`, `Pdf__TemplatePath`, `Frontend__Origin`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenMinutes`: optional settings overrides.

`appsettings.json` contains non-secret defaults only. Seeded users are created when absent; changing seed environment variables does not reset an existing password. See `README.md` for PowerShell setup and commands.

Logging uses built-in console providers and structured `ILogger` events. HTTP logging records method, path, status, and duration only; it does not include request/response bodies, headers, tokens, SSNs, or tax values. Keep PII out of all new log messages. Production exception responses are generic; full exception details are server-side only. No remote telemetry is configured by this application.

## 10. Testing strategy

- **Unit tests:** pure calculator and validator behavior; `LocalFileDocumentStore` path safety, hash, round-trip, missing file/cancellation; service use-case outcomes with repository/PDF/store fakes; token claims/expiry; PDF mapper tests against the actual official PDF fixture.
- **Integration tests:** API startup with isolated in-memory SQLite; login/logout/revocation; authorization matrix; submission lifecycle; masking; admin behavior; document authorization and response headers.
- **Client tests:** form calculations, validation display, route/role flows, API error handling, accessibility.
- **Build gates:** `dotnet build Api/Api.csproj`, `dotnet test tests/Api.Tests/Api.Tests.csproj`, and client production build. Tests use synthetic data only; never real taxpayer data.

The current test project contains a smoke/integration test and unit tests are being added. Coverage is not yet exhaustive and should not be represented as one test per file/method/scenario; focus on meaningful observable behaviors, branch boundaries, and security-sensitive cases.

## 11. Current implementation status and known gaps

Implemented code includes API controllers/services/repositories, Identity/JWT roles and revocation, SQLite entities/migrations, submission lifecycle, calculator/validator, PDFsharp service, local document store, React screens, and smoke tests. The repository has recently undergone layering and logging changes; run a full build and test suite to validate these together.

Before claiming completion or relying on generated returns, verify:

1. The current workspace search did not find `Api/assets/f1040.pdf`. The project expects this exact path; until the official 2025 fillable PDF is added, PDF generation is blocked and the existing field mapping is unverified.
2. PDF field mapping is correct for that exact template and visually verified.
3. Current calculation/validation rules match official 2025 instructions and all required lines/cross-field rules are covered.
4. Encryption-at-rest is either implemented or the local OS/disk protection limitation is accepted.
5. Docker Compose is added only if required; it is not currently present.
6. Backend, client, migrations, and expanded tests all pass from a clean checkout with secrets configured. A full build/test run after the recent refactor and logging changes has not yet been completed.

