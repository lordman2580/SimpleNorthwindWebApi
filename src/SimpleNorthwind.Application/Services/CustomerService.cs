using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Customers;
using SimpleNorthwind.Domain.Common;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Services;

public sealed class CustomerService(ICustomerRepository customers) : ICustomerService
{
    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, string actingUser, CancellationToken ct = default)
    {
        var customer = new Customer
        {
            CompanyName = request.CompanyName,
            ContactName = request.ContactName,
            ContactNumber = request.ContactNumber,
            ContactTitle = request.ContactTitle,
            Email = request.Email,
            CreateDate = DateTime.UtcNow,
            CreateUser = actingUser,
            IsOutContacted = false,
            OutContactedDate = null
        };

        customer.CustomerId = await customers.InsertAsync(customer, ct).ConfigureAwait(false);
        return ToDto(customer);
    }

    public async Task<Result<CustomerDto>> UpdateAsync(int customerId, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = await customers.GetByIdAsync(customerId, ct).ConfigureAwait(false);
        if (customer is null)
            return Error.NotFound("customer.not_found", $"找不到客戶 {customerId}。");

        // 未修改任何欄位 → 不寫 DB，回 400（與庫存不足同款 ProblemDetails）。
        if (request.CompanyName == customer.CompanyName &&
            request.ContactName == customer.ContactName &&
            request.ContactNumber == customer.ContactNumber &&
            request.ContactTitle == customer.ContactTitle &&
            request.Email == customer.Email &&
            request.IsOutContacted == customer.IsOutContacted &&
            request.OutContactedDate == customer.OutContactedDate)
        {
            return Error.Validation("customer.not_modified", "未修改任何欄位，未更新。");
        }

        customer.CompanyName = request.CompanyName;
        customer.ContactName = request.ContactName;
        customer.ContactNumber = request.ContactNumber;
        customer.ContactTitle = request.ContactTitle;
        customer.Email = request.Email;
        customer.IsOutContacted = request.IsOutContacted;
        customer.OutContactedDate = request.OutContactedDate;

        await customers.UpdateAsync(customer, ct).ConfigureAwait(false);
        return ToDto(customer);
    }

    public async Task<Result> DeleteAsync(int customerId, CancellationToken ct = default)
    {
        var deleted = await customers.DeleteAsync(customerId, ct).ConfigureAwait(false);
        return deleted
            ? Result.Success()
            : Result.Failure(Error.NotFound("customer.not_found", $"找不到客戶 {customerId}。"));
    }

    public async Task<Result<CustomerDto>> GetAsync(int customerId, CancellationToken ct = default)
    {
        var customer = await customers.GetByIdAsync(customerId, ct).ConfigureAwait(false);
        if (customer is null)
            return Error.NotFound("customer.not_found", $"找不到客戶 {customerId}。");

        return ToDto(customer);
    }

    public async Task<IReadOnlyList<CustomerDto>> ListAsync(CancellationToken ct = default)
    {
        var customerList = await customers.ListAsync(ct).ConfigureAwait(false);
        return customerList.Select(ToDto).ToList();
    }

    private static CustomerDto ToDto(Customer c) =>
        new(c.CustomerId, c.CompanyName, c.ContactName, c.ContactNumber, c.ContactTitle, c.Email,
            c.CreateDate, c.CreateUser, c.IsOutContacted, c.OutContactedDate);
}
