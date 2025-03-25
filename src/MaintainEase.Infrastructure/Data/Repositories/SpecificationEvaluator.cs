using System.Linq;
using Microsoft.EntityFrameworkCore;
using MaintainEase.Core.Domain.Interfaces;

namespace MaintainEase.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Evaluator for specifications
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public static class SpecificationEvaluator<T> where T : class, IEntity
    {
        public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
        {
            var query = inputQuery;

            // Apply the filter expression
            if (specification.ToExpression() != null)
            {
                query = query.Where(specification.ToExpression());
            }

            return query;
        }
    }
}
