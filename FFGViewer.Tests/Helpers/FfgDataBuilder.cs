using System.Linq;
using FFGViewer.Models;

namespace FFGViewer.Tests.Helpers;

public class FfgDataBuilder
{
    private string _title = "Test";
    private string _filePath = "test.ffg";
    private readonly List<DataPoint> _points = [];

    public FfgDataBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public FfgDataBuilder WithFilePath(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    public FfgDataBuilder AddPoint(double displacement, double load)
    {
        _points.Add(new DataPoint(displacement, load));
        return this;
    }

    public FfgData Build()
    {
        PeakData peak;
        if (_points.Count == 0)
        {
            peak = new PeakData(0, 0, 0, 0);
        }
        else
        {
            var maxPt = _points.MaxBy(p => p.Load)!;
            var minPt = _points.MinBy(p => p.Load)!;
            peak = new PeakData(maxPt.Load, maxPt.Displacement, minPt.Load, minPt.Displacement);
        }
        return new FfgData(_title, _filePath, _points.AsReadOnly(), peak);
    }
}
