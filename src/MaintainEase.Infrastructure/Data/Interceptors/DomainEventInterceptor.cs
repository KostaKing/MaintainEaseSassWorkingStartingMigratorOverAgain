using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MaintainEase.Core.Domain.Aggregates;
using MaintainEase.Core.Domain.Events;
using MaintainEase.Core.Domain.Interfaces;

namespace MaintainEase.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// Interceptor for handling domain events
    /// </summary>
    public class DomainEventInterceptor : IDomainEventInterceptor
    {
        private readonly IDomainEventDispatcher _domainEventDispatcher;

        public DomainEventInterceptor(IDomainEventDispatcher domainEventDispatcher)
        {
            _domainEventDispatcher = domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
        }

        public List<IDomainEvent> CaptureEvents(DbContext context)
        {
            var events = new List<IDomainEvent>();

            // Get all aggregate roots that have domain events
            var aggregateRoots = context.ChangeTracker.Entries<AggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            // Collect events and clear them from the entities
            foreach (var aggregateRoot in aggregateRoots)
            {
                events.AddRange(aggregateRoot.DomainEvents);
                aggregateRoot.ClearDomainEvents();
            }

            return events;
        }

        public async Task DispatchEventsAsync(List<IDomainEvent> events, CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in events)
            {
                await _domainEventDispatcher.DispatchAsync(domainEvent, cancellationToken);
            }
        }

        public void DispatchEvents(List<IDomainEvent> events)
        {
            // Dispatch synchronously (should be used sparingly)
            foreach (var domainEvent in events)
            {
                Task.Run(async () => await _domainEventDispatcher.DispatchAsync(domainEvent)).GetAwaiter().GetResult();
            }
        }
    }
}
