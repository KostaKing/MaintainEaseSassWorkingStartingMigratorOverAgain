using System;
using System.Collections.Generic;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.IsraeliMarket.Entities
{
    /// <summary>
    /// Represents an Israeli lease with specific attributes for the Israeli market
    /// </summary>
    public class IsraeliLease : Lease
    {
        public bool IsCpiIndexed { get; private set; }
        public int IndexationMonthInterval { get; private set; }
        public decimal IndexationPercentage { get; private set; }
        public bool IsProtectedTenant { get; private set; }
        public bool RequiresBankGuarantee { get; private set; }
        public Money BankGuaranteeAmount { get; private set; }
        public bool IncludesArnonaPayment { get; private set; }
        public bool IncludesVaadBayitPayment { get; private set; }
        public string GuarantorIdNumber { get; private set; }

        // For EF Core
        protected IsraeliLease() { }

        public IsraeliLease(
            Guid unitId,
            IEnumerable<Guid> tenantIds,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            Money monthlyRent,
            Money securityDeposit,
            DateTimeOffset signedDate,
            int noticePeriodDays,
            bool isCpiIndexed,
            int indexationMonthInterval,
            decimal indexationPercentage,
            bool isProtectedTenant,
            bool requiresBankGuarantee,
            Money bankGuaranteeAmount,
            bool includesArnonaPayment,
            bool includesVaadBayitPayment,
            string guarantorIdNumber = null,
            bool isRenewable = true,
            string terminationConditions = null,
            string specialConditions = null)
            : base(
                unitId,
                tenantIds,
                startDate,
                endDate,
                monthlyRent,
                securityDeposit,
                signedDate,
                noticePeriodDays,
                isRenewable,
                terminationConditions,
                specialConditions)
        {
            if (isCpiIndexed && indexationMonthInterval <= 0)
                throw new ArgumentException("Indexation interval must be positive", nameof(indexationMonthInterval));

            if (isCpiIndexed && indexationPercentage <= 0)
                throw new ArgumentException("Indexation percentage must be positive", nameof(indexationPercentage));

            if (requiresBankGuarantee && bankGuaranteeAmount == null)
                throw new ArgumentException("Bank guarantee amount must be provided", nameof(bankGuaranteeAmount));

            IsCpiIndexed = isCpiIndexed;
            IndexationMonthInterval = indexationMonthInterval;
            IndexationPercentage = indexationPercentage;
            IsProtectedTenant = isProtectedTenant;
            RequiresBankGuarantee = requiresBankGuarantee;
            BankGuaranteeAmount = bankGuaranteeAmount;
            IncludesArnonaPayment = includesArnonaPayment;
            IncludesVaadBayitPayment = includesVaadBayitPayment;
            GuarantorIdNumber = guarantorIdNumber;
        }

        public void UpdateIndexationDetails(
            bool isCpiIndexed,
            int indexationMonthInterval,
            decimal indexationPercentage)
        {
            if (isCpiIndexed && indexationMonthInterval <= 0)
                throw new ArgumentException("Indexation interval must be positive", nameof(indexationMonthInterval));

            if (isCpiIndexed && indexationPercentage <= 0)
                throw new ArgumentException("Indexation percentage must be positive", nameof(indexationPercentage));

            IsCpiIndexed = isCpiIndexed;
            IndexationMonthInterval = indexationMonthInterval;
            IndexationPercentage = indexationPercentage;
        }

        public void UpdateGuaranteeDetails(
            bool requiresBankGuarantee,
            Money bankGuaranteeAmount,
            string guarantorIdNumber)
        {
            if (requiresBankGuarantee && bankGuaranteeAmount == null)
                throw new ArgumentException("Bank guarantee amount must be provided", nameof(bankGuaranteeAmount));

            RequiresBankGuarantee = requiresBankGuarantee;
            BankGuaranteeAmount = bankGuaranteeAmount;
            GuarantorIdNumber = guarantorIdNumber;
        }

        public void UpdateIncludedPayments(
            bool includesArnonaPayment,
            bool includesVaadBayitPayment)
        {
            IncludesArnonaPayment = includesArnonaPayment;
            IncludesVaadBayitPayment = includesVaadBayitPayment;
        }
    }
}
