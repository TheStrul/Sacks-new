using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Models;
using SacksDataLayer.Services.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SacksDataLayer.Services.Implementations
{
    /// <summary>
    /// Service implementation for file validation and reading operations
    /// </summary>
    public class FileValidationService : IFileValidationService
    {
        private readonly IFileDataReader _fileDataReader;
        private readonly ILogger<FileValidationService> _logger;

        public FileValidationService(
            IFileDataReader fileDataReader,
            ILogger<FileValidationService> logger)
        {
            _fileDataReader = fileDataReader ?? throw new ArgumentNullException(nameof(fileDataReader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates if a file exists at the specified path
        /// </summary>
        public Task<bool> ValidateFileExistsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var exists = File.Exists(filePath);
                if (!exists)
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                }
                return Task.FromResult(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file existence for path: {FilePath}", filePath);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Reads file data from the specified path
        /// </summary>
        public async Task<FileData> ReadFileDataAsync(string filePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                _logger.LogInformation("Reading file data from: {FilePath}", filePath);
                var fileData = await _fileDataReader.ReadFileAsync(filePath);
                _logger.LogInformation("Successfully read {RowCount} rows from file: {FileName}", 
                    fileData.DataRows?.Count ?? 0, Path.GetFileName(filePath));
                return fileData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file data from: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Validates file structure against supplier configuration
        /// Samples 10% of data rows and validates that 80% have expected column count
        /// </summary>
        public FileValidationResult ValidateFileStructureAsync(
            FileData fileData, 
            SupplierConfiguration supplierConfig, 
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fileData);
            ArgumentNullException.ThrowIfNull(supplierConfig);
            
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = new FileValidationResult();
            
            try
            {

                // Sample 10% of data rows starting from DataStartRowIndex + 1
                int dataStartIndex = supplierConfig.FileStructure!.DataStartRowIndex + 1;
                var totalDataRows = Math.Max(0, fileData.DataRows.Count - dataStartIndex);
                
                if (totalDataRows == 0)
                {
                    result.ValidationWarnings.Add("No data rows found after header");
                }
                else if (result.ExpectedColumnCount > 0)
                {
                    var sampleSize = Math.Max(1, totalDataRows / 10); // At least 1 row, up to 10%
                    var validRowCount = 0;
                    var sampledRowCount = 0;

                    for (int i = dataStartIndex; i < fileData.DataRows.Count && sampledRowCount < sampleSize; i++)
                    {
                        var row = fileData.DataRows[i];
                        sampledRowCount++;
                        
                        if (row.Cells.Count == result.ExpectedColumnCount)
                        {
                            validRowCount++;
                        }
                    }

                    var validPercentage = (double)validRowCount / sampledRowCount * 100;
                    
                    if (validPercentage >= 80.0)
                    {
                        _logger.LogInformation("Column structure validation passed: {ValidPercentage:F1}% of sampled rows have expected column count ({Expected})", 
                            validPercentage, result.ExpectedColumnCount);
                    }
                    else
                    {
                        var warning = $"Column structure warning: Only {validPercentage:F1}% of sampled rows have expected column count ({result.ExpectedColumnCount}). Sampled {sampledRowCount} of {totalDataRows} data rows.";
                        result.ValidationWarnings.Add(warning);
                        _logger.LogWarning("Column structure validation warning: {Warning}", warning);
                    }
                }

                result.IsValid = !result.ValidationErrors.Any();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file structure");
                result.ValidationErrors.Add($"Validation error: {ex.Message}");
                result.IsValid = false;
                return result;
            }
        }

    }
}
