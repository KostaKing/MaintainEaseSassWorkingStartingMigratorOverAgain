using MaintainEase.Core.Domain.Entities;

namespace MaintainEase.Core.Domain.Events
{
    public class TenantCreatedEvent : DomainEvent
    {
        public Tenant Tenant { get; }

        public TenantCreatedEvent(Tenant tenant)
        {
            Tenant = tenant;
        }
    }

    public class TenantContactInformationUpdatedEvent : DomainEvent
    {
        public Tenant Tenant { get; }

        public TenantContactInformationUpdatedEvent(Tenant tenant)
        {
            Tenant = tenant;
        }
    }

    public class TenantDeactivatedEvent : DomainEvent
    {
        public Tenant Tenant { get; }

        public TenantDeactivatedEvent(Tenant tenant)
        {
            Tenant = tenant;
        }
    }

    public class TenantReactivatedEvent : DomainEvent
    {
        public Tenant Tenant { get; }

        public TenantReactivatedEvent(Tenant tenant)
        {
            Tenant = tenant;
        }
    }
}
