namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing
{ 
    public class CellData
    {

        public int Index { get; init; }

        public string Value { get; init; } = string.Empty;

        public CellData(int index, string value)
        {
            Index = index;
            Value = value ?? string.Empty; // Ensure Value is never null
        }
    }
}
