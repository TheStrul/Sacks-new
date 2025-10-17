using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SacksDataLayer.Data;
using SacksDataLayer.Entities;

namespace SacksApp
{
    public sealed class OffersForm : Form
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<OffersForm> _logger;
        private readonly SacksDbContext _db;

        private readonly ComboBox _comboSuppliers = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
        private readonly Button _btnRefresh = new() { Text = "Refresh" };
        private readonly Button _btnAdd = new() { Text = "New" };
        private readonly Button _btnEdit = new() { Text = "Edit" };
        private readonly Button _btnDelete = new() { Text = "Delete" };
        private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
        private readonly BindingSource _bs = new();

        public OffersForm(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = _services.GetRequiredService<ILogger<OffersForm>>();
            _db = _services.GetRequiredService<SacksDbContext>();

            Text = "Offers";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8) };
            top.Controls.Add(new Label { Text = "Supplier:", AutoSize = true, Padding = new Padding(0, 8, 8, 0) });
            top.Controls.Add(_comboSuppliers);
            top.Controls.Add(_btnRefresh);
            top.Controls.Add(new Label { Width = 16 });
            top.Controls.Add(_btnAdd);
            top.Controls.Add(_btnEdit);
            top.Controls.Add(_btnDelete);

            Controls.Add(_grid);
            Controls.Add(top);

            _grid.DataSource = _bs;
            _grid.AutoGenerateColumns = false;
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Offer.Id), HeaderText = "ID", Width = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Offer.OfferName), HeaderText = "Offer Name", Width = 220 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Offer.Currency), HeaderText = "Currency", Width = 80 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Offer.Description), HeaderText = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Offer.CreatedAt), HeaderText = "Created (UTC)", Width = 150 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Offer.ModifiedAt), HeaderText = "Modified (UTC)", Width = 150 });

            Load += async (_, __) => await LoadSuppliersAsync(CancellationToken.None);
            _comboSuppliers.SelectedIndexChanged += async (_, __) => await ReloadOffersAsync(CancellationToken.None);
            _btnRefresh.Click += async (_, __) => await ReloadOffersAsync(CancellationToken.None);
            _btnAdd.Click += async (_, __) => await AddOfferAsync(CancellationToken.None);
            _btnEdit.Click += async (_, __) => await EditSelectedOfferAsync(CancellationToken.None);
            _btnDelete.Click += async (_, __) => await DeleteSelectedOfferAsync(CancellationToken.None);
        }

        private sealed record SupplierItem(int Id, string Name)
        {
            public override string ToString() => Name;
        }

        private async Task LoadSuppliersAsync(CancellationToken ct)
        {
            try
            {
                var list = await _db.Suppliers
                    .AsNoTracking()
                    .OrderBy(s => s.Name)
                    .Select(s => new SupplierItem(s.Id, s.Name))
                    .ToListAsync(ct);

                _comboSuppliers.Items.Clear();
                foreach (var it in list) _comboSuppliers.Items.Add(it);
                if (_comboSuppliers.Items.Count > 0) _comboSuppliers.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load suppliers");
                MessageBox.Show(this, ex.Message, "Suppliers", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ReloadOffersAsync(CancellationToken ct)
        {
            try
            {
                var sel = _comboSuppliers.SelectedItem as SupplierItem;
                if (sel == null)
                {
                    _bs.DataSource = Array.Empty<Offer>();
                    return;
                }

                var offers = await _db.SupplierOffers
                    .AsNoTracking()
                    .Where(o => o.SupplierId == sel.Id)
                    .OrderBy(o => o.OfferName)
                    .ToListAsync(ct);

                _bs.DataSource = offers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load offers");
                MessageBox.Show(this, ex.Message, "Offers", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Offer? GetSelectedOffer()
        {
            if (_bs.Current is Offer o) return o;
            if (_grid.CurrentRow?.DataBoundItem is Offer o2) return o2;
            return null;
        }

        private async Task AddOfferAsync(CancellationToken ct)
        {
            var sel = _comboSuppliers.SelectedItem as SupplierItem;
            if (sel == null)
            {
                MessageBox.Show(this, "Select a supplier first.", "Offers", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new OfferEditDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var entity = new Offer
                {
                    SupplierId = sel.Id,
                    OfferName = dlg.OfferName,
                    Currency = dlg.Currency,
                    Description = dlg.Description
                };
                _db.SupplierOffers.Add(entity);
                await _db.SaveChangesAsync(ct);
                await ReloadOffersAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create offer");
                MessageBox.Show(this, ex.Message, "Create Offer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task EditSelectedOfferAsync(CancellationToken ct)
        {
            var row = GetSelectedOffer();
            if (row == null)
            {
                MessageBox.Show(this, "Select an offer to edit.", "Offers", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Re-read tracked entity
            var entity = await _db.SupplierOffers.FirstOrDefaultAsync(o => o.Id == row.Id, ct);
            if (entity == null)
            {
                MessageBox.Show(this, "Offer not found.", "Offers", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new OfferEditDialog(entity.OfferName, entity.Currency, entity.Description, canEditIdentity: false);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                // OfferName/Currency are init-only; keep them unchanged during edit
                entity.Description = dlg.Description;
                entity.ModifiedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                await ReloadOffersAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update offer {OfferId}", entity.Id);
                MessageBox.Show(this, ex.Message, "Update Offer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteSelectedOfferAsync(CancellationToken ct)
        {
            var row = GetSelectedOffer();
            if (row == null)
            {
                MessageBox.Show(this, "Select an offer to delete.", "Offers", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(this, $"Delete offer '{row.OfferName}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var entity = await _db.SupplierOffers.FirstOrDefaultAsync(o => o.Id == row.Id, ct);
                if (entity == null) return;
                _db.SupplierOffers.Remove(entity);
                await _db.SaveChangesAsync(ct);
                await ReloadOffersAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete offer {OfferId}", row.Id);
                MessageBox.Show(this, ex.Message, "Delete Offer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed class OfferEditDialog : Form
        {
            private readonly TextBox _tbName = new() { Width = 280 };
            private readonly TextBox _tbCurrency = new() { Width = 80, Text = "USD" };
            private readonly TextBox _tbDesc = new() { Width = 360 };

            public string OfferName => _tbName.Text.Trim();
            public string Currency => (_tbCurrency.Text ?? string.Empty).Trim().ToUpperInvariant();
            public string? Description => string.IsNullOrWhiteSpace(_tbDesc.Text) ? null : _tbDesc.Text.Trim();

            public OfferEditDialog(string? name = null, string? currency = null, string? desc = null, bool canEditIdentity = true)
            {
                Text = string.IsNullOrEmpty(name) ? "New Offer" : "Edit Offer";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                Width = 520;
                Height = 180;

                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };

                var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(8), AutoSize = true };
                table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                table.Controls.Add(new Label { Text = "Offer Name:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 0);
                table.Controls.Add(_tbName, 1, 0);
                table.Controls.Add(new Label { Text = "Currency:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 1);
                table.Controls.Add(_tbCurrency, 1, 1);
                table.Controls.Add(new Label { Text = "Description:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 2);
                table.Controls.Add(_tbDesc, 1, 2);

                var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(8) };
                buttons.Controls.Add(ok);
                buttons.Controls.Add(cancel);

                Controls.Add(table);
                Controls.Add(buttons);

                _tbName.Text = name ?? string.Empty;
                _tbCurrency.Text = string.IsNullOrWhiteSpace(currency) ? "USD" : currency;
                _tbDesc.Text = desc ?? string.Empty;

                // Respect init-only identity fields when editing
                if (!canEditIdentity)
                {
                    _tbName.ReadOnly = true;
                    _tbCurrency.ReadOnly = true;
                    _tbName.TabStop = false;
                    _tbCurrency.TabStop = false;
                }

                AcceptButton = ok;
                CancelButton = cancel;

                ok.Click += (_, __) =>
                {
                    if (string.IsNullOrWhiteSpace(_tbName.Text))
                    {
                        MessageBox.Show(this, "Offer Name is required", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                    }
                    else if (string.IsNullOrWhiteSpace(_tbCurrency.Text) || _tbCurrency.Text.Trim().Length != 3)
                    {
                        MessageBox.Show(this, "Currency must be a 3-letter code (e.g., USD)", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                    }
                };
            }
        }
    }
}
