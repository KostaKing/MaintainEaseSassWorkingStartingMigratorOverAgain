using System;

namespace MaintainEase.Infrastructure.Data
{
    /// <summary>
    /// Interface for soft-delete entities
    /// </summary>
    public interface ISoftDeleteEntity
    {
        bool IsDeleted { get; set; }
        DateTimeOffset? DeletedAt { get; set; }
        Guid? DeletedBy { get; set; }
    }
}
