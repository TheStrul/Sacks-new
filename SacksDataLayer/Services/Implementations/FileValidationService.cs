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
        /// </summary>
        public async Task<FileValidationResult> ValidateFileStructureAsync(
            FileData fileData, 
            SupplierConfiguration supplierConfig, 
            CancellationToken cancellationToken = default)
        {
            if (fileData == null) throw new ArgumentNullException(nameof(fileData));
            if (supplierConfig == null) throw new ArgumentNullException(nameof(supplierConfig));
            
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = new FileValidationResult();
            
            try
            {
                // Extract headers
                var headerRowIndex = supplierConfig.Transformation?.HeaderRowIndex ?? 0;
                result.FileHeaders = await ExtractFileHeadersAsync(fileData, headerRowIndex, cancellationToken);
                result.ColumnCount = result.FileHeaders.Count;
                result.ExpectedColumnCount = supplierConfig.Validation?.ExpectedColumnCount ?? 0;

                // Validate column count if specified
                if (result.ExpectedColumnCount > 0)
                {
                    if (result.ColumnCount == result.ExpectedColumnCount)
                    {
                        _logger.LogInformation("Column count validation passed: Expected {Expected}, found {Actual}", 
                            result.ExpectedColumnCount, result.ColumnCount);
                    }
                    else
                    {
                        var error = $"Column count mismatch. Expected {result.ExpectedColumnCount}, found {result.ColumnCount}";
                        result.ValidationWarnings.Add(error);
                        _logger.LogWarning("Column count validation warning: {Error}", error);
                    }
                }

                // Check if header row exists
                if (!result.FileHeaders.Any())
                {
                    var error = "No header row found in file";
                    result.ValidationErrors.Add(error);
                    _logger.LogError("File structure validation error: {Error}", error);
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

        /// <summary>
        /// Extracts headers from file data
        /// </summary>
        public Task<List<string>> ExtractFileHeadersAsync(
            FileData fileData, 
            int headerRowIndex = 0, 
            CancellationToken cancellationToken = default)
        {
            if (fileData == null) throw new ArgumentNullException(nameof(fileData));
            
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var headerRow = fileData.DataRows?.Skip(headerRowIndex).FirstOrDefault(r => r.HasData);
                if (headerRow == null)
                {
                    _logger.LogWarning("No header row found at index {HeaderRowIndex}", headerRowIndex);
                    return Task.FromResult(new List<string>());
                }

                var headers = headerRow.Cells
                    .Select(c => c.Value?.Trim())
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .Cast<string>()
                    .ToList();

                _logger.LogInformation("Extracted {HeaderCount} headers from file", headers.Count);
                return Task.FromResult(headers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting file headers");
                return Task.FromResult(new List<string>());
            }
        }
    }
}
