# Local SQL Server setup

Install the .NET 10 SDK and a local SQL Server Developer or Express instance. From PowerShell in the repository root, confirm Windows authentication works. Use your actual instance name, such as `localhost\SQLEXPRESS` for a named Express instance.

```powershell
sqlcmd -S localhost -E -C -l 5 -b -Q "SELECT 1"
dotnet tool restore
dotnet user-secrets set 'ConnectionStrings:BanesaHub' 'Server=localhost;Database=BanesaHub;Integrated Security=True;Encrypt=True;TrustServerCertificate=True' --project src/BanesaHub.API
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef dbcontext info --project src/BanesaHub.Infrastructure --startup-project src/BanesaHub.API
```

The example trusts a local development certificate. Use a trusted server certificate in other environments. Supply deployed connection settings through the host's secret configuration using `ConnectionStrings__BanesaHub`.

The EF command resolves the context and provider without creating a database. The API's `/health` endpoint measures process liveness and can run without SQL Server.

## Verify the baseline on a clean database

Use a fresh database name and a local SQL Server account with permission to create it. The migration records the EF baseline in the history table. Business tables arrive with their modules.

```powershell
$env:ConnectionStrings__BanesaHub = 'Server=localhost;Database=BanesaHub_MigrationCheck;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
try {
    dotnet ef database update --project src/BanesaHub.Infrastructure --startup-project src/BanesaHub.API
    if ($LASTEXITCODE -ne 0) { throw 'Migration failed' }
    sqlcmd -S localhost -E -C -b -d BanesaHub_MigrationCheck -Q "SELECT MigrationId FROM dbo.__EFMigrationsHistory"
} finally {
    Remove-Item Env:ConnectionStrings__BanesaHub
}
```

Expect one row for `20261113223412_InitialBaseline`.
