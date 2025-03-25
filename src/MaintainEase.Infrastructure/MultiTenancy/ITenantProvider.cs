using System;

namespace MaintainEase.Infrastructure.MultiTenancy
{
    /// <summary>
    /// Interface for tenant provider
    /// </summary>
    public interface ITenantProvider
    {
        Guid GetCurrentTenantId();
        string GetCurrentTenantName();
        string GetCurrentTenantConnectionString();
    }
}
