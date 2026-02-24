using System.Windows.Media;

namespace FFGViewer.ViewModels;

public class PeakDataRow
{
    public string SeriesName { get; init; } = string.Empty;
    public string Sign       { get; init; } = string.Empty;
    public double Load       { get; init; }
    public double Displacement { get; init; }
    public Color  SeriesColor { get; init; }

    public SolidColorBrush SeriesBrush => new(SeriesColor);
}
