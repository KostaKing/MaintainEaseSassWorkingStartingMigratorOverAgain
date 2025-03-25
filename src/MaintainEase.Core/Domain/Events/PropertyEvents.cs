using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.Events
{
    public class PropertyCreatedEvent : DomainEvent
    {
        public Property Property { get; }

        public PropertyCreatedEvent(Property property)
        {
            Property = property;
        }
    }

    public class PropertyValueUpdatedEvent : DomainEvent
    {
        public Property Property { get; }

        public PropertyValueUpdatedEvent(Property property)
        {
            Property = property;
        }
    }

    public class SignificantPropertyValueChangeEvent : DomainEvent
    {
        public Property Property { get; }
        public Money OldValue { get; }
        public Money NewValue { get; }
        public decimal ChangePercentage { get; }

        public SignificantPropertyValueChangeEvent(
            Property property,
            Money oldValue,
            Money newValue,
            decimal changePercentage)
        {
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
            ChangePercentage = changePercentage;
        }
    }

    public class PropertyDeactivatedEvent : DomainEvent
    {
        public Property Property { get; }

        public PropertyDeactivatedEvent(Property property)
        {
            Property = property;
        }
    }

    public class PropertyReactivatedEvent : DomainEvent
    {
        public Property Property { get; }

        public PropertyReactivatedEvent(Property property)
        {
            Property = property;
        }
    }

    public class UnitAddedToPropertyEvent : DomainEvent
    {
        public Property Property { get; }
        public Unit Unit { get; }

        public UnitAddedToPropertyEvent(Property property, Unit unit)
        {
            Property = property;
            Unit = unit;
        }
    }

    public class UnitRemovedFromPropertyEvent : DomainEvent
    {
        public Property Property { get; }
        public Unit Unit { get; }

        public UnitRemovedFromPropertyEvent(Property property, Unit unit)
        {
            Property = property;
            Unit = unit;
        }
    }
}
