using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MaintainEase.Infrastructure.Data.Configuration
{
    /// <summary>
    /// Configuration for Lease entity
    /// </summary>
    public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
    {
        public void Configure(EntityTypeBuilder<Lease> builder)
        {
            builder.ToTable("Leases");
            
            builder.HasKey(l => l.Id);
            
            builder.Property(l => l.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
                
            builder.Property(l => l.TerminationConditions)
                .HasMaxLength(500);
                
            builder.Property(l => l.SpecialConditions)
                .HasMaxLength(500);
                
            // Store tenant IDs as JSON array
            builder.Property<string>("TenantIdsJson")
                .HasColumnName("TenantIds")
                .HasMaxLength(1000);
                
            // Configure value conversion for TenantIds collection
            var tenantIdsConverter = new ValueConverter<IReadOnlyCollection<Guid>, string>(
                v => JsonSerializer.Serialize(v.ToArray(), new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<Guid[]>(v, new JsonSerializerOptions()) as IReadOnlyCollection<Guid>);
                
            builder.Property(l => l.TenantIds)
                .HasConversion(tenantIdsConverter);
                
            // Ignore domain events for storage
            builder.Ignore(l => l.DomainEvents);
        }
    }
}
