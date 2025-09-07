using SacksDataLayer.FileProcessing.Configuration;
using SacksDataLayer.FileProcessing.Models;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SacksDataLayer.Services.Interfaces
{
    /// <summary>
    /// Service for file validation and reading operations
    /// </summary>
    public interface IFileValidationService
    {

        /// <summary>
        /// Reads file data from the specified path
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File data structure</returns>
        Task<FileData> ReadFileDataAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates file structure against supplier configuration
        /// </summary>
        /// <param name="fileData">File data to validate</param>
        /// <param name="supplierConfig">Supplier configuration for validation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File validation result</returns>
        FileValidationResult ValidateFileStructureAsync(
            FileData fileData, 
            SupplierConfiguration supplierConfig, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of file validation operations
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> ValidationWarnings { get; set; } = new();
        public List<string> FileHeaders { get; set; } = new();
        public int ColumnCount { get; set; }
        public int ExpectedColumnCount { get; set; }
    }
}
