using Microsoft.EntityFrameworkCore.Storage;

namespace Sacks.Core.Services.Interfaces
{
    /// <summary>
    /// Unit of Work pattern for coordinating database operations across multiple repositories
    /// Ensures data consistency and proper transaction management
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Clears the EF Core change tracker to resolve entity tracking conflicts
        /// </summary>
        void ClearTracker();

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Database transaction</returns>
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits all pending changes to the database
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected records</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes multiple operations within a single transaction
        /// Automatically handles commit/rollback based on success/failure
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes multiple operations within a single transaction (void return)
        /// Automatically handles commit/rollback based on success/failure
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current transaction if one is active
        /// </summary>
        IDbContextTransaction? CurrentTransaction { get; }

        /// <summary>
        /// Indicates whether a transaction is currently active
        /// </summary>
        bool HasActiveTransaction { get; }
    }
}
