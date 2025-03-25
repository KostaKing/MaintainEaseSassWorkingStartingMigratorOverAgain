using System;
using System.Collections.Generic;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.IsraeliMarket.ValueObjects
{
    /// <summary>
    /// Represents an Arnona (municipal tax) zone in Israel
    /// </summary>
    public class ArnonaZone : ValueObject
    {
        public string ZoneCode { get; private set; }
        public string Municipality { get; private set; }
        public string ZoneName { get; private set; }
        public decimal BaseRate { get; private set; }
        public int TaxYear { get; private set; }

        // For EF Core
        private ArnonaZone() { }

        public ArnonaZone(
            string zoneCode,
            string municipality,
            string zoneName,
            decimal baseRate,
            int taxYear)
        {
            if (string.IsNullOrWhiteSpace(zoneCode))
                throw new ArgumentException("Zone code cannot be empty", nameof(zoneCode));

            if (string.IsNullOrWhiteSpace(municipality))
                throw new ArgumentException("Municipality cannot be empty", nameof(municipality));

            if (baseRate <= 0)
                throw new ArgumentException("Base rate must be positive", nameof(baseRate));

            if (taxYear < 2000 || taxYear > 2100)
                throw new ArgumentException("Tax year must be valid", nameof(taxYear));

            ZoneCode = zoneCode;
            Municipality = municipality;
            ZoneName = zoneName;
            BaseRate = baseRate;
            TaxYear = taxYear;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return ZoneCode;
            yield return Municipality;
            yield return TaxYear;
        }

        public override string ToString()
        {
            return $"{Municipality} - {ZoneName} ({ZoneCode})";
        }
    }
}
