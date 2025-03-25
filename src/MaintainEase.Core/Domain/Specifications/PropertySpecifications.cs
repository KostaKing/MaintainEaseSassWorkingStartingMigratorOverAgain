using System;
using System.Linq.Expressions;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Enums;

namespace MaintainEase.Core.Domain.Specifications
{
    public class ActivePropertySpecification : Specification<Property>
    {
        public override Expression<Func<Property, bool>> ToExpression()
        {
            return property => property.IsActive;
        }
    }

    public class PropertyTypeSpecification : Specification<Property>
    {
        private readonly PropertyType _propertyType;

        public PropertyTypeSpecification(PropertyType propertyType)
        {
            _propertyType = propertyType;
        }

        public override Expression<Func<Property, bool>> ToExpression()
        {
            return property => property.Type == _propertyType;
        }
    }

    public class PropertyWithAvailableUnitsSpecification : Specification<Property>
    {
        public override Expression<Func<Property, bool>> ToExpression()
        {
            return property => property.Units.Any(u => u.IsAvailableForRent && !u.IsOccupied);
        }
    }

    public class PropertyByAddressCitySpecification : Specification<Property>
    {
        private readonly string _city;

        public PropertyByAddressCitySpecification(string city)
        {
            _city = city;
        }

        public override Expression<Func<Property, bool>> ToExpression()
        {
            return property => property.Address.City == _city;
        }
    }
}
