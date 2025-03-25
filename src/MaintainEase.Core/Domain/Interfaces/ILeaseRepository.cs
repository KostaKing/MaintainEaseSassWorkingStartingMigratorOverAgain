using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Entities;

namespace MaintainEase.Core.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for lease entities
    /// </summary>
    public interface ILeaseRepository : IRepository<Lease>
    {
        Task<IEnumerable<Lease>> GetActiveLeasesByUnitIdAsync(Guid unitId);
        Task<IEnumerable<Lease>> GetLeasesByTenantIdAsync(Guid tenantId);
        Task<IEnumerable<Lease>> GetExpiringLeasesAsync(int daysToExpiration);
    }
}
