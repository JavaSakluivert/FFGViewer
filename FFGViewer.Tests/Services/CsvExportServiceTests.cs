using System.IO;
using FluentAssertions;
using FFGViewer.Services;
using FFGViewer.Tests.Helpers;

namespace FFGViewer.Tests.Services;

public class CsvExportServiceTests : IDisposable
{
    private readonly CsvExportService _sut = new();
    private readonly string _tempFile = Path.GetTempFileName();

    [Fact]
    public void Export_SingleSeries_CreatesHeaderCorrectly()
    {
        var data = new FfgDataBuilder().WithTitle("S1").AddPoint(1, 10).Build();

        _sut.Export([data], _tempFile);

        var lines = File.ReadAllLines(_tempFile);
        lines[0].Should().Be("Disp_S1,Load_S1");
    }

    [Fact]
    public void Export_SingleSeries_WritesDataCorrectly()
    {
        var data = new FfgDataBuilder().WithTitle("S1").AddPoint(1.0, 10.0).AddPoint(2.0, 20.0).Build();

        _sut.Export([data], _tempFile);

        var lines = File.ReadAllLines(_tempFile);
        lines.Should().HaveCount(3); // header + 2 data rows (+ possible empty line)
        lines[1].Should().Be("1,10");
    }

    [Fact]
    public void Export_MultiSeries_HeaderHasAllSeries()
    {
        var s1 = new FfgDataBuilder().WithTitle("S1").AddPoint(1, 10).Build();
        var s2 = new FfgDataBuilder().WithTitle("S2").AddPoint(2, 20).Build();

        _sut.Export([s1, s2], _tempFile);

        var lines = File.ReadAllLines(_tempFile);
        lines[0].Should().Be("Disp_S1,Load_S1,Disp_S2,Load_S2");
    }

    [Fact]
    public void Export_UnequalLength_PadsWithEmpty()
    {
        var s1 = new FfgDataBuilder().WithTitle("S1").AddPoint(1, 10).AddPoint(2, 20).Build();
        var s2 = new FfgDataBuilder().WithTitle("S2").AddPoint(3, 30).Build();

        _sut.Export([s1, s2], _tempFile);

        var lines = File.ReadAllLines(_tempFile);
        // Row 2 (index 2): S2 has no second point => empty cells
        lines[2].Should().Contain(",,");
    }

    [Fact]
    public void Export_EmptySeries_WritesHeaderOnly()
    {
        var data = new FfgDataBuilder().WithTitle("Empty").Build();

        _sut.Export([data], _tempFile);

        var content = File.ReadAllText(_tempFile).Trim();
        content.Should().Be("Disp_Empty,Load_Empty");
    }

    [Fact]
    public void Export_CreatesFileAtGivenPath()
    {
        var data = new FfgDataBuilder().WithTitle("X").AddPoint(1, 2).Build();

        _sut.Export([data], _tempFile);

        File.Exists(_tempFile).Should().BeTrue();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }
}
