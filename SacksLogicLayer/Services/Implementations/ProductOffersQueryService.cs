using System.Data;
using System.Globalization;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SacksDataLayer.Data;
using SacksDataLayer.Entities;
using SacksLogicLayer.Services.Interfaces;

namespace SacksLogicLayer.Services.Implementations;

/// <summary>
/// Service for managing ProductOffers view queries, filtering, and column operations
/// </summary>
public sealed class ProductOffersQueryService : IProductOffersQueryService
{
    private readonly SacksDbContext _dbContext;
    private readonly IQueryBuilderService _queryBuilderService;
    private readonly ILogger<ProductOffersQueryService> _logger;

    // Cached column information
    private static readonly Type ViewType = typeof(ProductOffersView);
    private static readonly string[] EntityPropertyNames = ViewType
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(p => p.Name)
        .OrderBy(n => n)
        .ToArray();

    // Column categorization
    private static readonly HashSet<string> ProductColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "EAN","Name","Category","Brand","Gender","Concentration","Size","Type 1","Type 2","Decoded","COO","Units","Ref"
    };

    private static readonly HashSet<string> OfferColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Price","Currency","Quantity","Description",
        "Supplier Name","Offer Name","Date Offer",
        "OfferRank","TotalOffers"
    };

    private static readonly HashSet<string> EditableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Price", "Currency", "Quantity", "Description", "Details"
    };

    private const string RowNumberHeader = "Row #";

    public ProductOffersQueryService(
        SacksDbContext dbContext,
        IQueryBuilderService queryBuilderService,
        ILogger<ProductOffersQueryService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _queryBuilderService = queryBuilderService ?? throw new ArgumentNullException(nameof(queryBuilderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<string>> GetAvailableColumnsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _dbContext.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Missing connection string");

            await using var connection = new SqlConnection(connectionString);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT TOP (0) * FROM [ProductOffersView]";
            
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly, cancellationToken).ConfigureAwait(false);
            
            var schema = reader.GetSchemaTable();
            var columns = new List<string>();
            
            if (schema != null)
            {
                foreach (DataRow row in schema.Rows)
                {
                    var name = row["ColumnName"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                        columns.Add(name);
                }
            }

            if (columns.Count > 0)
                return columns.Distinct().OrderBy(c => c).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read schema for ProductOffersView, falling back to entity properties");
        }

        // Fallback to entity properties
        return EntityPropertyNames.ToList().AsReadOnly();
    }

    public async Task<DataTable> ExecuteQueryAsync(
        IEnumerable<string> selectedColumns,
        IEnumerable<FilterCondition> filters,
        bool groupByProduct = false,
        CancellationToken cancellationToken = default)
    {
        var columns = selectedColumns?.ToList() ?? new List<string>();
        if (columns.Count == 0)
        {
            columns = (await GetAvailableColumnsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        }

        var (sql, parameters) = _queryBuilderService.BuildQuery(columns, filters, groupByProduct);
        
        _logger.LogDebug("Executing ProductOffers query with {ColumnCount} columns and {FilterCount} filters", 
            columns.Count, filters?.Count() ?? 0);

        var connectionString = _dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string unavailable");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = sql;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
        command.CommandTimeout = 30;

        // Add parameters
        foreach (var kvp in parameters)
        {
            command.Parameters.Add(new SqlParameter(kvp.Key, kvp.Value ?? DBNull.Value));
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var dataTable = new DataTable();
        dataTable.Load(reader);

        // Add row number column as first column
        AddRowNumberColumn(dataTable);

        return dataTable;
    }

    public IReadOnlyList<FilterOperator> GetFilterOperators(string columnName)
    {
        var columnType = GetColumnType(columnName);

        if (columnType == typeof(string))
        {
            return new[]
            {
                FilterOperator.Contains,
                FilterOperator.StartsWith,
                FilterOperator.EndsWith,
                FilterOperator.Equals,
                FilterOperator.NotEquals,
                FilterOperator.IsEmpty,
                FilterOperator.IsNotEmpty
            };
        }

        if (columnType == typeof(int) || columnType == typeof(decimal) || 
            columnType == typeof(double) || columnType == typeof(DateTime))
        {
            return new[]
            {
                FilterOperator.Equals,
                FilterOperator.NotEquals,
                FilterOperator.GreaterThan,
                FilterOperator.GreaterThanOrEqual,
                FilterOperator.LessThan,
                FilterOperator.LessThanOrEqual
            };
        }

        return new[] { FilterOperator.Equals, FilterOperator.NotEquals };
    }

    public bool ValidateFilterValue(string columnName, string? value, FilterOperator @operator)
    {
        // Empty/null operators don't require values
        if (@operator is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty)
            return true;

        // Other operators require non-empty values
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var columnType = GetColumnType(columnName);
        
        // String columns are always valid
        if (columnType == typeof(string))
            return true;

        // Try to convert to target type
        try
        {
            _ = ConvertToType(value, columnType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Type GetColumnType(string columnName)
    {
        var property = ViewType.GetProperty(columnName);
        if (property != null)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        }
        
        // Default to string for unknown columns
        return typeof(string);
    }

    public bool IsColumnEditable(string columnName)
    {
        return EditableColumns.Contains(columnName) &&
               !string.Equals(columnName, RowNumberHeader, StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(columnName, "EAN", StringComparison.OrdinalIgnoreCase);
    }

    public ColumnCategories GetColumnCategories()
    {
        return new ColumnCategories
        {
            ProductColumns = ProductColumns,
            OfferColumns = OfferColumns,
            EditableColumns = EditableColumns
        };
    }

    private static void AddRowNumberColumn(DataTable dataTable)
    {
        if (dataTable.Columns.Contains(RowNumberHeader))
        {
            // Refresh existing values
            for (int i = 0; i < dataTable.Rows.Count; i++)
                dataTable.Rows[i][RowNumberHeader] = i + 1;
            dataTable.Columns[RowNumberHeader]!.SetOrdinal(0);
            return;
        }

        var column = new DataColumn(RowNumberHeader, typeof(int));
        dataTable.Columns.Add(column);
        column.SetOrdinal(0);
        
        for (int i = 0; i < dataTable.Rows.Count; i++)
            dataTable.Rows[i][column] = i + 1;
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