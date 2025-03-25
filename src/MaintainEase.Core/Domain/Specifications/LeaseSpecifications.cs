using System;
using System.Linq.Expressions;
using MaintainEase.Core.Domain.Entities;

namespace MaintainEase.Core.Domain.Specifications
{
    public class ActiveLeaseSpecification : Specification<Lease>
    {
        public override Expression<Func<Lease, bool>> ToExpression()
        {
            return lease => lease.IsActive && lease.Status == LeaseStatus.Active;
        }
    }

    public class ExpiringLeaseSpecification : Specification<Lease>
    {
        private readonly int _daysToExpiration;

        public ExpiringLeaseSpecification(int daysToExpiration)
        {
            _daysToExpiration = daysToExpiration;
        }

        public override Expression<Func<Lease, bool>> ToExpression()
        {
            var expirationThreshold = DateTimeOffset.UtcNow.AddDays(_daysToExpiration);
            return lease => lease.IsActive && 
                           lease.Status == LeaseStatus.Active && 
                           lease.EndDate <= expirationThreshold;
        }
    }

    public class LeaseByTenantSpecification : Specification<Lease>
    {
        private readonly Guid _tenantId;

        public LeaseByTenantSpecification(Guid tenantId)
        {
            _tenantId = tenantId;
        }

        public override Expression<Func<Lease, bool>> ToExpression()
        {
            return lease => lease.TenantIds.Contains(_tenantId);
        }
    }

    public class LeaseByUnitSpecification : Specification<Lease>
    {
        private readonly Guid _unitId;

        public LeaseByUnitSpecification(Guid unitId)
        {
            _unitId = unitId;
        }

        public override Expression<Func<Lease, bool>> ToExpression()
        {
            return lease => lease.UnitId == _unitId;
        }
    }
}
