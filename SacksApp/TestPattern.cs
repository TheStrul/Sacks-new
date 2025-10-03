namespace SacksApp
{
    using System;
    using System.Text;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;
    using SacksLogicLayer.Services;
    using SacksApp.Utils;

    public partial class TestPattern : Form
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly AutocompleteHistoryStore _history = AutocompleteHistoryStore.Load();

        public TestPattern(IServiceProvider? serviceProvider = null)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;

            // Defaults
            textBoxInputKey.Text = string.IsNullOrWhiteSpace(textBoxInputKey.Text) ? "Text" : textBoxInputKey.Text;
            textBoxOutputName.Text = string.IsNullOrWhiteSpace(textBoxOutputName.Text) ? "Out" : textBoxOutputName.Text;
            if (!radioButtonTitle.Checked && !radioButtonUpper.Checked && !radioButtonLower.Checked)
                radioButtonTitle.Checked = true;

            // Enable autocomplete and load persisted history
            SetupAutocomplete(
                (textBoxInputText, nameof(textBoxInputText)),
                (textBoxInputKey, nameof(textBoxInputKey)),
                (textBoxOutputName, nameof(textBoxOutputName)),
                (textBoxPatterm, nameof(textBoxPatterm)),
                (textBoxPatternKey, nameof(textBoxPatternKey)),
                (textBoxSeedKey, nameof(textBoxSeedKey)),
                (textBoxSeedValue, nameof(textBoxSeedValue)),
                (textBoxMapTable, nameof(textBoxMapTable)),
                (textBoxMapInputKey, nameof(textBoxMapInputKey)),
                (textBoxDelimiter, nameof(textBoxDelimiter)),
                (textBoxSplitOutputKey, nameof(textBoxSplitOutputKey)),
                (textBoxCondition, nameof(textBoxCondition))
            );

            // Wire events
            buttonRun.Click += OnRunClick;
            comboBoxOp.SelectedIndexChanged += (_, __) => UpdatePanelsVisibility();
            this.FormClosed += (_, __) => _history.Save();

            // Initialize op selection
            if (comboBoxOp.Items.Count > 0)
            {
                comboBoxOp.SelectedItem = comboBoxOp.Items[0]; // default to first (Find)
            }
            UpdatePanelsVisibility();
        }

        private void SetupAutocomplete(params (TextBox box, string key)[] entries)
        {
            foreach (var (tb, key) in entries)
            {
                if (tb == null) continue;
                tb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                tb.AutoCompleteSource = AutoCompleteSource.CustomSource;
                tb.AutoCompleteCustomSource ??= new AutoCompleteStringCollection();
                // preload items
                var items = _history.Get(key);
                if (items.Count > 0)
                {
                    tb.AutoCompleteCustomSource.AddRange(items.ToArray());
                }
            }
        }

        private void AddHistory(TextBox? tb, string key, string? value)
        {
            if (tb == null) return;
            var v = (value ?? string.Empty).Trim();
            if (v.Length == 0) return;

            tb.AutoCompleteCustomSource ??= new AutoCompleteStringCollection();
            // avoid dup in textbox source
            foreach (string item in tb.AutoCompleteCustomSource)
            {
                if (string.Equals(item, v, StringComparison.OrdinalIgnoreCase))
                {
                    // still ensure persisted order freshness
                    _history.Add(key, v);
                    return;
                }
            }
            tb.AutoCompleteCustomSource.Add(v);
            _history.Add(key, v);
        }

        private void UpdatePanelsVisibility()
        {
            var op = (comboBoxOp?.SelectedItem?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
            tableLayoutPanelFindAction.Visible = string.Equals(op, "find", StringComparison.OrdinalIgnoreCase);
            tableLayoutPanelMapAction.Visible = string.Equals(op, "map", StringComparison.OrdinalIgnoreCase);
            tableLayoutPanelSplitAction.Visible = string.Equals(op, "split", StringComparison.OrdinalIgnoreCase);
            tableLayoutPanelCaseAction.Visible = string.Equals(op, "case", StringComparison.OrdinalIgnoreCase);
        }

        private string BuildFindOptionsString()
        {
            string? mode = null;
            if (chkFirst.Checked) mode = "first";
            else if (chkLast.Checked) mode = "last";
            else if (chkAll.Checked) mode = "all";

            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(mode)) parts.Add(mode);
            if (chkIgnoreCase.Checked) parts.Add("ignorecase");
            if (chkRemove.Checked) parts.Add("remove");
            return string.Join(',', parts);
        }

        private async System.Threading.Tasks.Task<System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>> LoadLookupsAsync()
        {
            try
            {
                if (_serviceProvider == null) return new(StringComparer.OrdinalIgnoreCase);
                var configMgr = _serviceProvider.GetService<SupplierConfigurationManager>();
                if (configMgr == null) return new(StringComparer.OrdinalIgnoreCase);
                var cfg = await configMgr.GetConfigurationAsync();
                return new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>(cfg.Lookups, StringComparer.OrdinalIgnoreCase);
            }
            catch { return new(StringComparer.OrdinalIgnoreCase); }
        }

        private async void OnRunClick(object? sender, EventArgs e)
        {
            var inputText = textBoxInputText.Text ?? string.Empty;
            var op = (comboBoxOp?.SelectedItem?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(op)) op = "find";

            var inputKey = string.IsNullOrWhiteSpace(textBoxInputKey.Text) ? "Text" : textBoxInputKey.Text.Trim();
            var outputKey = string.IsNullOrWhiteSpace(textBoxOutputName.Text) ? "Out" : textBoxOutputName.Text.Trim();
            var assign = checkAssign.Checked;
            var condition = string.IsNullOrWhiteSpace(textBoxCondition.Text) ? null : textBoxCondition.Text;

            var parameters = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            switch (op)
            {
                case "find":
                {
                    var opts = BuildFindOptionsString();
                    if (!string.IsNullOrWhiteSpace(opts)) parameters["Options"] = opts;
                    var patternKey = (textBoxPatternKey.Text ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(patternKey)) parameters["PatternKey"] = patternKey;
                    else parameters["Pattern"] = textBoxPatterm.Text ?? string.Empty;
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
                    var mode = radioButtonUpper.Checked ? "upper" : radioButtonLower.Checked ? "lower" : "title";
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
            if (result.Trace.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Trace:");
                foreach (var t in result.Trace) sb.AppendLine("  " + t);
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
