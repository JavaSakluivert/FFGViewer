using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

public partial class MainViewModel : ObservableObject
{
    private static readonly SKColor[] Palette =
    [
        new SKColor(0x42, 0x8B, 0xCA), // Steel Blue
        new SKColor(0xE3, 0x6C, 0x09), // Orange
        new SKColor(0x9B, 0xBB, 0x59), // Olive Green
        new SKColor(0x80, 0x64, 0xA2), // Purple
        new SKColor(0x4B, 0xAC, 0xC6), // Cyan
        new SKColor(0xC0, 0x50, 0x4D), // Muted Red
    ];

    // 補色ペア: 各シリーズの線色と明確に区別できる色
    private static readonly SKColor[] PeakPalette =
    [
        new SKColor(0xE5, 0x39, 0x35), // Vivid Red    (Steel Blue   に対して)
        new SKColor(0x00, 0x83, 0x8F), // Teal         (Orange       に対して)
        new SKColor(0xAD, 0x14, 0x57), // Magenta      (Olive Green  に対して)
        new SKColor(0xF5, 0x7F, 0x17), // Amber        (Purple       に対して)
        new SKColor(0xE6, 0x4A, 0x19), // Deep Orange  (Cyan         に対して)
        new SKColor(0x00, 0x69, 0x5C), // Deep Teal    (Muted Red    に対して)
    ];

    private readonly IFfgFileService _ffgFileService;
    private readonly ICsvExportService _csvExportService;
    private readonly IExcelExportService _excelExportService;
    private readonly List<(FfgData Data, SKColor LineColor, SKColor PeakColor)> _loadedSeries = [];
    private int _paletteIndex = 0;

    public ReactivePropertySlim<string> XAxisTitle { get; } = new("Disp. (mm)");
    public ReactivePropertySlim<string> YAxisTitle { get; } = new("Load (kN)");
    public ReactivePropertySlim<string> StatusMessage { get; } = new(string.Empty);
    public ReactivePropertySlim<string> CurrentFilePath { get; } = new(string.Empty);
    public ReactivePropertySlim<bool> IsDecimationEnabled { get; } = new(true);
    public ReactivePropertySlim<bool> CanToggleDecimation { get; } = new(false);

    public ObservableCollection<ISeries> Series { get; } = [];
    public ObservableCollection<Axis> XAxes { get; } = [new Axis { Name = "Disp. (mm)" }];
    public ObservableCollection<Axis> YAxes { get; } = [new Axis { Name = "Load (kN)" }];
    public ObservableCollection<PeakDataRow> PeakDataItems { get; } = [];

    public MainViewModel(
        IFfgFileService ffgFileService,
        ICsvExportService csvExportService,
        IExcelExportService excelExportService)
    {
        _ffgFileService = ffgFileService;
        _csvExportService = csvExportService;
        _excelExportService = excelExportService;

        XAxisTitle.Subscribe(title =>
        {
            if (XAxes.Count > 0) XAxes[0].Name = title;
        });
        YAxisTitle.Subscribe(title =>
        {
            if (YAxes.Count > 0) YAxes[0].Name = title;
        });

        // _loadedSeries.Count > 0 ガードにより初期化時の無駄な再描画を防ぐ
        IsDecimationEnabled.Subscribe(_ =>
        {
            if (_loadedSeries.Count > 0) RefreshChart(updateStatus: true);
        });
    }

    public void LoadFiles(string[] filePaths)
    {
        foreach (var path in filePaths)
            LoadSingleFile(path);
    }

    public void LoadSingleFile(string filePath)
    {
        try
        {
            var data = _ffgFileService.Load(filePath);
            var resolvedTitle = ResolveSeriesName(data.Title);
            var resolvedData = data with { Title = resolvedTitle };

            var lineColor = Palette[_paletteIndex % Palette.Length];
            var peakColor = PeakPalette[_paletteIndex % PeakPalette.Length];
            _paletteIndex++;

            _loadedSeries.Add((resolvedData, lineColor, peakColor));
            CurrentFilePath.Value = filePath;

            AddSeriesToChart(resolvedData, lineColor, peakColor);
            AddPeakDataRows(resolvedData, lineColor);
            AdjustAxisRange();
            UpdateCanToggleDecimation();

            var displayCount = IsDecimationEnabled.Value
                ? DataDecimator.Decimate(resolvedData.DataPoints).Count
                : resolvedData.DataPoints.Count;

            StatusMessage.Value = displayCount < resolvedData.DataPoints.Count
                ? $"読み込み完了: {resolvedTitle}（間引き: {resolvedData.DataPoints.Count:N0}点 → {displayCount:N0}点）"
                : $"読み込み完了: {resolvedTitle}";
        }
        catch (Exception ex)
        {
            StatusMessage.Value = $"エラー: {ex.Message}";
        }
    }

