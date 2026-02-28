using System.Linq;
using System.Windows;
using System.Windows.Data;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using FFGViewer.ViewModels;
using FFGViewer.Views;

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
        // Configure legend, zoom, border frame, and zero lines via reflection to avoid wpftmp assembly resolution issue
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

            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
                .ToList();

            // Chart border frame (black solid line)
            var drawMarginFrameType = allTypes.FirstOrDefault(
                t => t.Name == "DrawMarginFrame" && !t.IsGenericTypeDefinition);
            if (drawMarginFrameType != null)
            {
                var frame = Activator.CreateInstance(drawMarginFrameType);
                if (frame != null)
                {
                    drawMarginFrameType.GetProperty("Stroke")?.SetValue(frame, new SolidColorPaint(SKColors.Black, 0.5f));
                    chartType.GetProperty("DrawMarginFrame")?.SetValue(chart, frame);
                }
            }

            // Zero lines: horizontal (Y=0) and vertical (X=0) as black solid lines
            var sectionType = allTypes.FirstOrDefault(
                t => t.Name == "RectangularSection" && !t.IsGenericTypeDefinition);
            if (sectionType != null)
            {
                var strokeProp = sectionType.GetProperty("Stroke");

                var hLine = Activator.CreateInstance(sectionType);
                sectionType.GetProperty("Yi")?.SetValue(hLine, 0d);
                sectionType.GetProperty("Yj")?.SetValue(hLine, 0d);
                strokeProp?.SetValue(hLine, new SolidColorPaint(SKColors.Black, 0.5f));

                var vLine = Activator.CreateInstance(sectionType);
                sectionType.GetProperty("Xi")?.SetValue(vLine, 0d);
                sectionType.GetProperty("Xj")?.SetValue(vLine, 0d);
                strokeProp?.SetValue(vLine, new SolidColorPaint(SKColors.Black, 0.5f));

                var listType = typeof(List<>).MakeGenericType(sectionType);
                var sectionsList = Activator.CreateInstance(listType)!;
                listType.GetMethod("Add")?.Invoke(sectionsList, [hLine]);
                listType.GetMethod("Add")?.Invoke(sectionsList, [vLine]);
                chartType.GetProperty("Sections")?.SetValue(chart, sectionsList);
            }
        }
        catch { /* fallback: use defaults */ }
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
        => new AboutWindow { Owner = this }.ShowDialog();

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
        var supportedFiles = files
            .Where(f => f.EndsWith(".ffg", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (supportedFiles.Length > 0)
            _viewModel.LoadFiles(supportedFiles);
    }
}
