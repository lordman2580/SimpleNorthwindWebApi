using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleNorthwind.Infrastructure.Options;

namespace SimpleNorthwind.Infrastructure.Persistence;

internal interface IDbConnectionFactory
{
    SqlConnection Create();
}

internal sealed class DbConnectionFactory(IOptions<DbOptions> options) : IDbConnectionFactory
{
    public SqlConnection Create() => new(options.Value.ConnectionString);
}
