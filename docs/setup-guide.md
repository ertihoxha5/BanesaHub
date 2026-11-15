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

Open [BanesaHub](https://localhost:7042/), enter the organization ID, and sign in with an owner or property manager account. Use the ID returned when the organization was created. For the sample organizations, look up the IDs in the development database:

```powershell
sqlcmd -S localhost -E -C -b -d BanesaHub -Q "SELECT Id, Slug FROM dbo.Organizations WHERE Slug IN ('demo-a', 'demo-b')"
```

The property list matches the search text against names and addresses, offers draft and active filters, and displays up to 20 properties per page. The MVC host sends requests to the API at https://localhost:7041 by default. Set `BanesaHubApi__BaseUrl` on the MVC host to change the HTTPS API address. In Visual Studio, configure both projects as startup projects using the ports above.


## Password reset email

Configure an SMTP relay that supports STARTTLS. Set `PasswordReset:Smtp:Host` and `PasswordReset:Smtp:From` in API user secrets. The default port is 587. Optional keys are `Port`, `Username` and `Password` under the same section. Keep relay credentials outside Git.

Use the usual antiforgery cookie and `X-CSRF-TOKEN` header for both POST requests. Send `email` to `/api/v1/auth/forgot-password`. The API returns 202 for either account lookup result when delivery is configured. Submit `email`, `code` and `newPassword` to `/api/v1/auth/reset-password`. The code arrives by email, expires after 30 minutes, and becomes invalid after use. Existing login cookies are rejected on subsequent authenticated requests. The request endpoint returns 503 when SMTP settings are missing.

## Seed a local development database

Apply migrations first and confirm the API connection points at your development database. Set `DevelopmentSeed:Password` in the API project user secrets to a password that meets the Identity policy. The sample accounts share this local password.

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/BanesaHub.API --no-launch-profile -- seed-development
```

The command exits after creating `owner-a@example.test` for `demo-a` and `owner-b@example.test` for `demo-b`. A repeat run preserves intact fixtures and their passwords. If the sample emails or slugs already belong to other records, use a separate development database. Start the API normally to sign in through its account endpoints.

## Run the SQL Server isolation test

Use a development SQL Server login that can create and drop databases. The test replaces the connection's database name with a unique BanesaHub_Test name, applies migrations and removes that database after the test.

```powershell
$env:BANESAHUB_TEST_SQLSERVER = 'Server=localhost;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5'
try {
    dotnet test tests/BanesaHub.IntegrationTests --filter FullyQualifiedName~OrganizationIsolationOnSqlServer
} finally {
    Remove-Item Env:BANESAHUB_TEST_SQLSERVER
}
```

Expect one passing test. To verify the development seed separately, run the seed command twice against a clean, migrated development database. Confirm that demo-a and demo-b each have one owner membership and their owner accounts can still sign in after the second run.
