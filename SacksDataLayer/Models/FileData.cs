namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing
{
    using System;
    using System.Collections.Generic;

    public class FileData
    {
        public string FileName { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public List<RowData> DataRows { get; init; } = new List<RowData>();
        public int RowCount { get; set; }

        public FileData(string fullPath) 
        {
            FilePath = fullPath;
            FileName = Path.GetFileName(fullPath);
            DataRows = new List<RowData>();
        }

        public RowData? GetRow(int i)
        {
            if (DataRows.Count == 0 || i < 0 || i >= DataRows.Count)
            {
                return null; // Return null if no rows or index is out of bounds
            }
            return DataRows[i];
        }
    }
}