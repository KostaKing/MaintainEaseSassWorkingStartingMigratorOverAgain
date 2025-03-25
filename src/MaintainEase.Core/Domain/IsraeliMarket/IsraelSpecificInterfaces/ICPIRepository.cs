using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;

namespace MaintainEase.Core.Domain.IsraeliMarket.IsraelSpecificInterfaces
{
    /// <summary>
    /// Repository interface for Consumer Price Index (CPI) data in Israel
    /// </summary>
    public interface ICPIRepository : IRepository<CPIData>
    {
        /// <summary>
        /// Gets the CPI value for a specific year and month
        /// </summary>
        /// <param name="year">The year</param>
        /// <param name="month">The month (1-12)</param>
        /// <returns>The CPI value</returns>
        Task<decimal> GetCPIValueAsync(int year, int month);

        /// <summary>
        /// Gets the CPI change percentage between two dates
        /// </summary>
        /// <param name="baseYear">The base year</param>
        /// <param name="baseMonth">The base month (1-12)</param>
        /// <param name="targetYear">The target year</param>
        /// <param name="targetMonth">The target month (1-12)</param>
        /// <returns>The CPI change percentage</returns>
        Task<decimal> GetCPIChangePercentageAsync(
            int baseYear,
            int baseMonth,
            int targetYear,
            int targetMonth);
    }
}
