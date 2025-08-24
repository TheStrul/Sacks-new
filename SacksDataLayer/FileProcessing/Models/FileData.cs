namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing
{
    using System;
    using System.Collections.ObjectModel;

    public class FileData
    {
        public string FilePath { get; init; }
        public Collection<RowData> dataRows { get; init; } = new Collection<RowData>();
        public int RowCount { get; set; }

        public FileData(string fullPath) 
        {
            FilePath = fullPath;
            dataRows = new Collection<RowData>();
        }

        public RowData? GetRow(int i)
        {
            if (dataRows.Count == 0 || i < 0 || i >= dataRows.Count)
            {
                return null; // Return null if no rows or index is out of bounds
            }
            return dataRows[i];
        }
    }
}