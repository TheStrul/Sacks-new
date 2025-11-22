namespace Sacks.Core.Services.Interfaces;

/// <summary>
/// Service for building SQL queries for ProductOffersView
/// </summary>
public interface IQueryBuilderService
{
    /// <summary>
    /// Builds a SQL query with parameters
    /// </summary>
    (string Sql, Dictionary<string, object> Parameters) BuildQuery(
        IEnumerable<string> selectedColumns,
        IEnumerable<FilterCondition> filters,
        bool groupByProduct = false);
}
