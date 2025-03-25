using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Enums;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Infrastructure.Data.Context;

namespace MaintainEase.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository implementation for Lease entity
    /// </summary>
    public class LeaseRepository : BaseRepository<Lease>, ILeaseRepository
    {
        public LeaseRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Lease>> GetActiveLeasesByUnitIdAsync(Guid unitId)
        {
            return await _dbSet
                .Where(l => l.UnitId == unitId && l.IsActive && l.Status == LeaseStatus.Active)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lease>> GetLeasesByTenantIdAsync(Guid tenantId)
        {
            return await _dbSet
                .Where(l => l.TenantIds.Contains(tenantId))
                .ToListAsync();
        }

        public async Task<IEnumerable<Lease>> GetExpiringLeasesAsync(int daysToExpiration)
        {
            var expirationThreshold = DateTimeOffset.UtcNow.AddDays(daysToExpiration);
            
            return await _dbSet
                .Where(l => l.IsActive && 
                           l.Status == LeaseStatus.Active && 
                           l.EndDate <= expirationThreshold)
                .ToListAsync();
        }
    }
}
