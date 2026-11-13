# BanesaHub

BanesaHub is a property and rental management system for Kosovo. Its MVP covers organizations, properties, leases, monthly charges, manual payment records and maintenance. The interface defaults to Albanian and EUR.

## Build locally

Install the .NET 10 SDK. For IDE development, use Visual Studio 2026 with the ASP.NET and web development workload and open BanesaHub.sln. From the repository root:

```powershell
dotnet --list-sdks
dotnet restore BanesaHub.sln
dotnet build BanesaHub.sln --no-restore
```

M01 currently provides the project scaffold. Health, OpenAPI, xUnit, CI and SQL Server setup are tracked in [the milestone plan](docs/milestones.md). See [product requirements](docs/product-requirements.md) for MVP scope.

See the [setup guide](docs/setup-guide.md) for running both hosts with HTTPS and applying local database migrations.

## Project boundaries

| Project | Responsibility and direct project references |
| --- | --- |
| Domain | Business entities and rules. No project references. |
| Application | Use cases, DTOs and interfaces. References Domain. |
| Infrastructure | SQL Server, EF Core and external adapters. References Application and Domain. |
| API | HTTP interface and service composition. References Application and Infrastructure. |
| Web | MVC and Razor interface. Calls the API over HTTP. |
| UnitTests and IntegrationTests | Separate project shells. Test dependencies will be wired when xUnit is introduced. |
