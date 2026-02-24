using System.IO;
using ClosedXML.Excel;
using FluentAssertions;
using FFGViewer.Services;
using FFGViewer.Tests.Helpers;

namespace FFGViewer.Tests.Services;

public class ExcelExportServiceTests : IDisposable
{
    private readonly ExcelExportService _sut = new();
    private readonly string _tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".xlsx");

    [Fact]
    public void Export_CreatesXlsxFile()
    {
        var data = new FfgDataBuilder().WithTitle("S1").AddPoint(1, 10).Build();

        _sut.Export([data], _tempFile);

        File.Exists(_tempFile).Should().BeTrue();
    }

    [Fact]
    public void Export_ContainsPeakDataSheet()
    {
        var data = new FfgDataBuilder().WithTitle("S1").AddPoint(1, 10).Build();

        _sut.Export([data], _tempFile);

        using var wb = new XLWorkbook(_tempFile);
        wb.Worksheets.Any(ws => ws.Name == "PeakData").Should().BeTrue();
    }

    [Fact]
    public void Export_ContainsDataSheetPerSeries()
    {
        var s1 = new FfgDataBuilder().WithTitle("S1").AddPoint(1, 10).Build();
        var s2 = new FfgDataBuilder().WithTitle("S2").AddPoint(2, 20).Build();

        _sut.Export([s1, s2], _tempFile);

        using var wb = new XLWorkbook(_tempFile);
        wb.Worksheets.Any(ws => ws.Name == "Data_S1").Should().BeTrue();
        wb.Worksheets.Any(ws => ws.Name == "Data_S2").Should().BeTrue();
    }

    [Fact]
    public void Export_ContainsGraphSheet()
    {
        var data = new FfgDataBuilder().WithTitle("S1").AddPoint(1, 10).Build();

        _sut.Export([data], _tempFile);

        using var wb = new XLWorkbook(_tempFile);
        wb.Worksheets.Any(ws => ws.Name == "Graph").Should().BeTrue();
    }

    [Fact]
    public void Export_PeakDataSheetHasCorrectValues()
    {
        var data = new FfgDataBuilder().WithTitle("S1")
            .AddPoint(1.0, 10.0).AddPoint(2.0, 20.0).AddPoint(3.0, 5.0).Build();

        _sut.Export([data], _tempFile);

        using var wb = new XLWorkbook(_tempFile);
        var sheet = wb.Worksheet("PeakData");
        sheet.Cell(2, 1).GetString().Should().Be("S1");
        sheet.Cell(2, 2).GetDouble().Should().Be(20.0);
    }

    [Fact]
    public void Export_DataSheetHasCorrectRowCount()
    {
        var data = new FfgDataBuilder().WithTitle("A1")
            .AddPoint(1, 10).AddPoint(2, 20).AddPoint(3, 30).Build();

        _sut.Export([data], _tempFile);

        using var wb = new XLWorkbook(_tempFile);
        var sheet = wb.Worksheet("Data_A1");
        // Header row + 3 data rows = 4 rows used
        sheet.LastRowUsed()!.RowNumber().Should().Be(4);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }
}
