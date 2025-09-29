using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace SacksApp
{
    // Simple modal dialog hosting a checked-list of columns
    internal sealed partial class ColumnSelectorForm : Form
    {
        // Controls are defined and initialized in the designer partial

        public IEnumerable<string> SelectedColumns => list.CheckedItems.Cast<string>();

        public ColumnSelectorForm(IEnumerable<string> allColumns, IEnumerable<string>? checkedColumns)
        {
            InitializeComponent();

            list.Items.AddRange(allColumns.ToArray());
            var checkedSet = new HashSet<string>(checkedColumns ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < list.Items.Count; i++)
            {
                var v = list.Items[i] as string ?? string.Empty;
                list.SetItemChecked(i, checkedSet.Contains(v));
            }
        }

        private void ColumnSelectorForm_Load(object sender, EventArgs e)
        {
            Trace.AutoFlush = true;
        }
    }
}
