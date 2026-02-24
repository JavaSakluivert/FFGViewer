using FFGViewer.Models;

namespace FFGViewer.Services;

public interface IExcelExportService
{
    void Export(IReadOnlyList<FfgData> seriesList, string filePath);
}
