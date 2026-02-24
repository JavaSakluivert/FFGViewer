namespace FFGViewer.Models;

public record FfgData(
    string Title,
    string FilePath,
    IReadOnlyList<DataPoint> DataPoints,
    PeakData PeakData);
