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

## 2026-11-14 — Organization invitations (M02.2c)

- Active owners can issue invitations through POST /api/v1/organizations/{organizationId}/invitations. Share the returned token privately with the recipient. The token expires after 48 hours. Only its hash is stored. Invitations grant one of the four non-owner membership roles.
- The signed-in recipient submits the token to POST /api/v1/organizations/invitations/accept. The account email must match. Acceptance and membership are persisted in one save. Both endpoints require antiforgery validation.
- Build and all seven integration cases passed. EF found no pending model changes. Live SQL Server migration and concurrent acceptance remain unverified.

  git add -- src/BanesaHub.API/Program.cs src/BanesaHub.API/Controllers/OrganizationInvitationsController.cs src/BanesaHub.Application/Organizations/IOrganizationInvitations.cs src/BanesaHub.Domain/Organizations/OrganizationInvitation.cs src/BanesaHub.Infrastructure/Persistence/BanesaHubDbContext.cs src/BanesaHub.Infrastructure/Persistence/OrganizationInvitations.cs src/BanesaHub.Infrastructure/Persistence/Migrations tests/BanesaHub.IntegrationTests/AuthTests.cs docs/milestones.md docs/development-log.md
  git commit -m "feat(organizations): add membership invitations"

## 2026-11-15 — Organization resource authorization (M02.3)

- Added authenticated GET /api/v1/organizations/{organizationId}. The query requires active organization and membership records for the signed-in user. Outsiders receive 404. Invitation issuance uses the shared active-membership filter and requires the Owner role.
- Build passed without warnings or errors. All seven integration cases passed, including access before and after accepting an invitation. SQL Server isolation verification remains scheduled for M02.5.

  git add -- src/BanesaHub.API/Program.cs src/BanesaHub.API/Controllers/OrganizationsController.cs src/BanesaHub.Application/Organizations/IOrganizationDirectory.cs src/BanesaHub.Infrastructure/Persistence/OrganizationDirectory.cs src/BanesaHub.Infrastructure/Persistence/OrganizationInvitations.cs tests/BanesaHub.IntegrationTests/AuthTests.cs docs/milestones.md docs/development-log.md
  git commit -m "feat(organizations): scope access to active memberships"

## 2026-11-15 — Login and role audit (M02.4a)

- Added durable UTC audit records for successful, rejected and locked-out sign-ins. Account events have no organization scope. Records omit email addresses, passwords and tokens.
- Organization creation and invitation acceptance write role grant events in the same save as membership. Added the EF migration for audit storage.
- Build passed without warnings or errors. All seven integration cases passed, including audit assertions. EF found no model drift. Live SQL Server migration remains pending.

  git add -- src/BanesaHub.API/Controllers/AuthController.cs src/BanesaHub.Infrastructure/Persistence/AuditEvent.cs src/BanesaHub.Infrastructure/Persistence/BanesaHubDbContext.cs src/BanesaHub.Infrastructure/Persistence/OrganizationRegistration.cs src/BanesaHub.Infrastructure/Persistence/OrganizationInvitations.cs src/BanesaHub.Infrastructure/Persistence/Migrations tests/BanesaHub.IntegrationTests/AuthTests.cs docs/milestones.md docs/development-log.md
  git commit -m "feat(audit): record login outcomes and role grants"

## 2026-11-15 — Development seed data (M02.4b)

- Added the explicit Development-only seed-development command. It uses Identity and the existing organization registration service to create two sample owners and organizations in one SQL Server transaction. The password comes from API user secrets. Existing intact fixtures are preserved.
- Restored and built with zero warnings or errors. All seven existing integration cases passed. The compiled command rejected Production. Live seeding and repeat-run verification remain pending because this session cannot authenticate to local SQL Server.

  git add -- src/BanesaHub.API/Program.cs src/BanesaHub.API/Auth/DevelopmentSeed.cs docs/setup-guide.md docs/milestones.md docs/development-log.md
  git commit -m "feat(dev): add opt-in organization seed data"

## 2026-11-15 — SQL Server isolation verification (M02.5)

- Added an opt-in SQL Server integration case for the organization and invitation flow. It migrates a uniquely named test database and removes it after the run.
- Build passed with zero warnings and errors. Seven integration cases passed and the SQL Server case skipped. The SQL connection preflight failed with an SSPI authentication error. Live isolation and seed verification remain open.

  git add -- tests/BanesaHub.IntegrationTests/AuthTests.cs tests/BanesaHub.IntegrationTests/SqlServerFactAttribute.cs docs/setup-guide.md docs/milestones.md docs/development-log.md
  git commit -m "test(organizations): add SQL Server isolation coverage"

