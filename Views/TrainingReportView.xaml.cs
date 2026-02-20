using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ScoutsReporter.Converters;

namespace ScoutsReporter.Views;

public partial class TrainingReportView : UserControl
{
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
    }
}
