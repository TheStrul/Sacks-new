using System.Data;
using SacksLogicLayer.Services.Interfaces;

namespace SacksLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Service for managing ProductOffers view queries, filtering, and column operations
    /// </summary>
    public interface IProductOffersQueryService
    {
        /// <summary>
        /// Gets all available columns from the ProductOffersView
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available column names</returns>
        Task<IReadOnlyList<string>> GetAvailableColumnsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a query against ProductOffersView with the specified parameters
        /// </summary>
        /// <param name="selectedColumns">Columns to include in results</param>
        /// <param name="filters">Filter conditions to apply</param>
        /// <param name="groupByProduct">Whether to group results by product</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>DataTable containing query results</returns>
        Task<DataTable> ExecuteQueryAsync(
            IEnumerable<string> selectedColumns,
            IEnumerable<FilterCondition> filters,
            bool groupByProduct = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available filter operators for a specific column type
        /// </summary>
        /// <param name="columnName">Name of the column</param>
        /// <returns>List of supported filter operators</returns>
        IReadOnlyList<FilterOperator> GetFilterOperators(string columnName);

        /// <summary>
        /// Validates a filter value for a specific column
        /// </summary>
        /// <param name="columnName">Name of the column</param>
        /// <param name="value">Value to validate</param>
        /// <param name="operator">Filter operator being used</param>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateFilterValue(string columnName, string? value, FilterOperator @operator);

        /// <summary>
        /// Gets the property type for a specific column
        /// </summary>
        /// <param name="columnName">Name of the column</param>
        /// <returns>The .NET type of the column</returns>
        Type GetColumnType(string columnName);

        /// <summary>
        /// Determines if a column is editable in the grid
        /// </summary>
        /// <param name="columnName">Name of the column</param>
        /// <returns>True if column is editable, false otherwise</returns>
        bool IsColumnEditable(string columnName);

        /// <summary>
        /// Gets all columns categorized by type (Product vs Offer)
        /// </summary>
        /// <returns>Categorized columns</returns>
        ColumnCategories GetColumnCategories();
    }

    /// <summary>
    /// Categorizes columns into Product and Offer columns
    /// </summary>
    public sealed class ColumnCategories
    {
        public IReadOnlySet<string> ProductColumns { get; init; } = new HashSet<string>();
        public IReadOnlySet<string> OfferColumns { get; init; } = new HashSet<string>();
        public IReadOnlySet<string> EditableColumns { get; init; } = new HashSet<string>();
    }
}