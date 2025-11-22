using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using Sacks.Core.Services.Interfaces;
using Sacks.DataAccess.Data;

namespace Sacks.DataAccess.Services;

/// <summary>
/// Service for building SQL queries for ProductOffersView with filtering and grouping
/// </summary>
public sealed class QueryBuilderService : IQueryBuilderService
{
    private readonly SacksDbContext _dbContext;
    private readonly ILogger<QueryBuilderService> _logger;

    private static readonly HashSet<string> ProductColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "EAN","Name","Category","Brand","Gender","Concentration","Size",
        "Type 1","Type 2","Decoded","COO","Units","Ref"
    };

    private static readonly HashSet<string> OfferColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Price","Currency","Quantity","Description",
        "Supplier Name","Offer Name","Date Offer",
        "OfferRank","TotalOffers"
    };

    public QueryBuilderService(SacksDbContext dbContext, ILogger<QueryBuilderService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public (string Sql, Dictionary<string, object> Parameters) BuildQuery(
        IEnumerable<string> selectedColumns,
        IEnumerable<FilterCondition> filters,
        bool groupByProduct = false)
    {
        var columns = selectedColumns?.ToList() ?? new List<string>();
        var filterList = filters?.Where(f => f.Enabled).ToList() ?? new List<FilterCondition>();
        
        var sqlParams = new List<SqlParameter>();
        string sql;

        if (!groupByProduct)
        {
            var where = BuildBaseWhere(filterList, sqlParams);
            sql = BuildSimpleQuery(columns, where);
        }
        else
        {
            bool hasOfferFilters = filterList.Any(f => OfferColumns.Contains(f.PropertyName));
            if (!hasOfferFilters)
            {
                var where = BuildCollapsedViewWhere(filterList, sqlParams);
                sql = BuildCollapsedUsingView(columns, where);
            }
            else
            {
                var innerWhere = BuildDynamicInnerWhere(filterList, sqlParams);
                sql = BuildDynamicCollapsed(columns, innerWhere);
            }
        }

        var parameters = sqlParams.ToDictionary(p => p.ParameterName, p => (object)(p.Value ?? DBNull.Value));
        return (sql, parameters);
    }

    private string BuildBaseWhere(List<FilterCondition> filters, List<SqlParameter> parameters)
    {
        if (filters.Count == 0) return string.Empty;
        
        var whereParts = new List<string>();
        int paramIndex = 0;
        
        foreach (var f in filters)
        {
            if (!f.Enabled) continue;
            var part = BuildPredicate(string.Empty, f, ref paramIndex, parameters);
            if (!string.IsNullOrWhiteSpace(part)) whereParts.Add(part);
        }
        
        return whereParts.Count > 0 ? " WHERE " + string.Join(" AND ", whereParts) : string.Empty;
    }

    private string BuildCollapsedViewWhere(List<FilterCondition> filters, List<SqlParameter> parameters)
    {
        if (filters.Count == 0) return string.Empty;
        
        var whereParts = new List<string>();
        int paramIndex = 0;
        
        foreach (var f in filters)
        {
            if (!f.Enabled) continue;
            
            if (ProductColumns.Contains(f.PropertyName) && !OfferColumns.Contains(f.PropertyName))
            {
                var innerPred = BuildPredicate("v", f, ref paramIndex, parameters);
                if (!string.IsNullOrWhiteSpace(innerPred))
                {
                    whereParts.Add($"EXISTS (SELECT 1 FROM [ProductOffersView] AS v WHERE v.[EAN] = c.[EANKey] AND {innerPred})");
                }
            }
            else
            {
                var direct = BuildPredicate("c", f, ref paramIndex, parameters);
                if (!string.IsNullOrWhiteSpace(direct)) whereParts.Add(direct);
            }
        }
        
        return whereParts.Count > 0 ? " WHERE " + string.Join(" AND ", whereParts) : string.Empty;
    }

    private string BuildDynamicInnerWhere(List<FilterCondition> filters, List<SqlParameter> parameters)
    {
        if (filters.Count == 0) return string.Empty;
        
        var whereParts = new List<string>();
        int paramIndex = 0;
        
        foreach (var f in filters)
        {
            if (!f.Enabled) continue;
            var pred = BuildPredicate("v", f, ref paramIndex, parameters);
            if (!string.IsNullOrWhiteSpace(pred)) whereParts.Add(pred);
        }
        
        return whereParts.Count > 0 ? " WHERE " + string.Join(" AND ", whereParts) : string.Empty;
    }

    private static string EscapeIdentifier(string name) => "[" + name.Replace("]", "]]") + "]";

    private string BuildPredicate(string alias, FilterCondition f, ref int paramIndex, List<SqlParameter> parameters)
    {
        var prefix = string.IsNullOrEmpty(alias) ? string.Empty : alias + ".";
        var colRef = prefix + EscapeIdentifier(f.PropertyName);
        
        if (f.Operator is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty)
        {
            if (f.PropertyType == typeof(string))
            {
                return f.Operator == FilterOperator.IsEmpty
                    ? $"({colRef} IS NULL OR {colRef} = '')"
                    : $"({colRef} IS NOT NULL AND {colRef} <> '')";
            }
            else
            {
                return f.Operator == FilterOperator.IsEmpty 
                    ? $"{colRef} IS NULL" 
                    : $"{colRef} IS NOT NULL";
            }
        }
        else
        {
            var paramName = $"@p{paramIndex++}";
            object? valueObj = f.Value;
            
            if (valueObj != null && f.PropertyType != typeof(string))
            {
                valueObj = ConvertToType(f.Value!, f.PropertyType);
            }
            
            string opSql = f.Operator switch
            {
                FilterOperator.Contains => $"{colRef} LIKE {paramName}",
                FilterOperator.StartsWith => $"{colRef} LIKE {paramName}",
                FilterOperator.EndsWith => $"{colRef} LIKE {paramName}",
                FilterOperator.Equals => $"{colRef} = {paramName}",
                FilterOperator.NotEquals => $"{colRef} <> {paramName}",
                FilterOperator.GreaterThan => $"{colRef} > {paramName}",
                FilterOperator.GreaterThanOrEqual => $"{colRef} >= {paramName}",
                FilterOperator.LessThan => $"{colRef} < {paramName}",
                FilterOperator.LessThanOrEqual => $"{colRef} <= {paramName}",
                _ => string.Empty
            };

            object? paramVal = valueObj;
            switch (f.Operator)
            {
                case FilterOperator.Contains:
                    paramVal = $"%{valueObj}%";
                    break;
                case FilterOperator.StartsWith:
                    paramVal = $"{valueObj}%";
                    break;
                case FilterOperator.EndsWith:
                    paramVal = $"%{valueObj}";
                    break;
            }
            
            parameters.Add(new SqlParameter(paramName, paramVal ?? DBNull.Value));
            return opSql;
        }
    }

    private static string BuildSelectList(IEnumerable<string> columns)
    {
        return string.Join(", ", columns.Select(EscapeIdentifier));
    }

    private string BuildSimpleQuery(List<string> selectedColumns, string where)
    {
        var sb = new StringBuilder();
        var cols = BuildSelectList(selectedColumns);
        sb.Append($"SELECT {cols} FROM [ProductOffersView]");
        sb.Append(where);
        
        var orderCols = new List<string>();
        if (selectedColumns.Any(c => string.Equals(c, "EAN", StringComparison.OrdinalIgnoreCase))) 
            orderCols.Add("[EAN]");
        if (selectedColumns.Any(c => string.Equals(c, "OfferRank", StringComparison.OrdinalIgnoreCase))) 
            orderCols.Add("[OfferRank]");
        else if (selectedColumns.Any(c => string.Equals(c, "Price", StringComparison.OrdinalIgnoreCase))) 
            orderCols.Add("[Price]");
            
        if (orderCols.Count > 0) 
            sb.Append(" ORDER BY " + string.Join(", ", orderCols));
            
        return sb.ToString();
    }

    private string BuildCollapsedUsingView(List<string> selectedColumns, string where)
    {
        var sb = new StringBuilder();
        var cols = BuildSelectList(selectedColumns);
        sb.Append($"SELECT {cols} FROM [ProductOffersViewCollapse] AS c");
        sb.Append(where);
        sb.Append(" ORDER BY [EANKey], [OfferRank]");
        return sb.ToString();
    }

    private string BuildDynamicCollapsed(List<string> selectedColumns, string innerWhere)
    {
        var inner = new StringBuilder();
        inner.Append("SELECT v.*, ROW_NUMBER() OVER (PARTITION BY v.[EAN] ORDER BY v.[OfferRank]) AS rn, COUNT(*) OVER (PARTITION BY v.[EAN]) AS cnt FROM [ProductOffersView] AS v");
        inner.Append(innerWhere);

        var projected = new List<string>();
        foreach (var col in selectedColumns)
        {
            if (ProductColumns.Contains(col) && !OfferColumns.Contains(col))
                projected.Add($"CASE WHEN rn = 1 THEN {EscapeIdentifier(col)} ELSE NULL END AS {EscapeIdentifier(col)}");
            else
                projected.Add(EscapeIdentifier(col));
        }

        var sb = new StringBuilder();
        sb.Append("SELECT ");
        sb.Append(string.Join(", ", projected));
        sb.Append(" FROM (");
        sb.Append(inner);
        sb.Append(") AS x WHERE x.[cnt] > 1 ORDER BY x.[EAN], x.[OfferRank]");
        return sb.ToString();
    }

    private static object? ConvertToType(string raw, Type target)
    {
        if (target == typeof(string)) return raw;
        if (target == typeof(int)) return int.Parse(raw, CultureInfo.InvariantCulture);
        if (target == typeof(decimal)) return decimal.Parse(raw, CultureInfo.InvariantCulture);
        if (target == typeof(double)) return double.Parse(raw, CultureInfo.InvariantCulture);
        if (target == typeof(DateTime)) return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
        return Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
    }
}
