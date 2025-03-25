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
    /// Repository implementation for Property entity
    /// </summary>
    public class PropertyRepository : BaseRepository<Property>, IPropertyRepository
    {
        public PropertyRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Property>> GetActivePropertiesAsync()
        {
            return await _dbSet.Where(p => p.IsActive).ToListAsync();
        }

        public async Task<IEnumerable<Unit>> GetUnitsByPropertyIdAsync(Guid propertyId)
        {
            var property = await _dbSet
                .Include(p => p.Units)
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            return property?.Units ?? new List<Unit>();
        }

        public async Task<Unit> GetUnitByIdAsync(Guid unitId)
        {
            return await _context.Units.FindAsync(unitId);
        }

        public async Task<bool> UnitNumberExistsInPropertyAsync(Guid propertyId, string unitNumber)
        {
            return await _context.Units
                .AnyAsync(u => u.PropertyId == propertyId && u.UnitNumber == unitNumber);
        }

        public async Task<IEnumerable<Property>> GetPropertiesByTypeAsync(PropertyType type)
        {
            return await _dbSet.Where(p => p.Type == type).ToListAsync();
        }
    }
}
