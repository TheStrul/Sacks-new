using ModernWinForms.Controls;
using ModernWinForms.Theming;

namespace SacksApp
{
    /// <summary>
    /// Modal dialog for selecting visible columns.
    /// ZERO TOLERANCE: All controls are Modern, all parameters validated.
    /// </summary>
    internal sealed partial class ColumnSelectorForm : Form
    {
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

            InitializeComponent();

            // Apply theme
            ThemeManager.ApplyTheme(this);

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
    }
}
