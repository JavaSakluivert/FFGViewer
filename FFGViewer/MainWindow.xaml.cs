using System.Linq;
using System.Windows;
using System.Windows.Data;
using FFGViewer.ViewModels;

namespace FFGViewer;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        SetupChart();
    }

    private void SetupChart()
    {
        var chart = new LiveChartsCore.SkiaSharpView.WPF.CartesianChart();
        chart.SetBinding(LiveChartsCore.SkiaSharpView.WPF.CartesianChart.SeriesProperty, new Binding("Series"));
        chart.SetBinding(LiveChartsCore.SkiaSharpView.WPF.CartesianChart.XAxesProperty, new Binding("XAxes"));
        chart.SetBinding(LiveChartsCore.SkiaSharpView.WPF.CartesianChart.YAxesProperty, new Binding("YAxes"));
        // Configure legend and zoom via reflection to avoid wpftmp assembly resolution issue
        ConfigureChartSettings(chart);
        ChartHost.Children.Add(chart);
    }

    private static void ConfigureChartSettings(object chart)
    {
        var chartType = chart.GetType();
        try
        {
            var legendProp = chartType.GetProperty("LegendPosition");
            if (legendProp != null)
            {
                var legendType = legendProp.PropertyType;
                var rightVal = System.Enum.Parse(legendType, "Right");
                legendProp.SetValue(chart, rightVal);
            }
            var zoomProp = chartType.GetProperty("ZoomMode");
            if (zoomProp != null)
            {
                var zoomType = zoomProp.PropertyType;
                var bothVal = System.Enum.Parse(zoomType, "Both");
                zoomProp.SetValue(chart, bothVal);
            }
        }
        catch { /* fallback: use defaults */ }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var ffgFiles = files
            .Where(f => f.EndsWith(".ffg", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (ffgFiles.Length > 0)
            _viewModel.LoadFiles(ffgFiles);
    }
}
