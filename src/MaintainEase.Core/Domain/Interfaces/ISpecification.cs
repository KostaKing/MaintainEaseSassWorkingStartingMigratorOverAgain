using System;
using System.Linq.Expressions;

namespace MaintainEase.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for the specification pattern
    /// </summary>
    /// <typeparam name="T">Type to be checked against the specification</typeparam>
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>> ToExpression();
        bool IsSatisfiedBy(T entity);
    }
}
