using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Abstractions.Security;
using SimpleNorthwind.Infrastructure.Options;
using SimpleNorthwind.Infrastructure.Persistence;
using SimpleNorthwind.Infrastructure.Repositories;
using SimpleNorthwind.Infrastructure.Security;

namespace SimpleNorthwind.Infrastructure;

public static class DependencyInjection
{
    private const string EncPrefix = "enc:";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Dapper：snake_case 欄位對映 PascalCase 屬性
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        // 可逆機密保護（AES-256-GCM）；金鑰來自 APP_SECRET_KEY 或 secret.decryption.key
        services.AddSingleton<ISecretProtector>(_ => new AesSecretProtector(FindSecretKey()));

        // Options + enc: 解密 + 啟動驗證
        services.AddOptions<DbOptions>()
            .Configure(o => o.ConnectionString = configuration.GetConnectionString("SimpleNorthwind") ?? string.Empty)
            .PostConfigure<ISecretProtector>((o, protector) => o.ConnectionString = Reveal(o.ConnectionString, protector))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "ConnectionStrings:SimpleNorthwind 未設定。")
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .PostConfigure<ISecretProtector>((o, protector) => o.Secret = Reveal(o.Secret, protector))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret), "Jwt:Secret 未設定。")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Issuer/Audience 未設定。")
            .ValidateOnStart();

        services.AddOptions<AppOptions>()
            .Bind(configuration.GetSection(AppOptions.SectionName))
            .ValidateOnStart();

        // 連線與 Unit of Work
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
        services.AddScoped<IApiLogRepository, ApiLogRepository>();

        // 安全（無狀態，Singleton）
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHashing, PasswordHashing>();

        return services;
    }

    private static string Reveal(string value, ISecretProtector protector) =>
        value.StartsWith(EncPrefix, StringComparison.Ordinal)
            ? protector.Decrypt(value[EncPrefix.Length..])
            : value;

    /// <summary>prod 用環境變數 APP_SECRET_KEY；dev 由 base dir 往上找 gitignored secret.decryption.key。</summary>
    private static string? FindSecretKey()
    {
        var fromEnv = Environment.GetEnvironmentVariable("APP_SECRET_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Trim();

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "secret.decryption.key");
            if (File.Exists(candidate))
                return File.ReadAllText(candidate).Trim();
            dir = dir.Parent;
        }

        return null;
    }
}
