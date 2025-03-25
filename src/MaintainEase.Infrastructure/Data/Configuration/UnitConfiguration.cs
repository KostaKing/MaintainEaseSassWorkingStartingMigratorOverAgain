using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MaintainEase.Core.Domain.Entities;

namespace MaintainEase.Infrastructure.Data.Configuration
{
    /// <summary>
    /// Configuration for Unit entity
    /// </summary>
    public class UnitConfiguration : IEntityTypeConfiguration<Unit>
    {
        public void Configure(EntityTypeBuilder<Unit> builder)
        {
            builder.ToTable("Units");
            
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.UnitNumber)
                .IsRequired()
                .HasMaxLength(20);
                
            builder.Property(u => u.Area)
                .HasPrecision(10, 2);
                
            builder.Property(u => u.Description)
                .HasMaxLength(500);
                
            builder.HasIndex(u => new { u.PropertyId, u.UnitNumber })
                .IsUnique();
        }
    }
}
