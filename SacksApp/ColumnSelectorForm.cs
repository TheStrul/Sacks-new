using ModernWinForms.Controls;
using ModernWinForms.Theming;

namespace SacksApp
{
    /// <summary>
    /// Modal dialog for selecting visible columns.
    /// ZERO TOLERANCE: All controls are Modern, all parameters validated.
    /// </summary>
    internal sealed class ColumnSelectorForm : Form
    {
        private readonly ModernFlowLayoutPanel _checkBoxContainer;
        private readonly ModernPanel _scrollPanel;
        private readonly ModernButton _okButton;
        private readonly ModernButton _cancelButton;
        private readonly ModernButton _selectAllButton;
        private readonly ModernButton _deselectAllButton;
        private readonly ModernPanel _buttonPanel;
        private readonly ModernPanel _topButtonPanel;
        private readonly List<ModernCheckBox> _checkBoxes = new();

        /// <summary>
        /// Gets the selected column names.
        /// ZERO TOLERANCE: Never returns null, always returns valid collection.
        /// </summary>
        public IEnumerable<string> SelectedColumns => _checkBoxes
            .Where(cb => cb.Checked)
            .Select(cb => cb.Tag as string ?? throw new InvalidOperationException("CheckBox Tag must be a string"))
            .ToList();

        /// <summary>
        /// Initializes a new instance of ColumnSelectorForm.
        /// ZERO TOLERANCE: Parameters must not be null.
        /// </summary>
        /// <param name="allColumns">All available columns. Must not be null.</param>
        /// <param name="checkedColumns">Initially checked columns. Can be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown when allColumns is null.</exception>
        public ColumnSelectorForm(IEnumerable<string> allColumns, IEnumerable<string>? checkedColumns)
        {
            // ZERO TOLERANCE: Validate required parameters
            ArgumentNullException.ThrowIfNull(allColumns);

            // Form properties
            Text = "Show / Hide Columns";
            ClientSize = new Size(400, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            // Apply theme
            ThemeManager.ApplyTheme(this);

            // Create scroll panel with auto-scroll
            _scrollPanel = new ModernPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            // Create flow layout for checkboxes
            _checkBoxContainer = new ModernFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(5)
            };

            // Top button panel for Select All / Deselect All
            _topButtonPanel = new ModernPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10)
            };

            _selectAllButton = new ModernButton
            {
                Text = "Select All",
                Width = 120,
                Height = 36,
                Left = 10,
                Top = 7
            };
            _selectAllButton.Click += SelectAllButton_Click;

            _deselectAllButton = new ModernButton
            {
                Text = "Deselect All",
                Width = 120,
                Height = 36,
                Left = 140,
                Top = 7
            };
            _deselectAllButton.Click += DeselectAllButton_Click;

            _topButtonPanel.Controls.Add(_selectAllButton);
            _topButtonPanel.Controls.Add(_deselectAllButton);

            // Bottom button panel for OK/Cancel
            _buttonPanel = new ModernPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10)
            };

            _okButton = new ModernButton
            {
                Text = "OK",
                Width = 120,
                Height = 36,
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Right,
                Margin = new Padding(5, 0, 0, 0)
            };

            _cancelButton = new ModernButton
            {
                Text = "Cancel",
                Width = 120,
                Height = 36,
                DialogResult = DialogResult.Cancel,
                Dock = DockStyle.Right,
                Margin = new Padding(5, 0, 0, 0)
            };

            _buttonPanel.Controls.Add(_okButton);
            _buttonPanel.Controls.Add(_cancelButton);

            // Add checkboxes container to scroll panel
            _scrollPanel.Controls.Add(_checkBoxContainer);

            // Add panels to form
            Controls.Add(_scrollPanel);
            Controls.Add(_topButtonPanel);
            Controls.Add(_buttonPanel);

            // Set accept/cancel buttons
            AcceptButton = _okButton;
            CancelButton = _cancelButton;

            // Populate checkboxes
            PopulateCheckBoxes(allColumns, checkedColumns);
        }

        /// <summary>
        /// Populates the checkbox list with column names.
        /// ZERO TOLERANCE: Validates all column names are non-null/whitespace.
        /// </summary>
        private void PopulateCheckBoxes(IEnumerable<string> allColumns, IEnumerable<string>? checkedColumns)
        {
            var checkedSet = new HashSet<string>(
                checkedColumns ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            var columnsList = allColumns.ToList();

            // ZERO TOLERANCE: Validate all columns are non-empty
            if (columnsList.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("All column names must be non-null and non-whitespace.", nameof(allColumns));

            _checkBoxContainer.SuspendLayout();
            try
            {
                foreach (var column in columnsList.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
                {
                    var checkBox = new ModernCheckBox
                    {
                        Text = column,
                        Tag = column,
                        Checked = checkedSet.Contains(column),
                        AutoSize = true,
                        Margin = new Padding(5, 3, 5, 3)
                    };

                    _checkBoxes.Add(checkBox);
                    _checkBoxContainer.Controls.Add(checkBox);
                }
            }
            finally
            {
                _checkBoxContainer.ResumeLayout();
            }
        }

        /// <summary>
        /// Selects all checkboxes.
        /// </summary>
        private void SelectAllButton_Click(object? sender, EventArgs e)
        {
            _checkBoxContainer.SuspendLayout();
            try
            {
                foreach (var checkBox in _checkBoxes)
                {
                    checkBox.Checked = true;
                }
            }
            finally
            {
                _checkBoxContainer.ResumeLayout();
            }
        }

        /// <summary>
        /// Deselects all checkboxes.
        /// </summary>
        private void DeselectAllButton_Click(object? sender, EventArgs e)
        {
            _checkBoxContainer.SuspendLayout();
            try
            {
                foreach (var checkBox in _checkBoxes)
                {
                    checkBox.Checked = false;
                }
            }
            finally
            {
                _checkBoxContainer.ResumeLayout();
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose all checkbox controls
                foreach (var checkBox in _checkBoxes)
                {
                    checkBox?.Dispose();
                }
                _checkBoxes.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
