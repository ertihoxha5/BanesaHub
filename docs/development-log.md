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
