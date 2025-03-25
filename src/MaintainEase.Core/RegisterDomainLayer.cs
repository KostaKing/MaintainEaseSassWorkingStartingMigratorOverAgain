using Microsoft.Extensions.DependencyInjection;
using MaintainEase.Core.Domain;

namespace MaintainEase.Core
{
    /// <summary>
    /// Registration script for the domain layer
    /// </summary>
    public static class RegisterDomainLayer
    {
        public static IServiceCollection AddDomainLayer(this IServiceCollection services, bool includeIsraeliMarket = false)
        {
            // Register core domain services
            services.AddDomainServices();
            
            // Register domain event handlers
            services.AddDomainEventHandlers();
            
            // Optionally register Israeli market services
            if (includeIsraeliMarket)
            {
                services.AddIsraeliMarketServices();
            }
            
            return services;
        }
    }
}
