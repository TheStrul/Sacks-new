namespace SacksLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Service for discovering and filtering files for processing
    /// </summary>
    public interface IFileDiscoveryService
    {
        /// <summary>
        /// Discovers files in the configured input directory matching patterns
        /// </summary>
        /// <param name="patterns">File patterns to match (e.g., "*.xls*")</param>
        /// <param name="excludeTemporary">Whether to exclude temporary files (starting with ~)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of file paths</returns>
        Task<IReadOnlyList<string>> DiscoverFilesAsync(
            string[] patterns, 
            bool excludeTemporary = true, 
            CancellationToken cancellationToken = default);
    }
}
