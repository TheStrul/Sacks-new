using System.Data;

namespace Sacks.LogicLayer.Services.Interfaces
{
    /// <summary>
    /// Service for exporting data to various formats
    /// </summary>
    public interface IDataExportService
    {
        /// <summary>
        /// Exports data table to Excel file
        /// </summary>
        /// <param name="data">Data to export</param>
        /// <param name="filePath">Output file path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ExportToExcelAsync(
            DataTable data, 
            string filePath, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports data table to CSV file
        /// </summary>
        /// <param name="data">Data to export</param>
        /// <param name="filePath">Output file path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ExportToCsvAsync(
            DataTable data, 
            string filePath, 
            CancellationToken cancellationToken = default);
    }
}
