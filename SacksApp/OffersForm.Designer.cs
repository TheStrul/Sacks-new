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
            
            _comboSuppliers = new ModernComboBox();
            _btnRefresh = new ModernButton();
            _btnAdd = new ModernButton();
            _btnEdit = new ModernButton();
            _btnDelete = new ModernButton();
            _grid = new ModernDataGridView();
            _bs = new System.Windows.Forms.BindingSource(components);
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
            _comboSuppliers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _comboSuppliers.Width = 280;
            _comboSuppliers.SelectedIndexChanged += ComboSuppliers_SelectedIndexChanged;
            
            // 
            // _btnRefresh
            // 
            _btnRefresh.Text = "Refresh";
            _btnRefresh.Click += BtnRefresh_Click;
            
            // 
            // _btnAdd
            // 
            _btnAdd.Text = "New";
            _btnAdd.Click += BtnAdd_Click;
            
            // 
            // _btnEdit
            // 
            _btnEdit.Text = "Edit";
            _btnEdit.Click += BtnEdit_Click;
            
            // 
            // _btnDelete
            // 
            _btnDelete.Text = "Delete";
            _btnDelete.Click += BtnDelete_Click;
            
            // 
            // _grid
            // 
            _grid.Dock = System.Windows.Forms.DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.DataSource = _bs;
            _grid.AutoGenerateColumns = false;
            _grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                new System.Windows.Forms.DataGridViewTextBoxColumn 
                { 
                    DataPropertyName = "Id", 
                    HeaderText = "ID", 
                    Width = 60 
                },
                new System.Windows.Forms.DataGridViewTextBoxColumn 
                { 
                    DataPropertyName = "OfferName", 
                    HeaderText = "Offer Name", 
                    Width = 220 
                },
                new System.Windows.Forms.DataGridViewTextBoxColumn 
                { 
                    DataPropertyName = "Currency", 
                    HeaderText = "Currency", 
                    Width = 80 
                },
                new System.Windows.Forms.DataGridViewTextBoxColumn 
                { 
                    DataPropertyName = "Description", 
                    HeaderText = "Description", 
                    AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill 
                },
                new System.Windows.Forms.DataGridViewTextBoxColumn 
                { 
                    DataPropertyName = "CreatedAt", 
                    HeaderText = "Created (UTC)", 
                    Width = 150 
                },
                new System.Windows.Forms.DataGridViewTextBoxColumn 
                { 
                    DataPropertyName = "ModifiedAt", 
                    HeaderText = "Modified (UTC)", 
                    Width = 150 
                }
            });
            
            // 
            // _lblSupplier
            // 
            _lblSupplier.Text = "Supplier:";
            _lblSupplier.AutoSize = true;
            _lblSupplier.Padding = new System.Windows.Forms.Padding(0, 8, 8, 0);
            
            // 
            // _spacer
            // 
            _spacer.Width = 16;
            _spacer.Text = "";
            
            // 
            // _topPanel
            // 
            _topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            _topPanel.Height = 40;
            _topPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            _topPanel.Padding = new System.Windows.Forms.Padding(8);
            _topPanel.Controls.Add(_lblSupplier);
            _topPanel.Controls.Add(_comboSuppliers);
            _topPanel.Controls.Add(_btnRefresh);
            _topPanel.Controls.Add(_spacer);
            _topPanel.Controls.Add(_btnAdd);
            _topPanel.Controls.Add(_btnEdit);
            _topPanel.Controls.Add(_btnDelete);
            
            // 
            // OffersForm
            // 
            ClientSize = new System.Drawing.Size(900, 600);
            Controls.Add(_grid);
            Controls.Add(_topPanel);
            Text = "Offers";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
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
