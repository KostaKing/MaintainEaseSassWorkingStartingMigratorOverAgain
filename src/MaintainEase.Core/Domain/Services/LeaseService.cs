using System;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.Services
{
    /// <summary>
    /// Domain service for lease-related operations
    /// </summary>
    public class LeaseService : IDomainService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILeaseRepository _leaseRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly ITenantRepository _tenantRepository;

        public LeaseService(
            IUnitOfWork unitOfWork,
            ILeaseRepository leaseRepository,
            IPropertyRepository propertyRepository,
            ITenantRepository tenantRepository)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _leaseRepository = leaseRepository ?? throw new ArgumentNullException(nameof(leaseRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        }

        public async Task<bool> CanCreateLeaseAsync(Guid unitId, Guid tenantId, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            // Check if unit exists and is available
            var unit = await _propertyRepository.GetUnitByIdAsync(unitId);
            if (unit == null || !unit.IsAvailableForRent || unit.IsOccupied)
                return false;

            // Check if tenant exists and is active
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null || !tenant.IsActive)
                return false;

            // Check for overlapping leases
            var existingLeases = await _leaseRepository.GetActiveLeasesByUnitIdAsync(unitId);
            foreach (var lease in existingLeases)
            {
                // If lease periods overlap
                if (startDate < lease.EndDate && endDate > lease.StartDate)
                    return false;
            }

            return true;
        }

        public async Task<Lease> CreateLeaseAsync(
            Guid unitId,
            Guid tenantId,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            Money monthlyRent,
            Money securityDeposit,
            int noticePeriodDays = 30)
        {
            if (!await CanCreateLeaseAsync(unitId, tenantId, startDate, endDate))
                throw new InvalidOperationException("Cannot create lease with the provided parameters");

            var lease = new Lease(
                unitId,
                new[] { tenantId },
                startDate,
                endDate,
                monthlyRent,
                securityDeposit,
                DateTimeOffset.UtcNow,
                noticePeriodDays);

            _leaseRepository.Add(lease);
            await _unitOfWork.SaveChangesAsync();

            // Mark the unit as occupied
            var unit = await _propertyRepository.GetUnitByIdAsync(unitId);
            unit.MarkAsOccupied();
            await _unitOfWork.SaveChangesAsync();

            return lease;
        }

        public async Task RenewLeaseAsync(Guid leaseId, DateTimeOffset newEndDate, Money newMonthlyRent = null)
        {
            var lease = await _leaseRepository.GetByIdAsync(leaseId);
            if (lease == null)
                throw new ArgumentException("Lease not found", nameof(leaseId));

            lease.Renew(newEndDate, newMonthlyRent);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task TerminateLeaseAsync(Guid leaseId, DateTimeOffset terminationDate, string reason)
        {
            var lease = await _leaseRepository.GetByIdAsync(leaseId);
            if (lease == null)
                throw new ArgumentException("Lease not found", nameof(leaseId));

            lease.Terminate(terminationDate, reason);
            await _unitOfWork.SaveChangesAsync();

            // Mark the unit as vacant
            var unit = await _propertyRepository.GetUnitByIdAsync(lease.UnitId);
            unit.MarkAsVacant();
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
