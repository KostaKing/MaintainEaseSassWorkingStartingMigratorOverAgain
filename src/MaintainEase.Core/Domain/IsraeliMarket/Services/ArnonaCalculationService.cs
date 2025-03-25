using System;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;
using MaintainEase.Core.Domain.IsraeliMarket.ValueObjects;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.IsraeliMarket.Services
{
    /// <summary>
    /// Service for calculating Arnona (municipal tax) in Israel
    /// </summary>
    public class ArnonaCalculationService : IDomainService
    {
        public Money CalculateAnnualArnona(IsraeliProperty property, int taxYear)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (property.ArnonaZone == null)
                throw new InvalidOperationException("Property does not have an Arnona zone assigned");

            if (property.ArnonaZone.TaxYear != taxYear)
                throw new InvalidOperationException("Arnona zone is not valid for tax year " + taxYear);

            // Calculate based on property area and base rate from the zone
            decimal annualAmount = property.TotalArea * property.ArnonaZone.BaseRate;

            // Add adjustments based on property type
            switch (property.IsraeliPropertyType)
            {
                case IsraeliMarket.Enums.IsraeliPropertyType.Penthouse:
                    annualAmount *= 1.2m; // 20% premium for penthouses
                    break;
                case IsraeliMarket.Enums.IsraeliPropertyType.Villa:
                    annualAmount *= 1.3m; // 30% premium for villas
                    break;
                case IsraeliMarket.Enums.IsraeliPropertyType.GardenApartment:
                    annualAmount *= 1.1m; // 10% premium for garden apartments
                    break;
            }

            // Apply discounts based on property age
            int propertyAge = taxYear - property.YearBuilt;
            if (propertyAge > 25)
            {
                annualAmount *= 0.95m; // 5% discount for old properties
            }

            return new Money(annualAmount, "ILS");
        }

        public Money CalculateMonthlyArnona(IsraeliProperty property, int taxYear)
        {
            var annualArnona = CalculateAnnualArnona(property, taxYear);
            return new Money(annualArnona.Amount / 12, "ILS");
        }

        public Money CalculateArnonaWithDiscount(IsraeliProperty property, int taxYear, decimal discountPercentage)
        {
            if (discountPercentage < 0 || discountPercentage > 100)
                throw new ArgumentException("Discount percentage must be between 0 and 100", nameof(discountPercentage));

            var arnona = CalculateAnnualArnona(property, taxYear);
            var discountFactor = (100 - discountPercentage) / 100;
            return new Money(arnona.Amount * discountFactor, "ILS");
        }
    }
}
