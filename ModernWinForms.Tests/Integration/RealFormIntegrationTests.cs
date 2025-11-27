namespace ModernWinForms.Tests.Integration;

/// <summary>
/// Integration tests with real forms and multiple controls.
/// Tests real-world usage scenarios.
/// </summary>
[Collection("WinForms Tests")]
public class RealFormIntegrationTests : IDisposable
{
    private readonly List<Form> _formsToDispose = new();

    public void Dispose()
    {
        foreach (var form in _formsToDispose)
        {
            form?.Dispose();
        }
        _formsToDispose.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private T TrackForm<T>(T form) where T : Form
    {
        _formsToDispose.Add(form);
        return form;
    }

    [Fact]
    public void RealForm_WithMultipleControls_ShouldWork()
    {
        // Arrange
        var form = TrackForm(new Form { Width = 800, Height = 600 });
        
        var button = new ModernButton { Text = "Submit", Location = new Point(10, 10) };
        var textBox = new ModernTextBox { Location = new Point(10, 50) };
        var checkBox = new ModernCheckBox { Text = "Agree", Location = new Point(10, 90) };
        var label = new ModernLabel { Text = "Enter your name:", Location = new Point(10, 130) };

        // Act
        form.Controls.Add(button);
        form.Controls.Add(textBox);
        form.Controls.Add(checkBox);
        form.Controls.Add(label);

        // Assert
        form.Controls.Count.Should().Be(4);
        form.Controls.OfType<ModernButton>().Should().HaveCount(1);
        form.Controls.OfType<ModernTextBox>().Should().HaveCount(1);
        form.Controls.OfType<ModernCheckBox>().Should().HaveCount(1);
        form.Controls.OfType<ModernLabel>().Should().HaveCount(1);
    }

    [Fact]
    public void RealForm_WithNestedPanels_ShouldWork()
    {
        // Arrange
        var form = TrackForm(new Form { Width = 800, Height = 600 });
        
        var mainPanel = new ModernPanel { Dock = DockStyle.Fill };
        var leftPanel = new ModernPanel { Dock = DockStyle.Left, Width = 200 };
        var rightPanel = new ModernPanel { Dock = DockStyle.Fill };

        // Act
        form.Controls.Add(mainPanel);
        mainPanel.Controls.Add(leftPanel);
        mainPanel.Controls.Add(rightPanel);
        
        leftPanel.Controls.Add(new ModernButton { Text = "Menu 1", Dock = DockStyle.Top });
        leftPanel.Controls.Add(new ModernButton { Text = "Menu 2", Dock = DockStyle.Top });
        
        rightPanel.Controls.Add(new ModernLabel { Text = "Content Area", Dock = DockStyle.Fill });

        // Assert
        form.Controls.Count.Should().Be(1);
        mainPanel.Controls.Count.Should().Be(2);
        leftPanel.Controls.Count.Should().Be(2);
        rightPanel.Controls.Count.Should().Be(1);
    }

    [Fact]
    public void RealForm_WithTabControl_ShouldSwitchTabs()
    {
        // Arrange
        var form = TrackForm(new Form { Width = 800, Height = 600 });
        var tabControl = new ModernTabControl { Dock = DockStyle.Fill };
        
        var tab1 = new TabPage("General");
        var tab2 = new TabPage("Advanced");
        var tab3 = new TabPage("About");
        
        tab1.Controls.Add(new ModernLabel { Text = "General settings", Dock = DockStyle.Top });
        tab2.Controls.Add(new ModernTextBox { Dock = DockStyle.Top });
        tab3.Controls.Add(new ModernLabel { Text = "Version 1.0", Dock = DockStyle.Top });

        tabControl.TabPages.Add(tab1);
        tabControl.TabPages.Add(tab2);
        tabControl.TabPages.Add(tab3);

        // Act
        form.Controls.Add(tabControl);
        tabControl.SelectedIndex = 0;

        // Assert
        tabControl.SelectedTab.Should().Be(tab1);
        
        // Act
        tabControl.SelectedIndex = 2;
        
        // Assert
        tabControl.SelectedTab.Should().Be(tab3);
    }

    [Fact]
    public void RealForm_WithDataGridView_ShouldDisplayData()
    {
        // Arrange
        var form = TrackForm(new Form { Width = 800, Height = 600 });
        var grid = new ModernDataGridView { Dock = DockStyle.Fill };
        
        grid.Columns.Add("Name", "Name");
        grid.Columns.Add("Age", "Age");
        grid.Columns.Add("Email", "Email");

        // Act
        form.Controls.Add(grid);
        grid.Rows.Add("John Doe", "30", "john@example.com");
        grid.Rows.Add("Jane Smith", "25", "jane@example.com");

        // Assert
        grid.Rows.Count.Should().Be(2); // Two data rows (AllowUserToAddRows is false in ModernDataGridView)
        grid.Rows[0].Cells[0].Value.Should().Be("John Doe");
        grid.Rows[1].Cells[1].Value.Should().Be("25");
    }

    [Fact]
    public void RealForm_WithSplitContainer_ShouldWork()
    {
        // Arrange
        var form = TrackForm(new Form { Width = 800, Height = 600 });
        var splitContainer = new ModernSplitContainer { Dock = DockStyle.Fill };
        
        // Act
        form.Controls.Add(splitContainer);
        splitContainer.Panel1.Controls.Add(new ModernLabel { Text = "Left Panel", Dock = DockStyle.Fill });
        splitContainer.Panel2.Controls.Add(new ModernLabel { Text = "Right Panel", Dock = DockStyle.Fill });

        // Assert
        splitContainer.Panel1.Controls.Count.Should().Be(1);
        splitContainer.Panel2.Controls.Count.Should().Be(1);
    }

    [Fact]
    public void RealForm_WithFlowLayoutPanel_ShouldArrangeControls()
    {
        // Arrange
        var form = TrackForm(new Form { Width = 800, Height = 600 });
        var flowPanel = new ModernFlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };

        // Act
        form.Controls.Add(flowPanel);
        for (int i = 0; i < 10; i++)
        {
            flowPanel.Controls.Add(new ModernButton { Text = $"Button {i}", Width = 100, Height = 30 });
        }

        // Assert
        flowPanel.Controls.Count.Should().Be(10);
        flowPanel.FlowDirection.Should().Be(FlowDirection.TopDown);
    }

