using Microsoft.Extensions.DependencyInjection;

namespace CRUDzor.EFCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCRUDzorEFCore(this IServiceCollection services)
    {
        return services;
    }
}
