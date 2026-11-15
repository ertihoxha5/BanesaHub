using Xunit;

namespace BanesaHub.IntegrationTests;

internal sealed class SqlServerFactAttribute : FactAttribute
{
    public const string ConnectionVariable = "BANESAHUB_TEST_SQLSERVER";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
        {
            Skip = $"Set {ConnectionVariable} to run against a disposable SQL Server database.";
        }
    }
}
