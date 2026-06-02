using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleNorthwind.Migrator;
using SimpleNorthwind.Migrator.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);

var connectionString = builder.Configuration.GetConnectionString("SimpleNorthwind")
    ?? throw new InvalidOperationException(
        "找不到連線字串 ConnectionStrings:SimpleNorthwind（請設定 User Secrets 或環境變數 ConnectionStrings__SimpleNorthwind）。");

builder.Services.AddSingleton(new MigratorOptions { ConnectionString = connectionString });
builder.Services.AddSingleton<MigrationRunner>();

using var host = builder.Build();
var runner = host.Services.GetRequiredService<MigrationRunner>();
await runner.RunAsync();
