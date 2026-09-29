# Galent AI — One-page Design Note

Purpose
- Provide a concise summary of major design decisions, PII handling, and assumptions for the Galent 1040 (2025) local app.

Key decisions
- Backend: ASP.NET Core Web API (net8.0) with EF Core (SQLite) and Identity for authentication. JWT tokens for stateless auth between React frontend and API.
- Frontend: React + TypeScript (Vite) for fast developer experience and modern tooling.
- PDF Library: Default to PdfSharpCore (permissive) unless project owner accepts iText7's AGPL.

PII handling and security
- SSNs are treated as highly sensitive: avoid logging, mask in UI/API, and avoid transmission in URLs.
- Secrets (JWT secret, admin credentials) are not stored in Git; use environment variables or dotnet user-secrets.
- Storage: PDFs saved to a configurable storageRoot outside any public folder. Download endpoints validate authorization before streaming files.
- Encryption-at-rest: not implemented in this initial version. Documented limitation: local files in storageRoot are only protected by OS-level permissions. For production, use full disk encryption or encrypted file containers.

Validation strategy
- The server is authoritative: it recalculates all computed fields on save/submit and returns structured findings if validation fails.
- Client performs friendly validation for UX but server enforces rules.

PDF mapping strategy
- Create Form1040PdfMapper that maps domain fields to AcroForm fields discovered in PHASE 2.
- Keep mapping centralized in one class/file to aid maintainability and testing.

Operational assumptions
- The target is local execution with Docker Compose and SQLite, but those files are not present yet.
- Seed credentials must be supplied through environment variables or dotnet user-secrets. Existing appsettings values are insecure scaffold defaults and must be removed before the application is shared.

AI usage
- AI assistance (GitHub Copilot / ChatGPT) was used to draft scaffolding, tests, and documentation. All design and implementation decisions are reviewed and controlled by the developer.

Known limitations
- No encryption-at-rest in the current scaffold. Local data is only protected by OS-level file permissions until encryption is implemented.
- The official PDF is missing from the inspected workspace, so field discovery and PDF generation cannot begin until it is supplied under ./assets/f1040.pdf.
- No refresh token implementation; tokens are short-lived to reduce risk.

Next actions
- PHASE 2: add the official PDF in ./assets and run AcroForm field discovery.
- PHASE 3: remove scaffold secrets and implement secure authentication, validation, and storage.
