using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using QMobileDeviceServiceMenu;

using Sacks.Core.Services.Interfaces;
using Sacks.LogicLayer.Services.Interfaces;
using ModernWinForms.Theming;

namespace SacksApp;

/// <summary>
/// Main MDI container form for the Sacks Product Management System.
/// Hosts child forms (DashBoard, SqlQueryForm, OffersForm, etc.) within a single parent window.
/// </summary>
public partial class MainForm : Form
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainForm> _logger;

    private static readonly System.Text.Json.JsonSerializerOptions s_jsonFormatOptions = new()
    {
        WriteIndented = true
    };

    public MainForm(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetRequiredService<ILogger<MainForm>>();

        // Configure as MDI container
        IsMdiContainer = true;

        // Initialize ResponseMode dropdown with current config value
        var config = serviceProvider.GetRequiredService<Sacks.Configuration.SacksConfigurationOptions>();
        responseModeComboBox.SelectedIndex = config.Llm.ResponseMode switch
        {
            "ToolOnly" => 0,
            "Conversational" => 1,
            _ => 1 // Default to Conversational
        };

        // Apply app-wide theme to all ModernButtons
        ThemeManager.ApplyTheme(this);

        // Initialize Skin Menu
        InitializeSkinMenu();

        _logger.LogInformation("Main MDI form initialized");
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
    }

    #region Menu Event Handlers

    // File Menu
    private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    // Tools Menu


    private void SqlQueryToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ShowSqlQuery();
    }

    private void OffersManagerToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ShowOffersManager();
    }

    private void LookupEditorToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ShowLookupEditor();
    }

    private void LogViewerToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ShowLogViewer();
    }

    // Window Menu
    private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        LayoutMdi(MdiLayout.Cascade);
    }

    private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        LayoutMdi(MdiLayout.TileHorizontal);
    }

    private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        LayoutMdi(MdiLayout.TileVertical);
    }

    private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        LayoutMdi(MdiLayout.ArrangeIcons);
    }

    private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
    {
        foreach (Form child in MdiChildren)
        {
            child.Close();
        }
    }

    #endregion

    #region Skin Management

    private void InitializeSkinMenu()
    {
        // Find the menu strip
        var menuStrip = Controls.OfType<MenuStrip>().FirstOrDefault();
        if (menuStrip == null) return;

        // Create Skin menu
#pragma warning disable CA2000 // Dispose objects before losing scope
        var skinMenu = new ToolStripMenuItem("Skin");
#pragma warning restore CA2000

        // Get all available skins dynamically
        var availableSkins = new[] { "Light", "Dark", "Dracula", "Solarized Light", "Solarized Dark", 
                                     "Monokai", "Nord", "Material", "Fluent", "Cyberpunk", "Gruvbox" };
        foreach (var skin in availableSkins)
        {
            var item = new ToolStripMenuItem(skin);
            item.Click += SkinMenuItem_Click;
            // Check the current skin
            if (ThemeManager.CurrentSkin == skin)
            {
                item.Checked = true;
            }
            skinMenu.DropDownItems.Add(item);
        }

        // Find Window menu and insert Skin menu before it
        var windowMenu = menuStrip.Items.OfType<ToolStripMenuItem>().FirstOrDefault(m => m.Text?.Contains("Window") == true);
        if (windowMenu != null)
        {
            var index = menuStrip.Items.IndexOf(windowMenu);
            menuStrip.Items.Insert(index, skinMenu);
        }
        else
        {
            menuStrip.Items.Add(skinMenu);
        }

        // Create Theme menu
        InitializeThemeMenu();
    }

    private void InitializeThemeMenu()
    {
        // Find the menu strip
        var menuStrip = Controls.OfType<MenuStrip>().FirstOrDefault();
        if (menuStrip == null) return;

        // Create Theme menu
#pragma warning disable CA2000
        var themeMenu = new ToolStripMenuItem("Theme");
#pragma warning restore CA2000

        // Get all available themes
        var availableThemes = ThemeManager.AvailableThemes;
        foreach (var theme in availableThemes)
        {
            var item = new ToolStripMenuItem(theme);
            item.Click += ThemeMenuItem_Click;
            // Check the current theme
            if (ThemeManager.CurrentTheme == theme)
            {
                item.Checked = true;
            }
            themeMenu.DropDownItems.Add(item);
        }

        // Find Window menu and insert Theme menu before it (after Skin menu)
        var windowMenu = menuStrip.Items.OfType<ToolStripMenuItem>().FirstOrDefault(m => m.Text?.Contains("Window") == true);
        if (windowMenu != null)
        {
            var index = menuStrip.Items.IndexOf(windowMenu);
            menuStrip.Items.Insert(index, themeMenu);
        }
        else
        {
            menuStrip.Items.Add(themeMenu);
        }
    }

    private void ThemeMenuItem_Click(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            var theme = item.Text ?? "GitHub";
            ThemeManager.CurrentTheme = theme;
            ThemeManager.ApplyTheme(this);

            // Update checked state
            if (item.Owner is ToolStripDropDown dropdown && dropdown.OwnerItem is ToolStripMenuItem parent)
            {
                foreach (ToolStripMenuItem menuItem in parent.DropDownItems)
                {
                    menuItem.Checked = menuItem.Text == theme;
                }
            }

            ShowNotification($"Theme set to {theme}.", NotificationType.Success);
        }
    }

    private void SkinMenuItem_Click(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            var skin = item.Text ?? "Light";
            ThemeManager.CurrentSkin = skin;
            ThemeManager.ApplyTheme(this);

            // Update checked state
            if (item.Owner is ToolStripDropDown dropdown && dropdown.OwnerItem is ToolStripMenuItem parent)
            {
                foreach (ToolStripMenuItem menuItem in parent.DropDownItems)
                {
                    menuItem.Checked = menuItem.Text == skin;
                }
            }

            ShowNotification($"Skin set to {skin}.", NotificationType.Success);
        }
    }

    #endregion


    #region File Menu

    private void FileToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        // File menu click handler (placeholder)
    }

    #endregion



    #region Dashboard Logic Migration

    private async void ProcessFilesButton_Click(object sender, EventArgs e)
    {
        try
        {
            processFilesButton.Enabled = false;

            _logger.LogDebug("Starting file processing operation");

            var fileProcessingService = _serviceProvider.GetRequiredService<IFileProcessingService>();

            // Get input folder from configuration
            var config = _serviceProvider.GetRequiredService<Sacks.Configuration.SacksConfigurationOptions>();
            var inputsPath = GetInputDirectoryFromConfiguration(config.FileProcessing);

            var files = Directory.GetFiles(inputsPath, "*.xls*")
                               .Where(f => !Path.GetFileName(f).StartsWith("~"))
                               .ToArray();

            if (files.Length == 0)
            {
                CustomMessageBox.Show("No Excel files found in Inputs folder.",
                    "No Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _logger.LogDebug($"Found {files.Length} Excel file(s) in Inputs folder");

            foreach (var file in files)
            {
                _logger.LogDebug($"Processing {Path.GetFileName(file)}");
                await fileProcessingService.ProcessFileAsync(file, CancellationToken.None);
                _logger.LogDebug($"Finished processing {Path.GetFileName(file)}");
            }

            CustomMessageBox.Show($"Successfully processed {files.Length} file(s)!",
                "Processing Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing files");
            CustomMessageBox.Show($"Error processing files:\n{ex.Message}\n\nCheck LogViewer for details.",
                "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            processFilesButton.Enabled = true;
        }
    }

    private string GetInputDirectoryFromConfiguration(Sacks.Configuration.FileProcessingOptions config)
    {
        var configuredPath = config.InputDirectory;

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException("FileProcessing:InputDirectory is not configured");
        }

        // Handle relative paths - resolve relative to solution root instead of bin directory
        string resolvedPath;
        if (Path.IsPathRooted(configuredPath))
        {
            resolvedPath = configuredPath;
        }
        else
        {
            // Find solution root and resolve relative to it
            var solutionRoot = FindSolutionRoot();
            resolvedPath = Path.GetFullPath(Path.Combine(solutionRoot, configuredPath));
        }

        // Create directory if it doesn't exist
        if (!Directory.Exists(resolvedPath))
        {
            try
            {
                Directory.CreateDirectory(resolvedPath);
                _logger.LogDebug("Created input directory: {InputPath}", resolvedPath);
            }
            catch (Exception ex)
            {
                throw new DirectoryNotFoundException($"Cannot create input directory at {resolvedPath}: {ex.Message}", ex);
            }
        }

        _logger.LogDebug("Using configured input directory: {InputPath}", resolvedPath);
        return resolvedPath;
    }

    private async void ClearDatabaseButton_Click(object sender, EventArgs e)
    {
        var result = CustomMessageBox.Show("Are you sure you want to clear ALL data from the database?\n\nThis action cannot be undone!",
            "Confirm Database Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        try
        {
            clearDatabaseButton.Enabled = false;

            var databaseService = _serviceProvider.GetRequiredService<IDatabaseManagementService>();
            var clearResult = await databaseService.ClearAllDataAsync();

            if (clearResult.Success)
            {
                var summary = string.Join("\n", clearResult.DeletedCounts.Select(kvp => $"{kvp.Key}: {kvp.Value:N0} records"));
                CustomMessageBox.Show($"Database cleared successfully!\n\nDeleted:\n{summary}\n\nCompleted in {clearResult.ElapsedMilliseconds:N0}ms",
                    "Database Cleared", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var errors = string.Join("\n", clearResult.Errors);
                CustomMessageBox.Show($"Failed to clear database:\n{clearResult.Message}\n\nErrors:\n{errors}",
                    "Clear Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing database");
            CustomMessageBox.Show($"Error clearing database:\n{ex.Message}",
                "Clear Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            clearDatabaseButton.Enabled = true;
        }
    }

    private void TestConfigurationButton_Click(object sender, EventArgs e)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var t = new TestPattern();
#pragma warning restore CA2000
        t.ShowDialog(this);
    }

    private void SqlQueryButton_Click(object sender, EventArgs e)
    {
        ShowSqlQuery();
    }

    private void ViewLogsButton_Click(object sender, EventArgs e)
    {
        ShowLogViewer();
    }

    private void ButtonEditMaps_Click(object sender, EventArgs e)
    {
        ShowLookupEditor();
    }

    private void HandleOffersButton_Click(object sender, EventArgs e)
    {
        ShowOffersManager();
    }

    private void ResponseModeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var config = _serviceProvider.GetRequiredService<Sacks.Configuration.SacksConfigurationOptions>();
        
        // Update the config based on selection
        config.Llm.ResponseMode = responseModeComboBox.SelectedIndex switch
        {
            0 => "ToolOnly",
            1 => "Conversational",
            _ => "Conversational"
        };

        _logger.LogInformation("Response mode changed to: {Mode}", config.Llm.ResponseMode);
    }

    private void AiQueryTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true; // Prevent the "ding" sound
            ExecuteAiQueryButton_Click(sender, e);
        }
    }

    private async void ExecuteAiQueryButton_Click(object? sender, EventArgs e)
    {
        var query = aiQueryTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            CustomMessageBox.Show("Please enter a query.", "Empty Query", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            executeAiQueryButton.Enabled = false;
            aiMetadataTextBox.Text = "⏳ Processing query...";
            aiDataResultsTextBox.Text = "";

            _logger.LogInformation("Processing query: {Query}", query);

            // Use natural language routing for all queries
            await ProcessNaturalLanguageQueryAsync(query).ConfigureAwait(true);

            _logger.LogInformation("Query processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query");
            aiMetadataTextBox.Text = $"❌ Error: {ex.Message}";
            aiDataResultsTextBox.Text = "";
            CustomMessageBox.Show($"Error processing query: {ex.Message}", "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            executeAiQueryButton.Enabled = true;
        }
    }

    private async Task ProcessNaturalLanguageQueryAsync(string query)
    {
        var routerService = _serviceProvider.GetRequiredService<ILlmQueryRouterService>();
        
        // Route the query to the most appropriate tool
        var routingResult = await routerService.RouteQueryAsync(query, CancellationToken.None).ConfigureAwait(false);

        // Build metadata text (query info, routing analysis)
        var metadataSb = new System.Text.StringBuilder();
        metadataSb.AppendLine($"📝 Query: {query}");
        metadataSb.AppendLine($"🎯 Tool: {routingResult.SelectedToolName} | Confidence: {routingResult.RoutingConfidence:P0}");
        metadataSb.AppendLine($"💭 Reason: {routingResult.RoutingReason}");
        
        if (!routingResult.IsSuccessful)
        {
            metadataSb.AppendLine();
            metadataSb.AppendLine($"❌ Error: {routingResult.ErrorMessage}");
            var errorText = metadataSb.ToString();
            if (InvokeRequired)
            {
                Invoke(() =>
                {
                    aiMetadataTextBox.Text = errorText;
                    aiDataResultsTextBox.Text = "";
                });
            }
            else
            {
                aiMetadataTextBox.Text = errorText;
                aiDataResultsTextBox.Text = "";
            }
            return;
        }

        // Parse and display JSON result in user-friendly format
        string dataResultText;
        try
        {
            dataResultText = ParseAndFormatToolResult(routingResult.ToolResult);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse tool result, showing raw response");
            dataResultText = routingResult.ToolResult;
        }
        
        var metadataText = metadataSb.ToString();
        if (InvokeRequired)
        {
            Invoke(() =>
            {
                aiMetadataTextBox.Text = metadataText;
                aiDataResultsTextBox.Text = dataResultText;
            });
        }
        else
        {
            aiMetadataTextBox.Text = metadataText;
            aiDataResultsTextBox.Text = dataResultText;
        }
    }

    private string ParseAndFormatToolResult(string jsonResult)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        // Check for JSON-RPC error
        if (root.TryGetProperty("error", out var error))
        {
            var code = error.TryGetProperty("code", out var c) ? c.GetInt32() : 0;
            var message = error.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
            return $"❌ Error ({code}): {message}";
        }

        // Extract the actual result
        if (!root.TryGetProperty("result", out var result))
        {
            return jsonResult; // Fallback to raw JSON
        }

        var sb = new System.Text.StringBuilder();

        // Extract content array
        if (result.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var content in contentArray.EnumerateArray())
            {
                if (content.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "text")
                {
                    if (content.TryGetProperty("text", out var textElement))
                    {
                        var text = textElement.GetString() ?? "";
                        
                        // Try to parse the text as JSON (nested JSON from tools)
                        try
                        {
                            using var innerDoc = System.Text.Json.JsonDocument.Parse(text);
                            var innerRoot = innerDoc.RootElement;

                            // Check for success/data pattern
                            if (innerRoot.TryGetProperty("success", out var success) && success.GetBoolean())
                            {
                                if (innerRoot.TryGetProperty("data", out var data))
                                {
                                    sb.AppendLine(FormatDataObject(data));
                                }
                                else
                                {
                                    sb.AppendLine("✅ Success");
                                }
                            }
                            else if (innerRoot.TryGetProperty("error", out var innerError))
                            {
                                sb.AppendLine($"❌ {innerError.GetString()}");
                            }
                            else
                            {
                                // Format as readable JSON
                                sb.AppendLine(FormatDataObject(innerRoot));
                            }
                        }
                        catch
                        {
                            // Not JSON, display as-is
                            sb.AppendLine(text);
                        }
                    }
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private string FormatDataObject(System.Text.Json.JsonElement element)
    {
        var sb = new System.Text.StringBuilder();

        if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var count = 0;
            foreach (var item in element.EnumerateArray())
            {
                count++;
                sb.AppendLine($"\n[Item {count}]");
                sb.Append(FormatDataObject(item));
            }
            
            if (count == 0)
            {
                sb.AppendLine("(No items found)");
            }
            else
            {
                sb.Insert(0, $"Found {count} item(s):\n");
            }
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                var name = prop.Name;
                var value = prop.Value;

                if (value.ValueKind == System.Text.Json.JsonValueKind.Object || value.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    sb.AppendLine($"{name}:");
                    var nested = FormatDataObject(value);
                    foreach (var line in nested.Split('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            sb.AppendLine($"  {line}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine($"{name}: {GetValueAsString(value)}");
                }
            }
        }
        else
        {
            sb.Append(GetValueAsString(element));
        }

        return sb.ToString();
    }

    private string GetValueAsString(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => element.GetString() ?? "",
            System.Text.Json.JsonValueKind.Number => element.GetRawText(),
            System.Text.Json.JsonValueKind.True => "true",
            System.Text.Json.JsonValueKind.False => "false",
            System.Text.Json.JsonValueKind.Null => "(null)",
            _ => element.GetRawText()
        };
    }

    #endregion

    #region Child Form Management

    /// <summary>
    /// Shows the Dashboard (main control panel). Only one instance allowed.
    /// </summary>


    /// <summary>
    /// Opens a new SQL Query window. Multiple instances allowed.
    /// </summary>
    private void ShowSqlQuery()
    {
        try
        {
#pragma warning disable CA2000 // MDI child disposal managed by WinForms
            var sqlForm = new SqlQueryForm(_serviceProvider)
#pragma warning restore CA2000
            {
                MdiParent = this
            };
            sqlForm.Show();
            _logger.LogInformation("SQL Query window opened");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening SQL Query Tool");
            CustomMessageBox.Show($"Error opening SQL Query Tool: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Opens the Offers Manager. Only one instance allowed.
    /// </summary>
    private void ShowOffersManager()
    {
        var existing = FindMdiChild<OffersForm>();
        if (existing != null)
        {
            existing.Activate();
            return;
        }

        try
        {
#pragma warning disable CA2000 // MDI child disposal managed by WinForms
            var form = new OffersForm(_serviceProvider)
#pragma warning restore CA2000
            {
                MdiParent = this,
                WindowState = FormWindowState.Maximized
            };
            form.Show();
            _logger.LogInformation("Offers Manager opened");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening Offers Manager");
            CustomMessageBox.Show($"Error opening Offers Manager: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Opens the Lookup Editor. Only one instance allowed.
    /// </summary>
    private void ShowLookupEditor()
    {
        var existing = FindMdiChild<LookupEditorForm>();
        if (existing != null)
        {
            existing.Activate();
            return;
        }

        try
        {
#pragma warning disable CA2000 // MDI child disposal managed by WinForms
            var editor = new LookupEditorForm(_serviceProvider, string.Empty)
#pragma warning restore CA2000
            {
                MdiParent = this,
                WindowState = FormWindowState.Maximized
            };
            editor.Show();
            _logger.LogInformation("Lookup Editor opened");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening Lookup Editor");
            CustomMessageBox.Show($"Error opening Lookup Editor: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Opens the Log Viewer. Only one instance allowed.
    /// </summary>
    private void ShowLogViewer()
    {
        var existing = FindMdiChild<QMobileDeviceServiceMenu.LogViewerForm>();
        if (existing != null)
        {
            existing.Activate();
            return;
        }

        try
        {
            // Find solution root for log files
            var solutionRoot = FindSolutionRoot();
            QMobileDeviceServiceMenu.LogViewerForm.ShowServiceLogs(solutionRoot);
            _logger.LogInformation("Log Viewer opened");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening Log Viewer");
            CustomMessageBox.Show($"Error opening Log Viewer: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string FindSolutionRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory != null)
        {
            if (currentDirectory.GetFiles("*.sln").Length > 0)
            {
                return currentDirectory.FullName;
            }
            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException("Solution root directory not found - no .sln file found in directory hierarchy");
    }

    private async void ShowStatisticsButton_Click(object sender, EventArgs e)
    {
        try
        {
            showStatisticsButton.Enabled = false;

            var databaseService = _serviceProvider.GetRequiredService<IDatabaseManagementService>();
            var connectionResult = await databaseService.CheckConnectionAsync();

            if (!connectionResult.CanConnect)
            {
                CustomMessageBox.Show($"Cannot connect to database:\n{connectionResult.Message}",
                    "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var statistics = string.Join("\n",
                connectionResult.TableCounts.OrderBy(x => x.Key)
                .Select(kvp => $"{kvp.Key}: {kvp.Value:N0} records"));

            var totalRecords = connectionResult.TableCounts.Values.Sum();

            CustomMessageBox.Show($"Database Statistics\n{new string('=', 30)}\n\n{statistics}\n\nTotal records: {totalRecords:N0}\n\nServer: {connectionResult.ServerInfo}",
                "Database Statistics", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading statistics");
            CustomMessageBox.Show($"Error loading statistics:\n{ex.Message}",
                "Statistics Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            showStatisticsButton.Enabled = true;
        }
    }

    /// <summary>
    /// Finds an existing MDI child of the specified type.
    /// </summary>
    private T? FindMdiChild<T>() where T : Form
    {
        return MdiChildren.OfType<T>().FirstOrDefault();
    }

    #endregion

    #region Notification System

    /// <summary>
    /// Notification types for status messages
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Shows a notification message at the bottom of the form.
    /// Auto-dismisses after 5 seconds unless manually closed.
    /// </summary>
    /// <param name="message">The notification message to display</param>
    /// <param name="type">The type of notification (info, success, warning, error)</param>
    public void ShowNotification(string message, NotificationType type = NotificationType.Info)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Update UI on the main thread
        if (InvokeRequired)
        {
            Invoke(() => ShowNotification(message, type));
            return;
        }

        // Set icon and background color based on type
        (string icon, Color bgColor, Color textColor) = type switch
        {
            NotificationType.Success => ("✅", Color.FromArgb(240, 249, 235), Color.FromArgb(26, 127, 55)),
            NotificationType.Warning => ("⚠️", Color.FromArgb(255, 252, 235), Color.FromArgb(176, 133, 0)),
            NotificationType.Error => ("❌", Color.FromArgb(252, 240, 240), Color.FromArgb(218, 54, 51)),
            _ => ("ℹ️", Color.FromArgb(240, 249, 235), Color.FromArgb(13, 17, 23))
        };

        // Update notification panel
        notificationStatusIcon.Text = icon;
        notificationMessageLabel.Text = message;
        notificationMessageLabel.ForeColor = textColor;
        notificationTimeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        notificationPanel.BackColor = bgColor;

        // Show panel and start auto-dismiss timer
        notificationPanel.Visible = true;
        notificationTimer.Stop();
        notificationTimer.Start();

        _logger.LogInformation("Notification displayed: [{Type}] {Message}", type, message);
    }

    /// <summary>
    /// Event handler for notification clear button
    /// </summary>
    private void NotificationClearButton_Click(object? sender, EventArgs e)
    {
        notificationTimer.Stop();
        notificationPanel.Visible = false;
    }

    /// <summary>
    /// Event handler for notification auto-dismiss timer
    /// </summary>
    private void NotificationTimer_Tick(object? sender, EventArgs e)
    {
        notificationTimer.Stop();
        notificationPanel.Visible = false;
    }

    #endregion
}
