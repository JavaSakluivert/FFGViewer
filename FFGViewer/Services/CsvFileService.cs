using System.Globalization;
using System.IO;
using System.Text;
using FFGViewer.Models;

namespace FFGViewer.Services;

public class CsvFileService : ICsvFileService
{
    public FfgData Load(string filePath)
    {
        var title = Path.GetFileNameWithoutExtension(filePath);
        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        var dataPoints = new List<DataPoint>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            // カンマ区切り優先、カンマがなければスペース区切りにフォールバック
            var parts = trimmed.Contains(',')
                ? trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries)
                : trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2) continue;

            if (!double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var disp)) continue;
            if (!double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var load)) continue;

            dataPoints.Add(new DataPoint(disp, load));
        }

        var peak = CalculatePeak(dataPoints);
        return new FfgData(title, filePath, dataPoints, peak);
    }

    private static PeakData CalculatePeak(List<DataPoint> points)
    {
        if (points.Count == 0)
            return new PeakData(0, 0, 0, 0);

        var maxPoint = points.MaxBy(p => p.Load)!;
        var minPoint = points.MinBy(p => p.Load)!;

        return new PeakData(
            maxPoint.Load,
            maxPoint.Displacement,
            minPoint.Load,
            minPoint.Displacement);
    }
}
