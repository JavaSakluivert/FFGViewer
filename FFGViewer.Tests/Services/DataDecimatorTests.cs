using System.Linq;
using FluentAssertions;
using FFGViewer.Models;
using FFGViewer.Services;

namespace FFGViewer.Tests.Services;

public class DataDecimatorTests
{
    // ------------------------------------------------------------------
    // ヘルパー
    // ------------------------------------------------------------------

    /// <summary>変位が単調増加する直線データを生成する。</summary>
    private static IReadOnlyList<DataPoint> MakeMonotone(int count, double slope = 1.0)
        => Enumerable.Range(0, count)
            .Select(i => new DataPoint(i * 0.001, i * 0.001 * slope))
            .ToList();

    /// <summary>
    /// ヒステリシスに近いデータを生成する。
    /// 変位が 0→amplitude→0→-amplitude を繰り返す (cycles サイクル)。
    /// </summary>
    private static IReadOnlyList<DataPoint> MakeHysteresis(int pointsPerHalfCycle, int halfCycles, double amplitude = 5.0)
    {
        var points = new List<DataPoint>(pointsPerHalfCycle * halfCycles + 1);
        for (int hc = 0; hc < halfCycles; hc++)
        {
            double startDisp = hc % 2 == 0 ? 0.0 : amplitude * ((hc / 2) + 1);
            double endDisp   = hc % 2 == 0 ? amplitude * ((hc / 2) + 1) : 0.0;

            for (int i = 0; i < pointsPerHalfCycle; i++)
            {
                double t = (double)i / (pointsPerHalfCycle - 1);
                double disp = startDisp + (endDisp - startDisp) * t;
                double load = Math.Sin(Math.PI * t) * amplitude * (hc + 1);
                points.Add(new DataPoint(disp, load));
            }
        }
        return points;
    }

    // ------------------------------------------------------------------
    // Decimate
    // ------------------------------------------------------------------

    [Fact]
    public void Decimate_BelowThreshold_ReturnsSameReference()
    {
        var points = MakeMonotone(100);
        var result = DataDecimator.Decimate(points);

        result.Should().BeSameAs(points);
    }

    [Fact]
    public void Decimate_ExactlyAtThreshold_ReturnsSameReference()
    {
        var points = MakeMonotone(DataDecimator.DecimationThreshold);
        var result = DataDecimator.Decimate(points);

        result.Should().BeSameAs(points);
    }

    [Fact]
    public void Decimate_AboveThreshold_ReducesCount()
    {
        var points = MakeMonotone(DataDecimator.DecimationThreshold + 5_000);
        var result = DataDecimator.Decimate(points);

        result.Count.Should().BeLessThan(points.Count);
    }

    [Fact]
    public void Decimate_PreservesFirstAndLastPoint()
    {
        var points = MakeMonotone(DataDecimator.DecimationThreshold + 5_000);
        var result = DataDecimator.Decimate(points);

        result[0].Should().Be(points[0]);
        result[^1].Should().Be(points[^1]);
    }

    [Fact]
    public void Decimate_ReturnsNonEmptyResult()
    {
        var points = MakeMonotone(DataDecimator.DecimationThreshold + 1);
        var result = DataDecimator.Decimate(points);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public void Decimate_Hysteresis_ReducesCount()
    {
        // 2,000点/半サイクル × 20半サイクル = 40,000点（閾値超）
        var points = MakeHysteresis(2_000, 20);
        points.Count.Should().BeGreaterThan(DataDecimator.DecimationThreshold);

        var result = DataDecimator.Decimate(points);

        result.Count.Should().BeLessThan(points.Count);
    }

    [Fact]
    public void Decimate_Hysteresis_PreservesFirstAndLastPoint()
    {
        var points = MakeHysteresis(2_000, 20);
        var result = DataDecimator.Decimate(points);

        result[0].Should().Be(points[0]);
        result[^1].Should().Be(points[^1]);
    }

    // ------------------------------------------------------------------
    // FindTurningPoints
    // ------------------------------------------------------------------

    [Fact]
    public void FindTurningPoints_MonotoneData_ReturnsOnlyStartAndEnd()
    {
        // 単調増加データ → 転換点は始点と終点のみ
        var points = MakeMonotone(100).ToArray();
        var indices = DataDecimator.FindTurningPoints(points);

        indices.Should().HaveCount(2);
        indices[0].Should().Be(0);
        indices[^1].Should().Be(points.Length - 1);
    }

    [Fact]
    public void FindTurningPoints_SinglePeak_DetectsOneTurningPoint()
    {
        // 0→50→0 (1サイクル2半サイクル)
        var points = Enumerable.Range(0, 100)
            .Select(i => new DataPoint(i < 50 ? i * 0.1 : (100 - i) * 0.1, 0.0))
            .ToArray();

        var indices = DataDecimator.FindTurningPoints(points);

        // 始点、ピーク付近、終点 の3点が期待される
        indices.Should().HaveCountGreaterThanOrEqualTo(3);
        indices[0].Should().Be(0);
        indices[^1].Should().Be(points.Length - 1);
    }

    [Fact]
    public void FindTurningPoints_MultiCycle_DetectsMultipleTurningPoints()
    {
        // 2サイクル4半サイクルのデータ
        var points = MakeHysteresis(200, 4).ToArray();
        var indices = DataDecimator.FindTurningPoints(points);

        // 始点・各サイクルのピーク・終点が含まれるため4点以上
        indices.Should().HaveCountGreaterThanOrEqualTo(4);
        indices[0].Should().Be(0);
        indices[^1].Should().Be(points.Length - 1);
    }

    [Fact]
    public void FindTurningPoints_SmallData_ReturnsStartAndEnd()
    {
        var points = new[]
        {
            new DataPoint(0, 0),
            new DataPoint(1, 1),
        };
        var indices = DataDecimator.FindTurningPoints(points);

        indices.Should().HaveCount(2);
        indices[0].Should().Be(0);
        indices[1].Should().Be(1);
    }

    // ------------------------------------------------------------------
    // パフォーマンス確認（実行時間の目安チェック）
    // ------------------------------------------------------------------

    [Fact]
    public void Decimate_LargeData_CompletesWithinReasonableTime()
    {
        // 6シリーズ × 100,000点の合計に相当するデータを間引けるか
        var points = MakeMonotone(100_000);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 6; i++)
            DataDecimator.Decimate(points);

        sw.Stop();
        // 6シリーズ分の間引きが1秒以内に完了すること
        sw.ElapsedMilliseconds.Should().BeLessThan(1_000);
    }
}
