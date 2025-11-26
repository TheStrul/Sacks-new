using ModernWinForms.Controls;
using ModernWinForms.Theming;

namespace SacksApp
{
    /// <summary>
    /// Dialog for adding entries to lookup tables.
    /// ZERO TOLERANCE: All controls are Modern, all parameters validated.
    /// </summary>
    public class AddToLookupForm : Form
    {
        private readonly string _tableName;
        private readonly ModernTextBox _keyTextBox;
        private readonly ModernTextBox _valueTextBox;
        private readonly ModernButton _okButton;
        private readonly ModernButton _cancelButton;
        private readonly ModernLabel _lblKey;
        private readonly ModernLabel _lblValue;
        private readonly ModernTableLayoutPanel _layoutPanel;

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

            // Form properties
            ClientSize = new Size(420, 180);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Text = $"Add to lookup: {_tableName}";

            // Apply theme to form
            ThemeManager.ApplyTheme(this);

            // Create Modern controls
            _lblKey = new ModernLabel 
            { 
                Text = "Key:", 
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };

            _keyTextBox = new ModernTextBox 
            { 
                Dock = DockStyle.Fill,
                PlaceholderText = "Enter key..."
            };

            _lblValue = new ModernLabel 
            { 
                Text = "Value:", 
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };

            _valueTextBox = new ModernTextBox 
            { 
                Dock = DockStyle.Fill,
                PlaceholderText = "Enter value..."
            };

            _okButton = new ModernButton 
            { 
                Text = "OK", 
                Width = 100,
                Height = 36,
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Right
            };

            _cancelButton = new ModernButton 
            { 
                Text = "Cancel", 
                Width = 100,
                Height = 36,
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Right
            };

            // Create layout
            _layoutPanel = new ModernTableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(15)
            };

            // Configure column styles
            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Configure row styles
            _layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Key row
            _layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Value row
            _layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Button row

            // Add controls to layout
            _layoutPanel.Controls.Add(_lblKey, 0, 0);
            _layoutPanel.Controls.Add(_keyTextBox, 1, 0);
            _layoutPanel.Controls.Add(_lblValue, 0, 1);
            _layoutPanel.Controls.Add(_valueTextBox, 1, 1);

            // Button panel
            var buttonPanel = new ModernFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };
            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_okButton);

            _layoutPanel.Controls.Add(buttonPanel, 0, 2);
            _layoutPanel.SetColumnSpan(buttonPanel, 2);

            Controls.Add(_layoutPanel);

            // Wire events
            _okButton.Click += OkButton_Click;

            // Set accept/cancel buttons
            AcceptButton = _okButton;
            CancelButton = _cancelButton;

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
