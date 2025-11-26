using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ModernWinForms.Theming;
using Sacks.LogicLayer.Services;
using SacksApp.Utils;

namespace SacksApp
{
    /// <summary>
    /// Test form for pattern/action testing with ParsingEngine.
    /// ZERO TOLERANCE: All controls are Modern, all operations validated.
    /// </summary>
    public partial class TestPattern : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AutocompleteHistoryStore _history = AutocompleteHistoryStore.Load();

        /// <summary>
        /// Initializes a new instance of TestPattern.
        /// ZERO TOLERANCE: serviceProvider must not be null.
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when serviceProvider is null.</exception>
        public TestPattern(IServiceProvider serviceProvider)
        {
            // ZERO TOLERANCE: Validate required parameter
            ArgumentNullException.ThrowIfNull(serviceProvider);
            _serviceProvider = serviceProvider;

            InitializeComponent();

            // Apply theme
            ThemeManager.ApplyTheme(this);

            // Set defaults - ZERO TOLERANCE: No null coalescing, explicit values
            textBoxInputKey.Text = "Text";
            textBoxOutputName.Text = "Out";
            radioButtonTitle.Checked = true;

            // Enable autocomplete and load persisted history
            SetupAutocomplete();

            // Wire events
            buttonRun.Click += OnRunClick;
            comboBoxOp.SelectedIndexChanged += (_, __) => UpdatePanelsVisibility();
            FormClosed += (_, __) => _history.Save();

            // Initialize op selection - ZERO TOLERANCE: Validate items exist
            if (comboBoxOp.Items.Count == 0)
                throw new InvalidOperationException("ComboBox operation list is empty. Designer initialization failed.");

            comboBoxOp.SelectedIndex = 0; // Select first item (Find)
            UpdatePanelsVisibility();
        }

        /// <summary>
        /// Sets up autocomplete for all text boxes.
        /// ZERO TOLERANCE: Type-safe, no dynamic keyword.
        /// </summary>
        private void SetupAutocomplete()
        {
            var textBoxes = new Dictionary<ModernWinForms.Controls.ModernTextBox, string>
            {
                { textBoxInputText, nameof(textBoxInputText) },
                { textBoxInputKey, nameof(textBoxInputKey) },
                { textBoxOutputName, nameof(textBoxOutputName) },
                { textBoxPatterm, nameof(textBoxPatterm) },
                { textBoxPatternKey, nameof(textBoxPatternKey) },
                { textBoxSeedKey, nameof(textBoxSeedKey) },
                { textBoxSeedValue, nameof(textBoxSeedValue) },
                { textBoxMapTable, nameof(textBoxMapTable) },
                { textBoxMapInputKey, nameof(textBoxMapInputKey) },
                { textBoxDelimiter, nameof(textBoxDelimiter) },
                { textBoxSplitOutputKey, nameof(textBoxSplitOutputKey) },
                { textBoxCondition, nameof(textBoxCondition) }
            };

            foreach (var (textBox, key) in textBoxes)
            {
                textBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;

                var items = _history.Get(key);
                if (items.Count > 0)
                {
                    var collection = new AutoCompleteStringCollection();
                    collection.AddRange(items.ToArray());
                    textBox.AutoCompleteCustomSource = collection;
                }
            }
        }

