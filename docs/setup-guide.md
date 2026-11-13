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

The EF command resolves the context and provider without creating a database. The next checkpoint adds and applies the first migration. The API's `/health` endpoint measures process liveness and can run without SQL Server.
