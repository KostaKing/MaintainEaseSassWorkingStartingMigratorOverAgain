using System;

namespace MaintainEase.Core.Domain.Events
{
    /// <summary>
    /// Base class for all domain events
    /// </summary>
    public abstract class DomainEvent : IDomainEvent
    {
        public Guid Id { get; }
        public DateTimeOffset OccurredOn { get; }

        protected DomainEvent()
        {
            Id = Guid.NewGuid();
            OccurredOn = DateTimeOffset.UtcNow;
        }
    }
}
