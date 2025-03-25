using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;

namespace MaintainEase.Infrastructure.Data.Configuration
{
    /// <summary>
    /// Configuration for Jewish Calendar Data entity
    /// </summary>
    public class JewishCalendarDataConfiguration : IEntityTypeConfiguration<JewishCalendarData>
    {
        public void Configure(EntityTypeBuilder<JewishCalendarData> builder)
        {
            builder.ToTable("JewishCalendarData");
            
            builder.HasKey(j => j.Id);
            
            builder.Property(j => j.HebrewDate)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(j => j.Holiday)
                .HasMaxLength(100);
                
            builder.Property(j => j.ParashaName)
                .HasMaxLength(100);
                
            builder.HasIndex(j => j.GregorianDate)
                .IsUnique();
        }
    }
}
