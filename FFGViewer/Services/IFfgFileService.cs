using FFGViewer.Models;

namespace FFGViewer.Services;

public interface IFfgFileService
{
    FfgData Load(string filePath);
}
