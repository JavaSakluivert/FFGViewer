using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Packaging;
using FFGViewer.Models;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace FFGViewer.Services;

public class ExcelExportService : IExcelExportService
{
    public void Export(IReadOnlyList<FfgData> seriesList, string filePath)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: PeakData
        var peakSheet = workbook.AddWorksheet("PeakData");
        peakSheet.Cell(1, 1).Value = "Series";
        peakSheet.Cell(1, 2).Value = "MaxLoad";
        peakSheet.Cell(1, 3).Value = "MaxLoad_Disp";
        peakSheet.Cell(1, 4).Value = "MinLoad";
        peakSheet.Cell(1, 5).Value = "MinLoad_Disp";

        for (int i = 0; i < seriesList.Count; i++)
        {
            var s = seriesList[i];
            var row = i + 2;
            peakSheet.Cell(row, 1).Value = s.Title;
            peakSheet.Cell(row, 2).Value = s.PeakData.MaxLoad;
            peakSheet.Cell(row, 3).Value = s.PeakData.MaxLoadDisplacement;
            peakSheet.Cell(row, 4).Value = s.PeakData.MinLoad;
            peakSheet.Cell(row, 5).Value = s.PeakData.MinLoadDisplacement;
        }

        // Sheet 2+: Data per series
        foreach (var series in seriesList)
        {
            var sheetName = $"Data_{series.Title}";
            var dataSheet = workbook.AddWorksheet(sheetName);
            dataSheet.Cell(1, 1).Value = "Displacement";
            dataSheet.Cell(1, 2).Value = "Load";

            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                var pt = series.DataPoints[i];
                dataSheet.Cell(i + 2, 1).Value = pt.Displacement;
                dataSheet.Cell(i + 2, 2).Value = pt.Load;
            }
        }

        // Graph sheet placeholder (chart injected via OpenXml below)
        workbook.AddWorksheet("Graph");
        workbook.SaveAs(filePath);

        // Inject XY scatter chart using OpenXml SDK
        AddScatterChart(filePath, seriesList);
    }

    private static void AddScatterChart(string filePath, IReadOnlyList<FfgData> seriesList)
    {
        using var spreadDoc = SpreadsheetDocument.Open(filePath, true);
        var workbookPart = spreadDoc.WorkbookPart!;

        var graphSheet = workbookPart.Workbook.Descendants<DocumentFormat.OpenXml.Spreadsheet.Sheet>()
            .FirstOrDefault(s => s.Name == "Graph");
        if (graphSheet is null) return;

        var graphSheetPart = (WorksheetPart)workbookPart.GetPartById(graphSheet.Id!);
        var drawingsPart = graphSheetPart.AddNewPart<DrawingsPart>();
        graphSheetPart.Worksheet.AppendChild(
            new DocumentFormat.OpenXml.Spreadsheet.Drawing
            {
                Id = graphSheetPart.GetIdOfPart(drawingsPart)
            });
        graphSheetPart.Worksheet.Save();

        var chartPart = drawingsPart.AddNewPart<ChartPart>();
        BuildScatterChart(chartPart, seriesList);

        // Embed chart in drawing
        var wsDr = new WorksheetDrawing();
        wsDr.AddNamespaceDeclaration("xdr", "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing");
        wsDr.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
        wsDr.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");

        var anchor = new TwoCellAnchor();
        anchor.AppendChild(new FromMarker(
            new ColumnId("1"), new ColumnOffset("0"),
            new RowId("1"), new RowOffset("0")));
        anchor.AppendChild(new ToMarker(
            new ColumnId("12"), new ColumnOffset("0"),
            new RowId("25"), new RowOffset("0")));

        var frame = new GraphicFrame();
        frame.AppendChild(new NonVisualGraphicFrameProperties(
            new NonVisualDrawingProperties { Id = 2, Name = "Chart 1" },
            new NonVisualGraphicFrameDrawingProperties()));
        frame.AppendChild(new Transform(
            new DocumentFormat.OpenXml.Drawing.Offset { X = 0, Y = 0 },
            new DocumentFormat.OpenXml.Drawing.Extents { Cx = 0, Cy = 0 }));

        var graphic = new DocumentFormat.OpenXml.Drawing.Graphic();
        var graphicData = new DocumentFormat.OpenXml.Drawing.GraphicData
        {
            Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart"
        };
        graphicData.AppendChild(new ChartReference { Id = drawingsPart.GetIdOfPart(chartPart) });
        graphic.AppendChild(graphicData);
        frame.AppendChild(graphic);

        anchor.AppendChild(frame);
        anchor.AppendChild(new ClientData());
        wsDr.AppendChild(anchor);

        drawingsPart.WorksheetDrawing = wsDr;
        drawingsPart.WorksheetDrawing.Save();
    }

    private static void BuildScatterChart(ChartPart chartPart, IReadOnlyList<FfgData> seriesList)
    {
        var chartSpace = new ChartSpace();
        chartSpace.AddNamespaceDeclaration("c", "http://schemas.openxmlformats.org/drawingml/2006/chart");
        chartSpace.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
        chartSpace.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");

        var chart = new Chart();
        var plotArea = new PlotArea();
        var scatterChart = new ScatterChart();
        scatterChart.AppendChild(new ScatterStyle { Val = ScatterStyleValues.LineMarker });
        scatterChart.AppendChild(new VaryColors { Val = false });

        for (int idx = 0; idx < seriesList.Count; idx++)
        {
            var series = seriesList[idx];
            int rowCount = series.DataPoints.Count;
            if (rowCount == 0) continue;

            var sheetName = $"Data_{series.Title}";
            var scatterSeries = new ScatterChartSeries();
            scatterSeries.AppendChild(new DocumentFormat.OpenXml.Drawing.Charts.Index { Val = (uint)idx });
            scatterSeries.AppendChild(new Order { Val = (uint)idx });
            scatterSeries.AppendChild(new SeriesText(
                new StringReference(
                    new Formula($"'{sheetName}'!$A$1"),
                    new StringCache(
                        new PointCount { Val = 1 },
                        new StringPoint { Index = 0, NumericValue = new NumericValue(series.Title) }))));

            var xValues = new XValues(
                new NumberReference(new Formula($"'{sheetName}'!$A$2:$A${rowCount + 1}")));
            scatterSeries.AppendChild(xValues);

            var yValues = new YValues(
                new NumberReference(new Formula($"'{sheetName}'!$B$2:$B${rowCount + 1}")));
            scatterSeries.AppendChild(yValues);

            scatterChart.AppendChild(scatterSeries);
        }

        plotArea.AppendChild(scatterChart);
        chart.AppendChild(plotArea);
        chartSpace.AppendChild(chart);
        chartPart.ChartSpace = chartSpace;
        chartPart.ChartSpace.Save();
    }
}
