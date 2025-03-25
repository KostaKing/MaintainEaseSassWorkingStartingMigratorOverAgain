using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MaintainEase.Infrastructure.MultiTenancy;
using MaintainEase.Infrastructure.Security;

namespace MaintainEase.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// Interceptor for applying audit information to entities
    /// </summary>
    public class AuditInterceptor : IAuditInterceptor
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITenantProvider _tenantProvider;

        public AuditInterceptor(
            ICurrentUserService currentUserService,
            ITenantProvider tenantProvider)
        {
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        }

        public void ApplyAuditInformation(DbContext context)
        {
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            var userId = _currentUserService.GetCurrentUserId();
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var timestamp = DateTimeOffset.UtcNow;

            foreach (var entry in entries)
            {
                ApplyAuditProperties(entry, userId, timestamp);
                ApplyTenantProperties(entry, tenantId);
                ApplySoftDeleteProperties(entry);
            }
        }

        private void ApplyAuditProperties(EntityEntry entry, Guid userId, DateTimeOffset timestamp)
        {
            // For IAuditableEntity entities
            if (entry.Entity is IAuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    auditableEntity.CreatedBy = userId;
                    auditableEntity.CreatedAt = timestamp;
                }

                auditableEntity.LastModifiedBy = userId;
                auditableEntity.LastModifiedAt = timestamp;
            }
        }

        private void ApplyTenantProperties(EntityEntry entry, Guid tenantId)
        {
            // For ITenantEntity entities
            if (entry.State == EntityState.Added && entry.Entity is ITenantEntity tenantEntity)
            {
                tenantEntity.TenantId = tenantId;
            }
        }

        private void ApplySoftDeleteProperties(EntityEntry entry)
        {
            // For ISoftDeleteEntity entities
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeleteEntity softDeleteEntity)
            {
                // Convert delete operation to update
                entry.State = EntityState.Modified;
                softDeleteEntity.IsDeleted = true;
                softDeleteEntity.DeletedAt = DateTimeOffset.UtcNow;
                softDeleteEntity.DeletedBy = _currentUserService.GetCurrentUserId();
            }
        }
    }
}
