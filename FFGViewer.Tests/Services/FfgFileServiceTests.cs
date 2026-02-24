using System.IO;
using FluentAssertions;
using FFGViewer.Services;

namespace FFGViewer.Tests.Services;

public class FfgFileServiceTests
{
    private static readonly string TestDataDir =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

    private readonly FfgFileService _sut = new();

    [Fact]
    public void Load_NormalFile_ReturnsFfgData()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "normal_short_title.ffg"));

        result.Should().NotBeNull();
        result.DataPoints.Should().HaveCount(5);
    }

    [Fact]
    public void Load_NormalFile_TitleIsCorrect()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "normal_short_title.ffg"));

        result.Title.Should().Be("試験1");
    }

    [Fact]
    public void Load_LongTitle_TitleIsTruncatedTo5Chars()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "long_title.ffg"));

        result.Title.Length.Should().Be(5);
        result.Title.Should().Be("長いタイト"); // 10-char title truncated to first 5
    }

    [Fact]
    public void Load_EmptyData_ReturnsZeroDataPoints()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "empty_data.ffg"));

        result.DataPoints.Should().BeEmpty();
    }

    [Fact]
    public void Load_EmptyData_PeakDataIsZero()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "empty_data.ffg"));

        result.PeakData.MaxLoad.Should().Be(0);
        result.PeakData.MinLoad.Should().Be(0);
    }

    [Fact]
    public void Load_SinglePoint_HasOneDataPoint()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "single_point.ffg"));

        result.DataPoints.Should().HaveCount(1);
    }

    [Fact]
    public void Load_SinglePoint_PeakDataEqualsPoint()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "single_point.ffg"));

        result.PeakData.MaxLoad.Should().Be(50.0);
        result.PeakData.MaxLoadDisplacement.Should().Be(5.0);
        result.PeakData.MinLoad.Should().Be(50.0);
    }

    [Fact]
    public void Load_InvalidFormat_SkipsNonNumericLines()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "invalid_format.ffg"));

        // "ABC XYZ" line should be skipped, so only 2 valid points
        result.DataPoints.Should().HaveCount(2);
    }

    [Fact]
    public void Load_NormalFile_PeakMaxLoadIsCorrect()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "normal_short_title.ffg"));

        result.PeakData.MaxLoad.Should().Be(20.0);
        result.PeakData.MaxLoadDisplacement.Should().Be(2.0);
    }

    [Fact]
    public void Load_NormalFile_PeakMinLoadIsCorrect()
    {
        var result = _sut.Load(Path.Combine(TestDataDir, "normal_short_title.ffg"));

        result.PeakData.MinLoad.Should().Be(0.0);
        result.PeakData.MinLoadDisplacement.Should().Be(0.0);
    }

    [Fact]
    public void Load_FilePathIsStored()
    {
        var path = Path.Combine(TestDataDir, "normal_short_title.ffg");
        var result = _sut.Load(path);

        result.FilePath.Should().Be(path);
    }
}
