using System;

namespace MaintainEase.Infrastructure.MultiTenancy
{
    /// <summary>
    /// Interface for tenant resolver
    /// </summary>
    public interface ITenantResolver
    {
        Guid ResolveTenantId(string tenantIdentifier);
        string ResolveTenantName(string tenantIdentifier);
        string ResolveTenantConnectionString(string tenantIdentifier);
    }
}
