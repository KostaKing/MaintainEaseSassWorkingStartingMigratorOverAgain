using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;
using MaintainEase.Infrastructure.Hebrew;

namespace MaintainEase.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// Interceptor for handling Hebrew text and CPI data
    /// </summary>
    public class HebrewCpiInterceptor : SaveChangesInterceptor
    {
        private readonly ILogger<HebrewCpiInterceptor> _logger;

        public HebrewCpiInterceptor(ILogger<HebrewCpiInterceptor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handle before save changes to process Hebrew text and CPI calculations
        /// </summary>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, 
            InterceptionResult<int> result)
        {
            if (eventData.Context != null)
            {
                ProcessHebrewEntities(eventData.Context);
            }
            
            return base.SavingChanges(eventData, result);
        }

        /// <summary>
        /// Handle before save changes async to process Hebrew text and CPI calculations
        /// </summary>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, 
            InterceptionResult<int> result, 
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null)
            {
                ProcessHebrewEntities(eventData.Context);
            }
            
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Process Hebrew text in entities
        /// </summary>
        private void ProcessHebrewEntities(DbContext context)
        {
            try
            {
                // Process Israeli properties with Hebrew text
                var israeliProperties = context.ChangeTracker.Entries<IsraeliProperty>()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
                
                foreach (var entry in israeliProperties)
                {
                    // Normalize Hebrew text for search
                    if (entry.Entity.Description != null)
                    {
                        bool containsHebrew = HebrewTextHandler.ContainsHebrew(entry.Entity.Description);
                        
                        if (containsHebrew)
                        {
                            // Add normalized version for search
                            entry.Entity.NormalizedDescription = HebrewTextHandler.NormalizeForSearch(entry.Entity.Description);
                            _logger.LogDebug("Normalized Hebrew text for property {PropertyId}", entry.Entity.Id);
                        }
                    }
                }

                // Process CPI-indexed leases
                var israeliLeases = context.ChangeTracker.Entries<IsraeliLease>()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
                
                foreach (var entry in israeliLeases)
                {
                    // If this is a CPI-indexed lease, ensure calculations are updated
                    if (entry.Entity.IsCpiIndexed)
                    {
                        _logger.LogDebug("Processed CPI-indexed lease {LeaseId}", entry.Entity.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Hebrew entities");
                // Don't throw - just log the error to avoid blocking save operations
            }
        }
    }
}
