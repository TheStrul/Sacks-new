namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing
{
    using System.Text;

    public class RowData
    {
        public int Index { get; init; }

        /// <summary>
        /// Column values using Excel column letters as keys (A, B, C, etc.)
        /// </summary>
        public Dictionary<string, string> Cells { get; init; } = new();

        /// <summary>
        /// Indicates if this row was detected as a subtitle row
        /// </summary>
        public bool IsSubtitleRow { get; set; }

        /// <summary>
        /// The subtitle detection rule that matched this row (if any)
        /// </summary>
        public string? SubtitleRuleName { get; set; }

        /// <summary>
        /// Extracted subtitle data that should be applied to subsequent rows
        /// </summary>
        public Dictionary<string, object?> SubtitleData { get; set; } = new();

        /// <summary>
        /// Gets a value indicating whether this row contains meaningful data
        /// Returns false if the row is empty, contains only whitespace, or has no columns
        /// </summary>
        public bool HasData
        {
            get
            {
                // Check if there are any columns
                if (Cells == null || Cells.Count == 0)
                    return false;

                // Check if any column contains non-whitespace data
                foreach (var value in Cells.Values)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return true;
                    }
                }

                // All columns are empty or contain only whitespace
                return false;
            }
        }

        public RowData(int index)
        {
            Index = index;
        }

        /// <summary>
        /// Returns a string representation of the row data as comma-separated values
        /// Used for debugging and original source tracking
        /// </summary>
        public override string ToString()
        {
            if (Cells == null || Cells.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            var isFirst = true;
            
            // Sort by column letter to maintain A, B, C order
            foreach (var kvp in Cells.OrderBy(k => k.Key))
            {
                if (!isFirst)
                    sb.Append(",");
                isFirst = false;

                var cellValue = kvp.Value ?? string.Empty;
                // Escape commas and quotes in cell values for CSV format
                if (cellValue.Contains(",") || cellValue.Contains("\""))
                {
                    sb.Append("\"").Append(cellValue.Replace("\"", "\"\"")).Append("\"");
                }
                else
                {
                    sb.Append(cellValue);
                }
            }
            return sb.ToString();
        }
    }
}