using System;
using System.Collections.Generic;
using MaintainEase.Core.Domain.Aggregates;
using MaintainEase.Core.Domain.Enums;
using MaintainEase.Core.Domain.Events;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.Entities
{
    /// <summary>
    /// Represents a real estate property 
    /// </summary>
    public class Property : AggregateRoot
    {
        private readonly List<Unit> _units = new();
        
        public string Name { get; private set; }
        public Address Address { get; private set; }
        public PropertyType Type { get; private set; }
        public DateTimeOffset AcquisitionDate { get; private set; }
        public Money PurchasePrice { get; private set; }
        public Money CurrentValue { get; private set; }
        public int YearBuilt { get; private set; }
        public decimal TotalArea { get; private set; }
        public string LegalDescription { get; private set; }
        public string TaxIdentifier { get; private set; }
        public bool IsActive { get; private set; }
        public IReadOnlyCollection<Unit> Units => _units.AsReadOnly();

        // For EF Core
        protected Property() { }

        public Property(
            string name,
            Address address,
            PropertyType type,
            DateTimeOffset acquisitionDate,
            Money purchasePrice,
            Money currentValue,
            int yearBuilt,
            decimal totalArea,
            string legalDescription,
            string taxIdentifier)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Property name cannot be empty", nameof(name));

            if (totalArea <= 0)
                throw new ArgumentException("Total area must be positive", nameof(totalArea));

            if (yearBuilt <= 0)
                throw new ArgumentException("Year built must be valid", nameof(yearBuilt));

            Name = name;
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Type = type;
            AcquisitionDate = acquisitionDate;
            PurchasePrice = purchasePrice ?? throw new ArgumentNullException(nameof(purchasePrice));
            CurrentValue = currentValue ?? throw new ArgumentNullException(nameof(currentValue));
            YearBuilt = yearBuilt;
            TotalArea = totalArea;
            LegalDescription = legalDescription;
            TaxIdentifier = taxIdentifier;
            IsActive = true;

            AddDomainEvent(new PropertyCreatedEvent(this));
        }

        public void UpdateCurrentValue(Money newValue)
        {
            if (newValue == null)
                throw new ArgumentNullException(nameof(newValue));

            // Optional business logic: validate price change percentage
            var changePercentage = ((decimal)newValue.Amount - (decimal)CurrentValue.Amount) / (decimal)CurrentValue.Amount * 100;
            if (Math.Abs(changePercentage) > 20)
            {
                // For significant changes, we might want to require additional verification
                // This could involve adding a state to track if the change needs approval
                AddDomainEvent(new SignificantPropertyValueChangeEvent(this, CurrentValue, newValue, changePercentage));
            }

            CurrentValue = newValue;
            AddDomainEvent(new PropertyValueUpdatedEvent(this));
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            AddDomainEvent(new PropertyDeactivatedEvent(this));
        }

        public void Reactivate()
        {
            if (IsActive)
                return;

            IsActive = true;
            AddDomainEvent(new PropertyReactivatedEvent(this));
        }

        public Unit AddUnit(
            string unitNumber,
            decimal area,
            int numberOfBedrooms,
            int numberOfBathrooms,
            string description)
        {
            var unit = new Unit(
                unitNumber,
                this.Id,
                area,
                numberOfBedrooms,
                numberOfBathrooms,
                description);

            _units.Add(unit);
            AddDomainEvent(new UnitAddedToPropertyEvent(this, unit));
            return unit;
        }

        public void RemoveUnit(Unit unit)
        {
            if (unit == null)
                throw new ArgumentNullException(nameof(unit));

            if (_units.Remove(unit))
                AddDomainEvent(new UnitRemovedFromPropertyEvent(this, unit));
        }
    }
}
