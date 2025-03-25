using System;

namespace MaintainEase.Infrastructure.Security
{
    /// <summary>
    /// Interface for current user service
    /// </summary>
    public interface ICurrentUserService
    {
        Guid GetCurrentUserId();
        string GetCurrentUserName();
        bool IsAuthenticated();
        bool IsInRole(string role);
    }
}
