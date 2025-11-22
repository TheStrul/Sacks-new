namespace Sacks.Core.Services.Models;

/// <summary>
/// Result of a validation operation
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Error(string errorMessage) => new() { IsValid = false, ErrorMessage = errorMessage };
}
