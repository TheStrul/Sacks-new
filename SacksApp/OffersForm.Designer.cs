using ModernWinForms.Controls;

namespace SacksApp
{
    partial class OffersForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            _comboSuppliers = new ModernComboBox();
            _btnRefresh = new ModernButton();
            _btnAdd = new ModernButton();
            _btnEdit = new ModernButton();
            _btnDelete = new ModernButton();
            _grid = new ModernDataGridView();
            _bs = new BindingSource(components);
            _topPanel = new ModernFlowLayoutPanel();
            _lblSupplier = new ModernLabel();
            _spacer = new ModernLabel();
            ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_bs).BeginInit();
            _topPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _comboSuppliers
            // 
            _comboSuppliers.BackColor = Color.FromArgb(255, 255, 255);
            _comboSuppliers.DrawMode = DrawMode.OwnerDrawFixed;
            _comboSuppliers.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboSuppliers.FlatStyle = FlatStyle.Flat;
            _comboSuppliers.Font = new Font("Segoe UI", 9F);
            _comboSuppliers.ForeColor = Color.FromArgb(50, 49, 48);
            _comboSuppliers.Location = new Point(78, 11);
            _comboSuppliers.Name = "_comboSuppliers";
            _comboSuppliers.Size = new Size(280, 24);
            _comboSuppliers.TabIndex = 1;
            _comboSuppliers.SelectedIndexChanged += ComboSuppliers_SelectedIndexChanged;
            // 
            // _btnRefresh
            // 
            _btnRefresh.BackColor = Color.Transparent;
            _btnRefresh.FlatStyle = FlatStyle.Flat;
            _btnRefresh.Location = new Point(364, 11);
            _btnRefresh.Name = "_btnRefresh";
            _btnRefresh.Size = new Size(75, 23);
            _btnRefresh.TabIndex = 2;
            _btnRefresh.Text = "Refresh";
            _btnRefresh.UseVisualStyleBackColor = false;
            _btnRefresh.Click += BtnRefresh_Click;
            // 
            // _btnAdd
            // 
            _btnAdd.BackColor = Color.Transparent;
            _btnAdd.FlatStyle = FlatStyle.Flat;
            _btnAdd.Location = new Point(467, 11);
            _btnAdd.Name = "_btnAdd";
            _btnAdd.Size = new Size(75, 23);
            _btnAdd.TabIndex = 4;
            _btnAdd.Text = "New";
            _btnAdd.UseVisualStyleBackColor = false;
            _btnAdd.Click += BtnAdd_Click;
            // 
            // _btnEdit
            // 
            _btnEdit.BackColor = Color.Transparent;
            _btnEdit.FlatStyle = FlatStyle.Flat;
            _btnEdit.Location = new Point(548, 11);
            _btnEdit.Name = "_btnEdit";
            _btnEdit.Size = new Size(75, 23);
            _btnEdit.TabIndex = 5;
            _btnEdit.Text = "Edit";
            _btnEdit.UseVisualStyleBackColor = false;
            _btnEdit.Click += BtnEdit_Click;
            // 
            // _btnDelete
            // 
            _btnDelete.BackColor = Color.Transparent;
            _btnDelete.FlatStyle = FlatStyle.Flat;
            _btnDelete.Location = new Point(629, 11);
            _btnDelete.Name = "_btnDelete";
            _btnDelete.Size = new Size(75, 23);
            _btnDelete.TabIndex = 6;
            _btnDelete.Text = "Delete";
            _btnDelete.UseVisualStyleBackColor = false;
            _btnDelete.Click += BtnDelete_Click;
            // 
            // _grid
            // 
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AutoGenerateColumns = false;
            _grid.BackgroundColor = Color.FromArgb(255, 255, 255);
            _grid.DataSource = _bs;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = Color.FromArgb(50, 49, 48);
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            _grid.DefaultCellStyle = dataGridViewCellStyle1;
            _grid.Dock = DockStyle.Fill;
            _grid.Font = new Font("Segoe UI", 9F);
            _grid.GridColor = Color.FromArgb(138, 136, 134);
            _grid.Location = new Point(0, 40);
            _grid.MultiSelect = false;
            _grid.Name = "_grid";
            _grid.ReadOnly = true;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.Size = new Size(900, 560);
            _grid.TabIndex = 0;
            // 
            // _topPanel
            // 
            _topPanel.BackColor = Color.FromArgb(243, 243, 243);
            _topPanel.Controls.Add(_lblSupplier);
            _topPanel.Controls.Add(_comboSuppliers);
            _topPanel.Controls.Add(_btnRefresh);
            _topPanel.Controls.Add(_spacer);
            _topPanel.Controls.Add(_btnAdd);
            _topPanel.Controls.Add(_btnEdit);
            _topPanel.Controls.Add(_btnDelete);
            _topPanel.Dock = DockStyle.Top;
            _topPanel.ForeColor = Color.FromArgb(50, 49, 48);
            _topPanel.Location = new Point(0, 0);
            _topPanel.Name = "_topPanel";
            _topPanel.Padding = new Padding(8);
            _topPanel.Size = new Size(900, 40);
            _topPanel.TabIndex = 1;
            // 
            // _lblSupplier
            // 
            _lblSupplier.AutoSize = true;
            _lblSupplier.BackColor = Color.FromArgb(243, 243, 243);
            _lblSupplier.Font = new Font("Segoe UI", 9F);
            _lblSupplier.ForeColor = Color.FromArgb(50, 49, 48);
            _lblSupplier.Location = new Point(11, 8);
            _lblSupplier.Name = "_lblSupplier";
            _lblSupplier.Padding = new Padding(0, 8, 8, 0);
            _lblSupplier.Size = new Size(61, 23);
            _lblSupplier.TabIndex = 0;
            _lblSupplier.Text = "Supplier:";
            // 
            // _spacer
            // 
            _spacer.BackColor = Color.FromArgb(243, 243, 243);
            _spacer.Font = new Font("Segoe UI", 9F);
            _spacer.ForeColor = Color.FromArgb(50, 49, 48);
            _spacer.Location = new Point(445, 8);
            _spacer.Name = "_spacer";
            _spacer.Size = new Size(16, 23);
            _spacer.TabIndex = 3;
            // 
            // OffersForm
            // 
            ClientSize = new Size(900, 600);
            Controls.Add(_grid);
            Controls.Add(_topPanel);
            Name = "OffersForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Offers";
            Load += OffersForm_Load;
            ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
            ((System.ComponentModel.ISupportInitialize)_bs).EndInit();
            _topPanel.ResumeLayout(false);
            _topPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private ModernComboBox _comboSuppliers;
        private ModernButton _btnRefresh;
        private ModernButton _btnAdd;
        private ModernButton _btnEdit;
        private ModernButton _btnDelete;
        private ModernDataGridView _grid;
        private System.Windows.Forms.BindingSource _bs;
        private ModernFlowLayoutPanel _topPanel;
        private ModernLabel _lblSupplier;
        private ModernLabel _spacer;
    }
}
