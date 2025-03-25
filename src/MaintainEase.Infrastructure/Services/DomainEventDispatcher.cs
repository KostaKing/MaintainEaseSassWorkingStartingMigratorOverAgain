using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MaintainEase.Core.Domain.Events;
using MaintainEase.Core.Domain.Interfaces;

namespace MaintainEase.Infrastructure.Services
{
    /// <summary>
    /// Implementation of domain event dispatcher
    /// </summary>
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public DomainEventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            
            using (var scope = _serviceProvider.CreateScope())
            {
                var handlers = scope.ServiceProvider.GetServices(handlerType);
                
                var tasks = new List<Task>();
                foreach (var handler in handlers)
                {
                    var method = handlerType.GetMethod("HandleAsync");
                    var task = (Task)method.Invoke(handler, new object[] { domainEvent, cancellationToken });
                    tasks.Add(task);
                }
                
                await Task.WhenAll(tasks);
            }
        }
    }
}
