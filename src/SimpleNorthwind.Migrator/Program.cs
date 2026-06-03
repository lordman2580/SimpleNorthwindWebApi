using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleNorthwind.Migrator;
using SimpleNorthwind.Migrator.Options;

// 設定來源：appsettings.json（dev LocalDB 明文）+ 環境變數（prod 以 ConnectionStrings__SimpleNorthwind 覆寫）。不使用 User Secrets。
// ContentRootPath 指向輸出目錄（appsettings.json 已 CopyToOutputDirectory），避免從 CWD 找不到。
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

var connectionString = builder.Configuration.GetConnectionString("SimpleNorthwind")
    ?? throw new InvalidOperationException(
        "找不到連線字串 ConnectionStrings:SimpleNorthwind（請設定 appsettings.json 或環境變數 ConnectionStrings__SimpleNorthwind）。");

builder.Services.AddSingleton(new MigratorOptions { ConnectionString = connectionString });
builder.Services.AddSingleton<MigrationRunner>();

using var host = builder.Build();
var runner = host.Services.GetRequiredService<MigrationRunner>();
await runner.RunAsync();
