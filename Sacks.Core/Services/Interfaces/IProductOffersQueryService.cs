using System.Data;

namespace Sacks.Core.Services.Interfaces;

/// <summary>
/// Service for querying ProductOffers data with filtering and grouping
/// </summary>
public interface IProductOffersQueryService
{
    /// <summary>
    /// Executes a query against ProductOffersView
    /// </summary>
    Task<DataTable> ExecuteQueryAsync(
        IEnumerable<string> selectedColumns,
        IEnumerable<FilterCondition> filters,
        bool groupByProduct = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of available columns
    /// </summary>
    IReadOnlyList<string> GetAvailableColumns();

    /// <summary>
    /// Gets the type of a column
    /// </summary>
    Type GetColumnType(string columnName);

    /// <summary>
    /// Gets available filter operators for a column
    /// </summary>
    IReadOnlyList<Sacks.Core.Services.Models.FilterOperator> GetFilterOperators(string columnName);

    /// <summary>
    /// Gets the column categories
    /// </summary>
    Sacks.Core.Services.Models.ColumnCategories GetColumnCategories();

    /// <summary>
    /// Checks if a column is editable
    /// </summary>
    bool IsColumnEditable(string columnName);
}

/// <summary>
/// Represents a filter condition for querying
/// </summary>
public sealed class FilterCondition
{
    public required string Column { get; init; }
    public required string Operator { get; init; }
    public required string Value { get; init; }
    public bool Enabled { get; init; } = true;
}
