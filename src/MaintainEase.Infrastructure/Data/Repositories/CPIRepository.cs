using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;
using MaintainEase.Core.Domain.IsraeliMarket.IsraelSpecificInterfaces;
using MaintainEase.Infrastructure.Data.Context;

namespace MaintainEase.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository implementation for CPI data
    /// </summary>
    public class CPIRepository : BaseRepository<CPIData>, ICPIRepository
    {
        public CPIRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<decimal> GetCPIValueAsync(int year, int month)
        {
            var cpiData = await _dbSet
                .FirstOrDefaultAsync(c => c.Year == year && c.Month == month);
                
            if (cpiData == null)
                throw new InvalidOperationException($"CPI data not found for {month}/{year}");
                
            return cpiData.IndexValue;
        }

        public async Task<decimal> GetCPIChangePercentageAsync(int baseYear, int baseMonth, int targetYear, int targetMonth)
        {
            var baseValue = await GetCPIValueAsync(baseYear, baseMonth);
            var targetValue = await GetCPIValueAsync(targetYear, targetMonth);
            
            if (baseValue <= 0)
                throw new InvalidOperationException("Invalid base CPI value");
                
            // Calculate percentage change
            return (targetValue - baseValue) / baseValue * 100;
        }
    }
}