    private void AddSeriesToChart(FfgData data, SKColor lineColor, SKColor peakColor)
    {
        var displayPoints = IsDecimationEnabled.Value
            ? DataDecimator.Decimate(data.DataPoints)
            : data.DataPoints;

        var lineSeries = new LineSeries<ObservablePoint>
        {
            Name = data.Title,
            Values = displayPoints.Select(p => new ObservablePoint(p.Displacement, p.Load)).ToList(),
            Stroke = new SolidColorPaint(lineColor),
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
                Fill = new SolidColorPaint(peakColor),
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
        if (_loadedSeries.Count == 0 || !_loadedSeries.Any(t => t.Data.DataPoints.Count > 0)) return;

        var allPoints = _loadedSeries.SelectMany(t => t.Data.DataPoints).ToList();
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

    private void RefreshChart(bool updateStatus = false)
    {
        Series.Clear();
        foreach (var (data, lineColor, peakColor) in _loadedSeries)
            AddSeriesToChart(data, lineColor, peakColor);

        if (updateStatus && _loadedSeries.Count > 0)
        {
            StatusMessage.Value = IsDecimationEnabled.Value
                ? "間引き表示に切り替えました"
                : "全データ表示に切り替えました";
        }
    }

    private void UpdateCanToggleDecimation()
    {
        CanToggleDecimation.Value = _loadedSeries.Any(
            t => t.Data.DataPoints.Count > DataDecimator.DecimationThreshold);
    }

    [RelayCommand]
    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "FFG Files (*.ffg)|*.ffg|All Files (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog() != true) return;

        foreach (var path in dialog.FileNames)
            LoadSingleFile(path);
    }

    [RelayCommand]
    private void ClearGraph()
    {
        Series.Clear();
        PeakDataItems.Clear();
        _loadedSeries.Clear();
        _paletteIndex = 0;
        CurrentFilePath.Value = string.Empty;
        StatusMessage.Value = "グラフをクリアしました";
        CanToggleDecimation.Value = false;

        XAxes[0].MinLimit = null;
        XAxes[0].MaxLimit = null;
        YAxes[0].MinLimit = null;
        YAxes[0].MaxLimit = null;
    }

    [RelayCommand]
    private void CopyPeakData()
    {
        if (PeakDataItems.Count == 0) return;

        var lines = PeakDataItems.Select(row =>
            $"{row.SeriesName}\t{row.Sign}\t{row.Load}\t{row.Displacement}");
        Clipboard.SetText(string.Join(Environment.NewLine, lines));
        StatusMessage.Value = "ピークデータをクリップボードにコピーしました";
    }

    [RelayCommand]
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
            _excelExportService.Export(_loadedSeries.Select(t => t.Data).ToList(), dialog.FileName);
            StatusMessage.Value = $"Excel エクスポート完了: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage.Value = $"Excel エクスポートエラー: {ex.Message}";
        }
    }

    [RelayCommand]
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
            _csvExportService.Export(_loadedSeries.Select(t => t.Data).ToList(), dialog.FileName);
            StatusMessage.Value = $"CSV エクスポート完了: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage.Value = $"CSV エクスポートエラー: {ex.Message}";
        }
    }

    private string ResolveSeriesName(string baseTitle)
    {
        var existingNames = _loadedSeries.Select(t => t.Data.Title).ToHashSet();
        if (!existingNames.Contains(baseTitle)) return baseTitle;

        for (int i = 2; ; i++)
        {
            var candidate = $"{baseTitle}_{i}";
            if (!existingNames.Contains(candidate)) return candidate;
        }
    }
}
