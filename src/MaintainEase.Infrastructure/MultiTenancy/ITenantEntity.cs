using System;

namespace MaintainEase.Infrastructure.MultiTenancy
{
    /// <summary>
    /// Interface for tenant-specific entities
    /// </summary>
    public interface ITenantEntity
    {
        Guid TenantId { get; set; }
    }
}
