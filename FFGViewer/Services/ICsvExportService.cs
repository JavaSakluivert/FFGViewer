using FFGViewer.Models;

namespace FFGViewer.Services;

public interface ICsvExportService
{
    void Export(IReadOnlyList<FfgData> seriesList, string filePath);
}
