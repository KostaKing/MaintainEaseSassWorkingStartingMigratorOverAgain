using System.Collections.Generic;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Events;
using MaintainEase.Core.Domain.Interfaces;

namespace MaintainEase.Core.Domain.Aggregates
{
    /// <summary>
    /// Base class for all aggregate roots
    /// </summary>
    public abstract class AggregateRoot : Entity, IAggregateRoot
    {
        private readonly List<IDomainEvent> _domainEvents = new();
        
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
