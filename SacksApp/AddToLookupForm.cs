namespace SacksApp
{
    public class AddToLookupForm : Form
    {
        private readonly string _tableName;
        private TextBox? keyTextBox;
        private TextBox? valueTextBox;
        private Button? okButton;
        private Button? cancelButton;

        public string KeyText => keyTextBox?.Text.Trim() ?? string.Empty;
        public string ValueText => valueTextBox?.Text.Trim() ?? string.Empty;

        public AddToLookupForm(string tableName, string? prefillKey = null, string? prefillValue = null)
        {
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            InitializeComponent();
            this.Text = $"Add to lookup: {_tableName}";
            if (!string.IsNullOrEmpty(prefillKey)) keyTextBox!.Text = prefillKey;
            if (!string.IsNullOrEmpty(prefillValue)) valueTextBox!.Text = prefillValue;
        }

        private void InitializeComponent()
        {
            this.ClientSize = new Size(420, 160);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            var lblKey = new Label { Text = "Key:", Left = 12, Top = 15, Width = 50 };
            keyTextBox = new TextBox { Left = 70, Top = 12, Width = 330 }; 

            var lblValue = new Label { Text = "Value:", Left = 12, Top = 50, Width = 50 };
            valueTextBox = new TextBox { Left = 70, Top = 47, Width = 330 };

            okButton = new Button { Text = "OK", Left = 230, Width = 80, Top = 100, DialogResult = DialogResult.OK };
            cancelButton = new Button { Text = "Cancel", Left = 320, Width = 80, Top = 100, DialogResult = DialogResult.Cancel };

            okButton.Click += OkButton_Click;

            this.Controls.Add(lblKey);
            this.Controls.Add(keyTextBox);
            this.Controls.Add(lblValue);
            this.Controls.Add(valueTextBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(KeyText))
            {
                CustomMessageBox.Show("Key cannot be empty", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
            if (string.IsNullOrWhiteSpace(ValueText))
            {
                CustomMessageBox.Show("Value cannot be empty", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
