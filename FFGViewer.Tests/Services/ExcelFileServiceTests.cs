using System.IO;
using FluentAssertions;
using FFGViewer.Services;

namespace FFGViewer.Tests.Services;

public class ExcelFileServiceTests
{
    private static readonly string TestDataDir =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

    private readonly ExcelFileService _sut = new();

    [Fact]
    public void Load_NoHeader_ReturnsCorrectDataPoints()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "excel_no_header.xlsx"));

        result.DataPoints.Should().HaveCount(5);
    }

    [Fact]
    public void Load_WithHeader_SkipsHeaderAndReturnsDataOnly()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "excel_with_header.xlsx"));

        result.DataPoints.Should().HaveCount(5);
    }

    [Fact]
    public void Load_TitleIsFileNameWithoutExtension()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "excel_no_header.xlsx"));

        result.Title.Should().Be("excel_no_header");
    }

    [Fact]
    public void Load_EmptyRowsAndInvalidRows_AreSkipped()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "excel_with_empty_rows.xlsx"));

        result.DataPoints.Should().HaveCount(3);
    }

    [Fact]
    public void Load_NoHeader_PeakMaxIsCorrect()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "excel_no_header.xlsx"));

        result.PeakData.MaxLoad.Should().BeApproximately(30.5, 0.001);
        result.PeakData.MaxLoadDisplacement.Should().BeApproximately(3.0, 0.001);
    }

    [Fact]
    public void Load_NoHeader_PeakMinIsCorrect()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "excel_no_header.xlsx"));

        result.PeakData.MinLoad.Should().BeApproximately(-30.5, 0.001);
        result.PeakData.MinLoadDisplacement.Should().BeApproximately(-3.0, 0.001);
    }

    [Fact]
    public void Load_SinglePoint_PeakMaxAndMinAreSamePoint()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "excel_single_point.xlsx"));

        result.DataPoints.Should().HaveCount(1);
        result.PeakData.MaxLoad.Should().BeApproximately(25.0, 0.001);
        result.PeakData.MinLoad.Should().BeApproximately(25.0, 0.001);
    }

    [Fact]
    public void Load_FilePathIsPreserved()
    {
        var path = Path.Combine(TestDataDir, "excel_no_header.xlsx");

        var result = _sut.Load(path);

        result.FilePath.Should().Be(path);
    }

    [Fact]
    public void Load_NoHeader_FirstDataPointValues()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "excel_no_header.xlsx"));

        result.DataPoints[0].Displacement.Should().BeApproximately(0.0, 0.001);
        result.DataPoints[0].Load.Should().BeApproximately(0.0, 0.001);
    }

    [Fact]
    public void Load_WithHeader_FirstDataPointValues()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "excel_with_header.xlsx"));

        result.DataPoints[0].Displacement.Should().BeApproximately(0.0, 0.001);
        result.DataPoints[0].Load.Should().BeApproximately(0.0, 0.001);
    }
}
