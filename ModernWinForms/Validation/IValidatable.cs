namespace ModernWinForms.Validation;

/// <summary>
/// Validation state for controls.
/// </summary>
public enum ValidationState
{
    /// <summary>
    /// Normal state, no validation applied.
    /// </summary>
    None,

    /// <summary>
    /// Validation passed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Validation warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Validation error.
    /// </summary>
    Error
}

/// <summary>
/// Provides validation support for modern controls.
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// Gets or sets the current validation state.
    /// </summary>
    ValidationState ValidationState { get; set; }

    /// <summary>
    /// Gets or sets the validation message.
    /// </summary>
    string ValidationMessage { get; set; }
}
