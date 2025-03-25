using System;
using System.Collections.Generic;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.IsraeliMarket.ValueObjects
{
    /// <summary>
    /// Represents a Tabu (land registry) extract in Israel
    /// </summary>
    public class TabuExtract : ValueObject
    {
        public string BlockNumber { get; private set; }
        public string ParcelNumber { get; private set; }
        public string SubParcelNumber { get; private set; }
        public string PropertyRightsType { get; private set; }
        public DateTimeOffset ExtractionDate { get; private set; }
        public string RegistryOffice { get; private set; }
        public bool HasEncumbrances { get; private set; }
        public string EncumbrancesDescription { get; private set; }

        // For EF Core
        private TabuExtract() { }

        public TabuExtract(
            string blockNumber,
            string parcelNumber,
            string subParcelNumber,
            string propertyRightsType,
            DateTimeOffset extractionDate,
            string registryOffice,
            bool hasEncumbrances = false,
            string encumbrancesDescription = null)
        {
            if (string.IsNullOrWhiteSpace(blockNumber))
                throw new ArgumentException("Block number cannot be empty", nameof(blockNumber));

            if (string.IsNullOrWhiteSpace(parcelNumber))
                throw new ArgumentException("Parcel number cannot be empty", nameof(parcelNumber));

            if (string.IsNullOrWhiteSpace(propertyRightsType))
                throw new ArgumentException("Property rights type cannot be empty", nameof(propertyRightsType));

            if (string.IsNullOrWhiteSpace(registryOffice))
                throw new ArgumentException("Registry office cannot be empty", nameof(registryOffice));

            BlockNumber = blockNumber;
            ParcelNumber = parcelNumber;
            SubParcelNumber = subParcelNumber;
            PropertyRightsType = propertyRightsType;
            ExtractionDate = extractionDate;
            RegistryOffice = registryOffice;
            HasEncumbrances = hasEncumbrances;
            EncumbrancesDescription = encumbrancesDescription;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return BlockNumber;
            yield return ParcelNumber;
            yield return SubParcelNumber;
            yield return PropertyRightsType;
            yield return ExtractionDate;
            yield return RegistryOffice;
            yield return HasEncumbrances;
            yield return EncumbrancesDescription;
        }

        public override string ToString()
        {
            return $"Block {BlockNumber}, Parcel {ParcelNumber}, Sub-Parcel {SubParcelNumber}";
        }
    }
}
