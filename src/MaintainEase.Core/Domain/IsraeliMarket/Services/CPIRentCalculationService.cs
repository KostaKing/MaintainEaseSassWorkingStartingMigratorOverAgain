using System;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;
using MaintainEase.Core.Domain.IsraeliMarket.IsraelSpecificInterfaces;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.IsraeliMarket.Services
{
    /// <summary>
    /// Service for calculating rent adjustments based on CPI (מדד) in Israel
    /// </summary>
    public class CPIRentCalculationService : IDomainService
    {
        private readonly ICPIRepository _cpiRepository;

        public CPIRentCalculationService(ICPIRepository cpiRepository)
        {
            _cpiRepository = cpiRepository ?? throw new ArgumentNullException(nameof(cpiRepository));
        }

        public async Task<Money> CalculateIndexedRent(IsraeliLease lease, DateTimeOffset calculationDate)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            if (!lease.IsCpiIndexed)
                return lease.MonthlyRent;

            // Calculate months since lease start
            int monthsSinceStart = (calculationDate.Year - lease.StartDate.Year) * 12 +
                                  (calculationDate.Month - lease.StartDate.Month);

            // Check if indexation is due (based on interval)
            if (monthsSinceStart < lease.IndexationMonthInterval || monthsSinceStart % lease.IndexationMonthInterval != 0)
                return lease.MonthlyRent;

            // Get CPI values
            decimal baseIndexValue = await _cpiRepository.GetCPIValueAsync(lease.StartDate.Year, lease.StartDate.Month);
            decimal currentIndexValue = await _cpiRepository.GetCPIValueAsync(calculationDate.Year, calculationDate.Month);

            if (baseIndexValue <= 0)
                throw new InvalidOperationException("Invalid base CPI value");

            if (currentIndexValue <= 0)
                throw new InvalidOperationException("Invalid current CPI value");

            // Calculate CPI change percentage
            decimal cpiChangePercentage = (currentIndexValue - baseIndexValue) / baseIndexValue * 100;

            // Apply indexation cap if needed
            decimal appliedPercentage = Math.Min(cpiChangePercentage, lease.IndexationPercentage);

            // Calculate new rent
            decimal newRentAmount = lease.MonthlyRent.Amount * (1 + appliedPercentage / 100);

            return new Money(newRentAmount, lease.MonthlyRent.Currency);
        }

        public async Task<Money> CalculateIndexedRentWithHistory(IsraeliLease lease)
        {
            // This method would track a history of all indexations applied to the lease
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            if (!lease.IsCpiIndexed)
                return lease.MonthlyRent;

            // Current date for calculation
            var calculationDate = DateTimeOffset.UtcNow;
            
            // Initial rent amount
            decimal currentRentAmount = lease.MonthlyRent.Amount;
            
            // Start date for first period
            var periodStartDate = lease.StartDate;
            
            // Calculate all indexation periods
            while (true)
            {
                // Calculate next indexation date
                var nextIndexationDate = periodStartDate.AddMonths(lease.IndexationMonthInterval);
                
                // If next indexation is in the future, stop
                if (nextIndexationDate > calculationDate)
                    break;
                
                // Get CPI values for this period
                decimal periodStartIndexValue = await _cpiRepository.GetCPIValueAsync(
                    periodStartDate.Year, periodStartDate.Month);
                    
                decimal periodEndIndexValue = await _cpiRepository.GetCPIValueAsync(
                    nextIndexationDate.Year, nextIndexationDate.Month);
                
                if (periodStartIndexValue <= 0 || periodEndIndexValue <= 0)
                    throw new InvalidOperationException("Invalid CPI values");
                
                // Calculate CPI change percentage for this period
                decimal cpiChangePercentage = (periodEndIndexValue - periodStartIndexValue) / 
                                             periodStartIndexValue * 100;
                
                // Apply indexation cap if needed
                decimal appliedPercentage = Math.Min(cpiChangePercentage, lease.IndexationPercentage);
                
                // Update rent amount
                currentRentAmount *= (1 + appliedPercentage / 100);
                
                // Move to next period
                periodStartDate = nextIndexationDate;
            }
            
            return new Money(currentRentAmount, lease.MonthlyRent.Currency);
        }
    }
}