## 2026-11-15 — Property inventory schema (M03.1)

- Added Property, Building and Unit models and a SQL Server migration. Organization-scoped foreign keys prevent cross-organization parent links. Unit numbers are unique within each building. Draft properties can omit address and category.
- Build passed with zero warnings or errors. EF reported no pending model changes. Reviewed the generated SQL constraints. Seven integration cases passed and the opt-in SQL Server case skipped.
- SQL Server Windows authentication remains unavailable in this session. M02.5 verification stays open and the inventory migration has yet to be applied to a live database.

  git add -- src/BanesaHub.Domain/Properties src/BanesaHub.Infrastructure/Persistence/PropertyModelConfiguration.cs src/BanesaHub.Infrastructure/Persistence/BanesaHubDbContext.cs src/BanesaHub.Infrastructure/Persistence/Migrations docs/milestones.md docs/development-log.md
  git commit -m "feat(properties): add property inventory schema"

## 2026-11-15 — Property API (M03.2a)

- Added paginated list, detail, draft creation, metadata update and soft archive under /api/v1/organizations/{organizationId}/properties. Active owners and property managers have access within their organization.
- Build passed without warnings or errors. Seven integration cases passed, including draft creation, scoped access, a rejected cross-organization update, and archived record retention. The opt-in SQL Server case was skipped.
- Commit commands from the repository root

  git add -- src/BanesaHub.API/Program.cs src/BanesaHub.API/Controllers/PropertiesController.cs src/BanesaHub.Application/Properties/IPropertyInventory.cs src/BanesaHub.Infrastructure/Persistence/PropertyInventory.cs tests/BanesaHub.IntegrationTests/AuthTests.cs docs/milestones.md docs/development-log.md
  git commit -m "feat(properties): add scoped property endpoints"

## 2026-11-15 — Building API (M03.2b)

- Added paginated list, detail, creation, metadata update and soft archive under /api/v1/organizations/{organizationId}/properties/{propertyId}/buildings. Active owners and property managers can manage buildings within a visible property. Floor count is optional and must be positive when supplied.
- Added controller and application contract, wired the EF Core implementation into API dependency injection, and shared the property management role check. No schema changes.
- Restore and build passed with zero warnings and errors. Seven integration cases passed, including building validation, pagination, wrong property and organization rejection, archived record retention, and hiding buildings when their property is archived. The opt-in SQL Server case was skipped. Diff and scoped secret checks passed.

  git add -- src/BanesaHub.API/Program.cs src/BanesaHub.API/Controllers/BuildingsController.cs src/BanesaHub.Application/Properties/IBuildingInventory.cs src/BanesaHub.Infrastructure/Persistence/BuildingInventory.cs src/BanesaHub.Infrastructure/Persistence/PropertyInventory.cs tests/BanesaHub.IntegrationTests/AuthTests.cs docs/milestones.md docs/development-log.md
  git commit -m "feat(properties): add scoped building endpoints"

## 2026-11-15 — Unit API (M03.2c)

- Added paginated list, detail, create, update and archive endpoints under each building. The API verifies the organization, property and building and reuses the property management role check.
- Validated unit numbers, decimal area and occupancy status. Duplicate numbers return HTTP 409. Numbers remain reserved after archive, consistent with the existing SQL Server unique index. The API also translates a database uniqueness violation from a concurrent write into HTTP 409.
- Restore and build passed with zero warnings and errors. Seven integration cases passed, including unit validation, pagination, duplicate handling, wrong building and organization rejection, and archive behavior. The opt-in SQL Server test was skipped, so the database conflict path was not exercised. Diff and scoped secret checks passed.

  git add -- src/BanesaHub.API/Program.cs src/BanesaHub.API/Controllers/UnitsController.cs src/BanesaHub.Application/Properties/IUnitInventory.cs src/BanesaHub.Infrastructure/Persistence/UnitInventory.cs src/BanesaHub.Infrastructure/Persistence/BuildingInventory.cs tests/BanesaHub.IntegrationTests/AuthTests.cs docs/milestones.md docs/development-log.md
  git commit -m "feat(properties): add scoped unit endpoints"

## 2026-11-15 — MVC property list (M03.3a)

