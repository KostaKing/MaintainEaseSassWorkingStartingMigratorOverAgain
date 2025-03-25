using System;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.Services
{
    /// <summary>
    /// Domain service for tenant-related operations
    /// </summary>
    public class TenantService : IDomainService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantRepository _tenantRepository;

        public TenantService(
            IUnitOfWork unitOfWork,
            ITenantRepository tenantRepository)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        }

        public async Task<Tenant> CreateTenantAsync(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            Identification idDocument,
            Address permanentAddress,
            int creditScore = 0,
            string emergencyContactName = null,
            string emergencyContactPhone = null)
        {
            // Check for duplicate email
            if (await _tenantRepository.EmailExistsAsync(email))
                throw new InvalidOperationException($"Email '{email}' is already registered");

            var tenant = new Tenant(
                firstName,
                lastName,
                email,
                phoneNumber,
                idDocument,
                permanentAddress,
                creditScore,
                emergencyContactName,
                emergencyContactPhone);

            _tenantRepository.Add(tenant);
            await _unitOfWork.SaveChangesAsync();

            return tenant;
        }

        public async Task UpdateTenantContactInfoAsync(
            Guid tenantId,
            string email,
            string phoneNumber,
            Address permanentAddress)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null)
                throw new ArgumentException("Tenant not found", nameof(tenantId));

            // If email is changing, check for duplicates
            if (tenant.Email != email && await _tenantRepository.EmailExistsAsync(email))
                throw new InvalidOperationException($"Email '{email}' is already registered");

            tenant.UpdateContactInformation(email, phoneNumber, permanentAddress);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeactivateTenantAsync(Guid tenantId)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null)
                throw new ArgumentException("Tenant not found", nameof(tenantId));

            tenant.Deactivate();
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ReactivateTenantAsync(Guid tenantId)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null)
                throw new ArgumentException("Tenant not found", nameof(tenantId));

            tenant.Reactivate();
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
