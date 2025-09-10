namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing
{
    using System.Threading.Tasks;
    using System.IO;
    using System.Text;
    using ExcelDataReader;
    using Microsoft.Extensions.Logging;
    using SacksDataLayer.FileProcessing.Configuration;
    using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Services;

    public class FileDataReader : IFileDataReader
    {
        private readonly ILogger<FileDataReader>? _logger;
        private readonly SubtitleRowProcessor _subtitleProcessor;

        public FileDataReader(ILogger<FileDataReader>? logger = null, SubtitleRowProcessor? subtitleProcessor = null)
        {
            _logger = logger;
            _subtitleProcessor = subtitleProcessor ?? new SubtitleRowProcessor();
        }

        public async Task<FileData> ReadFileAsync(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(fullPath));

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {fullPath}");

            var fileExtension = Path.GetExtension(fullPath).ToLowerInvariant();
            var fileData = new FileData(fullPath);

            try
            {
                _logger?.LogDebug("Starting to read file: {FilePath} with extension: {FileExtension}", fullPath, fileExtension);

                switch (fileExtension)
                {
                    case ".csv":
                        await ReadCsvFileAsync(fileData, fullPath);
                        break;
                    case ".xlsx":
                    case ".xls":
                        await ReadExcelFileAsync(fileData, fullPath);
                        break;
                    default:
                        // Assume CSV format for unknown extensions
                        _logger?.LogWarning("Unknown file extension {FileExtension}, treating as CSV", fileExtension);
                        await ReadCsvFileAsync(fileData, fullPath);
                        break;
                }

                fileData.RowCount = fileData.DataRows.Count;
                return fileData;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to read file: {FilePath}", fullPath);
                throw new InvalidOperationException($"Failed to read file '{fullPath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads file with subtitle processing support
        /// </summary>
        public async Task<FileData> ReadFileWithSubtitleProcessingAsync(
            string fullPath, 
            SupplierConfiguration? supplierConfig = null, 
            CancellationToken cancellationToken = default)
        {
            var fileData = await ReadFileAsync(fullPath);

            // Apply subtitle processing if configuration is provided
            if (supplierConfig?.SubtitleHandling?.Enabled == true)
            {
                _logger?.LogDebug("Processing subtitles for file: {FilePath}", fullPath);
                fileData = _subtitleProcessor.ProcessSubtitleRows(fileData, supplierConfig, cancellationToken);
            }

            return fileData;
        }

        private async Task ReadCsvFileAsync(FileData fileData, string fullPath)
        {
            _logger?.LogDebug("Reading CSV file: {FilePath}", fullPath);
            
            using var reader = new StreamReader(fullPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            int rowIndex = 0;

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var rowData = new RowData(rowIndex);
                var cells = ParseCsvLine(line);

                for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
                {
                    var cellData = new CellData(cellIndex, cells[cellIndex]);
                    rowData.Cells.Add(cellData);
                }

                fileData.DataRows.Add(rowData);
                rowIndex++;
            }

            _logger?.LogDebug("Completed reading CSV file: {FilePath} with {RowCount} rows", fullPath, rowIndex);
        }

        private async Task ReadExcelFileAsync(FileData fileData, string fullPath)
        {
            _logger?.LogDebug("Reading Excel file: {FilePath}", fullPath);
            
            // Register the encoding provider for Excel files
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            // Read the data row by row instead of loading entire dataset into memory
            int rowIndex = 0;
            int worksheetIndex = 0;

            do
            {
                _logger?.LogDebug("Processing worksheet {WorksheetIndex} in file: {FilePath}", worksheetIndex + 1, fullPath);
                int rowsInCurrentWorksheet = 0;

                while (reader.Read())
                {
                    var rowData = new RowData(rowIndex);
                    
                    // Get the field count for this row
                    int fieldCount = reader.FieldCount;
                    
                    for (int cellIndex = 0; cellIndex < fieldCount; cellIndex++)
                    {
                        // Handle different data types properly
                        var cellValue = string.Empty;
                        
                        if (!reader.IsDBNull(cellIndex))
                        {
                            var rawValue = reader.GetValue(cellIndex);
                            cellValue = rawValue?.ToString()?.Trim() ?? string.Empty;
                        }
                        
                        var cellData = new CellData(cellIndex, cellValue);
                        rowData.Cells.Add(cellData);
                    }

                    fileData.DataRows.Add(rowData);
                    rowIndex++;
                    rowsInCurrentWorksheet++;
                }

                _logger?.LogDebug("Worksheet {WorksheetIndex} completed: {RowsProcessed} rows processed", 
                    worksheetIndex + 1, rowsInCurrentWorksheet);
                worksheetIndex++;
                
            } while (reader.NextResult()); // Move to next worksheet if any

            
            await Task.CompletedTask; // Make method async-compatible
        }

        private string[] ParseCsvLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return Array.Empty<string>();

            var cells = new List<string>();
            var currentCell = new StringBuilder();
            bool inQuotes = false;
            bool inEscapedQuote = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inEscapedQuote)
                {
                    if (c == '"')
                    {
                        // Double quote - add single quote to cell
                        currentCell.Append('"');
                        inEscapedQuote = false;
                    }
                    else
                    {
                        // End of quoted field
                        inQuotes = false;
                        inEscapedQuote = false;
                        // Process this character normally
                        i--; // Back up to process this character again
                    }
                }
                else if (c == '"')
                {
                    if (inQuotes)
                    {
                        // Check if next character is also a quote (escaped quote)
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            inEscapedQuote = true;
                        }
                        else
                        {
                            // End of quoted field
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        // Start of quoted field
                        inQuotes = true;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // Field separator - add current cell and start new one
                    cells.Add(currentCell.ToString().Trim());
                    currentCell.Clear();
                }
                else
                {
                    // Regular character
                    currentCell.Append(c);
                }
            }

            // Add the last cell
            cells.Add(currentCell.ToString().Trim());

            return cells.ToArray();
        }
    }
}
