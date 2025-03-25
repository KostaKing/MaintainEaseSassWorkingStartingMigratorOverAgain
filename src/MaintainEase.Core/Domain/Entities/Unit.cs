using System;
using MaintainEase.Core.Domain.Events;

namespace MaintainEase.Core.Domain.Entities
{
    /// <summary>
    /// Represents a unit within a property (apartment, office, etc.)
    /// </summary>
    public class Unit : Entity
    {
        public string UnitNumber { get; private set; }
        public Guid PropertyId { get; private set; }
        public decimal Area { get; private set; }
        public int NumberOfBedrooms { get; private set; }
        public int NumberOfBathrooms { get; private set; }
        public string Description { get; private set; }
        public bool IsOccupied { get; private set; }
        public bool IsAvailableForRent { get; private set; }

        // For EF Core
        protected Unit() { }

        public Unit(
            string unitNumber,
            Guid propertyId,
            decimal area,
            int numberOfBedrooms,
            int numberOfBathrooms,
            string description)
        {
            if (string.IsNullOrWhiteSpace(unitNumber))
                throw new ArgumentException("Unit number cannot be empty", nameof(unitNumber));

            if (area <= 0)
                throw new ArgumentException("Area must be positive", nameof(area));

            if (numberOfBedrooms < 0)
                throw new ArgumentException("Number of bedrooms cannot be negative", nameof(numberOfBedrooms));

            if (numberOfBathrooms < 0)
                throw new ArgumentException("Number of bathrooms cannot be negative", nameof(numberOfBathrooms));

            UnitNumber = unitNumber;
            PropertyId = propertyId;
            Area = area;
            NumberOfBedrooms = numberOfBedrooms;
            NumberOfBathrooms = numberOfBathrooms;
            Description = description;
            IsAvailableForRent = true;
            IsOccupied = false;
        }

        public void MarkAsOccupied()
        {
            if (IsOccupied)
                return;

            IsOccupied = true;
            IsAvailableForRent = false;
        }

        public void MarkAsVacant()
        {
            if (!IsOccupied)
                return;

            IsOccupied = false;
            IsAvailableForRent = true;
        }

        public void SetAvailableForRent(bool isAvailable)
        {
            if (IsOccupied && isAvailable)
                throw new InvalidOperationException("Cannot mark an occupied unit as available for rent");

            IsAvailableForRent = isAvailable;
        }

        public void UpdateDetails(
            decimal area,
            int numberOfBedrooms,
            int numberOfBathrooms,
            string description)
        {
            if (area <= 0)
                throw new ArgumentException("Area must be positive", nameof(area));

            if (numberOfBedrooms < 0)
                throw new ArgumentException("Number of bedrooms cannot be negative", nameof(numberOfBedrooms));

            if (numberOfBathrooms < 0)
                throw new ArgumentException("Number of bathrooms cannot be negative", nameof(numberOfBathrooms));

            Area = area;
            NumberOfBedrooms = numberOfBedrooms;
            NumberOfBathrooms = numberOfBathrooms;
            Description = description;
        }
    }
}
