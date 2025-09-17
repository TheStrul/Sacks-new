namespace SacksDataLayer.FileProcessing.Normalizers
{
    /// <summary>
    /// Result of column validation indicating whether validation passed and what action to take
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public bool SkipEntireRow { get; set; }
        public string? ErrorMessage { get; set; }

        public static ValidationResult Valid() => new() { IsValid = true };
        public static ValidationResult Invalid(bool skipEntireRow = false, string? errorMessage = null) =>
            new() { IsValid = false, SkipEntireRow = skipEntireRow, ErrorMessage = errorMessage };
    }
}
