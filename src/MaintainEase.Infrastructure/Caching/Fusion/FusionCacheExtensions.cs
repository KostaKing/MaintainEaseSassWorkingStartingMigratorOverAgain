using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using EFCoreSecondLevelCacheInterceptor;

namespace MaintainEase.Infrastructure.Caching.Fusion
{
    /// <summary>
    /// Extensions for fusion cache registration
    /// </summary>
    public static class FusionCacheExtensions
    {
        /// <summary>
        /// Add fusion caching services
        /// </summary>
        public static IServiceCollection AddFusionCache(this IServiceCollection services, IConfiguration configuration)
        {
            // Register EF Core second level cache
            services.AddEFSecondLevelCache(options =>
                options.UseEasyCachingCoreProvider("fusion-cache", isHybridCache: false)
                    .DisableLogging(false)
                    .UseCacheKeyPrefix("EF_")
            );

            // Configure EF Core to use caching interceptor
            services.AddDbContext<Data.Context.AppDbContext>((provider, options) =>
            {
                // Get the original builder and use it as base
                var originalBuilder = options.Options.FindExtension<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>();
                
                // Add second level cache interceptor
                options.AddInterceptors(provider.GetRequiredService<SecondLevelCacheInterceptor>());
            }, ServiceLifetime.Scoped);

            // Register fusion cache service
            services.AddScoped<ICacheService, FusionCacheService>();

            return services;
        }
    }
}
