using System;

namespace MaintainEase.Core.Domain.Events
{
    /// <summary>
    /// Interface for all domain events
    /// </summary>
    public interface IDomainEvent
    {
        Guid Id { get; }
        DateTimeOffset OccurredOn { get; }
    }
}
