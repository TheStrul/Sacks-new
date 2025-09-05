using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using QMobileDeviceServiceMenu;

using SacksDataLayer.Configuration;
using SacksDataLayer.FileProcessing.Interfaces;
using SacksDataLayer.Services.Implementations;
using SacksDataLayer.Services.Interfaces;

namespace SacksApp
{
    public partial class MainForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MainForm> _logger;

        public MainForm(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = _serviceProvider.GetRequiredService<ILogger<MainForm>>();

            InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Ensure database exists and create if needed
                var connectionService = _serviceProvider.GetRequiredService<IDatabaseConnectionService>();
                var (success, message, exception) = await connectionService.EnsureDatabaseExistsAsync();

                if (!success)
                {
                    _logger.LogWarning("Database operation failed: {Message}", message);

                    if (exception != null)
                    {
                        _logger.LogError(exception, "Database operation error details");
                    }

                    MessageBox.Show($"Database Issue: {message}\n\nPlease check your database configuration in appsettings.json",
                        "Database Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _logger.LogInformation("Database ready: {Message}", message);

                // Update status if controls exist
                if (Controls.Find("statusLabel", true).FirstOrDefault() is Label statusLabel)
                {
                    statusLabel.Text = $"? {message}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal application error during initialization");
                MessageBox.Show($"Fatal error during initialization: {ex.Message}",
                    "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ProcessFilesButton_Click(object sender, EventArgs e)
        {
            try
            {
                processFilesButton.Enabled = false;
                
                _logger.LogInformation("Starting file processing operation");
                
                // Use timeout and explicit error handling for service resolution
                var fileProcessingService = await ResolveFileProcessingServiceAsync();
                if (fileProcessingService == null)
                {
                    MessageBox.Show("File processing service is not available. Please check configuration files and try again.",
                        "Service Not Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                var inputsPath = FindInputsFolder();

                if (!Directory.Exists(inputsPath))
                {
                    MessageBox.Show($"Inputs folder not found at: {inputsPath}",
                        "Folder Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var files = Directory.GetFiles(inputsPath, "*.xlsx")
                                   .Where(f => !Path.GetFileName(f).StartsWith("~"))
                                   .ToArray();

                if (files.Length == 0)
                {
                    MessageBox.Show("No Excel files found in Inputs folder.",
                        "No Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _logger.LogInformation($"Found {files.Length} Excel file(s) in Inputs folder");

                foreach (var file in files)
                {
                    _logger.LogInformation($"Processing {Path.GetFileName(file)}");
                    await fileProcessingService.ProcessFileAsync(file, CancellationToken.None);
                    _logger.LogInformation($"Finished processing {Path.GetFileName(file)}");
                }

                MessageBox.Show($"Successfully processed {files.Length} file(s)!",
                    "Processing Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing files");
                MessageBox.Show($"Error processing files:\n{ex.Message}\n\nCheck LogViewer for details.",
                    "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                processFilesButton.Enabled = true;
            }
        }

        private async Task<IFileProcessingService?> ResolveFileProcessingServiceAsync()
        {
            try
            {
                _logger.LogDebug("Attempting to resolve IFileProcessingService...");
                
                // Use a timeout to prevent hanging
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                
                return await Task.Run(() =>
                {
                    try
                    {
                        return _serviceProvider.GetRequiredService<IFileProcessingService>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to resolve IFileProcessingService");
                        return null;
                    }
                }, cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Service resolution timed out after 10 seconds");
                MessageBox.Show("Service initialization is taking too long. This is likely due to configuration file loading issues.\n\nPlease check:\n• Configuration files exist\n• No circular dependencies\n• Async/await patterns in DI registration",
                    "Service Resolution Timeout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during service resolution");
                return null;
            }
        }

        private async void ClearDatabaseButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear ALL data from the database?\n\nThis action cannot be undone!",
                "Confirm Database Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes) return;

            try
            {
                clearDatabaseButton.Enabled = false;
                
                var databaseService = _serviceProvider.GetRequiredService<IDatabaseManagementService>();
                var clearResult = await databaseService.ClearAllDataAsync();

                if (clearResult.Success)
                {
                    var summary = string.Join("\n", clearResult.DeletedCounts.Select(kvp => $"{kvp.Key}: {kvp.Value:N0} records"));
                    MessageBox.Show($"Database cleared successfully!\n\nDeleted:\n{summary}\n\nCompleted in {clearResult.ElapsedMilliseconds:N0}ms",
                        "Database Cleared", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var errors = string.Join("\n", clearResult.Errors);
                    MessageBox.Show($"Failed to clear database:\n{clearResult.Message}\n\nErrors:\n{errors}",
                        "Clear Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing database");
                MessageBox.Show($"Error clearing database:\n{ex.Message}",
                    "Clear Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                clearDatabaseButton.Enabled = true;
            }
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
                    MessageBox.Show($"Cannot connect to database:\n{connectionResult.Message}",
                        "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var statistics = string.Join("\n",
                    connectionResult.TableCounts.OrderBy(x => x.Key)
                    .Select(kvp => $"{kvp.Key}: {kvp.Value:N0} records"));

                var totalRecords = connectionResult.TableCounts.Values.Sum();

                MessageBox.Show($"Database Statistics\n{new string('=', 30)}\n\n{statistics}\n\nTotal records: {totalRecords:N0}\n\nServer: {connectionResult.ServerInfo}",
                    "Database Statistics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading statistics");
                MessageBox.Show($"Error loading statistics:\n{ex.Message}",
                    "Statistics Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                showStatisticsButton.Enabled = true;
            }
        }

        private void TestConfigurationButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Configuration testing functionality will be implemented later.\n\nFor now, you can verify that the application starts correctly and connects to the database.",
                "Configuration Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ViewLogsButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Use the dedicated LogViewerForm for real-time monitoring
                LogViewerForm.ShowServiceLogs(AppContext.BaseDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening log viewer");
                MessageBox.Show($"Error opening log viewer: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SqlQueryButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Form disposal is managed by Windows Forms when form is closed
                // Suppressing CA2000 as this is standard pattern for non-modal forms
#pragma warning disable CA2000 // Dispose objects before losing scope
                var sqlForm = new SqlQueryForm(_serviceProvider);
#pragma warning restore CA2000
                sqlForm.Show(); // Non-modal so user can have multiple query windows
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening SQL Query Tool");
                MessageBox.Show($"Error opening SQL Query Tool: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string FindInputsFolder()
        {
            var currentDirectory = Environment.CurrentDirectory;

            // Strategy 1: Check if we're running from project folder
            var strategy1 = Path.Combine(currentDirectory, "..", "Inputs");
            if (Directory.Exists(strategy1))
            {
                return Path.GetFullPath(strategy1);
            }

            // Strategy 2: Check if we're running from bin folder
            var strategy2 = Path.Combine(currentDirectory, "..", "..", "..", "..", "Inputs");
            if (Directory.Exists(strategy2))
            {
                return Path.GetFullPath(strategy2);
            }

            // Strategy 3: Search upward for solution file
            var searchDir = new DirectoryInfo(currentDirectory);
            while (searchDir != null)
            {
                var solutionFile = searchDir.GetFiles("*.sln").FirstOrDefault();
                if (solutionFile != null)
                {
                    var solutionInputsPath = Path.Combine(searchDir.FullName, "Inputs");
                    if (Directory.Exists(solutionInputsPath))
                    {
                        return solutionInputsPath;
                    }
                }
                searchDir = searchDir.Parent;
            }

            return Path.Combine(currentDirectory, "Inputs");
        }
    }
}
