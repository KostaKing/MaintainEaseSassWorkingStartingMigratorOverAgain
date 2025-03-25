using System;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.Services
{
    /// <summary>
    /// Domain service for property-related operations
    /// </summary>
    public class PropertyService : IDomainService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPropertyRepository _propertyRepository;

        public PropertyService(
            IUnitOfWork unitOfWork,
            IPropertyRepository propertyRepository)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
        }

        public async Task<Property> CreatePropertyAsync(
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
            var property = new Property(
                name,
                address,
                type,
                acquisitionDate,
                purchasePrice,
                currentValue,
                yearBuilt,
                totalArea,
                legalDescription,
                taxIdentifier);

            _propertyRepository.Add(property);
            await _unitOfWork.SaveChangesAsync();

            return property;
        }

        public async Task<Unit> AddUnitToPropertyAsync(
            Guid propertyId,
            string unitNumber,
            decimal area,
            int numberOfBedrooms,
            int numberOfBathrooms,
            string description)
        {
            var property = await _propertyRepository.GetByIdAsync(propertyId);
            if (property == null)
                throw new ArgumentException("Property not found", nameof(propertyId));

            // Check for duplicate unit numbers
            if (await _propertyRepository.UnitNumberExistsInPropertyAsync(propertyId, unitNumber))
                throw new InvalidOperationException($"Unit number '{unitNumber}' already exists in this property");

            var unit = property.AddUnit(
                unitNumber,
                area,
                numberOfBedrooms,
                numberOfBathrooms,
                description);

            await _unitOfWork.SaveChangesAsync();

            return unit;
        }

        public async Task UpdatePropertyValueAsync(Guid propertyId, Money newValue)
        {
            var property = await _propertyRepository.GetByIdAsync(propertyId);
            if (property == null)
                throw new ArgumentException("Property not found", nameof(propertyId));

            property.UpdateCurrentValue(newValue);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
