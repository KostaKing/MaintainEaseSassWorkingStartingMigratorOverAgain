using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace MaintainEase.Infrastructure.MultiTenancy
{
    /// <summary>
    /// Implementation of tenant resolver using configuration and caching
    /// </summary>
    public class TenantResolver : ITenantResolver
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        public TenantResolver(
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public Guid ResolveTenantId(string tenantIdentifier)
        {
            var cacheKey = $"TenantId_{tenantIdentifier}";
            
            if (!_cache.TryGetValue(cacheKey, out Guid tenantId))
            {
                // In a real implementation, query a tenant database
                // Here we just use configuration for demo purposes
                var tenantIdString = _configuration[$"Tenants:{tenantIdentifier}:Id"];
                
                if (string.IsNullOrEmpty(tenantIdString) || !Guid.TryParse(tenantIdString, out tenantId))
                {
                    throw new ArgumentException($"Unknown tenant identifier: {tenantIdentifier}");
                }
                
                _cache.Set(cacheKey, tenantId, _cacheDuration);
            }
            
            return tenantId;
        }

        public string ResolveTenantName(string tenantIdentifier)
        {
            var cacheKey = $"TenantName_{tenantIdentifier}";
            
            if (!_cache.TryGetValue(cacheKey, out string tenantName))
            {
                // In a real implementation, query a tenant database
                tenantName = _configuration[$"Tenants:{tenantIdentifier}:Name"] ?? tenantIdentifier;
                _cache.Set(cacheKey, tenantName, _cacheDuration);
            }
            
            return tenantName;
        }

        public string ResolveTenantConnectionString(string tenantIdentifier)
        {
            var cacheKey = $"TenantConnectionString_{tenantIdentifier}";
            
            if (!_cache.TryGetValue(cacheKey, out string connectionString))
            {
                // In a real implementation, generate or retrieve a connection string for the tenant
                // For schema-per-tenant approach, you might use the same server but different schema
                var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
                
                // Add search path for tenant schema in PostgreSQL
                connectionString = $"{baseConnectionString};SearchPath={tenantIdentifier},public";
                
                _cache.Set(cacheKey, connectionString, _cacheDuration);
            }
            
            return connectionString;
        }
    }
}
