using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModernWinForms.Controls;
using ModernWinForms.Theming;
using Sacks.Core.Entities;
using Sacks.Core.Services.Interfaces;

namespace SacksApp
{
    /// <summary>
    /// Form for managing supplier offers.
    /// ZERO TOLERANCE: All controls are Modern, all parameters validated.
    /// </summary>
    public sealed partial class OffersForm : Form
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<OffersForm> _logger;
        private readonly ISuppliersService _suppliersService;
        private readonly ISupplierOffersService _offersService;

        /// <summary>
        /// Initializes a new instance of OffersForm.
        /// ZERO TOLERANCE: services parameter must not be null.
        /// </summary>
        /// <param name="services">Service provider for dependency resolution. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        public OffersForm(IServiceProvider services)
        {
            // ZERO TOLERANCE: Validate required parameter
            ArgumentNullException.ThrowIfNull(services);
            
            _services = services;
            _logger = _services.GetRequiredService<ILogger<OffersForm>>();
            _suppliersService = _services.GetRequiredService<ISuppliersService>();
            _offersService = _services.GetRequiredService<ISupplierOffersService>();

            InitializeComponent();

            // Apply theme
            ThemeManager.ApplyTheme(this);
        }

        private async void OffersForm_Load(object sender, EventArgs e)
        {
            await LoadSuppliersAsync(CancellationToken.None);
        }

        private async void ComboSuppliers_SelectedIndexChanged(object sender, EventArgs e)
        {
            await ReloadOffersAsync(CancellationToken.None);
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            await ReloadOffersAsync(CancellationToken.None);
        }

        private async void BtnAdd_Click(object sender, EventArgs e)
        {
            await AddOfferAsync(CancellationToken.None);
        }

