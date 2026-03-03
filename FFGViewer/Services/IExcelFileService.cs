using FFGViewer.Models;

namespace FFGViewer.Services;

public interface IExcelFileService
{
    FfgData Load(string filePath);
}
