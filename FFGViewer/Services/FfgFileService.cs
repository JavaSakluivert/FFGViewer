using System.IO;
using System.Text;
using FFGViewer.Models;

namespace FFGViewer.Services;

public class FfgFileService : IFfgFileService
{
    private static readonly double FooterValue = -999.0;

    public FfgData Load(string filePath)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding("shift_jis");
        var lines = File.ReadAllLines(filePath, encoding);

        // Line 0: empty (skip)
        // Line 1: title
        var rawTitle = lines.Length > 1 ? lines[1].Trim() : string.Empty;
        var title = rawTitle.Length > 5 ? rawTitle[..5] : rawTitle;

        // Line 2+: "displacement load" pairs until -999.0 footer
        var dataPoints = new List<DataPoint>();
        for (int i = 2; i < lines.Length; i++)
        {
            var parts = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            if (!double.TryParse(parts[0], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var disp)) continue;
            if (!double.TryParse(parts[1], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var load)) continue;

            if (disp == FooterValue && load == FooterValue) break;

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
