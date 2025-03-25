using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Enums;

namespace MaintainEase.Infrastructure.Data.Configuration
{
    /// <summary>
    /// Configuration for Property entity
    /// </summary>
    public class PropertyConfiguration : IEntityTypeConfiguration<Property>
    {
        public void Configure(EntityTypeBuilder<Property> builder)
        {
            builder.ToTable("Properties");
            
            builder.HasKey(p => p.Id);
            
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(p => p.LegalDescription)
                .HasMaxLength(500);
                
            builder.Property(p => p.TaxIdentifier)
                .HasMaxLength(50);
                
            builder.Property(p => p.TotalArea)
                .HasPrecision(10, 2);
                
            builder.Property(p => p.Type)
                .HasConversion<string>()
                .HasMaxLength(20);
                
            // One-to-many relationship with Units
            builder.HasMany(p => p.Units)
                .WithOne()
                .HasForeignKey(u => u.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Ignore domain events for storage
            builder.Ignore(p => p.DomainEvents);
        }
    }
}
