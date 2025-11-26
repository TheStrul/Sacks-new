using ModernWinForms.Controls;
using ModernWinForms.Theming;

namespace SacksApp
{
    /// <summary>
    /// Dialog for adding entries to lookup tables.
    /// ZERO TOLERANCE: All controls are Modern, all parameters validated.
    /// </summary>
    public partial class AddToLookupForm : Form
    {
        private readonly string _tableName;

        /// <summary>
        /// Gets the trimmed key text. ZERO TOLERANCE: Never returns null.
        /// </summary>
        public string KeyText => _keyTextBox.Text.Trim();

        /// <summary>
        /// Gets the trimmed value text. ZERO TOLERANCE: Never returns null.
        /// </summary>
        public string ValueText => _valueTextBox.Text.Trim();

        /// <summary>
        /// Initializes a new instance of AddToLookupForm.
        /// ZERO TOLERANCE: tableName must not be null or whitespace.
        /// </summary>
        /// <param name="tableName">The name of the lookup table. Cannot be null or whitespace.</param>
        /// <param name="prefillKey">Optional key to prefill.</param>
        /// <param name="prefillValue">Optional value to prefill.</param>
        /// <exception cref="ArgumentException">Thrown when tableName is null or whitespace.</exception>
        public AddToLookupForm(string tableName, string? prefillKey = null, string? prefillValue = null)
        {
            // ZERO TOLERANCE: Validate required parameters
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));

            _tableName = tableName;

            InitializeComponent();

            Text = $"Add to lookup: {_tableName}";

            // Apply theme to form
            ThemeManager.ApplyTheme(this);

            // Prefill if provided
            if (!string.IsNullOrWhiteSpace(prefillKey))
                _keyTextBox.Text = prefillKey;

            if (!string.IsNullOrWhiteSpace(prefillValue))
                _valueTextBox.Text = prefillValue;
        }

        /// <summary>
        /// Validates input and closes the dialog.
        /// ZERO TOLERANCE: Validates that key and value are not empty.
        /// </summary>
        private void OkButton_Click(object? sender, EventArgs e)
        {
            // ZERO TOLERANCE: Validate key
            if (string.IsNullOrWhiteSpace(KeyText))
            {
                CustomMessageBox.Show("Key cannot be empty", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                _keyTextBox.Focus();
                return;
            }

            // ZERO TOLERANCE: Validate value
            if (string.IsNullOrWhiteSpace(ValueText))
            {
                CustomMessageBox.Show("Value cannot be empty", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                _valueTextBox.Focus();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
