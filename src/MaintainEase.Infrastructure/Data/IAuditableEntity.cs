using System;

namespace MaintainEase.Infrastructure.Data
{
    /// <summary>
    /// Interface for auditable entities
    /// </summary>
    public interface IAuditableEntity
    {
        Guid CreatedBy { get; set; }
        DateTimeOffset CreatedAt { get; set; }
        Guid? LastModifiedBy { get; set; }
        DateTimeOffset? LastModifiedAt { get; set; }
    }
}
