using System.IO;
using FluentAssertions;
using FFGViewer.Services;

namespace FFGViewer.Tests.Services;

public class CsvFileServiceTests
{
    private static readonly string TestDataDir =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

    private readonly CsvFileService _sut = new();

    [Fact]
    public void Load_CommaSeparated_ReturnsCorrectDataPoints()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "normal_comma.csv"));

        result.DataPoints.Should().HaveCount(5);
    }

    [Fact]
    public void Load_SpaceSeparated_ReturnsCorrectDataPoints()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "normal_space.csv"));

        result.DataPoints.Should().HaveCount(5);
    }

    [Fact]
    public void Load_TitleIsFileNameWithoutExtension()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "normal_comma.csv"));

        result.Title.Should().Be("normal_comma");
    }

    [Fact]
    public void Load_EmptyLinesAndInvalidLines_AreSkipped()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "csv_with_empty_lines.csv"));

        result.DataPoints.Should().HaveCount(3);
    }

    [Fact]
    public void Load_NormalData_PeakMaxIsCorrect()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "normal_comma.csv"));

        result.PeakData.MaxLoad.Should().BeApproximately(30.5, 0.001);
        result.PeakData.MaxLoadDisplacement.Should().BeApproximately(3.0, 0.001);
    }

    [Fact]
    public void Load_NormalData_PeakMinIsCorrect()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "normal_comma.csv"));

        result.PeakData.MinLoad.Should().BeApproximately(-30.5, 0.001);
        result.PeakData.MinLoadDisplacement.Should().BeApproximately(-3.0, 0.001);
    }

    [Fact]
    public void Load_SinglePoint_PeakMaxAndMinAreSamePoint()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "csv_single_point.csv"));

        result.DataPoints.Should().HaveCount(1);
        result.PeakData.MaxLoad.Should().BeApproximately(25.0, 0.001);
        result.PeakData.MinLoad.Should().BeApproximately(25.0, 0.001);
    }

    [Fact]
    public void Load_FilePathIsPreserved()
    {
        var path = Path.Combine(TestDataDir, "normal_comma.csv");

        var result = _sut.Load(path);

        result.FilePath.Should().Be(path);
    }
}
