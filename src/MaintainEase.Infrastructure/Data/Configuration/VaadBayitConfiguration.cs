using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MaintainEase.Infrastructure.Data.Configuration
{
    /// <summary>
    /// Configuration for Vaad Bayit entity
    /// </summary>
    public class VaadBayitConfiguration : IEntityTypeConfiguration<VaadBayit>
    {
        public void Configure(EntityTypeBuilder<VaadBayit> builder)
        {
            builder.ToTable("VaadBayit");
            
            builder.HasKey(v => v.Id);
            
            builder.Property(v => v.BuildingName)
                .IsRequired()
                .HasMaxLength(100);
            
            // Store committee members and property IDs as JSON arrays
            builder.Property<string>("CommitteeMembersJson")
                .HasColumnName("CommitteeMembers")
                .HasMaxLength(1000);
                
            builder.Property<string>("BuildingPropertyIdsJson")
                .HasColumnName("BuildingPropertyIds")
                .HasMaxLength(1000);
                
            // Configure value conversion for collections
            var guidListConverter = new ValueConverter<IReadOnlyCollection<Guid>, string>(
                v => JsonSerializer.Serialize(v.ToArray(), new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<Guid[]>(v, new JsonSerializerOptions()) as IReadOnlyCollection<Guid>);
                
            builder.Property(v => v.CommitteeMembers)
                .HasConversion(guidListConverter);
                
            builder.Property(v => v.BuildingPropertyIds)
                .HasConversion(guidListConverter);
        }
    }
}
