using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using Reactive.Bindings;
using SkiaSharp;
using FFGViewer.Models;
using FFGViewer.Services;

namespace FFGViewer.ViewModels;

public class MainViewModel
{
    private static readonly SKColor[] Palette =
    [
        new SKColor(0x42, 0x8B, 0xCA),
        new SKColor(0xE3, 0x6C, 0x09),
        new SKColor(0x9B, 0xBB, 0x59),
        new SKColor(0x80, 0x64, 0xA2),
        new SKColor(0x4B, 0xAC, 0xC6),
        new SKColor(0xC0, 0x50, 0x4D),
    ];

    private readonly IFfgFileService _ffgFileService;
    private readonly ICsvExportService _csvExportService;
    private readonly IExcelExportService _excelExportService;
    private readonly List<FfgData> _loadedSeries = [];
    private int _paletteIndex = 0;

    public ReactivePropertySlim<string> XAxisTitle { get; } = new("変位 (mm)");
    public ReactivePropertySlim<string> YAxisTitle { get; } = new("荷重 (kN)");
    public ReactivePropertySlim<string> StatusMessage { get; } = new(string.Empty);
    public ReactivePropertySlim<string> CurrentFilePath { get; } = new(string.Empty);

    public ObservableCollection<ISeries> Series { get; } = [];
    public ObservableCollection<Axis> XAxes { get; } = [new Axis { Name = "変位 (mm)" }];
    public ObservableCollection<Axis> YAxes { get; } = [new Axis { Name = "荷重 (kN)" }];
    public ObservableCollection<PeakDataRow> PeakDataItems { get; } = [];

    public ReactiveCommand OpenFileCommand { get; }
    public ReactiveCommand ClearGraphCommand { get; }
    public ReactiveCommand CopyPeakDataCommand { get; }
    public ReactiveCommand ExportExcelCommand { get; }
    public ReactiveCommand ExportCsvCommand { get; }

    public MainViewModel(
        IFfgFileService ffgFileService,
        ICsvExportService csvExportService,
        IExcelExportService excelExportService)
    {
        _ffgFileService = ffgFileService;
        _csvExportService = csvExportService;
        _excelExportService = excelExportService;

        OpenFileCommand = new ReactiveCommand();
        OpenFileCommand.Subscribe(_ => OpenFile());

        ClearGraphCommand = new ReactiveCommand();
        ClearGraphCommand.Subscribe(_ => ClearGraph());

        CopyPeakDataCommand = new ReactiveCommand();
        CopyPeakDataCommand.Subscribe(_ => CopyPeakData());

        ExportExcelCommand = new ReactiveCommand();
        ExportExcelCommand.Subscribe(_ => ExportExcel());

        ExportCsvCommand = new ReactiveCommand();
        ExportCsvCommand.Subscribe(_ => ExportCsv());

        XAxisTitle.Subscribe(title =>
        {
            if (XAxes.Count > 0) XAxes[0].Name = title;
        });
        YAxisTitle.Subscribe(title =>
        {
            if (YAxes.Count > 0) YAxes[0].Name = title;
        });
    }

    public void LoadFiles(string[] filePaths)
    {
        foreach (var path in filePaths)
        {
            LoadSingleFile(path);
        }
    }

    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "FFG Files (*.ffg)|*.ffg|All Files (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog() != true) return;

