using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace PharmaDesk.Tests.Integration;

/// <summary>
/// Smoke test for Database/PharmaDesk_Complete.sql: requires a reachable SQL Server
/// instance. Point PHARMADESK_TEST_MASTER_CONNECTION at it (defaults to a local
/// Windows-auth instance) before running `dotnet test` on this project.
/// </summary>
public class SchemaBootstrapTests
{
    private static readonly Regex GoBatchSeparator = new(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static string MasterConnectionString =>
        Environment.GetEnvironmentVariable("PHARMADESK_TEST_MASTER_CONNECTION")
        ?? "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    private static string PharmaDeskConnectionString =>
        Environment.GetEnvironmentVariable("PHARMADESK_TEST_CONNECTION")
        ?? "Server=localhost;Database=PharmaDesk;Trusted_Connection=True;TrustServerCertificate=True;";

    [Fact]
    public void RunningTheScriptTwice_IsIdempotentAndRecordsSchemaVersion()
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Database", "PharmaDesk_Complete.sql");
        Assert.True(File.Exists(scriptPath), $"SQL script not found at {scriptPath}");

        var script = File.ReadAllText(scriptPath);
        var batches = GoBatchSeparator.Split(script)
            .Select(b => b.Trim())
            .Where(b => b.Length > 0)
            .ToArray();

        // First run: fresh or already-upgraded database, must succeed either way.
        RunScript(batches);

        // Second run: every block must detect it already applied and skip, without
        // throwing a duplicate-key or duplicate-object error.
        RunScript(batches);

        using var connection = new SqlConnection(PharmaDeskConnectionString);
        connection.Open();

        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.DatabaseSchemaVersions WHERE VersionNumber = '0.1.0'",
            connection);
        var count = (int)command.ExecuteScalar()!;

        Assert.Equal(1, count);
    }

    private static void RunScript(IEnumerable<string> batches)
    {
        using var connection = new SqlConnection(MasterConnectionString);
        connection.Open();

        foreach (var batch in batches)
        {
            using var command = new SqlCommand(batch, connection);
            command.ExecuteNonQuery();
        }
    }
}
