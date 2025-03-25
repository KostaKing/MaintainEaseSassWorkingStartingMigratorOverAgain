using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;

namespace MaintainEase.Infrastructure.Data.Configuration
{
    /// <summary>
    /// Configuration for CPI Data entity
    /// </summary>
    public class CPIDataConfiguration : IEntityTypeConfiguration<CPIData>
    {
        public void Configure(EntityTypeBuilder<CPIData> builder)
        {
            builder.ToTable("CPIData");
            
            builder.HasKey(c => c.Id);
            
            builder.Property(c => c.IndexValue)
                .HasPrecision(10, 4);
                
            builder.Property(c => c.MonthlyChangePercentage)
                .HasPrecision(5, 2);
                
            builder.Property(c => c.YearlyChangePercentage)
                .HasPrecision(5, 2);
                
            builder.HasIndex(c => new { c.Year, c.Month })
                .IsUnique();
        }
    }
}
