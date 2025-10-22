using System.Data;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using QMobileDeviceServiceMenu;

using SacksDataLayer.Services.Interfaces;

using SacksLogicLayer.Services.Interfaces;

namespace SacksApp
{
    public partial class DashBoard : Form
    {
        IServiceProvider _serviceProvider;
        ILogger _logger;

        // Designer-friendly ctor: lets the WinForms designer instantiate the form and see themed buttons
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Design-time only")]
        public DashBoard()
        {
            InitializeComponent();
            _serviceProvider = new ServiceCollection().BuildServiceProvider();
            _logger = NullLogger.Instance;
        }

        public DashBoard(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            this._serviceProvider = serviceProvider;
            this._logger = serviceProvider.GetRequiredService<ILogger<DashBoard>>();

            // CustomButton styling is now handled directly in the designer - no need for ApplyModernTheme
        }

        private async void ProcessFilesButton_Click(object sender, EventArgs e)
        {
            try
            {
                processFilesButton.Enabled = false;

                _logger.LogDebug("Starting file processing operation");

                var fileProcessingService = _serviceProvider.GetRequiredService<IFileProcessingService>();

                // Get input folder from configuration
                var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
                var inputsPath = GetInputDirectoryFromConfiguration(configuration);

                var files = Directory.GetFiles(inputsPath, "*.xls*")
                                   .Where(f => !Path.GetFileName(f).StartsWith("~"))
                                   .ToArray();

                if (files.Length == 0)
                {
                    MessageBox.Show("No Excel files found in Inputs folder.",
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

        /// <summary>
        /// Gets input directory from configuration using proper path resolution
        /// </summary>
        private string GetInputDirectoryFromConfiguration(IConfiguration configuration)
        {
            var configuredPath = configuration["FileProcessingSettings:InputDirectory"];

            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                throw new InvalidOperationException("FileProcessingSettings:InputDirectory is not configured in appsettings.json");
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
            // Non-modal forms are not owned by a using scope; suppress CA2000 as WinForms disposes the form when closed
#pragma warning disable CA2000 // Dispose objects before losing scope
            var t = new TestPattern();
#pragma warning restore CA2000
            t.ShowDialog(this);
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

        private void ViewLogsButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Look for logs at solution root instead of executable directory
                var solutionRoot = FindSolutionRoot();
                LogViewerForm.ShowServiceLogs(solutionRoot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening log viewer");
                MessageBox.Show($"Error opening log viewer: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Finds the solution root directory by searching upward from the current executable location
        /// </summary>
        private static string FindSolutionRoot()
        {
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

            // Search upward for solution file (.sln)
            while (currentDirectory != null)
            {
                var solutionFile = currentDirectory.GetFiles("*.sln").FirstOrDefault();
                if (solutionFile != null)
                {
                    return currentDirectory.FullName;
                }
                currentDirectory = currentDirectory.Parent;
            }

            throw new DirectoryNotFoundException("Solution root directory not found - no .sln file found in directory hierarchy");
        }

        private void ButtonEditMaps_Click(object sender, EventArgs e)
        {
            try
            {
                // Open the general Lookup editor. Initial selection is empty -> form will pick first available lookup.
#pragma warning disable CA2000 // Dispose objects before losing scope
                var editor = new LookupEditorForm(_serviceProvider, string.Empty);
#pragma warning restore CA2000
                editor.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening Lookup Editor");
                MessageBox.Show($"Error opening Lookup Editor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Gets products directory from configuration or uses default
        /// </summary>
        private string GetProductsDirectoryFromConfiguration(IConfiguration configuration)
        {
            var configuredPath = configuration["OpenBeautyFactsSettings:InputDirectory"];

            // Default to AllInputs/db if not configured
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                var solutionRoot = FindSolutionRoot();
                configuredPath = Path.Combine(solutionRoot, "AllInputs", "db");
            }
            else if (!Path.IsPathRooted(configuredPath))
            {
                // Handle relative paths
                var solutionRoot = FindSolutionRoot();
                configuredPath = Path.GetFullPath(Path.Combine(solutionRoot, configuredPath));
            }

            // Create directory if it doesn't exist
            if (!Directory.Exists(configuredPath))
            {
                try
                {
                    Directory.CreateDirectory(configuredPath);
                    _logger.LogDebug("Created products directory: {ProductsPath}", configuredPath);
                }
                catch (Exception ex)
                {
                    throw new DirectoryNotFoundException($"Cannot create products directory at {configuredPath}: {ex.Message}", ex);
                }
            }

            _logger.LogDebug("Using products directory: {ProductsPath}", configuredPath);
            return configuredPath;
        }

        private void HandleOffersButton_Click(object sender, EventArgs e)
        {
            try
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                var form = new OffersForm(_serviceProvider);
#pragma warning restore CA2000
                form.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening Offers manager");
                MessageBox.Show($"Error opening Offers manager: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
