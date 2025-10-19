namespace SacksLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Service for building SQL queries with filters
    /// </summary>
    public interface IQueryBuilderService
    {
        /// <summary>
        /// Builds a SQL query for ProductOffersView with filters
        /// </summary>
        /// <param name="selectedColumns">Columns to select</param>
        /// <param name="filters">Filter conditions</param>
        /// <param name="groupByProduct">Whether to group by product</param>
        /// <returns>SQL query string and parameters</returns>
        (string Sql, Dictionary<string, object> Parameters) BuildQuery(
            IEnumerable<string> selectedColumns,
            IEnumerable<FilterCondition> filters,
            bool groupByProduct = false);
    }

    /// <summary>
    /// Represents a filter condition for queries
    /// </summary>
    public class FilterCondition
    {
        public required string PropertyName { get; init; }
        public required FilterOperator Operator { get; init; }
        public string? Value { get; init; }
        public Type PropertyType { get; init; } = typeof(string);
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Filter operators supported
    /// </summary>
    public enum FilterOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        IsEmpty,
        IsNotEmpty
    }
}
