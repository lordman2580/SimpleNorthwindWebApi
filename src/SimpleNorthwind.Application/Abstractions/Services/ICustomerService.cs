using SimpleNorthwind.Application.Customers;
using SimpleNorthwind.Domain.Common;

namespace SimpleNorthwind.Application.Abstractions.Services;

public interface ICustomerService
{
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, string actingUser, CancellationToken ct = default);
    Task<Result<CustomerDto>> UpdateAsync(int customerId, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int customerId, CancellationToken ct = default);
    Task<Result<CustomerDto>> GetAsync(int customerId, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerDto>> ListAsync(CancellationToken ct = default);
}
