using System.IO;
using System.Linq;
using FluentAssertions;
using Moq;
using FFGViewer.Models;
using FFGViewer.Services;
using FFGViewer.Tests.Helpers;
using FFGViewer.ViewModels;

namespace FFGViewer.Tests.ViewModels;

public class MainViewModelTests
{
    private static FfgData MakeSeries(string title, params (double d, double l)[] pts)
    {
        var builder = new FfgDataBuilder().WithTitle(title).WithFilePath($"{title}.ffg");
        foreach (var (d, l) in pts) builder.AddPoint(d, l);
        return builder.Build();
    }

    private MainViewModel CreateSut(IFfgFileService? ffgSvc = null)
    {
        var csvMock = new Mock<ICsvExportService>();
        var xlsMock = new Mock<IExcelExportService>();
        ffgSvc ??= new Mock<IFfgFileService>().Object;
        return new MainViewModel(ffgSvc, csvMock.Object, xlsMock.Object);
    }

    [Fact]
    public void LoadSingleFile_AddsSeriesEntry()
    {
        var data = MakeSeries("S1", (1, 10), (2, 20));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load("test.ffg")).Returns(data);
        var vm = CreateSut(mockSvc.Object);

        vm.LoadSingleFile("test.ffg");

        vm.Series.Should().NotBeEmpty();
    }

    [Fact]
    public void LoadSingleFile_AddsPeakDataRows()
    {
        var data = MakeSeries("S1", (1, 10), (2, 20));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load("test.ffg")).Returns(data);
        var vm = CreateSut(mockSvc.Object);

        vm.LoadSingleFile("test.ffg");

        vm.PeakDataItems.Should().HaveCount(2);
    }

    [Fact]
    public void LoadSingleFile_PeakDataHasCorrectSign()
    {
        var data = MakeSeries("S1", (1, 10), (2, -5));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load("test.ffg")).Returns(data);
        var vm = CreateSut(mockSvc.Object);

        vm.LoadSingleFile("test.ffg");

        vm.PeakDataItems[0].Sign.Should().Be("(+)");
        vm.PeakDataItems[1].Sign.Should().Be("(-)");
    }

    [Fact]
    public void LoadSingleFile_DuplicateTitle_AppendsSuffix()
    {
        var data1 = MakeSeries("S1", (1, 10));
        var data2 = MakeSeries("S1", (2, 20));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.SetupSequence(s => s.Load(It.IsAny<string>()))
            .Returns(data1)
            .Returns(data2);
        var vm = CreateSut(mockSvc.Object);

        vm.LoadSingleFile("a.ffg");
        vm.LoadSingleFile("b.ffg");

        vm.PeakDataItems.Select(r => r.SeriesName).Distinct().Should().HaveCount(2);
        vm.PeakDataItems.Any(r => r.SeriesName == "S1_2").Should().BeTrue();
    }

    [Fact]
    public void ClearGraph_RemovesAllSeries()
    {
        var data = MakeSeries("S1", (1, 10));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load("test.ffg")).Returns(data);
        var vm = CreateSut(mockSvc.Object);
        vm.LoadSingleFile("test.ffg");

        vm.ClearGraphCommand.Execute();

        vm.Series.Should().BeEmpty();
        vm.PeakDataItems.Should().BeEmpty();
    }

    [Fact]
    public void LoadSingleFile_UpdatesCurrentFilePath()
    {
        var data = MakeSeries("S1", (1, 10));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load("path/to/file.ffg")).Returns(data);
        var vm = CreateSut(mockSvc.Object);

        vm.LoadSingleFile("path/to/file.ffg");

        vm.CurrentFilePath.Value.Should().Be("path/to/file.ffg");
    }

    [Fact]
    public void LoadSingleFile_UpdatesStatusMessage()
    {
        var data = MakeSeries("AB", (1, 5));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load("x.ffg")).Returns(data);
        var vm = CreateSut(mockSvc.Object);

        vm.LoadSingleFile("x.ffg");

        vm.StatusMessage.Value.Should().Contain("AB");
    }

    [Fact]
    public void LoadSingleFile_AdjustsAxisRange()
    {
        var data = MakeSeries("S1", (0, 0), (10, 100));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load(It.IsAny<string>())).Returns(data);
        var vm = CreateSut(mockSvc.Object);

        vm.LoadSingleFile("test.ffg");

        vm.XAxes[0].MinLimit.Should().BeLessThan(0);
        vm.XAxes[0].MaxLimit.Should().BeGreaterThan(10);
    }

    [Fact]
    public void ClearGraph_ResetsAxisLimits()
    {
        var data = MakeSeries("S1", (1, 10));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load(It.IsAny<string>())).Returns(data);
        var vm = CreateSut(mockSvc.Object);
        vm.LoadSingleFile("test.ffg");

        vm.ClearGraphCommand.Execute();

        vm.XAxes[0].MinLimit.Should().BeNull();
        vm.XAxes[0].MaxLimit.Should().BeNull();
    }

    [Fact]
    public void XAxisTitle_UpdatesAxisName()
    {
        var vm = CreateSut();
        vm.XAxisTitle.Value = "Displacement (mm)";

        vm.XAxes[0].Name.Should().Be("Displacement (mm)");
    }

    [Fact]
    public void YAxisTitle_UpdatesAxisName()
    {
        var vm = CreateSut();
        vm.YAxisTitle.Value = "Force (N)";

        vm.YAxes[0].Name.Should().Be("Force (N)");
    }

    [Fact]
    public void LoadFiles_LoadsMultipleFiles()
    {
        var data1 = MakeSeries("A1", (1, 10));
        var data2 = MakeSeries("B1", (2, 20));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load("a.ffg")).Returns(data1);
        mockSvc.Setup(s => s.Load("b.ffg")).Returns(data2);
        var vm = CreateSut(mockSvc.Object);

        vm.LoadFiles(["a.ffg", "b.ffg"]);

        vm.PeakDataItems.Select(r => r.SeriesName).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public void LoadSingleFile_LoadError_SetsErrorStatus()
    {
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load(It.IsAny<string>())).Throws(new IOException("File not found"));
        var vm = CreateSut(mockSvc.Object);

        vm.LoadSingleFile("nonexistent.ffg");

        vm.StatusMessage.Value.Should().Contain("エラー");
    }
}
