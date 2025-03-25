using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.IsraeliMarket.IsraelSpecificInterfaces;
using MaintainEase.Core.Domain.IsraeliMarket.Interfaces;
using MaintainEase.Infrastructure.Data;
using MaintainEase.Infrastructure.Data.Context;
using MaintainEase.Infrastructure.Data.Interceptors;
using MaintainEase.Infrastructure.Data.Repositories;
using MaintainEase.Infrastructure.MultiTenancy;
using MaintainEase.Infrastructure.Security;
using MaintainEase.Infrastructure.Caching;
using MaintainEase.Infrastructure.Services;
using MaintainEase.Infrastructure.Files;

namespace MaintainEase.Infrastructure
{
    /// <summary>
    /// Registration of infrastructure services
    /// </summary>
    public static class ServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add HTTP context accessor
            services.AddHttpContextAccessor();
            
            // Add database
            services.AddDatabaseServices(configuration);
            
            // Add repositories
            services.AddRepositories();
            
            // Add multi-tenancy
            services.AddMultiTenancy();
            
            // Add caching
            services.AddCachingServices(configuration);
            
            // Add security
            services.AddSecurityServices();
            
            // Add other infrastructure services
            services.AddInfrastructureComponents();
            
            // Add file storage
            services.AddFileStorage(configuration);
            
            return services;
        }

        private static IServiceCollection AddDatabaseServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add database context
            services.AddDbContext<AppDbContext>((provider, options) =>
            {
                // Get tenant provider
                var tenantProvider = provider.GetRequiredService<ITenantProvider>();
                var connectionString = tenantProvider.GetCurrentTenantConnectionString();
                
                // Use PostgreSQL
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                    npgsqlOptions.EnableRetryOnFailure(3);
                });
                
                // Enable sensitive data logging in development
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging();
                }
            });
            
            // Add unit of work
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            // Add interceptors
            services.AddScoped<IAuditInterceptor, AuditInterceptor>();
            services.AddScoped<IDomainEventInterceptor, DomainEventInterceptor>();
            
            return services;
        }

        private static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Add repositories
            services.AddScoped<IPropertyRepository, PropertyRepository>();
            services.AddScoped<ITenantRepository, TenantRepository>();
            services.AddScoped<ILeaseRepository, LeaseRepository>();
            
            // Add Israeli market repositories
            services.AddScoped<ICPIRepository, CPIRepository>();
            services.AddScoped<IJewishCalendarRepository, JewishCalendarRepository>();
            
            return services;
        }

        private static IServiceCollection AddMultiTenancy(this IServiceCollection services)
        {
            // Add multi-tenancy services
            services.AddSingleton<ITenantResolver, TenantResolver>();
            services.AddScoped<ITenantProvider, TenantProvider>();
            
            return services;
        }

        private static IServiceCollection AddCachingServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add memory cache
            services.AddMemoryCache();
            
            // Add distributed cache (Redis in production, memory cache in development)
            var cacheSettings = configuration.GetSection("Caching");
            var useRedis = cacheSettings["Provider"] == "Redis";
            
            if (useRedis)
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = cacheSettings["RedisConnection"];
                    options.InstanceName = "MaintainEase:";
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }
            
            // Add cache service
            services.AddScoped<ICacheService, DistributedCacheService>();
            
            return services;
        }

        private static IServiceCollection AddSecurityServices(this IServiceCollection services)
        {
            // Add security services
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            
            return services;
        }

        private static IServiceCollection AddInfrastructureComponents(this IServiceCollection services)
        {
            // Add domain event dispatcher
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
            
            // Add date time service
            services.AddSingleton<IDateTimeService, DateTimeService>();
            
            return services;
        }

        private static IServiceCollection AddFileStorage(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add file storage
            var useCloudStorage = configuration["FileStorage:Provider"] == "Cloud";
            
            if (useCloudStorage)
            {
                // Add cloud file storage implementation
                // services.AddScoped<IFileStorageService, CloudFileStorageService>();
            }
            else
            {
                // Add local file storage implementation
                services.AddScoped<IFileStorageService, LocalFileStorageService>();
            }
            
            return services;
        }
    }
}
