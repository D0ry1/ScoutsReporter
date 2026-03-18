using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ScoutsReporter.Converters;

namespace ScoutsReporter.Views;

public partial class TrainingReportView : UserControl
{
    private static readonly HashSet<string> TrainingNames = new()
    {
        "First Response", "Safeguarding", "Safety"
    };

    public TrainingReportView()
    {
        InitializeComponent();
    }

    private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        // Apply color to the Flag column
        if (e.PropertyName == "Flag")
        {
            var templateCol = new DataGridTemplateColumn
            {
                Header = "Flag",
                SortMemberPath = "Flag",
            };

            var factory = new FrameworkElementFactory(typeof(TextBlock));
            factory.SetBinding(TextBlock.TextProperty, new Binding("Flag"));
            factory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            factory.SetValue(TextBlock.PaddingProperty, new Thickness(4, 2, 4, 2));
            factory.SetBinding(TextBlock.ForegroundProperty,
                new Binding("Flag") { Converter = new FlagToColorConverter() });
            factory.SetBinding(TextBlock.BackgroundProperty,
                new Binding("Flag") { Converter = new FlagToBackgroundConverter() });

            templateCol.CellTemplate = new DataTemplate { VisualTree = factory };
            e.Column = templateCol;
        }
        // Apply color to training name columns (e.g. "First Response") — "MISSING" shows red
        else if (TrainingNames.Contains(e.PropertyName))
        {
            var templateCol = new DataGridTemplateColumn
            {
                Header = e.PropertyName,
                SortMemberPath = $"[{e.PropertyName}]",
            };

            var factory = new FrameworkElementFactory(typeof(TextBlock));
            var bindingPath = $"[{e.PropertyName}]";
            factory.SetBinding(TextBlock.TextProperty, new Binding(bindingPath));
            factory.SetValue(TextBlock.PaddingProperty, new Thickness(4, 2, 4, 2));
            factory.SetBinding(TextBlock.ForegroundProperty,
                new Binding(bindingPath) { Converter = new FlagToColorConverter() });
            factory.SetBinding(TextBlock.BackgroundProperty,
                new Binding(bindingPath) { Converter = new FlagToBackgroundConverter() });

            templateCol.CellTemplate = new DataTemplate { VisualTree = factory };
            e.Column = templateCol;
        }
        // Apply color to Warning columns
        else if (e.PropertyName.EndsWith(" Warning"))
        {
            var templateCol = new DataGridTemplateColumn
            {
                Header = e.PropertyName,
                SortMemberPath = $"[{e.PropertyName}]",
            };

            var factory = new FrameworkElementFactory(typeof(TextBlock));
            var bindingPath = $"[{e.PropertyName}]";
            factory.SetBinding(TextBlock.TextProperty, new Binding(bindingPath));
            factory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            factory.SetValue(TextBlock.PaddingProperty, new Thickness(4, 2, 4, 2));
            factory.SetBinding(TextBlock.ForegroundProperty,
                new Binding(bindingPath) { Converter = new WarningToColorConverter() });
            factory.SetBinding(TextBlock.BackgroundProperty,
                new Binding(bindingPath) { Converter = new WarningToBackgroundConverter() });

            templateCol.CellTemplate = new DataTemplate { VisualTree = factory };
            e.Column = templateCol;
        }
    }
}
