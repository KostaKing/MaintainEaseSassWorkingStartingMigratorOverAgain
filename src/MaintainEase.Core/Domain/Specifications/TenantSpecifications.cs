using System;
using System.Linq.Expressions;
using MaintainEase.Core.Domain.Entities;

namespace MaintainEase.Core.Domain.Specifications
{
    public class ActiveTenantSpecification : Specification<Tenant>
    {
        public override Expression<Func<Tenant, bool>> ToExpression()
        {
            return tenant => tenant.IsActive;
        }
    }

    public class TenantWithGoodCreditSpecification : Specification<Tenant>
    {
        private readonly int _minimumCreditScore;

        public TenantWithGoodCreditSpecification(int minimumCreditScore = 700)
        {
            _minimumCreditScore = minimumCreditScore;
        }

        public override Expression<Func<Tenant, bool>> ToExpression()
        {
            return tenant => tenant.CreditScore >= _minimumCreditScore;
        }
    }

    public class TenantWithValidIdSpecification : Specification<Tenant>
    {
        public override Expression<Func<Tenant, bool>> ToExpression()
        {
            return tenant => !tenant.IdDocument.IsExpired();
        }
    }
}
