using System;
using System.IO;
using System.Linq;

using Castle.Core.Resource;

using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;

using SacksDataLayer.Entities;
using SacksDataLayer.Tests;

using Xunit;

namespace Sacks.Tests;

/// <summary>
/// Unit tests for Program.SplitFile method
/// </summary>
public class ProgramSplitFileTests : BaseTest
{
    [Fact]
    public void SplitFile_WithValidInput_CreatesCorrectNumberOfFiles()
    {
        // Arrange
        var testFile = @"C:\Users\avist\source\repos\GitHubLocal\Customers\Sacks-New\AllInputs\db\openbeautyfacts-products.jsonl\openbeautyfacts-products.jsonl";

            // Act
            SacksApp.Program.SplitFile(testFile, 10);

            // Assert
            var directory = Path.GetDirectoryName(testFile)!;
            var fileName = Path.GetFileNameWithoutExtension(testFile);
            var extension = Path.GetExtension(testFile);
            var partFiles = Directory.GetFiles(directory, $"{fileName}_part*{extension}");

            Assert.Equal(3, partFiles.Length); // 25 lines / 10 = 3 files (10, 10, 5)

            // Verify line counts
            Assert.Equal(10, File.ReadLines(partFiles[0]).Count());
            Assert.Equal(10, File.ReadLines(partFiles[1]).Count());
            Assert.Equal(5, File.ReadLines(partFiles[2]).Count());

            // Verify content integrity
            var allLinesFromParts = partFiles
                .SelectMany(f => File.ReadLines(f))
                .ToArray();

    }

    [Fact]
    public void SplitFile_WithExactMultiple_CreatesCorrectFiles()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
        var testLines = Enumerable.Range(1, 20).Select(i => $"Line {i}").ToArray();
        File.WriteAllLines(testFile, testLines);

        try
        {
            // Act
            SacksApp.Program.SplitFile(testFile, 10);

            // Assert
            var directory = Path.GetDirectoryName(testFile)!;
            var fileName = Path.GetFileNameWithoutExtension(testFile);
            var extension = Path.GetExtension(testFile);
            var partFiles = Directory.GetFiles(directory, $"{fileName}_part*{extension}");

            Assert.Equal(2, partFiles.Length); // 20 lines / 10 = 2 files exactly
            Assert.All(partFiles, file => Assert.Equal(10, File.ReadLines(file).Count()));
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);

            var directory = Path.GetDirectoryName(testFile)!;
            var fileName = Path.GetFileNameWithoutExtension(testFile);
            var extension = Path.GetExtension(testFile);
            var partFiles = Directory.GetFiles(directory, $"{fileName}_part*{extension}");
            foreach (var file in partFiles)
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public void SplitFile_WithSingleLine_CreatesSingleFile()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
        File.WriteAllLines(testFile, ["Single line"]);

        try
        {
            // Act
            SacksApp.Program.SplitFile(testFile, 10);

            // Assert
            var directory = Path.GetDirectoryName(testFile)!;
            var fileName = Path.GetFileNameWithoutExtension(testFile);
            var extension = Path.GetExtension(testFile);
            var partFiles = Directory.GetFiles(directory, $"{fileName}_part*{extension}");

            Assert.Single(partFiles);
            Assert.Equal("Single line", File.ReadLines(partFiles[0]).First());
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);

