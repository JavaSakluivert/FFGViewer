using System.IO;
using ClosedXML.Excel;
using FFGViewer.Models;

namespace FFGViewer.Services;

public class ExcelFileService : IExcelFileService
{
    public FfgData Load(string filePath)
    {
        var title = Path.GetFileNameWithoutExtension(filePath);
        var dataPoints = new List<DataPoint>();

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed()?.RowsUsed();
        if (rows is null)
            return new FfgData(title, filePath, dataPoints, new PeakData(0, 0, 0, 0));

        foreach (var row in rows)
        {
            var cellA = row.Cell(1);
            var cellB = row.Cell(2);

            if (!cellA.TryGetValue<double>(out var disp)) continue;
            if (!cellB.TryGetValue<double>(out var load)) continue;

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
