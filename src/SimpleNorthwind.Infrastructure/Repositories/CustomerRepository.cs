using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class CustomerRepository(IUnitOfWork uow) : ICustomerRepository
{
    private const string Columns =
        "customer_id, company_name, contact_number, contact_title, create_date, create_user, is_out_contacted, out_contacted_date";

    public async Task<Customer?> GetByIdAsync(int customerId, CancellationToken ct = default)
    {
        var sql = $"SELECT {Columns} FROM dbo.customers WHERE customer_id = @customerId;";
        return await uow.Connection.QuerySingleOrDefaultAsync<Customer>(
            new CommandDefinition(sql, new { customerId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Customer>> ListAsync(CancellationToken ct = default)
    {
        var sql = $"SELECT {Columns} FROM dbo.customers ORDER BY customer_id;";
        var rows = await uow.Connection.QueryAsync<Customer>(
            new CommandDefinition(sql, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<int> InsertAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.customers (company_name, contact_number, contact_title, create_date, create_user, is_out_contacted, out_contacted_date)
            OUTPUT INSERTED.customer_id
            VALUES (@CompanyName, @ContactNumber, @ContactTitle, @CreateDate, @CreateUser, @IsOutContacted, @OutContactedDate);
            """;
        return await uow.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, customer, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.customers
            SET company_name = @CompanyName, contact_number = @ContactNumber, contact_title = @ContactTitle,
                is_out_contacted = @IsOutContacted, out_contacted_date = @OutContactedDate
            WHERE customer_id = @CustomerId;
            """;
        var affected = await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, customer, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int customerId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dbo.customers WHERE customer_id = @customerId;";
        var affected = await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { customerId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return affected > 0;
    }
}
