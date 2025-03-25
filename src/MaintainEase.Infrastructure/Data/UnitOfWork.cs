using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Infrastructure.Data.Context;
using MaintainEase.Infrastructure.Data.Repositories;

namespace MaintainEase.Infrastructure.Data
{
    /// <summary>
    /// Implementation of the unit of work pattern
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction _transaction;
        private bool _disposed;

        // Repository instances
        private IPropertyRepository _propertyRepository;
        private ITenantRepository _tenantRepository;
        private ILeaseRepository _leaseRepository;

        public UnitOfWork(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Repository properties
        public IPropertyRepository PropertyRepository => 
            _propertyRepository ??= new PropertyRepository(_context);
            
        public ITenantRepository TenantRepository => 
            _tenantRepository ??= new TenantRepository(_context);
            
        public ILeaseRepository LeaseRepository => 
            _leaseRepository ??= new LeaseRepository(_context);

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _context.Dispose();
                _transaction?.Dispose();
            }
            _disposed = true;
        }
    }
}
