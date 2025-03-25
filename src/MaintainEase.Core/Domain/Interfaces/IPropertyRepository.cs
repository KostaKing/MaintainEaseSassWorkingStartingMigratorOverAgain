using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Enums;

namespace MaintainEase.Core.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for property entities
    /// </summary>
    public interface IPropertyRepository : IRepository<Property>
    {
        Task<IEnumerable<Property>> GetActivePropertiesAsync();
        Task<IEnumerable<Unit>> GetUnitsByPropertyIdAsync(Guid propertyId);
        Task<Unit> GetUnitByIdAsync(Guid unitId);
        Task<bool> UnitNumberExistsInPropertyAsync(Guid propertyId, string unitNumber);
        Task<IEnumerable<Property>> GetPropertiesByTypeAsync(PropertyType type);
    }
}
