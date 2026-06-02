using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Abstractions.Persistence;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct = default);
}
