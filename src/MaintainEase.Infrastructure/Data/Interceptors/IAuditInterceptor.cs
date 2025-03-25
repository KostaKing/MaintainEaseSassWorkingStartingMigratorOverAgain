using Microsoft.EntityFrameworkCore;

namespace MaintainEase.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// Interface for audit information interceptor
    /// </summary>
    public interface IAuditInterceptor
    {
        void ApplyAuditInformation(DbContext context);
    }
}
