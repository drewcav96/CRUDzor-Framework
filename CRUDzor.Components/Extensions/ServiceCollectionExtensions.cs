using Microsoft.Extensions.DependencyInjection;
using Radzen;

namespace CRUDzor.Components.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCRUDzorComponents(this IServiceCollection services)
    {
        CRUDzor.Extensions.ServiceCollectionExtensions.AddCRUDzor(services);

        // Configure Radzen
        services.AddRadzenComponents();

        // Configure Blazor 
        services.AddCascadingAuthenticationState();

        return services;
    }
}
