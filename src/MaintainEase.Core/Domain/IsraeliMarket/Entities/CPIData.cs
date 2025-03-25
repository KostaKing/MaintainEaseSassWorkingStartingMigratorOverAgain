using System;
using MaintainEase.Core.Domain.Entities;

namespace MaintainEase.Core.Domain.IsraeliMarket.Entities
{
    /// <summary>
    /// Represents Consumer Price Index (CPI) data for a specific month
    /// </summary>
    public class CPIData : Entity
    {
        public int Year { get; private set; }
        public int Month { get; private set; }
        public decimal IndexValue { get; private set; }
        public decimal MonthlyChangePercentage { get; private set; }
        public decimal YearlyChangePercentage { get; private set; }
        public DateTimeOffset PublicationDate { get; private set; }

        // For EF Core
        protected CPIData() { }

        public CPIData(
            int year,
            int month,
            decimal indexValue,
            decimal monthlyChangePercentage,
            decimal yearlyChangePercentage,
            DateTimeOffset publicationDate)
        {
            if (year < 1950 || year > 2100)
                throw new ArgumentException("Year must be valid", nameof(year));

            if (month < 1 || month > 12)
                throw new ArgumentException("Month must be between 1 and 12", nameof(month));

            if (indexValue <= 0)
                throw new ArgumentException("Index value must be positive", nameof(indexValue));

            Year = year;
            Month = month;
            IndexValue = indexValue;
            MonthlyChangePercentage = monthlyChangePercentage;
            YearlyChangePercentage = yearlyChangePercentage;
            PublicationDate = publicationDate;
        }
    }
}