- Added the Albanian property list and its organization entry, sign-in, empty and error states. The MVC host uses the existing Identity API over HTTPS. The API applies name/address search and status filters before pagination.
- Added an integration flow through the MVC and API test hosts covering sign-in, filtered data, paging, and rejection of access to another organization. Restore and build passed with zero warnings and errors. Seven integration tests passed, with the opt-in SQL Server test skipped. Scoped secret and diff checks passed.
- Updated the local setup guide with browser and organization ID instructions.

  git add -- src/BanesaHub.API/Controllers/PropertiesController.cs src/BanesaHub.Application/Properties/IPropertyInventory.cs src/BanesaHub.Infrastructure/Persistence/PropertyInventory.cs src/BanesaHub.Web/Program.cs src/BanesaHub.Web/Controllers/InventoryController.cs src/BanesaHub.Web/Inventory/InventoryApi.cs src/BanesaHub.Web/Inventory/PropertyList.cs src/BanesaHub.Web/Views/Inventory/Index.cshtml src/BanesaHub.Web/wwwroot/css/inventory.css tests/BanesaHub.IntegrationTests/AuthTests.cs tests/BanesaHub.IntegrationTests/BanesaHub.IntegrationTests.csproj docs/milestones.md docs/setup-guide.md docs/development-log.md
  git commit -m "feat(properties): add filtered MVC property list"

## 2026-11-15 — MVC property forms (M03.3b)

- Added Albanian property creation, editing and archive confirmation from the property list. Address and category are optional, and new properties start as drafts.
- Reused the API for authorization and writes. MVC validates its own form token and fetches an API token for the signed-in session before each write.
- Restore and build passed with zero warnings and errors. Seven integration tests passed, including draft creation, validation, editing, archiving, missing-CSRF rejection and cross-organization write denial. The opt-in SQL Server test was skipped. Diff and scoped secret checks passed.

  git add -- src/BanesaHub.Web/Controllers/PropertyFormsController.cs src/BanesaHub.Web/Inventory/PropertyForm.cs src/BanesaHub.Web/Inventory/InventoryApi.cs src/BanesaHub.Web/Views/Inventory/Index.cshtml src/BanesaHub.Web/Views/PropertyForms/Form.cshtml src/BanesaHub.Web/Views/Shared/_InventoryLayout.cshtml tests/BanesaHub.IntegrationTests/AuthTests.cs docs/milestones.md docs/development-log.md
  git commit -m "feat(properties): add MVC property forms"

## 2026-11-15 — MVC building list and forms (M03.3c)

- Linked each property to its building list with up to 20 buildings per page. Added Albanian create, edit and archive forms with optional positive floor counts and archive confirmation.
- Reused API authorization and the existing antiforgery flow for writes. Organization, property and building IDs are bound from the route.
- Restore and build passed with zero warnings and errors. Seven integration tests passed, including building navigation, paging, validation, create/edit/archive, and wrong-property write rejection. The opt-in SQL Server test was skipped. Diff and scoped secret checks passed.

  git add -- src/BanesaHub.Web/Controllers/BuildingsController.cs src/BanesaHub.Web/Inventory/BuildingModels.cs src/BanesaHub.Web/Inventory/InventoryApi.cs src/BanesaHub.Web/Views/Buildings/Form.cshtml src/BanesaHub.Web/Views/Buildings/Index.cshtml src/BanesaHub.Web/Views/Inventory/Index.cshtml tests/BanesaHub.IntegrationTests/AuthTests.cs docs/milestones.md docs/development-log.md
  git commit -m "feat(properties): add MVC building management"

## 2026-11-15 — MVC unit list and forms (M03.3d)

- Linked each building to its paginated Albanian unit list. Added forms for creating, editing and confirming archive of units.
- Displayed Albanian occupancy labels and formatted area for Kosovo Albanian. Area accepts a decimal comma or point. Duplicate numbers display an error on the form.
- Restore and build passed with zero warnings and errors. Seven integration tests passed, covering navigation, paging, localized area, duplicate feedback, create/edit/archive, and wrong-building write rejection. The opt-in SQL Server test was skipped. Diff and scoped secret checks passed.

  git add -- src/BanesaHub.Web/Controllers/UnitsController.cs src/BanesaHub.Web/Inventory/UnitModels.cs src/BanesaHub.Web/Inventory/InventoryApi.cs src/BanesaHub.Web/Views/Units/Form.cshtml src/BanesaHub.Web/Views/Units/Index.cshtml src/BanesaHub.Web/Views/Buildings/Index.cshtml tests/BanesaHub.IntegrationTests/AuthTests.cs docs/milestones.md docs/development-log.md
  git commit -m "feat(properties): add MVC unit management"
