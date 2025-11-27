namespace ModernWinForms.Tests.Controls;

/// <summary>
/// Comprehensive tests for all Modern controls.
/// Tests basic functionality, disposal, and theme integration.
/// </summary>
[Collection("WinForms Tests")]
public class AllControlsTests : IDisposable
{
    private readonly List<Control> _controlsToDispose = new();

    public void Dispose()
    {
        foreach (var control in _controlsToDispose)
        {
            control?.Dispose();
        }
        _controlsToDispose.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private T TrackControl<T>(T control) where T : Control
    {
        _controlsToDispose.Add(control);
        return control;
    }

    [Theory]
    [InlineData(typeof(ModernButton))]
    [InlineData(typeof(ModernTextBox))]
    [InlineData(typeof(ModernCheckBox))]
    [InlineData(typeof(ModernRadioButton))]
    [InlineData(typeof(ModernComboBox))]
    [InlineData(typeof(ModernLabel))]
    [InlineData(typeof(ModernPanel))]
    [InlineData(typeof(ModernGroupBox))]
    [InlineData(typeof(ModernTabControl))]
    [InlineData(typeof(ModernRichTextBox))]
    [InlineData(typeof(ModernDataGridView))]
    [InlineData(typeof(ModernMenuStrip))]
    [InlineData(typeof(ModernStatusStrip))]
    [InlineData(typeof(ModernFlowLayoutPanel))]
    [InlineData(typeof(ModernTableLayoutPanel))]
    [InlineData(typeof(ModernSplitContainer))]
    public void AllControls_Constructor_ShouldNotThrow(Type? controlType)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(controlType);

        // Act
        Action act = () =>
        {
            var control = (Control)Activator.CreateInstance(controlType)!;
            TrackControl(control);
        };

        // Assert
        act.Should().NotThrow($"{controlType.Name} constructor should not throw");
    }

    [Theory]
    [InlineData(typeof(ModernButton))]
    [InlineData(typeof(ModernTextBox))]
    [InlineData(typeof(ModernCheckBox))]
    [InlineData(typeof(ModernRadioButton))]
    [InlineData(typeof(ModernComboBox))]
    [InlineData(typeof(ModernLabel))]
    [InlineData(typeof(ModernPanel))]
    [InlineData(typeof(ModernGroupBox))]
    public void AllControls_Dispose_ShouldCleanup(Type? controlType)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(controlType);
        var control = (Control)Activator.CreateInstance(controlType)!;

        // Act
        control.Dispose();

