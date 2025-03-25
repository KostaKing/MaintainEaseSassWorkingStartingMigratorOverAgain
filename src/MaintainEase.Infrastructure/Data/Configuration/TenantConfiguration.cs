using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MaintainEase.Core.Domain.Entities;

namespace MaintainEase.Infrastructure.Data.Configuration
{
    /// <summary>
    /// Configuration for Tenant entity
    /// </summary>
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenants");
            
            builder.HasKey(t => t.Id);
            
            builder.Property(t => t.FirstName)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(t => t.LastName)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(t => t.Email)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(t => t.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);
                
            builder.Property(t => t.EmergencyContactName)
                .HasMaxLength(100);
                
            builder.Property(t => t.EmergencyContactPhone)
                .HasMaxLength(20);
                
            builder.HasIndex(t => t.Email)
                .IsUnique();
                
            // Ignore domain events for storage
            builder.Ignore(t => t.DomainEvents);
        }
    }
}
