namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing
{
    using System.Collections.ObjectModel;
    using System.Text;

    public class RowData
    {
        public int Index { get; init; }

        public Collection<CellData> Cells { get; init; } = new Collection<CellData>();

        /// <summary>
        /// Gets a value indicating whether this row contains meaningful data
        /// Returns false if the row is empty, contains only whitespace, or has no cells
        /// </summary>
        public bool HasData
        {
            get
            {
                // Check if there are any cells
                if (Cells == null || Cells.Count == 0)
                    return false;

                // Check if any cell contains non-whitespace data
                foreach (var cell in Cells)
                {
                    if (cell != null && !string.IsNullOrWhiteSpace(cell.Value))
                    {
                        return true;
                    }
                }

                // All cells are empty or contain only whitespace
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
            for (int i = 0; i < Cells.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");

                var cellValue = Cells[i]?.Value ?? string.Empty;
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