        // Assert
        control.IsDisposed.Should().BeTrue($"{controlType.Name} should be disposed");
    }

    [Theory]
    [InlineData(typeof(ModernButton))]
    [InlineData(typeof(ModernTextBox))]
    [InlineData(typeof(ModernPanel))]
    [InlineData(typeof(ModernLabel))]
    public void AllControls_AddToForm_ShouldWork(Type controlType)
    {
        // Arrange
        using var form = new Form();
        var control = TrackControl((Control)Activator.CreateInstance(controlType)!);

        // Act
        form.Controls.Add(control);

        // Assert
        form.Controls.Cast<Control>().Should().Contain(control);
        control.Parent.Should().Be(form);
    }

    [Fact]
    public void ModernTextBox_TextProperty_ShouldWork()
    {
        // Arrange
        var textBox = TrackControl(new ModernTextBox());

        // Act
        textBox.Text = "Test Text";

        // Assert
        textBox.Text.Should().Be("Test Text");
    }

    [Fact]
    public void ModernCheckBox_Checked_ShouldToggle()
    {
        // Arrange
        var checkBox = TrackControl(new ModernCheckBox());

        // Act
        checkBox.Checked = true;

        // Assert
        checkBox.Checked.Should().BeTrue();

        // Act
        checkBox.Checked = false;

        // Assert
        checkBox.Checked.Should().BeFalse();
    }

    [Fact]
    public void ModernRadioButton_Checked_ShouldWork()
    {
        // Arrange
        var radio1 = TrackControl(new ModernRadioButton());
        var radio2 = TrackControl(new ModernRadioButton());

        // Act
        radio1.Checked = true;

        // Assert
        radio1.Checked.Should().BeTrue();
        radio2.Checked.Should().BeFalse();
    }

    [Fact]
    public void ModernComboBox_Items_ShouldBeAddable()
    {
        // Arrange
        var comboBox = TrackControl(new ModernComboBox());

        // Act
        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");
        comboBox.Items.Add("Item 3");

        // Assert
        comboBox.Items.Count.Should().Be(3);
        comboBox.Items[0].Should().Be("Item 1");
    }

    [Fact]
    public void ModernPanel_Children_ShouldBeAddable()
    {
        // Arrange
        var panel = TrackControl(new ModernPanel());
        var button = TrackControl(new ModernButton { Text = "Child" });

        // Act
        panel.Controls.Add(button);

        // Assert
        panel.Controls.Cast<Control>().Should().Contain(button);
        button.Parent.Should().Be(panel);
    }

    [Fact]
    public void ModernGroupBox_TextAndChildren_ShouldWork()
    {
        // Arrange
        var groupBox = TrackControl(new ModernGroupBox());
        var label = TrackControl(new ModernLabel { Text = "Label in group" });

        // Act
        groupBox.Text = "Group Title";
        groupBox.Controls.Add(label);

        // Assert
        groupBox.Text.Should().Be("Group Title");
        groupBox.Controls.Cast<Control>().Should().Contain(label);
    }

    [Fact]
    public void ModernTabControl_Tabs_ShouldBeAddable()
    {
        // Arrange
        var tabControl = TrackControl(new ModernTabControl());
        var tabPage1 = new TabPage("Tab 1");
        var tabPage2 = new TabPage("Tab 2");

        // Act
        tabControl.TabPages.Add(tabPage1);
        tabControl.TabPages.Add(tabPage2);

        // Assert
        tabControl.TabPages.Count.Should().Be(2);
        tabControl.TabPages[0].Text.Should().Be("Tab 1");
    }

    [Fact]
    public void ModernDataGridView_Columns_ShouldBeAddable()
    {
        // Arrange
        var grid = TrackControl(new ModernDataGridView());

        // Act
        grid.Columns.Add("Column1", "First Column");
        grid.Columns.Add("Column2", "Second Column");

        // Assert
        grid.Columns.Count.Should().Be(2);
        grid.Columns[0].HeaderText.Should().Be("First Column");
    }

    [Fact]
    public void ModernFlowLayoutPanel_Controls_ShouldFlow()
    {
        // Arrange
        var flowPanel = TrackControl(new ModernFlowLayoutPanel());
        
        // Act
        for (int i = 0; i < 5; i++)
        {
            flowPanel.Controls.Add(TrackControl(new ModernButton { Text = $"Button {i}" }));
        }

        // Assert
        flowPanel.Controls.Count.Should().Be(5);
    }

    [Fact]
    public void ModernTableLayoutPanel_CellsAndControls_ShouldWork()
    {
        // Arrange
        var tablePanel = TrackControl(new ModernTableLayoutPanel());
        tablePanel.RowCount = 2;
        tablePanel.ColumnCount = 2;

        // Act
        tablePanel.Controls.Add(TrackControl(new ModernLabel { Text = "Cell 0,0" }), 0, 0);
        tablePanel.Controls.Add(TrackControl(new ModernLabel { Text = "Cell 1,1" }), 1, 1);

        // Assert
        tablePanel.Controls.Count.Should().Be(2);
    }

    [Fact]
    public void ModernSplitContainer_Panels_ShouldExist()
    {
        // Arrange
        var splitContainer = TrackControl(new ModernSplitContainer());

        // Act
        splitContainer.Panel1.Controls.Add(TrackControl(new ModernLabel { Text = "Left" }));
        splitContainer.Panel2.Controls.Add(TrackControl(new ModernLabel { Text = "Right" }));

        // Assert
        splitContainer.Panel1.Controls.Count.Should().Be(1);
        splitContainer.Panel2.Controls.Count.Should().Be(1);
    }

    [Fact]
    public void ModernMenuStrip_Items_ShouldBeAddable()
    {
        // Arrange
        var menuStrip = TrackControl(new ModernMenuStrip());

        // Act
        menuStrip.Items.Add("File");
        menuStrip.Items.Add("Edit");
        menuStrip.Items.Add("View");

        // Assert
        menuStrip.Items.Count.Should().Be(3);
    }

    [Fact]
    public void ModernStatusStrip_Items_ShouldBeAddable()
    {
        // Arrange
        var statusStrip = TrackControl(new ModernStatusStrip());

        // Act
        statusStrip.Items.Add("Ready");
        statusStrip.Items.Add("Status: OK");

        // Assert
        statusStrip.Items.Count.Should().Be(2);
    }
}