        foreach (var path in dialog.FileNames)
        {
            LoadSingleFile(path);
        }
    }

    public void LoadSingleFile(string filePath)
    {
        try
        {
            var data = _ffgFileService.Load(filePath);
            var resolvedTitle = ResolveSeriesName(data.Title);
            var resolvedData = data with { Title = resolvedTitle };

            _loadedSeries.Add(resolvedData);
            CurrentFilePath.Value = filePath;
            StatusMessage.Value = $"読み込み完了: {resolvedTitle}";

            var color = Palette[_paletteIndex % Palette.Length];
            _paletteIndex++;

            AddSeriesToChart(resolvedData, color);
            AddPeakDataRows(resolvedData, color);
            AdjustAxisRange();
        }
        catch (Exception ex)
        {
            StatusMessage.Value = $"エラー: {ex.Message}";
        }
    }

    private void AddSeriesToChart(FfgData data, SKColor color)
    {
        var paint = new SolidColorPaint(color);

        var lineSeries = new LineSeries<ObservablePoint>
        {
            Name = data.Title,
            Values = data.DataPoints.Select(p => new ObservablePoint(p.Displacement, p.Load)).ToList(),
            Stroke = paint,
            Fill = null,
            GeometrySize = 0,
            GeometryStroke = null,
        };
        Series.Add(lineSeries);

        if (data.DataPoints.Count > 0)
        {
            var peakPoints = new List<ObservablePoint>
            {
                new(data.PeakData.MaxLoadDisplacement, data.PeakData.MaxLoad),
                new(data.PeakData.MinLoadDisplacement, data.PeakData.MinLoad)
            };

            var scatterSeries = new ScatterSeries<ObservablePoint>
            {
                Name = $"{data.Title} peak",
                Values = peakPoints,
                Fill = paint,
                Stroke = null,
                GeometrySize = 10,
            };
            Series.Add(scatterSeries);
        }
    }

    private void AddPeakDataRows(FfgData data, SKColor color)
    {
        var wpfColor = Color.FromRgb(color.Red, color.Green, color.Blue);

        PeakDataItems.Add(new PeakDataRow
        {
            SeriesName = data.Title,
            Sign = "(+)",
            Load = data.PeakData.MaxLoad,
            Displacement = data.PeakData.MaxLoadDisplacement,
            SeriesColor = wpfColor
        });
        PeakDataItems.Add(new PeakDataRow
        {
            SeriesName = data.Title,
            Sign = "(-)",
            Load = data.PeakData.MinLoad,
            Displacement = data.PeakData.MinLoadDisplacement,
            SeriesColor = wpfColor
        });
    }

    private void AdjustAxisRange()
    {
        if (_loadedSeries.Count == 0 || !_loadedSeries.Any(s => s.DataPoints.Count > 0)) return;

        var allPoints = _loadedSeries.SelectMany(s => s.DataPoints).ToList();
        double minX = allPoints.Min(p => p.Displacement);
        double maxX = allPoints.Max(p => p.Displacement);
        double minY = allPoints.Min(p => p.Load);
        double maxY = allPoints.Max(p => p.Load);

        double xRange = maxX - minX;
        double yRange = maxY - minY;
        double xPad = xRange == 0 ? 1.0 : xRange * 0.1;
        double yPad = yRange == 0 ? 1.0 : yRange * 0.1;

        XAxes[0].MinLimit = minX - xPad;
        XAxes[0].MaxLimit = maxX + xPad;
        YAxes[0].MinLimit = minY - yPad;
        YAxes[0].MaxLimit = maxY + yPad;
    }

    private void ClearGraph()
    {
        Series.Clear();
        PeakDataItems.Clear();
        _loadedSeries.Clear();
        _paletteIndex = 0;
        CurrentFilePath.Value = string.Empty;
        StatusMessage.Value = "グラフをクリアしました";

        XAxes[0].MinLimit = null;
        XAxes[0].MaxLimit = null;
        YAxes[0].MinLimit = null;
        YAxes[0].MaxLimit = null;
    }

    private void CopyPeakData()
    {
        if (PeakDataItems.Count == 0) return;

        var lines = PeakDataItems.Select(row =>
            $"{row.SeriesName}\t{row.Sign}\t{row.Load}\t{row.Displacement}");
        Clipboard.SetText(string.Join(Environment.NewLine, lines));
        StatusMessage.Value = "ピークデータをクリップボードにコピーしました";
    }

    private void ExportExcel()
    {
        if (_loadedSeries.Count == 0) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = "FFGData.xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _excelExportService.Export(_loadedSeries, dialog.FileName);
            StatusMessage.Value = $"Excel エクスポート完了: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage.Value = $"Excel エクスポートエラー: {ex.Message}";
        }
    }

    private void ExportCsv()
    {
        if (_loadedSeries.Count == 0) return;

        var dialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = "FFGData.csv"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _csvExportService.Export(_loadedSeries, dialog.FileName);
            StatusMessage.Value = $"CSV エクスポート完了: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage.Value = $"CSV エクスポートエラー: {ex.Message}";
        }
    }

    private string ResolveSeriesName(string baseTitle)
    {
        var existingNames = _loadedSeries.Select(s => s.Title).ToHashSet();
        if (!existingNames.Contains(baseTitle)) return baseTitle;

        for (int i = 2; ; i++)
        {
            var candidate = $"{baseTitle}_{i}";
            if (!existingNames.Contains(candidate)) return candidate;
        }
    }
}
