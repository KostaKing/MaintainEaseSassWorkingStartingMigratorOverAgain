using System;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.IsraeliMarket.Services
{
    /// <summary>
    /// Service for handling protected tenancy rules specific to Israel
    /// </summary>
    public class ProtectedTenancyService : IDomainService
    {
        public bool IsEligibleForProtectedTenancy(IsraeliLease lease, int occupancyYears)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            // Basic criteria for protected tenancy eligibility
            // 1. Occupancy for more than 10 years
            // 2. Tenant has made significant investments in the property
            // 3. Lease specifically designates tenant as protected
            
            if (occupancyYears >= 10 || lease.IsProtectedTenant)
                return true;
                
            return false;
        }

        public Money CalculateMaximumRentIncrease(IsraeliLease lease, Money currentRent)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            if (currentRent == null)
                throw new ArgumentNullException(nameof(currentRent));

            // For protected tenants, rent increases are limited by law
            if (lease.IsProtectedTenant)
            {
                // Protected tenants are limited to 5% increase per year
                return new Money(currentRent.Amount * 1.05m, currentRent.Currency);
            }

            // No special limitations for non-protected tenants
            return null;
        }

        public int CalculateExtendedNoticePeriod(IsraeliLease lease)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            // Protected tenants get extended notice periods
            if (lease.IsProtectedTenant)
            {
                // Base notice period + additional 60 days for protected tenants
                return lease.NoticePeriodDays + 60;
            }

            return lease.NoticePeriodDays;
        }

        public bool CanEvictTenant(IsraeliLease lease, string reason)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be empty", nameof(reason));

            if (lease.IsProtectedTenant)
            {
                // Protected tenants can only be evicted for specific legal reasons
                // such as non-payment, illegal use, or owner's personal need
                switch (reason.ToLowerInvariant())
                {
                    case "non-payment":
                    case "illegal use":
                    case "property destruction":
                    case "owner occupation":
                        return true;
                    default:
                        return false;
                }
            }

            // Non-protected tenants can be evicted for any valid reason
            // as specified in their lease agreement
            return true;
        }
    }
}
