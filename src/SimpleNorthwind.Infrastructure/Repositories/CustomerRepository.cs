using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Domain.Entities;
using SimpleNorthwind.Infrastructure.Persistence;
using static SimpleNorthwind.Infrastructure.Persistence.SqlNaming;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class CustomerRepository(IUnitOfWork uow) : ICustomerRepository
{
    // 欄位名一律由實體屬性以 nameof / reflection 推導（point 5/6），不硬寫字串。
    private static readonly string Columns = EntityColumns<Customer>.All;

    // 寫入欄位（排除 IDENTITY 主鍵 CustomerId）。
    private static readonly string[] InsertProps =
    [
        nameof(Customer.CompanyName), nameof(Customer.ContactName), nameof(Customer.ContactNumber), nameof(Customer.ContactTitle),
        nameof(Customer.Email), nameof(Customer.CreateDate), nameof(Customer.CreateUser), nameof(Customer.IsOutContacted),
        nameof(Customer.OutContactedDate),
    ];

    private static readonly string GetByIdSql =
        $"SELECT {Columns} FROM dbo.customers WHERE {Col(nameof(Customer.CustomerId))} = @customerId;";

    private static readonly string ListSql =
        $"SELECT {Columns} FROM dbo.customers ORDER BY {Col(nameof(Customer.CustomerId))};";

    private static readonly string InsertSql =
        $"""
        INSERT INTO dbo.customers ({Cols(InsertProps)})
        OUTPUT INSERTED.{Col(nameof(Customer.CustomerId))}
        VALUES ({Params(InsertProps)});
        """;

    private static readonly string UpdateSql =
        $"""
        UPDATE dbo.customers
        SET {Col(nameof(Customer.CompanyName))} = @{nameof(Customer.CompanyName)},
            {Col(nameof(Customer.ContactName))} = @{nameof(Customer.ContactName)},
            {Col(nameof(Customer.ContactNumber))} = @{nameof(Customer.ContactNumber)},
            {Col(nameof(Customer.ContactTitle))} = @{nameof(Customer.ContactTitle)},
            {Col(nameof(Customer.Email))} = @{nameof(Customer.Email)},
            {Col(nameof(Customer.IsOutContacted))} = @{nameof(Customer.IsOutContacted)},
            {Col(nameof(Customer.OutContactedDate))} = @{nameof(Customer.OutContactedDate)}
        WHERE {Col(nameof(Customer.CustomerId))} = @{nameof(Customer.CustomerId)};
        """;

    private static readonly string DeleteSql =
        $"DELETE FROM dbo.customers WHERE {Col(nameof(Customer.CustomerId))} = @customerId;";

    public async Task<Customer?> GetByIdAsync(int customerId, CancellationToken ct = default) =>
        await uow.Connection.QuerySingleOrDefaultAsync<Customer>(
            new CommandDefinition(GetByIdSql, new { customerId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);

    public async Task<IReadOnlyList<Customer>> ListAsync(CancellationToken ct = default)
    {
        var rows = await uow.Connection.QueryAsync<Customer>(
            new CommandDefinition(ListSql, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<int> InsertAsync(Customer customer, CancellationToken ct = default) =>
        await uow.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(InsertSql, customer, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);

    public async Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        var affected = await uow.Connection.ExecuteAsync(
            new CommandDefinition(UpdateSql, customer, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int customerId, CancellationToken ct = default)
    {
        var affected = await uow.Connection.ExecuteAsync(
            new CommandDefinition(DeleteSql, new { customerId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return affected > 0;
    }
}