        /// <summary>
        /// Adds value to autocomplete history.
        /// ZERO TOLERANCE: Type-safe, validates inputs.
        /// </summary>
        private void AddHistory(ModernWinForms.Controls.ModernTextBox textBox, string key, string? value)
        {
            // ZERO TOLERANCE: Validate inputs
            ArgumentNullException.ThrowIfNull(textBox);
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("History key cannot be null or whitespace.", nameof(key));

            var trimmedValue = (value ?? string.Empty).Trim();
            if (trimmedValue.Length == 0) return;

            // Ensure collection exists
            textBox.AutoCompleteCustomSource ??= new AutoCompleteStringCollection();

            // Check for duplicates
            bool exists = false;
            foreach (string item in textBox.AutoCompleteCustomSource)
            {
                if (string.Equals(item, trimmedValue, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                textBox.AutoCompleteCustomSource.Add(trimmedValue);
            }

            _history.Add(key, trimmedValue);
        }

        /// <summary>
        /// Updates visibility of action-specific panels based on selected operation.
        /// </summary>
        private void UpdatePanelsVisibility()
        {
            var op = (comboBoxOp.SelectedItem?.ToString() ?? string.Empty).Trim().ToLowerInvariant();

            tableLayoutPanelFindAction.Visible = string.Equals(op, "find", StringComparison.OrdinalIgnoreCase);
            tableLayoutPanelMapAction.Visible = string.Equals(op, "map", StringComparison.OrdinalIgnoreCase);
            tableLayoutPanelSplitAction.Visible = string.Equals(op, "split", StringComparison.OrdinalIgnoreCase);
            tableLayoutPanelCaseAction.Visible = string.Equals(op, "case", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Builds find options string from checkboxes.
        /// </summary>
        private string BuildFindOptionsString()
        {
            string? mode = null;
            if (chkFirst.Checked) mode = "first";
            else if (chkLast.Checked) mode = "last";
            else if (chkAll.Checked) mode = "all";

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(mode)) parts.Add(mode);
            if (chkIgnoreCase.Checked) parts.Add("ignorecase");
            if (chkRemove.Checked) parts.Add("remove");
            
            return string.Join(',', parts);
        }

        /// <summary>
        /// Loads lookup tables from configuration.
        /// ZERO TOLERANCE: Returns empty dictionary on failure, never null.
        /// </summary>
        private async Task<Dictionary<string, Dictionary<string, string>>> LoadLookupsAsync()
        {
            try
            {
                var configMgr = _serviceProvider.GetService<SupplierConfigurationManager>();
                if (configMgr == null) 
                    return new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

                var cfg = await configMgr.GetConfigurationAsync();
                return new Dictionary<string, Dictionary<string, string>>(cfg.Lookups, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Executes the action test.
        /// </summary>
        private async void OnRunClick(object? sender, EventArgs e)
        {
            var inputText = textBoxInputText.Text ?? string.Empty;
            var op = (comboBoxOp.SelectedItem?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(op)) op = "find";

            var inputKey = string.IsNullOrWhiteSpace(textBoxInputKey.Text) ? "Text" : textBoxInputKey.Text.Trim();
            var outputKey = string.IsNullOrWhiteSpace(textBoxOutputName.Text) ? "Out" : textBoxOutputName.Text.Trim();
            var assign = checkAssign.Checked;
            var condition = string.IsNullOrWhiteSpace(textBoxCondition.Text) ? null : textBoxCondition.Text;

            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            switch (op)
            {
                case "find":
                {
                    var opts = BuildFindOptionsString();
                    if (!string.IsNullOrWhiteSpace(opts)) parameters["Options"] = opts;
                    
                    var patternKey = (textBoxPatternKey.Text ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(patternKey)) 
                        parameters["PatternKey"] = patternKey;
                    else 
                        parameters["Pattern"] = textBoxPatterm.Text ?? string.Empty;
                    break;
                }
                case "map":
                {
                    var table = (textBoxMapTable.Text ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(table)) parameters["Table"] = table;
                    
                    var mapIn = (textBoxMapInputKey.Text ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(mapIn)) inputKey = mapIn;
                    break;
                }
                case "split":
                {
                    parameters["Delimiter"] = textBoxDelimiter.Text ?? string.Empty;
                    var partsOut = (textBoxSplitOutputKey.Text ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(partsOut)) outputKey = partsOut;
                    break;
                }
                case "case":
                {
                    var mode = radioButtonUpper.Checked ? "upper" 
                             : radioButtonLower.Checked ? "lower" 
                             : "title";
                    parameters["Mode"] = mode;
                    break;
                }
                case "assign":
                default:
                    break;
            }

            var lookups = await LoadLookupsAsync();

            var result = ActionTestRunner.Run(
                op: op,
                inputText: inputText,
                inputKey: inputKey,
                outputKey: outputKey,
                assign: assign,
                condition: condition,
                parameters: parameters,
                lookups: lookups,
                seedKey: string.IsNullOrWhiteSpace(textBoxSeedKey.Text) ? null : textBoxSeedKey.Text,
                seedValue: string.IsNullOrWhiteSpace(textBoxSeedValue.Text) ? null : textBoxSeedValue.Text,
                culture: System.Globalization.CultureInfo.InvariantCulture);

            var sb = new StringBuilder();
            sb.AppendLine(result.Success ? "Action: success" : "Action: no match / no-op");
            foreach (var kv in result.Bag)
            {
                sb.AppendLine($"{kv.Key} = '{kv.Value}'");
            }
            textBoxResults.Text = sb.ToString();
            textBoxActionJson.Text = result.JsonAction;

            // Persist autocomplete history for values used in this run
            AddHistory(textBoxInputText, nameof(textBoxInputText), textBoxInputText.Text);
            AddHistory(textBoxInputKey, nameof(textBoxInputKey), textBoxInputKey.Text);
            AddHistory(textBoxOutputName, nameof(textBoxOutputName), textBoxOutputName.Text);
            AddHistory(textBoxPatterm, nameof(textBoxPatterm), textBoxPatterm.Text);
            AddHistory(textBoxPatternKey, nameof(textBoxPatternKey), textBoxPatternKey.Text);
            AddHistory(textBoxSeedKey, nameof(textBoxSeedKey), textBoxSeedKey.Text);
            AddHistory(textBoxSeedValue, nameof(textBoxSeedValue), textBoxSeedValue.Text);
            AddHistory(textBoxMapTable, nameof(textBoxMapTable), textBoxMapTable.Text);
            AddHistory(textBoxMapInputKey, nameof(textBoxMapInputKey), textBoxMapInputKey.Text);
            AddHistory(textBoxDelimiter, nameof(textBoxDelimiter), textBoxDelimiter.Text);
            AddHistory(textBoxSplitOutputKey, nameof(textBoxSplitOutputKey), textBoxSplitOutputKey.Text);
            AddHistory(textBoxCondition, nameof(textBoxCondition), textBoxCondition.Text);

            // Save to disk now to avoid data loss
            _history.Save();
        }
    }
}
