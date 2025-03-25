using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Infrastructure.Data.Context;

namespace MaintainEase.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository implementation for Tenant entity
    /// </summary>
    public class TenantRepository : BaseRepository<Tenant>, ITenantRepository
    {
        public TenantRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Tenant>> GetActiveTenantAsync()
        {
            return await _dbSet.Where(t => t.IsActive).ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(t => t.Email == email);
        }

        public async Task<Tenant> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.Email == email);
        }
    }
}
