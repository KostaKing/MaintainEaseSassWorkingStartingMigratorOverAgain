using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MaintainEase.Core.Domain.Events;

namespace MaintainEase.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// Interface for domain event interceptor
    /// </summary>
    public interface IDomainEventInterceptor
    {
        List<IDomainEvent> CaptureEvents(DbContext context);
        Task DispatchEventsAsync(List<IDomainEvent> events, CancellationToken cancellationToken = default);
        void DispatchEvents(List<IDomainEvent> events);
    }
}
