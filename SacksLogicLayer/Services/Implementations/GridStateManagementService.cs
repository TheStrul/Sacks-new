using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SacksLogicLayer.Services.Interfaces;

namespace SacksLogicLayer.Services.Implementations;

/// <summary>
/// Service for managing grid and filter state persistence
/// </summary>
public sealed class GridStateManagementService : IGridStateManagementService
{
    private readonly ILogger<GridStateManagementService> _logger;
    private readonly string _stateDirectory;
    private readonly string _gridStatePath;
    private readonly string _filtersStatePath;
    private readonly string _columnsStatePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public GridStateManagementService(ILogger<GridStateManagementService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _stateDirectory = Path.Combine(appData, "SacksApp");
        Directory.CreateDirectory(_stateDirectory);
        
        _gridStatePath = Path.Combine(_stateDirectory, "resultsGrid.state.json");
        _filtersStatePath = Path.Combine(_stateDirectory, "filters.state.json");
        _columnsStatePath = Path.Combine(_stateDirectory, "columns.state.json");
    }

    public async Task SaveGridStateAsync(GridState gridState, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(gridState, JsonOptions);
            await File.WriteAllTextAsync(_gridStatePath, json, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
            
            _logger.LogDebug("Grid state saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save grid state");
        }
    }

    public async Task<GridState?> LoadGridStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_gridStatePath))
                return null;

            var json = await File.ReadAllTextAsync(_gridStatePath, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
            
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var state = JsonSerializer.Deserialize<GridState>(json);
            
            _logger.LogDebug("Grid state loaded successfully");
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load grid state");
            return null;
        }
    }

    public async Task SaveFiltersStateAsync(IEnumerable<FilterCondition> filters, CancellationToken cancellationToken = default)
    {
        try
        {
            var filterList = filters?.ToList() ?? new List<FilterCondition>();
            var persistedFilters = filterList.Select(f => new PersistedFilter
            {
                PropertyName = f.PropertyName,
                Operator = f.Operator.ToString(),
                Value = f.Value,
                Enabled = f.Enabled
            }).ToList();

            var json = JsonSerializer.Serialize(persistedFilters, JsonOptions);
            await File.WriteAllTextAsync(_filtersStatePath, json, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
            
            _logger.LogDebug("Filters state saved with {FilterCount} filters", filterList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save filters state");
        }
    }

    public async Task<IReadOnlyList<FilterCondition>> LoadFiltersStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_filtersStatePath))
                return Array.Empty<FilterCondition>();

            var json = await File.ReadAllTextAsync(_filtersStatePath, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
            
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<FilterCondition>();

            var persistedFilters = JsonSerializer.Deserialize<List<PersistedFilter>>(json);
            if (persistedFilters == null)
                return Array.Empty<FilterCondition>();

            var filters = new List<FilterCondition>();
            foreach (var pf in persistedFilters)
            {
                if (Enum.TryParse<FilterOperator>(pf.Operator, out var op))
                {
                    filters.Add(new FilterCondition
                    {
                        PropertyName = pf.PropertyName,
                        Operator = op,
                        Value = pf.Value,
                        Enabled = pf.Enabled
                    });
                }
            }

            _logger.LogDebug("Loaded {FilterCount} filters from state", filters.Count);
            return filters.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load filters state");
            return Array.Empty<FilterCondition>();
        }
    }

    public async Task SaveSelectedColumnsAsync(IEnumerable<string> selectedColumns, CancellationToken cancellationToken = default)
    {
        try
        {
            var columns = selectedColumns?.ToList() ?? new List<string>();
            var json = JsonSerializer.Serialize(columns, JsonOptions);
            
            await File.WriteAllTextAsync(_columnsStatePath, json, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
            
            _logger.LogDebug("Selected columns saved: {ColumnCount} columns", columns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save selected columns state");
        }
    }

    public async Task<IReadOnlyList<string>> LoadSelectedColumnsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_columnsStatePath))
                return Array.Empty<string>();

            var json = await File.ReadAllTextAsync(_columnsStatePath, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
            
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<string>();

            var columns = JsonSerializer.Deserialize<List<string>>(json);
            
            _logger.LogDebug("Loaded {ColumnCount} selected columns from state", columns?.Count ?? 0);
            if (columns == null)
            {
                return Array.Empty<string>();
            }
            return columns.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load selected columns state");
            return Array.Empty<string>();
        }
    }

    public async Task ClearAllStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = new[]
            {
                DeleteFileIfExistsAsync(_gridStatePath, cancellationToken),
                DeleteFileIfExistsAsync(_filtersStatePath, cancellationToken),
                DeleteFileIfExistsAsync(_columnsStatePath, cancellationToken)
            };

            await Task.WhenAll(tasks).ConfigureAwait(false);
            
            _logger.LogInformation("All grid and filter state cleared");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear all state");
        }
    }

    private static async Task DeleteFileIfExistsAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(filePath))
            {
                // File.Delete is not async, but we're in an async context
                await Task.Run(() => File.Delete(filePath), cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            // Ignore individual file deletion errors
        }
    }

    /// <summary>
    /// Helper class for JSON serialization of filter conditions
    /// </summary>
    private sealed class PersistedFilter
    {
        public string PropertyName { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }
        public bool Enabled { get; set; }
    }
}