        private async void BtnEdit_Click(object sender, EventArgs e)
        {
            await EditSelectedOfferAsync(CancellationToken.None);
        }

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            await DeleteSelectedOfferAsync(CancellationToken.None);
        }

        private sealed record SupplierItem(int Id, string Name)
        {
            public override string ToString() => Name;
        }

        private async Task LoadSuppliersAsync(CancellationToken ct)
        {
            try
            {
                var (suppliers, _) = await _suppliersService.GetSuppliersAsync(pageNumber: 1, pageSize: 1000);
                var list = suppliers
                    .OrderBy(s => s.Name)
                    .Select(s => new SupplierItem(s.Id, s.Name))
                    .ToList();

                _comboSuppliers.Items.Clear();
                foreach (var it in list) _comboSuppliers.Items.Add(it);
                if (_comboSuppliers.Items.Count > 0) _comboSuppliers.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load suppliers");
                CustomMessageBox.Show(ex.Message, "Suppliers", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                var offers = await _offersService.GetOffersBySupplierAsync(sel.Id);
                _bs.DataSource = offers.OrderBy(o => o.OfferName).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load offers");
                CustomMessageBox.Show(ex.Message, "Offers", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                CustomMessageBox.Show("Select a supplier first.", "Offers", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                await _offersService.CreateOfferAsync(entity, createdBy: Environment.UserName);
                await ReloadOffersAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create offer");
                CustomMessageBox.Show(ex.Message, "Create Offer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task EditSelectedOfferAsync(CancellationToken ct)
        {
            var row = GetSelectedOffer();
            if (row == null)
            {
                CustomMessageBox.Show("Select an offer to edit.", "Offers", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Re-read entity via service
            var entity = await _offersService.GetOfferAsync(row.Id);
            if (entity == null)
            {
                CustomMessageBox.Show("Offer not found.", "Offers", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new OfferEditDialog(entity.OfferName, entity.Currency, entity.Description, canEditIdentity: false);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                // OfferName/Currency are init-only; keep them unchanged during edit
                entity.Description = dlg.Description;
                await _offersService.UpdateOfferAsync(entity, modifiedBy: Environment.UserName);
                await ReloadOffersAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update offer {OfferId}", entity.Id);
                CustomMessageBox.Show(ex.Message, "Update Offer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteSelectedOfferAsync(CancellationToken ct)
        {
            var row = GetSelectedOffer();
            if (row == null)
            {
                CustomMessageBox.Show("Select an offer to delete.", "Offers", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (CustomMessageBox.Show($"Delete offer '{row.OfferName}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                await _offersService.DeleteOfferAsync(row.Id);
                await ReloadOffersAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete offer {OfferId}", row.Id);
                CustomMessageBox.Show(ex.Message, "Delete Offer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Nested dialog for creating/editing offers.
        /// ZERO TOLERANCE: All controls are Modern, validation enforced.
        /// </summary>
        private sealed class OfferEditDialog : Form
        {
            private readonly ModernTextBox _tbName = new() { Width = 280 };
            private readonly ModernTextBox _tbCurrency = new() { Width = 80, Text = "USD" };
            private readonly ModernTextBox _tbDesc = new() { Width = 360 };

            /// <summary>
            /// Gets the offer name. ZERO TOLERANCE: Never returns null, always trimmed.
            /// </summary>
            public string OfferName => _tbName.Text.Trim();

            /// <summary>
            /// Gets the currency code. ZERO TOLERANCE: Never returns null, always uppercase.
            /// </summary>
            public string Currency => _tbCurrency.Text.Trim().ToUpperInvariant();

            /// <summary>
            /// Gets the description. Returns null if whitespace-only.
            /// </summary>
            public string? Description => string.IsNullOrWhiteSpace(_tbDesc.Text) ? null : _tbDesc.Text.Trim();

            /// <summary>
            /// Initializes a new instance of OfferEditDialog.
            /// </summary>
            /// <param name="name">Initial offer name.</param>
            /// <param name="currency">Initial currency code.</param>
            /// <param name="desc">Initial description.</param>
            /// <param name="canEditIdentity">Whether identity fields (name/currency) can be edited.</param>
            public OfferEditDialog(string? name = null, string? currency = null, string? desc = null, bool canEditIdentity = true)
            {
                Text = string.IsNullOrEmpty(name) ? "New Offer" : "Edit Offer";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                Width = 520;
                Height = 180;

                // Apply theme
                ThemeManager.ApplyTheme(this);

                var ok = new ModernButton { Text = "OK", DialogResult = DialogResult.OK };
                var cancel = new ModernButton { Text = "Cancel", DialogResult = DialogResult.Cancel };

                var table = new ModernTableLayoutPanel 
                { 
                    Dock = DockStyle.Fill, 
                    ColumnCount = 2, 
                    RowCount = 3, 
                    Padding = new Padding(8), 
                    AutoSize = true 
                };
                
                table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                table.Controls.Add(new ModernLabel { Text = "Offer Name:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 0);
                table.Controls.Add(_tbName, 1, 0);
                table.Controls.Add(new ModernLabel { Text = "Currency:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 1);
                table.Controls.Add(_tbCurrency, 1, 1);
                table.Controls.Add(new ModernLabel { Text = "Description:", AutoSize = true, Padding = new Padding(0, 6, 6, 0) }, 0, 2);
                table.Controls.Add(_tbDesc, 1, 2);

                var buttons = new ModernFlowLayoutPanel 
                { 
                    FlowDirection = FlowDirection.RightToLeft, 
                    Dock = DockStyle.Bottom, 
                    Height = 40, 
                    Padding = new Padding(8) 
                };
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

                // ZERO TOLERANCE: Validate on OK click
                ok.Click += (_, __) =>
                {
                    if (string.IsNullOrWhiteSpace(_tbName.Text))
                    {
                        CustomMessageBox.Show("Offer Name is required", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                        _tbName.Focus();
                    }
                    else if (string.IsNullOrWhiteSpace(_tbCurrency.Text) || _tbCurrency.Text.Trim().Length != 3)
                    {
                        CustomMessageBox.Show("Currency must be a 3-letter code (e.g., USD)", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                        _tbCurrency.Focus();
                    }
                };
            }
        }
    }
}
