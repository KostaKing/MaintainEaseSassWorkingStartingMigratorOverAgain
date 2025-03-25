using System;
using System.Collections.Generic;
using MaintainEase.Core.Domain.Aggregates;
using MaintainEase.Core.Domain.Events;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.Entities
{
    /// <summary>
    /// Represents a lease agreement between a tenant and property owner
    /// </summary>
    public class Lease : AggregateRoot
    {
        private readonly List<Guid> _tenantIds = new();

        public Guid UnitId { get; private set; }
        public IReadOnlyCollection<Guid> TenantIds => _tenantIds.AsReadOnly();
        public DateTimeOffset StartDate { get; private set; }
        public DateTimeOffset EndDate { get; private set; }
        public Money MonthlyRent { get; private set; }
        public Money SecurityDeposit { get; private set; }
        public bool IsActive { get; private set; }
        public LeaseStatus Status { get; private set; }
        public DateTimeOffset SignedDate { get; private set; }
        public int NoticePeriodDays { get; private set; }
        public bool IsRenewable { get; private set; }
        public string TerminationConditions { get; private set; }
        public string SpecialConditions { get; private set; }

        // For EF Core
        protected Lease() { }

        public Lease(
            Guid unitId,
            IEnumerable<Guid> tenantIds,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            Money monthlyRent,
            Money securityDeposit,
            DateTimeOffset signedDate,
            int noticePeriodDays = 30,
            bool isRenewable = true,
            string terminationConditions = null,
            string specialConditions = null)
        {
            if (startDate >= endDate)
                throw new ArgumentException("End date must be after start date", nameof(endDate));

            if (tenantIds == null)
                throw new ArgumentNullException(nameof(tenantIds));

            UnitId = unitId;
            _tenantIds.AddRange(tenantIds);
            StartDate = startDate;
            EndDate = endDate;
            MonthlyRent = monthlyRent ?? throw new ArgumentNullException(nameof(monthlyRent));
            SecurityDeposit = securityDeposit ?? throw new ArgumentNullException(nameof(securityDeposit));
            SignedDate = signedDate;
            NoticePeriodDays = noticePeriodDays;
            IsRenewable = isRenewable;
            TerminationConditions = terminationConditions;
            SpecialConditions = specialConditions;
            Status = LeaseStatus.Active;
            IsActive = true;

            AddDomainEvent(new LeaseCreatedEvent(this));
        }

        public void Renew(DateTimeOffset newEndDate, Money newMonthlyRent = null)
        {
            if (Status != LeaseStatus.Active)
                throw new InvalidOperationException("Only active leases can be renewed");

            if (newEndDate <= EndDate)
                throw new ArgumentException("New end date must be after current end date", nameof(newEndDate));

            var oldEndDate = EndDate;
            EndDate = newEndDate;

            if (newMonthlyRent != null)
            {
                MonthlyRent = newMonthlyRent;
            }

            AddDomainEvent(new LeaseRenewedEvent(this, oldEndDate));
        }

        public void Terminate(DateTimeOffset terminationDate, string reason)
        {
            if (Status != LeaseStatus.Active)
                throw new InvalidOperationException("Only active leases can be terminated");

            if (terminationDate < StartDate)
                throw new ArgumentException("Termination date cannot be before start date", nameof(terminationDate));

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Termination reason cannot be empty", nameof(reason));

            Status = LeaseStatus.Terminated;
            IsActive = false;
            EndDate = terminationDate;

            AddDomainEvent(new LeaseTerminatedEvent(this, reason));
        }

        public void UpdateRent(Money newMonthlyRent)
        {
            if (Status != LeaseStatus.Active)
                throw new InvalidOperationException("Cannot update rent for non-active lease");

            if (newMonthlyRent == null)
                throw new ArgumentNullException(nameof(newMonthlyRent));

            var oldRent = MonthlyRent;
            MonthlyRent = newMonthlyRent;

            AddDomainEvent(new LeaseRentUpdatedEvent(this, oldRent));
        }

        public void AddTenant(Guid tenantId)
        {
            if (_tenantIds.Contains(tenantId))
                return;

            _tenantIds.Add(tenantId);
            AddDomainEvent(new TenantAddedToLeaseEvent(this, tenantId));
        }

        public void RemoveTenant(Guid tenantId)
        {
            if (!_tenantIds.Contains(tenantId))
                return;

            _tenantIds.Remove(tenantId);
            AddDomainEvent(new TenantRemovedFromLeaseEvent(this, tenantId));
        }
    }
}
