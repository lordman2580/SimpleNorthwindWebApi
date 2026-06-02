using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SimpleNorthwind.Migrator.Options;

namespace SimpleNorthwind.Migrator;

/// <summary>
/// 自製 migration runner：
/// 1) 連 master 建立目標資料庫（若不存在）→ 2) 確保 schema_versions →
/// 3) 依檔名數字排序逐一套用未執行的 embedded .sql（各自交易內、成功才記版本）。全程 idempotent。
/// </summary>
public sealed class MigrationRunner(MigratorOptions options, ILogger<MigrationRunner> logger)
{
    private const string VersionsTable = "dbo.schema_versions";
    private const string ResourceMarker = ".Migrations.";

    public async Task RunAsync(CancellationToken ct = default)
    {
        var csb = new SqlConnectionStringBuilder(options.ConnectionString);
        var targetDb = csb.InitialCatalog;
        if (string.IsNullOrWhiteSpace(targetDb))
            throw new InvalidOperationException("連線字串缺少 Initial Catalog（目標資料庫名）。");

        await EnsureDatabaseAsync(csb, targetDb, ct).ConfigureAwait(false);

        await using var conn = new SqlConnection(options.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await EnsureVersionsTableAsync(conn, ct).ConfigureAwait(false);
        var applied = await GetAppliedVersionsAsync(conn, ct).ConfigureAwait(false);

        var migrations = LoadEmbeddedMigrations();
        logger.LogInformation("找到 {Count} 個 migration script，已套用 {Applied} 個。", migrations.Count, applied.Count);

        foreach (var migration in migrations)
        {
            if (applied.Contains(migration.Version))
            {
                logger.LogInformation("跳過已套用 {Version:0000} {Name}", migration.Version, migration.Name);
                continue;
            }

            await ApplyAsync(conn, migration, ct).ConfigureAwait(false);
            logger.LogInformation("已套用 {Version:0000} {Name}", migration.Version, migration.Name);
        }

        logger.LogInformation("Migration 完成，資料庫 {Db} 為最新狀態。", targetDb);
    }

    private async Task EnsureDatabaseAsync(SqlConnectionStringBuilder csb, string targetDb, CancellationToken ct)
    {
        var masterCsb = new SqlConnectionStringBuilder(csb.ConnectionString) { InitialCatalog = "master" };
        await using var conn = new SqlConnection(masterCsb.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        const string sql = """
            IF DB_ID(@db) IS NULL
            BEGIN
                DECLARE @cmd nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@db);
                EXEC sp_executesql @cmd;
            END
            """;
        await conn.ExecuteAsync(new CommandDefinition(sql, new { db = targetDb }, cancellationToken: ct)).ConfigureAwait(false);
        logger.LogInformation("已確保資料庫 {Db} 存在。", targetDb);
    }

    private static async Task EnsureVersionsTableAsync(SqlConnection conn, CancellationToken ct)
    {
        const string sql = """
            IF OBJECT_ID('dbo.schema_versions', 'U') IS NULL
            CREATE TABLE dbo.schema_versions
            (
                version        INT           NOT NULL CONSTRAINT PK_schema_versions PRIMARY KEY,
                script_name    NVARCHAR(200) NOT NULL,
                applied_at_utc datetime2(0)  NOT NULL
            );
            """;
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct)).ConfigureAwait(false);
    }

    private static async Task<HashSet<int>> GetAppliedVersionsAsync(SqlConnection conn, CancellationToken ct)
    {
        var versions = await conn.QueryAsync<int>(
            new CommandDefinition($"SELECT version FROM {VersionsTable}", cancellationToken: ct)).ConfigureAwait(false);
        return versions.ToHashSet();
    }

    private static async Task ApplyAsync(SqlConnection conn, Migration migration, CancellationToken ct)
    {
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                migration.Sql, transaction: tx, commandTimeout: 180, cancellationToken: ct)).ConfigureAwait(false);

            await conn.ExecuteAsync(new CommandDefinition(
                $"INSERT INTO {VersionsTable} (version, script_name, applied_at_utc) VALUES (@v, @n, SYSUTCDATETIME());",
                new { v = migration.Version, n = migration.Name }, transaction: tx, cancellationToken: ct)).ConfigureAwait(false);

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    private static List<Migration> LoadEmbeddedMigrations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var migrations = new List<Migration>();

        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (!resource.Contains(ResourceMarker, StringComparison.Ordinal) ||
                !resource.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileName = resource[(resource.IndexOf(ResourceMarker, StringComparison.Ordinal) + ResourceMarker.Length)..];
            var versionToken = fileName.Split('_', 2)[0];
            if (!int.TryParse(versionToken, out var version))
                continue;

            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            migrations.Add(new Migration(version, fileName, reader.ReadToEnd()));
        }

        return migrations.OrderBy(m => m.Version).ToList();
    }

    private sealed record Migration(int Version, string Name, string Sql);
}
