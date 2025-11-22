namespace Sacks.LogicLayer.Services.Interfaces
{
    /// <summary>
    /// Service for managing grid and filter state persistence
    /// </summary>
    public interface IGridStateManagementService
    {
        /// <summary>
        /// Saves the current state of a DataGridView
        /// </summary>
        /// <param name="gridState">The grid state to save</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SaveGridStateAsync(GridState gridState, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads and applies previously saved grid state
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The loaded grid state</returns>
        Task<GridState?> LoadGridStateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the current filter conditions
        /// </summary>
        /// <param name="filters">Filter conditions to save</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SaveFiltersStateAsync(IEnumerable<FilterCondition> filters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads previously saved filter conditions
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of saved filter conditions</returns>
        Task<IReadOnlyList<FilterCondition>> LoadFiltersStateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves selected column configuration
        /// </summary>
        /// <param name="selectedColumns">Currently selected columns</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SaveSelectedColumnsAsync(IEnumerable<string> selectedColumns, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads previously saved column selection
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of selected columns</returns>
        Task<IReadOnlyList<string>> LoadSelectedColumnsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all saved state
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ClearAllStateAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the state of a DataGridView for persistence
    /// </summary>
    public sealed class GridState
    {
        public List<ColumnState> Columns { get; set; } = new();
        public string? SortedColumnHeader { get; set; }
        public string? SortDirection { get; set; }
    }

    /// <summary>
    /// Represents the state of a single column
    /// </summary>
    public sealed class ColumnState
    {
        public string Header { get; set; } = string.Empty;
        public bool Visible { get; set; }
        public int Width { get; set; }
        public int DisplayIndex { get; set; }
    }
}
