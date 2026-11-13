# Development log

## 2026-11-13 — Milestone 1, Checkpoint 1 (PDF M00.1)

- Reviewed all 16 pages of specification v1.0. Mapped the two milestone schemes, preserved PDF dependencies and acceptance criteria, and documented MVP scope, architecture and open decisions.
- Files: docs/milestones.md and docs/development-log.md.
- Tool audit: .NET SDK 9.0.306; Visual Studio Community 2022 17.14.19 with ASP.NET and web development workload; Git 2.42.0.windows.2; EF CLI 8.0.11. SQL Server has a registered default instance (MSSQL16), and SQL Server 2019 LocalDB and sqlcmd are present. Instance connectivity was not verified.
- Before scaffolding, install Visual Studio 2026 with the ASP.NET and web development workload and its matching .NET 10 SDK. Verify using `dotnet --list-sdks`. See [Microsoft's compatibility matrix](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs). Pin an EF Core 10 CLI tool locally when adding EF Core, and confirm SQL Server connectivity before creating the first migration.
- Validation: requirements and milestone mapping reviewed against the PDF. Build and tests are not applicable to this documentation checkpoint.
- Recommended commit: `docs(planning): record BanesaHub milestones and prerequisites`.


## 2026-11-13 — Milestone 1, Checkpoint 2 (PDF M00.2)

- Prepared an Albanian interview guide, anonymized note template and criteria for evaluating evidence.
- Blocked: the project owner confirmed that no interviews have taken place. No findings or confirmations are recorded.
- Files: docs/discovery-interviews.md, docs/milestones.md and docs/development-log.md.
- Validation: checked the guide against PDF M00 participant and acceptance targets. Build and tests do not apply to this documentation checkpoint.
- Recommended commit: `docs(discovery): prepare property management interviews`.


## 2026-11-13 — Milestone 1, Checkpoint 3 (PDF M00.3)

- Drafted six personas and twelve prioritized stories with acceptance criteria from the specification. Interview validation remains open under M00.2.
- Added a root Visual Studio cache ignore rule at the owner's request. The previously tracked .vs/slnx.sqlite requires separate removal from the Git index.
- Files: .gitignore, docs/personas-and-user-stories.md, docs/milestones.md and docs/development-log.md.
- Validation: checked story IDs and priorities, whitespace, credential patterns and the ignore rule. Build and tests do not apply.
- Recommended commit: `docs(discovery): draft personas and prioritized stories`.


## 2026-11-13 — Milestone 1, Checkpoint 4 (PDF M00.4)

- Drafted eight Albanian screen wireframes and four MVP workflows. Grouped related views to cover all nine PDF screen groups. Interview validation remains pending.
- Files: docs/mvp-wireframes.md, docs/milestones.md and docs/development-log.md.
- Validation: checked coverage against the specification, screen and workflow IDs, whitespace and credential patterns. Build and tests do not apply.
- Recommended commit: `docs(discovery): sketch MVP screens and workflows`.


## 2026-11-13 — Milestone 1, Checkpoint 5 (PDF M00.5)

- Consolidated the draft PRD, prioritized delivery backlog and definition of done. Linked the personas and wireframes.
- Discovery remains open pending M00.2 interview evidence and review. M01 stays unstarted. The tool recheck found .NET SDK 9.0.306 and Visual Studio 2022 17.14.
- Validation checked requirements coverage, document links, whitespace and credential patterns. Build and tests do not apply.
- Recommended commit: `docs(discovery): consolidate MVP requirements and backlog`.

## 2026-11-13 — Milestone 1, Setup Checkpoint 1 (PDF M01.1)

- Created BanesaHub.sln and seven project shells targeting net10.0. Added minimal controller and MVC startup files. Project references and xUnit setup are later checkpoints.
- Started setup with real interview validation still open. Practice interviews remain fictional and do not count as evidence.
- Verified solution membership and target frameworks. The build failed. A direct project build confirmed NETSDK1045 because only SDK 9.0.306 is installed. Install the .NET 10 SDK and a compatible Visual Studio before validating.
- Files: BanesaHub.sln, five projects under src, two under tests, docs/milestones.md and docs/development-log.md.
- Recommended commit: chore(solution): initialize BanesaHub .NET 10 scaffold

## 2026-11-13 — M01.1 build validation

- Restored and built all seven projects with isolated .NET SDK 10.0.404. Build passed with zero warnings and errors.
- The isolated SDK lives outside the repository. Visual Studio toolchain validation remains pending. xUnit setup is scheduled for M01.4.
- Recommended commit: docs(solution): record .NET 10 scaffold validation

## 2026-11-13 — Milestone 1, Setup Checkpoint 2 (PDF M01.2)

- Added the five production project references defined by the specification. Domain and Web have no project references. Test references are scheduled with xUnit.
- Updated README.md and added AGENTS.md. Verified the existing rules for .vs, bin and obj.
- Restored and built all seven projects using isolated .NET SDK 10.0.404 with zero warnings and errors. Diff and credential checks passed. Test execution is scheduled for M01.4.
- Recommended commit: chore(solution): configure project references and repository guidance

## 2026-11-13 — Milestone 1, Setup Checkpoint 3 (PDF M01.3)

- Registered health checks and OpenAPI in the API host. GET /health checks process liveness. GET /openapi/v1.json serves the generated document in Development.
- Configured JSON console logs with trace and span IDs. Database readiness will follow database setup.
- Restored and built all seven projects with zero warnings or errors. Verified health 200 and JSON trace scopes in both environments. OpenAPI returned 200 in Development and 404 in Production.
- Commit commands from the repository root

  git add -- src/BanesaHub.API/Program.cs src/BanesaHub.API/BanesaHub.API.csproj docs/milestones.md docs/development-log.md
  git commit -m "feat(api): add health checks logging and OpenAPI"

## 2026-11-13 — Milestone 1, Setup Checkpoint 4 (PDF M01.4)

- Configured both xUnit projects. Added three API smoke cases for health and Development versus Production OpenAPI exposure. The unit test project has no cases yet.
- Added a GitHub Actions workflow that restores, builds and tests on pull requests and pushes to main.
- Local Release restore and build passed with zero build warnings or errors. All three smoke cases passed. The test runner reported the empty unit test assembly and used its default temporary directory. GitHub Actions has yet to run this workflow.
- Commit commands from the repository root

  git add -- .github/workflows/ci.yml src/BanesaHub.API/BanesaHub.API.csproj tests/BanesaHub.IntegrationTests/ApiSmokeTests.cs tests/BanesaHub.IntegrationTests/BanesaHub.IntegrationTests.csproj tests/BanesaHub.UnitTests/BanesaHub.UnitTests.csproj docs/milestones.md docs/development-log.md
  git commit -m "test(api): add smoke tests and CI workflow"

## 2026-11-13 — Setup Checkpoint 5 (PDF M01.5)

- Registered a scoped EF Core context with the SQL Server provider. Connection settings come from configuration and are required when resolving the context.
- Pinned the EF CLI and documented the local Windows authentication and user secrets setup.
- The solution restored and built without warnings or errors. Three API smoke cases passed. The EF design-time operation resolved BanesaHubDbContext with the SQL Server provider.
- Live SQL Server access remains unverified. Windows authentication failed from the agent session. No migration or database was created.

  git add -- .config/dotnet-tools.json src/BanesaHub.API/BanesaHub.API.csproj src/BanesaHub.API/Program.cs src/BanesaHub.Infrastructure/BanesaHub.Infrastructure.csproj src/BanesaHub.Infrastructure/Persistence/BanesaHubDbContext.cs src/BanesaHub.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs docs/setup-guide.md docs/milestones.md docs/development-log.md
  git commit -m "feat(persistence): configure EF Core and SQL Server"

## 2026-11-13 — Setup Checkpoint 6 (PDF M01.6)

- Generated the initial EF migration and snapshot for the current empty model. Reviewed the idempotent SQL and confirmed no pending model changes.
- Solution built without warnings or errors. Three API smoke cases passed. SQL Server preflight failed with an SSPI authentication error, so live application and history verification remain blocked.
- Commit commands from the repository root

  git add -- src/BanesaHub.Infrastructure/Persistence/Migrations docs/setup-guide.md docs/milestones.md docs/development-log.md
  git commit -m "feat(persistence): add initial EF Core baseline migration"

## 2026-11-13 — Milestone 3, Identity persistence (PDF M02.1a)

- The owner reported that the M01.6 clean database verification succeeded.
- Registered ASP.NET Core Identity with GUID keys and EF Core stores. Required unique emails and configured the lockout policy. Sign-in enforcement will be added with the account endpoints.
- Generated the SQL Server Identity schema migration. Build and three API smoke tests passed. EF reported no pending model changes. The Identity migration has yet to be applied to live SQL Server.
- Commit commands from the repository root

  git add -- src/BanesaHub.API/Program.cs src/BanesaHub.Infrastructure/BanesaHub.Infrastructure.csproj src/BanesaHub.Infrastructure/Persistence/BanesaHubDbContext.cs src/BanesaHub.Infrastructure/Persistence/Migrations docs/milestones.md docs/development-log.md
  git commit -m "feat(auth): configure Identity persistence"

## 2026-11-13 — Account endpoints (M02.1b)

- Added registration, login and logout under /api/v1/auth using secure Identity cookies, antiforgery and per-IP rate limiting. Over HTTPS, fetch /api/v1/auth/antiforgery and send the requestToken as X-CSRF-TOKEN with both the relevant cookies and POST request. Fetch a fresh token after login.
- Build passed without warnings or errors. Five integration cases passed, including two account flow cases with EF InMemory and ephemeral data protection. Live SQL Server account validation remains pending.
- Commit commands from the repository root

  git add -- src/BanesaHub.API/Program.cs src/BanesaHub.API/Controllers/AuthController.cs tests/BanesaHub.IntegrationTests/AuthTests.cs tests/BanesaHub.IntegrationTests/BanesaHub.IntegrationTests.csproj docs/milestones.md docs/development-log.md
  git commit -m "feat(auth): add registration login and logout"

## 2026-11-13 — Password reset (M02.1c)

- Added Identity password reset tokens with a 30-minute lifetime and SMTP delivery over STARTTLS. The reset flow rejects previously issued sessions on the next authenticated request.
- Added API and MVC launch instructions. Build passed with zero warnings or errors. All six integration cases passed using test email capture and in-memory storage. Live SMTP and SQL Server reset were not verified.

  git add -- README.md docs/setup-guide.md docs/milestones.md docs/development-log.md src/BanesaHub.API/Program.cs src/BanesaHub.API/Auth/PasswordResetDelivery.cs src/BanesaHub.API/Controllers/AuthController.cs tests/BanesaHub.IntegrationTests/AuthTests.cs
  git commit -m "feat(auth): add password reset"

## 2026-11-14 — Organization and membership schema (M02.2a)

- Added organizations and memberships with organization-specific roles. The migration enforces a unique slug, one membership per organization and user, valid roles, and foreign keys.
- Build and all six integration cases passed. EF found no model drift. Reviewed the generated SQL constraints. The migration has yet to be applied to live SQL Server.

  git add -- src/BanesaHub.Domain/Organizations src/BanesaHub.Infrastructure/Persistence/BanesaHubDbContext.cs src/BanesaHub.Infrastructure/Persistence/Migrations docs/milestones.md docs/development-log.md
  git commit -m "feat(organizations): add organization and membership schema"

## 2026-11-14 — Organization creation (M02.2b)

- Added authenticated POST /api/v1/organizations with antiforgery validation. The creator receives the Owner role from the authenticated user ID. The organization and membership share one EF Core save. Slugs are normalized to lowercase and duplicate slugs return 409.
- Build passed without warnings or errors. All seven integration cases passed. The new case checks authentication, antiforgery, validation, duplicates, and persisted ownership using EF Core InMemory. Live SQL Server transaction and concurrent slug behavior remain unverified.

  git add -- src/BanesaHub.API/Program.cs src/BanesaHub.API/Controllers/OrganizationsController.cs src/BanesaHub.Application/Organizations/IOrganizationRegistration.cs src/BanesaHub.Infrastructure/Persistence/OrganizationRegistration.cs tests/BanesaHub.IntegrationTests/AuthTests.cs docs/milestones.md docs/development-log.md
  git commit -m "feat(organizations): create organizations with owner membership"
