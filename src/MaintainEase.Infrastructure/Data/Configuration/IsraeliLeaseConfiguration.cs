using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;

namespace MaintainEase.Infrastructure.Data.Configuration
{
    /// <summary>
    /// Configuration for Israeli Lease entity
    /// </summary>
    public class IsraeliLeaseConfiguration : IEntityTypeConfiguration<IsraeliLease>
    {
        public void Configure(EntityTypeBuilder<IsraeliLease> builder)
        {
            builder.ToTable("IsraeliLeases");
            
            // This is a table-per-hierarchy mapping, so we configure additional
            // fields specific to IsraeliLease here
            
            builder.Property(l => l.IsCpiIndexed)
                .HasDefaultValue(false);
                
            builder.Property(l => l.IndexationMonthInterval)
                .HasDefaultValue(12);
                
            builder.Property(l => l.IndexationPercentage)
                .HasPrecision(5, 2)
                .HasDefaultValue(100);
                
            builder.Property(l => l.IsProtectedTenant)
                .HasDefaultValue(false);
                
            builder.Property(l => l.RequiresBankGuarantee)
                .HasDefaultValue(false);
                
            builder.Property(l => l.IncludesArnonaPayment)
                .HasDefaultValue(false);
                
            builder.Property(l => l.IncludesVaadBayitPayment)
                .HasDefaultValue(false);
                
            builder.Property(l => l.GuarantorIdNumber)
                .HasMaxLength(20);
        }
    }
}
