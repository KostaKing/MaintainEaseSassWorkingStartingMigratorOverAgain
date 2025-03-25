using System.Threading;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Events;

namespace MaintainEase.Core.Domain.Interfaces
{
    /// <summary>
    /// Dispatcher for domain events
    /// </summary>
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
