using System;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.Events
{
    public class LeaseCreatedEvent : DomainEvent
    {
        public Lease Lease { get; }

        public LeaseCreatedEvent(Lease lease)
        {
            Lease = lease;
        }
    }

    public class LeaseRenewedEvent : DomainEvent
    {
        public Lease Lease { get; }
        public DateTimeOffset OldEndDate { get; }

        public LeaseRenewedEvent(Lease lease, DateTimeOffset oldEndDate)
        {
            Lease = lease;
            OldEndDate = oldEndDate;
        }
    }

    public class LeaseTerminatedEvent : DomainEvent
    {
        public Lease Lease { get; }
        public string Reason { get; }

        public LeaseTerminatedEvent(Lease lease, string reason)
        {
            Lease = lease;
            Reason = reason;
        }
    }

    public class LeaseRentUpdatedEvent : DomainEvent
    {
        public Lease Lease { get; }
        public Money OldRent { get; }

        public LeaseRentUpdatedEvent(Lease lease, Money oldRent)
        {
            Lease = lease;
            OldRent = oldRent;
        }
    }

    public class TenantAddedToLeaseEvent : DomainEvent
    {
        public Lease Lease { get; }
        public Guid TenantId { get; }

        public TenantAddedToLeaseEvent(Lease lease, Guid tenantId)
        {
            Lease = lease;
            TenantId = tenantId;
        }
    }

    public class TenantRemovedFromLeaseEvent : DomainEvent
    {
        public Lease Lease { get; }
        public Guid TenantId { get; }

        public TenantRemovedFromLeaseEvent(Lease lease, Guid tenantId)
        {
            Lease = lease;
            TenantId = tenantId;
        }
    }
}
