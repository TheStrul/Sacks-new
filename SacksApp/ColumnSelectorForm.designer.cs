using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SacksApp
{
    internal sealed partial class ColumnSelectorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = new Container();

        private CheckedListBox list;
        private Button ok;
        private Button cancel;

        private void InitializeComponent()
        {
            list = new CheckedListBox();
            ok = new Button();
            cancel = new Button();
            panel = new Panel();
            panel.SuspendLayout();
            SuspendLayout();
            // 
            // list
            // 
            list.CheckOnClick = true;
            list.Dock = DockStyle.Fill;
            list.Location = new Point(0, 0);
            list.Name = "list";
            list.Size = new Size(303, 397);
            list.TabIndex = 0;
            list.Font = new Font("Segoe UI", 10F);
            // 
            // ok
            // 
            ok.DialogResult = DialogResult.OK;
            ok.Dock = DockStyle.Right;
            ok.Location = new Point(183, 0);
            ok.Name = "ok";
            ok.Size = new Size(120, 40);
            ok.TabIndex = 1;
            ok.Text = "OK";
            ok.FlatStyle = FlatStyle.Flat;
            ok.FlatAppearance.BorderSize = 0;
            // 
            // cancel
            // 
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Dock = DockStyle.Left;
            cancel.Location = new Point(0, 0);
            cancel.Name = "cancel";
            cancel.Size = new Size(120, 40);
            cancel.TabIndex = 0;
            cancel.Text = "Cancel";
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.FlatAppearance.BorderSize = 0;
            // 
            // panel
            // 
            panel.Controls.Add(cancel);
            panel.Controls.Add(ok);
            panel.Dock = DockStyle.Bottom;
            panel.Location = new Point(0, 397);
            panel.Name = "panel";
            panel.Size = new Size(303, 40);
            panel.TabIndex = 1;
            // 
            // ColumnSelectorForm
            // 
            AcceptButton = ok;
            CancelButton = cancel;
            ClientSize = new Size(303, 437);
            Controls.Add(list);
            Controls.Add(panel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ColumnSelectorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Show / Hide Columns";
            BackColor = Color.FromArgb(250, 250, 252);
            Load += ColumnSelectorForm_Load;
            panel.ResumeLayout(false);
            ResumeLayout(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        private Panel panel;
    }
}
