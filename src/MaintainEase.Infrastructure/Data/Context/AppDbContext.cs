using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Events;
using MaintainEase.Core.Domain.ValueObjects;
using MaintainEase.Infrastructure.Data.Configuration;
using MaintainEase.Infrastructure.Data.Interceptors;
using MaintainEase.Infrastructure.MultiTenancy;
using System.Linq.Expressions;

namespace MaintainEase.Infrastructure.Data.Context
{
    /// <summary>
    /// Main database context for the application
    /// </summary>
    public class AppDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly IAuditInterceptor _auditInterceptor;
        private readonly IDomainEventInterceptor _domainEventInterceptor;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ITenantProvider tenantProvider,
            IAuditInterceptor auditInterceptor,
            IDomainEventInterceptor domainEventInterceptor) : base(options)
        {
            _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
            _auditInterceptor = auditInterceptor ?? throw new ArgumentNullException(nameof(auditInterceptor));
            _domainEventInterceptor = domainEventInterceptor ?? throw new ArgumentNullException(nameof(domainEventInterceptor));
        }

        // DbSets for domain entities
        public DbSet<Property> Properties { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Lease> Leases { get; set; }

        // Israeli market specific entities
        public DbSet<Core.Domain.IsraeliMarket.Entities.IsraeliProperty> IsraeliProperties { get; set; }
        public DbSet<Core.Domain.IsraeliMarket.Entities.IsraeliLease> IsraeliLeases { get; set; }
        public DbSet<Core.Domain.IsraeliMarket.Entities.CPIData> CPIData { get; set; }
        public DbSet<Core.Domain.IsraeliMarket.Entities.JewishCalendarData> JewishCalendarData { get; set; }
        public DbSet<Core.Domain.IsraeliMarket.Entities.VaadBayit> VaadBayit { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply entity configurations
            modelBuilder.ApplyConfiguration(new PropertyConfiguration());
            modelBuilder.ApplyConfiguration(new UnitConfiguration());
            modelBuilder.ApplyConfiguration(new TenantConfiguration());
            modelBuilder.ApplyConfiguration(new LeaseConfiguration());
            
            // Apply Israeli market entity configurations
            modelBuilder.ApplyConfiguration(new IsraeliPropertyConfiguration());
            modelBuilder.ApplyConfiguration(new IsraeliLeaseConfiguration());
            modelBuilder.ApplyConfiguration(new CPIDataConfiguration());
            modelBuilder.ApplyConfiguration(new JewishCalendarDataConfiguration());
            modelBuilder.ApplyConfiguration(new VaadBayitConfiguration());

            // Configure value object conversions
            ConfigureValueObjects(modelBuilder);
            
            // Apply multi-tenancy filter
            ApplyMultiTenancyFilter(modelBuilder);
        }

        private void ConfigureValueObjects(ModelBuilder modelBuilder)
        {
            // Configure Money value object
            modelBuilder.Entity<Property>()
                .Property(p => p.PurchasePrice)
                .HasConversion(
                    v => $"{v.Amount}|{v.Currency}",
                    v => {
                        var parts = v.Split('|');
                        return new Money(decimal.Parse(parts[0]), parts[1]);
                    });

            modelBuilder.Entity<Property>()
                .Property(p => p.CurrentValue)
                .HasConversion(
                    v => $"{v.Amount}|{v.Currency}",
                    v => {
                        var parts = v.Split('|');
                        return new Money(decimal.Parse(parts[0]), parts[1]);
                    });

            modelBuilder.Entity<Lease>()
                .Property(l => l.MonthlyRent)
                .HasConversion(
                    v => $"{v.Amount}|{v.Currency}",
                    v => {
                        var parts = v.Split('|');
                        return new Money(decimal.Parse(parts[0]), parts[1]);
                    });

            modelBuilder.Entity<Lease>()
                .Property(l => l.SecurityDeposit)
                .HasConversion(
                    v => $"{v.Amount}|{v.Currency}",
                    v => {
                        var parts = v.Split('|');
                        return new Money(decimal.Parse(parts[0]), parts[1]);
                    });

            // Configure Address value object
            // Use JSON conversion for complex value objects
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var addressConverter = new ValueConverter<Address, string>(
                v => System.Text.Json.JsonSerializer.Serialize(v, jsonOptions),
                v => System.Text.Json.JsonSerializer.Deserialize<Address>(v, jsonOptions));

            var identificationConverter = new ValueConverter<Identification, string>(
                v => System.Text.Json.JsonSerializer.Serialize(v, jsonOptions),
                v => System.Text.Json.JsonSerializer.Deserialize<Identification>(v, jsonOptions));

            modelBuilder.Entity<Property>()
                .Property(p => p.Address)
                .HasConversion(addressConverter);

            modelBuilder.Entity<Tenant>()
                .Property(t => t.PermanentAddress)
                .HasConversion(addressConverter);

            modelBuilder.Entity<Tenant>()
                .Property(t => t.IdDocument)
                .HasConversion(identificationConverter);

            // Configure Israeli market value objects
            var tabuExtractConverter = new ValueConverter<Core.Domain.IsraeliMarket.ValueObjects.TabuExtract, string>(
                v => System.Text.Json.JsonSerializer.Serialize(v, jsonOptions),
                v => System.Text.Json.JsonSerializer.Deserialize<Core.Domain.IsraeliMarket.ValueObjects.TabuExtract>(v, jsonOptions));

            var arnonaZoneConverter = new ValueConverter<Core.Domain.IsraeliMarket.ValueObjects.ArnonaZone, string>(
                v => System.Text.Json.JsonSerializer.Serialize(v, jsonOptions),
                v => System.Text.Json.JsonSerializer.Deserialize<Core.Domain.IsraeliMarket.ValueObjects.ArnonaZone>(v, jsonOptions));

            modelBuilder.Entity<Core.Domain.IsraeliMarket.Entities.IsraeliProperty>()
                .Property(p => p.TabuExtract)
                .HasConversion(tabuExtractConverter);

            modelBuilder.Entity<Core.Domain.IsraeliMarket.Entities.IsraeliProperty>()
                .Property(p => p.ArnonaZone)
                .HasConversion(arnonaZoneConverter);
        }

        private void ApplyMultiTenancyFilter(ModelBuilder modelBuilder)
        {
            // Apply tenant filter to all tenant-specific entities
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                .Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType)))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, "TenantId");
                var tenantIdValue = Expression.Constant(_tenantProvider.GetCurrentTenantId());
                var body = Expression.Equal(property, tenantIdValue);
                var lambda = Expression.Lambda(body, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Apply audit information
            _auditInterceptor.ApplyAuditInformation(this);
            
            // Capture domain events before saving changes
            var domainEvents = _domainEventInterceptor.CaptureEvents(this);
            
            var result = await base.SaveChangesAsync(cancellationToken);
            
            // Dispatch domain events after saving changes
            await _domainEventInterceptor.DispatchEventsAsync(domainEvents, cancellationToken);
            
            return result;
        }

        public override int SaveChanges()
        {
            // Apply audit information
            _auditInterceptor.ApplyAuditInformation(this);
            
            // Capture domain events before saving changes
            var domainEvents = _domainEventInterceptor.CaptureEvents(this);
            
            var result = base.SaveChanges();
            
            // Dispatch domain events after saving changes
            _domainEventInterceptor.DispatchEvents(domainEvents);
            
            return result;
        }
    }
}
