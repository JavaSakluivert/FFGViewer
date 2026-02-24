using System.IO;
using System.Text;
using FFGViewer.Models;

namespace FFGViewer.Services;

public class CsvExportService : ICsvExportService
{
    public void Export(IReadOnlyList<FfgData> seriesList, string filePath)
    {
        var sb = new StringBuilder();

        // Header row: Disp_[name], Load_[name] for each series
        var headers = seriesList.SelectMany(s => new[]
        {
            $"Disp_{s.Title}",
            $"Load_{s.Title}"
        });
        sb.AppendLine(string.Join(",", headers));

        // Data rows
        int maxRows = seriesList.Max(s => s.DataPoints.Count);
        for (int i = 0; i < maxRows; i++)
        {
            var cells = seriesList.SelectMany(s =>
            {
                if (i < s.DataPoints.Count)
                {
                    var pt = s.DataPoints[i];
                    return new[] { pt.Displacement.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
                                   pt.Load.ToString("G", System.Globalization.CultureInfo.InvariantCulture) };
                }
                return new[] { string.Empty, string.Empty };
            });
            sb.AppendLine(string.Join(",", cells));
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }
}
