using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Domain.Entities;
using SimpleNorthwind.Infrastructure.Persistence;
using static SimpleNorthwind.Infrastructure.Persistence.SqlNaming;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class CategoryRepository(IUnitOfWork uow) : ICategoryRepository
{
    private static readonly string ListSql =
        $"SELECT {EntityColumns<Category>.All} FROM dbo.categories ORDER BY {Col(nameof(Category.CategoryId))};";

    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct = default)
    {
        var rows = await uow.Connection.QueryAsync<Category>(
            new CommandDefinition(ListSql, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }
}
