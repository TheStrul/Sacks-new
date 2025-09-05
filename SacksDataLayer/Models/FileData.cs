namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing
{
    using System;
    using System.Collections.ObjectModel;

    public class FileData
    {
        public string FileName { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public Collection<RowData> DataRows { get; init; } = new Collection<RowData>();
        public int RowCount { get; set; }

        public FileData() { }

        public FileData(string fileName, IEnumerable<RowData> dataRows)
        {
            FileName = fileName;
            DataRows = new Collection<RowData>(dataRows.ToList());
            RowCount = DataRows.Count;
        }

        public FileData(string fullPath) 
        {
            FilePath = fullPath;
            DataRows = new Collection<RowData>();
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