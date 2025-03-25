using System.Threading;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Events;

namespace MaintainEase.Core.Domain.Interfaces
{
    /// <summary>
    /// Handler for domain events
    /// </summary>
    /// <typeparam name="TEvent">Type of domain event to handle</typeparam>
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