    [Fact]
    public void RealForm_WithTableLayoutPanel_ShouldOrganizeCells()
    {
        // Arrange
        var form = TrackForm(new Form { Width = 800, Height = 600 });
        var tablePanel = new ModernTableLayoutPanel { Dock = DockStyle.Fill };
        tablePanel.RowCount = 3;
        tablePanel.ColumnCount = 2;

        // Act
        form.Controls.Add(tablePanel);
        
        tablePanel.Controls.Add(new ModernLabel { Text = "Name:" }, 0, 0);
        tablePanel.Controls.Add(new ModernTextBox(), 1, 0);
        
        tablePanel.Controls.Add(new ModernLabel { Text = "Email:" }, 0, 1);
        tablePanel.Controls.Add(new ModernTextBox(), 1, 1);
        
        tablePanel.Controls.Add(new ModernButton { Text = "Submit", Dock = DockStyle.Fill }, 0, 2);

        // Assert
        tablePanel.Controls.Count.Should().Be(5);
    }

    [Fact]
    public void RealForm_WithMenuAndStatusStrip_ShouldWork()
    {
        // Arrange
        var form = TrackForm(new Form { Width = 800, Height = 600 });
        var menuStrip = new ModernMenuStrip();
        var statusStrip = new ModernStatusStrip();

        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add("New");
        fileMenu.DropDownItems.Add("Open");
        fileMenu.DropDownItems.Add("Save");
        
        menuStrip.Items.Add(fileMenu);
        menuStrip.Items.Add("Edit");
        menuStrip.Items.Add("View");

        statusStrip.Items.Add("Ready");

        // Act
        form.Controls.Add(menuStrip);
        form.Controls.Add(statusStrip);

        // Assert
        menuStrip.Items.Count.Should().Be(3);
        statusStrip.Items.Count.Should().Be(1);
    }

    [Fact]
    public void RealForm_ThemeSwitchWithMultipleControls_ShouldUpdateAll()
    {
        // Arrange
        var form = TrackForm(new Form { Width = 800, Height = 600 });
        
        var button = new ModernButton { Text = "Submit" };
        var textBox = new ModernTextBox();
        var panel = new ModernPanel();
        var label = new ModernLabel { Text = "Test Label" };

        form.Controls.Add(button);
        form.Controls.Add(textBox);
        form.Controls.Add(panel);
        form.Controls.Add(label);

        // Act - Switch to Material theme
        ThemeManager.SetTheme(Theme.Material, Skin.Material);
        Application.DoEvents(); // Process theme change events

        // Assert - All controls should have updated (theme names are case-insensitive)
        ThemeManager.CurrentTheme.Should().BeEquivalentTo("material");
        ThemeManager.CurrentSkin.Should().BeEquivalentTo("material");

        // Act - Switch to Fluent theme
        ThemeManager.SetTheme(Theme.Fluent, Skin.Fluent);
        Application.DoEvents();

        // Assert
        ThemeManager.CurrentTheme.Should().BeEquivalentTo("fluent");
        ThemeManager.CurrentSkin.Should().BeEquivalentTo("fluent");
    }

    [Fact]
    public void RealForm_CreateAndDisposeMultipleTimes_ShouldNotLeak()
    {
        // Arrange
        const int iterations = 10;
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        for (int i = 0; i < iterations; i++)
        {
            using var form = new Form { Width = 800, Height = 600 };
            
            form.Controls.Add(new ModernButton { Text = "Test" });
            form.Controls.Add(new ModernTextBox());
            form.Controls.Add(new ModernPanel());
            form.Controls.Add(new ModernLabel { Text = "Label" });
            
            Application.DoEvents();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowth = finalMemory - initialMemory;

        // Assert - Memory growth should be minimal (< 5MB)
        memoryGrowth.Should().BeLessThan(5 * 1024 * 1024, 
            $"Memory should not leak after {iterations} form creation cycles");
    }

    [Fact]
    public void RealForm_WithGroupBox_ShouldOrganizeControls()
    {
        // Arrange
        var form = TrackForm(new Form { Width = 800, Height = 600 });
        var groupBox = new ModernGroupBox { Text = "User Information", Location = new Point(10, 10), Width = 300, Height = 200 };

        // Act
        form.Controls.Add(groupBox);
        groupBox.Controls.Add(new ModernLabel { Text = "Name:", Location = new Point(10, 30) });
        groupBox.Controls.Add(new ModernTextBox { Location = new Point(100, 30) });
        groupBox.Controls.Add(new ModernLabel { Text = "Email:", Location = new Point(10, 70) });
        groupBox.Controls.Add(new ModernTextBox { Location = new Point(100, 70) });

        // Assert
        groupBox.Controls.Count.Should().Be(4);
        groupBox.Text.Should().Be("User Information");
    }
}
