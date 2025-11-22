using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using McpServer.Core.Tools;

namespace SacksMcp.Tools;

/// <summary>
/// System-level MCP tools for managing domain knowledge and system configuration.
/// These tools allow the AI to improve itself by recording insights and learnings.
/// </summary>
[McpServerToolType]
public class SystemTools : BaseMcpToolCollection
{
    private readonly string _domainContextPath;

    public SystemTools(ILogger<SystemTools> logger) : base(logger)
    {
        // AI learnings file is separate from user-maintained domain context
        _domainContextPath = Path.Combine(
            Directory.GetCurrentDirectory(), 
            "Sacks.Configuration", 
            "ai-learnings.md");
    }

    /// <summary>
    /// Record a learning or insight about the domain to improve future queries.
    /// </summary>
    [McpServerTool]
    [Description("Record an insight or learning about the domain (entities, relationships, user patterns) to improve future query understanding. Use this when you discover important patterns, common user terminology, or data relationships.")]
    public async Task<string> RecordDomainLearning(
        [Description("The insight or learning to record (e.g., 'Users often ask for travel size meaning under 100ml')")] string learning,
        [Description("Optional category: terminology, pattern, relationship, or insight")] string? category = null)
    {
        ValidateRequired(learning, nameof(learning));
        
        Logger.LogInformation("Recording domain learning: {Learning}", learning);

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_domainContextPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Read existing content
            string existingContent;
            if (File.Exists(_domainContextPath))
            {
                existingContent = await File.ReadAllTextAsync(_domainContextPath).ConfigureAwait(false);
            }
            else
            {
                // Create basic structure if file doesn't exist
                existingContent = "# AI Self-Learning Knowledge Base\n\n## Recorded Learnings\n\n";
            }

            // Find or create the Recorded Learnings section
            var learningSection = "## Recorded Learnings";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
            var categoryTag = !string.IsNullOrWhiteSpace(category) ? $"[{category}]" : "";
            var newEntry = $"- {timestamp} {categoryTag}: {learning}";

            string updatedContent;
            if (existingContent.Contains(learningSection))
            {
                // Append to existing learning section
                var sectionIndex = existingContent.IndexOf(learningSection, StringComparison.Ordinal);
                var afterSection = existingContent.Substring(sectionIndex + learningSection.Length);
                var nextSectionIndex = afterSection.IndexOf("\n## ", StringComparison.Ordinal);
                
                if (nextSectionIndex > 0)
                {
                    // Insert before next section
                    var beforeNextSection = existingContent.Substring(0, sectionIndex + learningSection.Length + nextSectionIndex);
                    var afterNextSection = existingContent.Substring(sectionIndex + learningSection.Length + nextSectionIndex);
                    updatedContent = $"{beforeNextSection}\n{newEntry}\n{afterNextSection}";
                }
                else
                {
                    // Append to end
                    updatedContent = $"{existingContent.TrimEnd()}\n{newEntry}\n";
                }
            }
            else
            {
                // Add new learning section at the end
                updatedContent = $"{existingContent.TrimEnd()}\n\n{learningSection}\n\n{newEntry}\n";
            }

            // Write back to file
            await File.WriteAllTextAsync(_domainContextPath, updatedContent).ConfigureAwait(false);

            Logger.LogInformation("Successfully recorded domain learning to {Path}", _domainContextPath);

            return FormatSuccess(new
            {
                recorded = true,
                learning,
                category = category ?? "general",
                timestamp,
                filePath = _domainContextPath
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to record domain learning");
            return FormatError($"Failed to record learning: {ex.Message}");
        }
    }

    /// <summary>
    /// Read the AI learnings to see what has been discovered so far.
    /// </summary>
    [McpServerTool]
    [Description("Read the AI learnings file to see what insights have been recorded about user terminology, patterns, and domain relationships.")]
    public async Task<string> ReadAiLearnings()
    {
        Logger.LogInformation("Reading AI learnings from {Path}", _domainContextPath);

        try
        {
            if (!File.Exists(_domainContextPath))
            {
                return FormatSuccess(new
                {
                    found = false,
                    message = "AI learnings file not found - no learnings recorded yet",
                    filePath = _domainContextPath
                });
            }

            var content = await File.ReadAllTextAsync(_domainContextPath).ConfigureAwait(false);

            return FormatSuccess(new
            {
                found = true,
                filePath = _domainContextPath,
                content,
                lastModified = File.GetLastWriteTimeUtc(_domainContextPath)
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read AI learnings");
            return FormatError($"Failed to read AI learnings: {ex.Message}");
        }
    }
}
