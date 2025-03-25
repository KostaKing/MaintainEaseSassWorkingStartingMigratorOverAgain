using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.Services;
using MaintainEase.Core.Domain.IsraeliMarket.Services;

namespace MaintainEase.Core.Domain
{
    /// <summary>
    /// Extensions for registering domain services with DI container
    /// </summary>
    public static class ServiceRegistration
    {
        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            // Register base domain services
            services.AddScoped<LeaseService>();
            services.AddScoped<PropertyService>();
            services.AddScoped<TenantService>();

            // Register all domain services using reflection
            var domainServiceTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType
                            && typeof(IDomainService).IsAssignableFrom(t));

            foreach (var serviceType in domainServiceTypes)
            {
                services.AddScoped(serviceType);
                services.AddScoped(typeof(IDomainService), serviceType);
            }

            return services;
        }

        public static IServiceCollection AddIsraeliMarketServices(this IServiceCollection services)
        {
            // Register Israeli market specific services
            services.AddScoped<ArnonaCalculationService>();
            services.AddScoped<CPIRentCalculationService>();
            services.AddScoped<HolidayAwareSchedulingService>();
            services.AddScoped<ProtectedTenancyService>();

            return services;
        }

        public static IServiceCollection AddDomainEventHandlers(this IServiceCollection services)
        {
            // Register all domain event handlers using reflection
            var handlerTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType
                            && t.GetInterfaces().Any(i => 
                                i.IsGenericType && 
                                i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)));

            foreach (var handlerType in handlerTypes)
            {
                var interfaceType = handlerType.GetInterfaces()
                    .First(i => i.IsGenericType && 
                                i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));

                services.AddScoped(interfaceType, handlerType);
            }

            return services;
        }
    }
}
