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
