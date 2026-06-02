using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class CategoryRepository(IUnitOfWork uow) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT category_id, category_name, description FROM dbo.categories ORDER BY category_id;";
        var rows = await uow.Connection.QueryAsync<Category>(
            new CommandDefinition(sql, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }
}
