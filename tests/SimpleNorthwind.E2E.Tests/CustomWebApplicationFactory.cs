using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleNorthwind.Migrator;
using SimpleNorthwind.Migrator.Options;

namespace SimpleNorthwind.E2E.Tests;

/// <summary>
/// E2E factory：對本機 LocalDB 的獨立測試庫 <c>SimpleNorthwind_E2E</c> 跑真實 WebApi（WebApplicationFactory&lt;Program&gt;）。
///
/// 隔離策略（與 14-Checkpoint 原案的 Respawn 不同，見該 doc「完成紀錄」）：
/// Respawn 會清空所有資料表（含 schema_versions），但種子是透過 migration 植入，全清後 migrator 會重跑
/// CREATE TABLE 而失敗，且登入所需的員工種子也會消失。故改為「<see cref="InitializeAsync"/> 時 drop→重建→
/// migrate→seed 一次（決定性種子）」，測試以自建資料 + 直查 DB 驗 delta 達到隔離；整個 collection 序列化執行。
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string LocalDbServer = @"(localdb)\MSSQLLocalDB";
    private const string Database = "SimpleNorthwind_E2E";

    public string ConnectionString { get; } =
        $"Server={LocalDbServer};Database={Database};Integrated Security=True;TrustServerCertificate=True;";

    private string MasterConnectionString =>
        $"Server={LocalDbServer};Database=master;Integrated Security=True;TrustServerCertificate=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Testing 環境：不載入 dev User Secrets（避免覆蓋成 dev 庫），機密改由下方 in-memory 提供。
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SimpleNorthwind"] = ConnectionString,
                ["Jwt:Secret"] = "e2e-only-test-signing-secret-please-change-0123456789",
            });
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await DropDatabaseAsync().ConfigureAwait(false);
        var runner = new MigrationRunner(
            new MigratorOptions { ConnectionString = ConnectionString },
            NullLogger<MigrationRunner>.Instance);
        await runner.RunAsync().ConfigureAwait(false);
    }

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync().ConfigureAwait(false);

    private async Task DropDatabaseAsync()
    {
        await using var conn = new SqlConnection(MasterConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            IF DB_ID('{Database}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{Database}];
            END
            """;
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    // ---- 直查 DB 的測試輔助（沒有 products 端點，庫存只能直查驗證）----

    public async Task<int> GetProductStockAsync(int productId)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT quantities FROM dbo.products WHERE product_id = @id;";
        cmd.Parameters.Add(new SqlParameter("@id", productId));
        return (int)(await cmd.ExecuteScalarAsync().ConfigureAwait(false))!;
    }

    public async Task ClearApiLogsAsync()
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM dbo.api_logs;";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task<ApiLogRow?> GetLatestApiLogAsync(string actions)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT TOP (1) user_id, actions, action_detail, summary_date
            FROM dbo.api_logs
            WHERE actions = @actions
            ORDER BY summary_date DESC;
            """;
        cmd.Parameters.Add(new SqlParameter("@actions", actions));
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
            return null;

        return new ApiLogRow(
            reader.IsDBNull(0) ? null : reader.GetInt32(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetDateTime(3));
    }
}

/// <summary>單列 api_logs 投影（測試用）。</summary>
public sealed record ApiLogRow(int? UserId, string Actions, string? Detail, DateTime SummaryDate);

/// <summary>整個 E2E 測試集合共用同一個 factory + 同一個測試庫，並序列化執行避免互相污染。</summary>
[CollectionDefinition(E2ECollection.Name)]
public sealed class E2ECollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "E2E";
}
