using System;
using Microsoft.AspNetCore.Http;

namespace MaintainEase.Infrastructure.MultiTenancy
{
    /// <summary>
    /// Implementation of tenant provider that uses HTTP context
    /// </summary>
    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITenantResolver _tenantResolver;

        public TenantProvider(
            IHttpContextAccessor httpContextAccessor,
            ITenantResolver tenantResolver)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _tenantResolver = tenantResolver ?? throw new ArgumentNullException(nameof(tenantResolver));
        }

        public Guid GetCurrentTenantId()
        {
            // Get tenant identifier from HTTP context (from header, subdomain, claim, etc.)
            var tenantIdentifier = GetTenantIdentifierFromHttpContext();
            
            if (string.IsNullOrEmpty(tenantIdentifier))
            {
                // Default tenant or throw exception based on business rules
                return Guid.Parse("00000000-0000-0000-0000-000000000001"); // Default tenant
            }
            
            // Resolve tenant ID from identifier
            return _tenantResolver.ResolveTenantId(tenantIdentifier);
        }

        public string GetCurrentTenantName()
        {
            // Get tenant identifier from HTTP context
            var tenantIdentifier = GetTenantIdentifierFromHttpContext();
            
            if (string.IsNullOrEmpty(tenantIdentifier))
            {
                return "Default"; // Default tenant name
            }
            
            // Resolve tenant name from identifier
            return _tenantResolver.ResolveTenantName(tenantIdentifier);
        }

        public string GetCurrentTenantConnectionString()
        {
            // Get tenant identifier from HTTP context
            var tenantIdentifier = GetTenantIdentifierFromHttpContext();
            
            if (string.IsNullOrEmpty(tenantIdentifier))
            {
                return "DefaultConnection"; // Default connection string name
            }
            
            // Resolve tenant connection string from identifier
            return _tenantResolver.ResolveTenantConnectionString(tenantIdentifier);
        }

        private string GetTenantIdentifierFromHttpContext()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return null;
            }
            
            // Option 1: From subdomain
            var host = httpContext.Request.Host.Value;
            var subdomain = host.Split('.')[0];
            if (!string.IsNullOrEmpty(subdomain) && subdomain != "www")
            {
                return subdomain;
            }
            
            // Option 2: From header
            if (httpContext.Request.Headers.TryGetValue("X-Tenant", out var tenantHeader))
            {
                return tenantHeader;
            }
            
            // Option 3: From claim
            var user = httpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var tenantClaim = user.FindFirst("tenant");
                if (tenantClaim != null)
                {
                    return tenantClaim.Value;
                }
            }
            
            return null;
        }
    }
}
