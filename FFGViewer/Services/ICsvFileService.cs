using FFGViewer.Models;

namespace FFGViewer.Services;

public interface ICsvFileService
{
    FfgData Load(string filePath);
}
