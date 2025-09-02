using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SacksDataLayer.Data;
using SacksDataLayer.Services.Interfaces;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Unit of Work implementation for coordinating database operations
    /// Provides transaction management and ensures data consistency across repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SacksDbContext _context;
        private IDbContextTransaction? _currentTransaction;
        private bool _disposed = false;

        public UnitOfWork(SacksDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets the current transaction if one is active
        /// </summary>
        public IDbContextTransaction? CurrentTransaction => _currentTransaction;

        /// <summary>
        /// Indicates whether a transaction is currently active
        /// </summary>
        public bool HasActiveTransaction => _currentTransaction != null;

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("A transaction is already active. Call CommitTransactionAsync or RollbackTransactionAsync first.");
            }

            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return _currentTransaction;
        }

        /// <summary>
        /// Commits all pending changes to the database
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No active transaction to commit.");
            }

            try
            {
                await SaveChangesAsync(cancellationToken);
                await _currentTransaction.CommitAsync(cancellationToken);
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No active transaction to rollback.");
            }

            try
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        /// <summary>
        /// Executes multiple operations within a single transaction with return value
        /// Automatically handles commit/rollback based on success/failure
        /// </summary>
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Use execution strategy to handle connection resiliency
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            
            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await BeginTransactionAsync(cancellationToken);
                try
                {
                    var result = await operation(cancellationToken);
                    await CommitTransactionAsync(cancellationToken);
                    return result;
                }
                catch
                {
                    await RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            });
        }

        /// <summary>
        /// Executes multiple operations within a single transaction (void return)
        /// Automatically handles commit/rollback based on success/failure
        /// </summary>
        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Use execution strategy to handle connection resiliency
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            
            await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await BeginTransactionAsync(cancellationToken);
                try
                {
                    await operation(cancellationToken);
                    await CommitTransactionAsync(cancellationToken);
                }
                catch
                {
                    await RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            });
        }

        /// <summary>
        /// Disposes the Unit of Work and any active transaction
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _currentTransaction?.Dispose();
                _disposed = true;
            }
        }
    }
}
