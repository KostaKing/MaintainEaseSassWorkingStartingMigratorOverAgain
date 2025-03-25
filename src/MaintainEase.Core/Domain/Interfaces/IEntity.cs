using System;

namespace MaintainEase.Core.Domain.Interfaces
{
    /// <summary>
    /// Base interface for all domain entities
    /// </summary>
    public interface IEntity
    {
        Guid Id { get; }
    }
}
