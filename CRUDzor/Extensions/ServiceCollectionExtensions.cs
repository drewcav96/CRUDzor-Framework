using Microsoft.Extensions.DependencyInjection;

namespace CRUDzor.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCRUDzor(this IServiceCollection services)
    {
        return services;
    }
}
