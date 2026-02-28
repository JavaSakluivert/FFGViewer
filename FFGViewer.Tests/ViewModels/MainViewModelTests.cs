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
        var csvFileMock = new Mock<ICsvFileService>();
        var csvExportMock = new Mock<ICsvExportService>();
        var xlsMock = new Mock<IExcelExportService>();
        ffgSvc ??= new Mock<IFfgFileService>().Object;
        return new MainViewModel(ffgSvc, csvFileMock.Object, csvExportMock.Object, xlsMock.Object);
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

        // [RelayCommand] により生成される IRelayCommand は ICommand.Execute(object?) を使う
        vm.ClearGraphCommand.Execute(null);

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

        vm.ClearGraphCommand.Execute(null);

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

    [Fact]
    public void IsDecimationEnabled_DefaultsToTrue()
    {
        var vm = CreateSut();

        vm.IsDecimationEnabled.Value.Should().BeTrue();
    }

    [Fact]
    public void CanToggleDecimation_DefaultsFalse()
    {
        var vm = CreateSut();

        vm.CanToggleDecimation.Value.Should().BeFalse();
    }

    [Fact]
    public void CanToggleDecimation_RemainsFlaseForSmallSeries()
    {
        // 閾値以下（2点）のシリーズを読み込んでも有効にならない
        var data = MakeSeries("S1", (1, 10), (2, 20));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load(It.IsAny<string>())).Returns(data);
        var vm = CreateSut(mockSvc.Object);

        vm.LoadSingleFile("test.ffg");

        vm.CanToggleDecimation.Value.Should().BeFalse();
    }

    [Fact]
    public void CanToggleDecimation_BecomesTrueForLargeSeries()
    {
        // 閾値を超えるデータ点数のシリーズを読み込むと有効になる
        var builder = new FfgDataBuilder().WithTitle("Big").WithFilePath("big.ffg");
        for (int i = 0; i <= DataDecimator.DecimationThreshold; i++)
            builder.AddPoint(i * 0.001, i * 0.001);
        var largeData = builder.Build();

        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load(It.IsAny<string>())).Returns(largeData);
        var vm = CreateSut(mockSvc.Object);

        vm.LoadSingleFile("big.ffg");

        vm.CanToggleDecimation.Value.Should().BeTrue();
    }

    [Fact]
    public void ClearGraph_ResetsCanToggleDecimation()
    {
        var builder = new FfgDataBuilder().WithTitle("Big").WithFilePath("big.ffg");
        for (int i = 0; i <= DataDecimator.DecimationThreshold; i++)
            builder.AddPoint(i * 0.001, i * 0.001);
        var largeData = builder.Build();

        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load(It.IsAny<string>())).Returns(largeData);
        var vm = CreateSut(mockSvc.Object);
        vm.LoadSingleFile("big.ffg");

        vm.ClearGraphCommand.Execute(null);

        vm.CanToggleDecimation.Value.Should().BeFalse();
    }

    [Fact]
    public void PeakDataRows_UseLineColor_NotPeakColor()
    {
        // ピークデータ行（テーブル）はシリーズ線の色で表示する
        var data = MakeSeries("S1", (1, 10), (2, 20));
        var mockSvc = new Mock<IFfgFileService>();
        mockSvc.Setup(s => s.Load("test.ffg")).Returns(data);
        var vm = CreateSut(mockSvc.Object);

        vm.LoadSingleFile("test.ffg");

        // Steel Blue (#428BCA) が線の色として最初のシリーズに割り当てられる
        vm.PeakDataItems[0].SeriesColor.R.Should().Be(0x42);
        vm.PeakDataItems[0].SeriesColor.G.Should().Be(0x8B);
        vm.PeakDataItems[0].SeriesColor.B.Should().Be(0xCA);
    }
}
