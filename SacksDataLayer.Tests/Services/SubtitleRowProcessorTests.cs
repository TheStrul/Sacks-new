using Microsoft.Extensions.Logging;
using SacksDataLayer.FileProcessing.Configuration;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Services;
using Xunit;

namespace SacksDataLayer.Tests.Services
{
    /// <summary>
    /// Unit tests for SubtitleRowProcessor functionality
    /// </summary>
    public class SubtitleRowProcessorTests
    {
        private readonly SubtitleRowProcessor _processor;

        public SubtitleRowProcessorTests()
        {
            _processor = new SubtitleRowProcessor();
        }

        [Fact]
        public async Task ProcessSubtitleRowsAsync_WithDisabledConfig_ReturnsOriginalData()
        {
            // Arrange
            var fileData = CreateTestFileData();
            var supplierConfig = new SupplierConfiguration
            {
                Name = "TestSupplier",
                SubtitleHandling = new SubtitleRowHandlingConfiguration { Enabled = false }
            };

            // Act
            var result = await _processor.ProcessSubtitleRowsAsync(fileData, supplierConfig);

            // Assert
            Assert.Equal(fileData, result);
            Assert.False(result.DataRows.Any(r => r.IsSubtitleRow));
        }

        [Fact]
        public async Task ProcessSubtitleRowsAsync_WithBrandSubtitleRule_DetectsAndExtractsBrand()
        {
            // Arrange
            var fileData = CreateTestFileDataWithBrandSubtitle();
            var supplierConfig = CreateMBSupplierConfiguration();

            // Act
            var result = await _processor.ProcessSubtitleRowsAsync(fileData, supplierConfig);

            // Assert
            var subtitleRow = result.DataRows.FirstOrDefault(r => r.IsSubtitleRow);
            Assert.NotNull(subtitleRow);
            Assert.Equal("BrandSubtitle", subtitleRow.SubtitleRuleName);
            Assert.True(subtitleRow.SubtitleData.ContainsKey("Brand"));
            Assert.Equal("CHANEL", subtitleRow.SubtitleData["Brand"]);

            // Check that subsequent rows have the brand applied
            var dataRows = result.DataRows.Where(r => !r.IsSubtitleRow && r.HasData).ToList();
            Assert.All(dataRows, row => Assert.True(row.SubtitleData.ContainsKey("Brand")));
        }

        [Fact]
        public void FilterDataRows_WithSubtitleHandling_FiltersSubtitleRows()
        {
            // Arrange
            var rows = new List<RowData>
            {
                new RowData(0) { IsSubtitleRow = false },
                new RowData(1) { IsSubtitleRow = true },
                new RowData(2) { IsSubtitleRow = false }
            };

            var config = new SubtitleRowHandlingConfiguration { Enabled = true };

            // Act
            var result = SubtitleRowProcessor.FilterDataRows(rows, config).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, row => Assert.False(row.IsSubtitleRow));
        }

        private FileData CreateTestFileData()
        {
            var fileData = new FileData("test.xlsx");
            
            // Add header row
            var headerRow = new RowData(0);
            headerRow.Cells.Add(new CellData(0, "Brand"));
            headerRow.Cells.Add(new CellData(1, "Price"));
            headerRow.Cells.Add(new CellData(2, "Quantity"));
            headerRow.Cells.Add(new CellData(3, "EAN"));
            fileData.DataRows.Add(headerRow);

            // Add data rows
            var dataRow = new RowData(1);
            dataRow.Cells.Add(new CellData(0, "CHANEL"));
            dataRow.Cells.Add(new CellData(1, "120.00"));
            dataRow.Cells.Add(new CellData(2, "5"));
            dataRow.Cells.Add(new CellData(3, "1234567890123"));
            fileData.DataRows.Add(dataRow);

            return fileData;
        }

        private FileData CreateTestFileDataWithBrandSubtitle()
        {
            var fileData = new FileData("test.xlsx");
            
            // Add header row
            var headerRow = new RowData(0);
            headerRow.Cells.Add(new CellData(0, "Brand"));
            headerRow.Cells.Add(new CellData(1, "Price"));
            headerRow.Cells.Add(new CellData(2, "Quantity"));
            headerRow.Cells.Add(new CellData(3, "EAN"));
            fileData.DataRows.Add(headerRow);

            // Add subtitle row (brand section)
            var subtitleRow = new RowData(1);
            subtitleRow.Cells.Add(new CellData(0, "CHANEL"));
            fileData.DataRows.Add(subtitleRow);

            // Add data rows
            var dataRow1 = new RowData(2);
            dataRow1.Cells.Add(new CellData(0, ""));
            dataRow1.Cells.Add(new CellData(1, "120.00"));
            dataRow1.Cells.Add(new CellData(2, "5"));
            dataRow1.Cells.Add(new CellData(3, "1234567890123"));
            fileData.DataRows.Add(dataRow1);

            var dataRow2 = new RowData(3);
            dataRow2.Cells.Add(new CellData(0, ""));
            dataRow2.Cells.Add(new CellData(1, "85.50"));
            dataRow2.Cells.Add(new CellData(2, "3"));
            dataRow2.Cells.Add(new CellData(3, "1234567890124"));
            fileData.DataRows.Add(dataRow2);

            return fileData;
        }

        private SupplierConfiguration CreateMBSupplierConfiguration()
        {
            return new SupplierConfiguration
            {
                Name = "MB",
                SubtitleHandling = new SubtitleRowHandlingConfiguration
                {
                    Enabled = true,
                    Action = "parse",
                    DetectionRules = new List<SubtitleDetectionRule>
                    {
                        new SubtitleDetectionRule
                        {
                            Name = "BrandSubtitle",
                            Description = "Detects brand information from subtitle rows in column A",
                            DetectionMethod = "columnCount",
                            ExpectedColumnCount = 1,
                            ApplyToSubsequentRows = true,
                            ValidationPatterns = new List<string>()
                        }
                    },
                    FallbackAction = "skip"
                }
            };
        }
    }
}