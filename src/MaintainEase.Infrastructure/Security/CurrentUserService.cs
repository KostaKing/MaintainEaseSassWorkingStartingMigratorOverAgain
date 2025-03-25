using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MaintainEase.Infrastructure.Security
{
    /// <summary>
    /// Implementation of current user service using HTTP context
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public Guid GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            
            if (user == null || !user.Identity.IsAuthenticated)
            {
                // For system operations or anonymous users
                return Guid.Parse("00000000-0000-0000-0000-000000000000");
            }
            
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            
            return Guid.Parse("00000000-0000-0000-0000-000000000000");
        }

        public string GetCurrentUserName()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            
            if (user == null || !user.Identity.IsAuthenticated)
            {
                return "System";
            }
            
            return user.Identity.Name ?? "Unknown";
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        public bool IsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }
    }
}
