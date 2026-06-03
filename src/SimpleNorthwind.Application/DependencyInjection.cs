using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Services;

namespace SimpleNorthwind.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();

        services.AddValidatorsFromAssemblyContaining<AddApplicationMarker>();

        return services;
    }

    /// <summary>標記類別，供 FluentValidation 掃描本組件的 validator。</summary>
    private sealed class AddApplicationMarker;
}
