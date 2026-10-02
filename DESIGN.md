# Galent 1040 (2025) — Design Note

## Purpose
A local-first React and ASP.NET Core application for authorized team members to prepare, validate, submit, and review Form 1040 (2025) data, then generate and retrieve a locally stored PDF. It is a workflow tool, not tax advice or a guarantee of return correctness.

## Key design decisions
- **Backend and persistence:** ASP.NET Core Web API on .NET 10; EF Core 10 and SQLite; ASP.NET Core Identity handles password hashing and role membership.
- **Authentication:** Short-lived JWT bearer tokens carry identity and role claims. Logout writes the JWT id to a local revocation table. There are no refresh tokens; re-login is required after expiration.
- **Roles:** Preparer (create/edit/submit own drafts and access own documents), Reviewer (view/validate), Admin (user management and broad review access). Controller role attributes and service-level ownership checks are both required.
- **Layering:** Controllers adapt HTTP; application services orchestrate use cases and enforce business/access rules; repositories encapsulate Identity/EF persistence; PDF and storage have separate interfaces.
- **Client:** React + TypeScript + Vite. Client-side calculations/validation support responsiveness only; the server recalculates and validates before persistence, submission, and PDF generation.
- **PDF and files:** PDFsharp populates a local configured AcroForm template. `IDocumentStore` writes private local files outside web static content; database metadata stores relative path and SHA-256. The official 2025 PDF and mapping must be verified together before relying on output.
- **Local operation:** SQLite and files remain local; no application cloud services or telemetry are configured. The current documented workflow runs the API and Vite separately; Docker Compose is a future deliverable if required.

## PII and security
- Form contents, SSNs, bank details, identity-protection PINs, and generated returns are sensitive. Use synthetic data in development and tests.
- SSNs/account numbers are masked in applicable non-owner/list responses; authorization must be checked before every submission/document read or write.
- Never log credentials, passwords, JWTs, SSNs, bank details, form contents, or raw request/response bodies. HTTP logging is restricted to method, path, status, and duration; structured application logs use identifiers and outcome counts where appropriate.
- Keep JWT and seed passwords out of Git. Configure `JWT_SECRET` (at least 32 bytes) and initial account credentials with environment variables or .NET user-secrets. Do not use committed defaults as credentials.
- Local document paths are constrained beneath the configured storage root; the storage directory must not be public and should have restrictive OS ACLs.
- **Encryption at rest:** Application-level database and file encryption are not currently implemented. SQLite and PDF files rely on host disk/OS permissions. Treat this as a known limitation; use full-disk encryption and restrictive account ACLs for local evaluation. A deployment requiring stronger protection needs a documented encryption/key-management design.
- Production errors return generic responses; details are logged on the local server. No remote log export should be enabled without an explicit privacy/security decision.

## Validation and correctness
Server-side rules return structured findings (code, severity, field, message). The API recalculates derived lines on save and before workflow transitions/PDF rendering. Validator logic includes required identity/status, formats, dependent and banking rules, monetary constraints, designee requirements, and refund/amount-owed consistency. Confirm every calculation and cross-field rule against official 2025 IRS instructions; passing unit tests does not certify tax correctness.

## Assumptions and limitations
- The application targets the official IRS Form 1040 for tax year 2025. The template must be supplied locally (expected `Api/assets/f1040.pdf`); it is never fetched at runtime.
- PDF field mappings need tests and visual verification against that exact official file. If the file is missing or differs, generation is not verified.
- SQLite is appropriate for local/team evaluation with a single local instance; it is not configured for multi-host concurrent deployment.
- JWT revocation adds a local database lookup to protected-token validation. Expired revocation cleanup and refresh-token flows are not currently part of the design.
- Role combinations may be assigned; a user needs the Preparer role for preparer-only create/edit/submit endpoints.
- Docker artifacts and exhaustive unit/integration/client coverage are not yet confirmed in the repository.

## AI assistance
AI tools assisted with code, tests, and documentation drafts. A developer must review changes, tax calculations, security controls, and PDF output. No AI service is used by the running application.
