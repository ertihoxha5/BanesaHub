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

## Run the API and MVC projects

From the repository root, verify the .NET 10 SDK and trust the local HTTPS certificate:

```powershell
dotnet --list-sdks
dotnet dev-certs https --trust
dotnet restore BanesaHub.sln
dotnet tool restore
```

Configure the API's development connection string as shown above. Apply the migrations to your local BanesaHub development database before using account endpoints:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project src/BanesaHub.Infrastructure --startup-project src/BanesaHub.API
dotnet run --project src/BanesaHub.API --no-launch-profile -- --urls https://localhost:7041
```

Open [API health](https://localhost:7041/health) or the [Development OpenAPI JSON](https://localhost:7041/openapi/v1.json). The health endpoint checks API liveness. In a second PowerShell window at the repository root:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/BanesaHub.Web --no-launch-profile -- --urls https://localhost:7042
```

The MVC host starts on port 7042. Its root currently returns 404 because the UI controllers and views have yet to be implemented. In Visual Studio, open BanesaHub.sln, select BanesaHub.API as the startup project, and set the same Development environment and URL in a local debug profile. Run it with Ctrl+F5. You can run BanesaHub.Web separately with the second command above.

## Password reset email

Configure an SMTP relay that supports STARTTLS. Set `PasswordReset:Smtp:Host` and `PasswordReset:Smtp:From` in API user secrets. The default port is 587. Optional keys are `Port`, `Username` and `Password` under the same section. Keep relay credentials outside Git.

Use the usual antiforgery cookie and `X-CSRF-TOKEN` header for both POST requests. Send `email` to `/api/v1/auth/forgot-password`. The API returns 202 for either account lookup result when delivery is configured. Submit `email`, `code` and `newPassword` to `/api/v1/auth/reset-password`. The code arrives by email, expires after 30 minutes, and becomes invalid after use. Existing login cookies are rejected on subsequent authenticated requests. The request endpoint returns 503 when SMTP settings are missing.