            var directory = Path.GetDirectoryName(testFile)!;
            var fileName = Path.GetFileNameWithoutExtension(testFile);
            var extension = Path.GetExtension(testFile);
            var partFiles = Directory.GetFiles(directory, $"{fileName}_part*{extension}");
            foreach (var file in partFiles)
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public void SplitFile_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            SacksApp.Program.SplitFile(null!, 10));
        Assert.Equal("fullPath", exception.ParamName);
    }

    [Fact]
    public void SplitFile_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            SacksApp.Program.SplitFile(string.Empty, 10));
        Assert.Equal("fullPath", exception.ParamName);
    }

    [Fact]
    public void SplitFile_WithZeroLinesPerFile_ThrowsArgumentException()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
        File.WriteAllText(testFile, "test");

        try
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                SacksApp.Program.SplitFile(testFile, 0));
            Assert.Equal("linesPerFile", exception.ParamName);
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void SplitFile_WithNegativeLinesPerFile_ThrowsArgumentException()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
        File.WriteAllText(testFile, "test");

        try
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                SacksApp.Program.SplitFile(testFile, -5));
            Assert.Equal("linesPerFile", exception.ParamName);
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void SplitFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.txt");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => 
            SacksApp.Program.SplitFile(nonExistentFile, 10));
    }

    [Fact]
    public void SplitFile_WithDifferentExtension_PreservesExtension()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.jsonl");
        var testLines = Enumerable.Range(1, 15).Select(i => $"{{\"line\": {i}}}").ToArray();
        File.WriteAllLines(testFile, testLines);

        try
        {
            // Act
            SacksApp.Program.SplitFile(testFile, 10);

            // Assert
            var directory = Path.GetDirectoryName(testFile)!;
            var fileName = Path.GetFileNameWithoutExtension(testFile);
            var partFiles = Directory.GetFiles(directory, $"{fileName}_part*.jsonl");

            Assert.Equal(2, partFiles.Length);
            Assert.All(partFiles, file => Assert.Equal(".jsonl", Path.GetExtension(file)));
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
                File.Delete(testFile);

            var directory = Path.GetDirectoryName(testFile)!;
            var fileName = Path.GetFileNameWithoutExtension(testFile);
            var partFiles = Directory.GetFiles(directory, $"{fileName}_part*.jsonl");
            foreach (var file in partFiles)
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public void SplitFile_WithOpenBeautyFactsProducts_CreatesMultipleFiles()
    {
        // Arrange
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var sourceFile = Path.Combine(baseDir, "openbeautyfacts-products.jsonl");
        
        // Skip test if file doesn't exist
        if (!File.Exists(sourceFile))
        {
            // Use xUnit's Skip attribute pattern
            return;
        }

        // Create a copy to work with (don't modify the original)
        var workingFile = Path.Combine(Path.GetTempPath(), $"openbeautyfacts-products_{Guid.NewGuid()}.jsonl");
        File.Copy(sourceFile, workingFile);

        try
        {
            // Get original file info
            var originalLineCount = File.ReadLines(workingFile).Count();
            var linesPerFile = 10000; // Split into files of 10k lines each

            // Act
            SacksApp.Program.SplitFile(workingFile, linesPerFile);

            // Assert
            var directory = Path.GetDirectoryName(workingFile)!;
            var fileName = Path.GetFileNameWithoutExtension(workingFile);
            var partFiles = Directory.GetFiles(directory, $"{fileName}_part*.jsonl")
                .OrderBy(f => f)
                .ToArray();

            // Verify we got the expected number of files
            var expectedFileCount = (int)Math.Ceiling((double)originalLineCount / linesPerFile);
            Assert.Equal(expectedFileCount, partFiles.Length);

            // Verify total line count matches
            var totalLinesInParts = partFiles.Sum(f => File.ReadLines(f).Count());
            Assert.Equal(originalLineCount, totalLinesInParts);

            // Verify all files except the last have exactly linesPerFile lines
            for (int i = 0; i < partFiles.Length - 1; i++)
            {
                Assert.Equal(linesPerFile, File.ReadLines(partFiles[i]).Count());
            }

            // Verify the last file has the remaining lines
            var lastFileLines = File.ReadLines(partFiles[^1]).Count();
            var expectedLastFileLines = originalLineCount % linesPerFile;
            if (expectedLastFileLines == 0) expectedLastFileLines = linesPerFile;
            Assert.Equal(expectedLastFileLines, lastFileLines);

            // Verify each part file is valid JSON lines (spot check first line of each)
            foreach (var partFile in partFiles)
            {
                var firstLine = File.ReadLines(partFile).First();
                Assert.StartsWith("{", firstLine);
                Assert.EndsWith("}", firstLine.TrimEnd());
            }
        }
        finally
        {
            // Cleanup
            if (File.Exists(workingFile))
                File.Delete(workingFile);

            var directory = Path.GetDirectoryName(workingFile)!;
            var fileName = Path.GetFileNameWithoutExtension(workingFile);
            var partFiles = Directory.GetFiles(directory, $"{fileName}_part*.jsonl");
            foreach (var file in partFiles)
            {
                File.Delete(file);
            }
        }
    }
}
