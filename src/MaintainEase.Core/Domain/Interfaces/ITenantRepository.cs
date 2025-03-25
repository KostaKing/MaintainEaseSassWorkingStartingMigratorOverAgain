using System.Collections.Generic;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Entities;

namespace MaintainEase.Core.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for tenant entities
    /// </summary>
    public interface ITenantRepository : IRepository<Tenant>
    {
        Task<IEnumerable<Tenant>> GetActiveTenantAsync();
        Task<bool> EmailExistsAsync(string email);
        Task<Tenant> GetByEmailAsync(string email);
    }
}
