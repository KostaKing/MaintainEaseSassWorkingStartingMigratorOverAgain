using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;
using MaintainEase.Core.Domain.IsraeliMarket.Enums;

namespace MaintainEase.Infrastructure.Data.Configuration
{
    /// <summary>
    /// Configuration for Israeli Property entity
    /// </summary>
    public class IsraeliPropertyConfiguration : IEntityTypeConfiguration<IsraeliProperty>
    {
        public void Configure(EntityTypeBuilder<IsraeliProperty> builder)
        {
            builder.ToTable("IsraeliProperties");
            
            // This is a table-per-hierarchy mapping, so we configure additional
            // fields specific to IsraeliProperty here
            
            builder.Property(p => p.IsraeliPropertyType)
                .HasConversion<string>()
                .HasMaxLength(30);
                
            builder.Property(p => p.IsKosher)
                .HasDefaultValue(false);
                
            builder.Property(p => p.HasShabbatElevator)
                .HasDefaultValue(false);
                
            builder.Property(p => p.HasSukkahBalcony)
                .HasDefaultValue(false);
                
            builder.Property(p => p.IsVaadBayitMember)
                .HasDefaultValue(false);
                
            builder.Property(p => p.VaadBayitMonthlyFee)
                .HasPrecision(10, 2);
                
            builder.Property(p => p.ArnonaBillingId)
                .HasMaxLength(50);
        }
    }
}
