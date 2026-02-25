using FFGViewer.Models;

namespace FFGViewer.Services;

/// <summary>
/// 大量データ点の表示用間引きを行うユーティリティクラス。
/// サイクル認識型 LTTB アルゴリズムを使用して視覚的特徴を保ちながら点数を削減する。
/// ピーク計算・エクスポートには元データを使用すること。
/// </summary>
public static class DataDecimator
{
    /// <summary>間引きを発動するデータ点数の閾値。これ以下は全点をそのまま返す。</summary>
    public const int DecimationThreshold = 15_000;

    /// <summary>各半サイクルあたりの目標点数。</summary>
    private const int TargetPointsPerCycle = 400;

    /// <summary>転換点として認識する最小変位変化量（総変位レンジに対する比率）。</summary>
    private const double MinDeltaRatio = 0.01;

    /// <summary>
    /// データ点列を表示用に間引く。
    /// データ数が <see cref="DecimationThreshold"/> 以下の場合は元のリストをそのまま返す。
    /// </summary>
    public static IReadOnlyList<DataPoint> Decimate(IReadOnlyList<DataPoint> points)
    {
        if (points.Count <= DecimationThreshold) return points;

        // IReadOnlyList を配列に変換してインデックスアクセスを高速化
        var arr = points is DataPoint[] a ? a : points.ToArray();

        var turningIndices = FindTurningPoints(arr);

        var result = new List<DataPoint>();
        for (int seg = 0; seg < turningIndices.Count - 1; seg++)
        {
            int start = turningIndices[seg];
            int end   = turningIndices[seg + 1];
            int segLen = end - start + 1;

            var decimated = segLen > TargetPointsPerCycle
                ? LttbSegment(arr, start, end, TargetPointsPerCycle)
                : ExtractRange(arr, start, end);

            // 先頭以外のセグメントは境界点の重複を避けるため最初の1点をスキップ
            if (seg == 0)
                result.AddRange(decimated);
            else
                result.AddRange(decimated.Skip(1));
        }

        return result;
    }

    /// <summary>
    /// 変位の増減方向が反転する転換点（半サイクル境界）のインデックスリストを返す。
    /// 微小ノイズによる誤検出を防ぐため、総変位レンジの 1% 未満の変化は無視する。
    /// </summary>
    public static List<int> FindTurningPoints(DataPoint[] points)
    {
        var indices = new List<int> { 0 };

        if (points.Length < 3)
        {
            indices.Add(points.Length - 1);
            return indices;
        }

        double minDisp = double.MaxValue;
        double maxDisp = double.MinValue;
        foreach (var p in points)
        {
            if (p.Displacement < minDisp) minDisp = p.Displacement;
            if (p.Displacement > maxDisp) maxDisp = p.Displacement;
        }
        double minDelta = (maxDisp - minDisp) * MinDeltaRatio;

        int direction = 0; // 0=未定, 1=増加, -1=減少
        double lastTurningDisp = points[0].Displacement;

        for (int i = 1; i < points.Length; i++)
        {
            double diff = points[i].Displacement - points[i - 1].Displacement;
            if (Math.Abs(diff) < 1e-12) continue;

            int currentDir = diff > 0 ? 1 : -1;

            if (direction == 0)
            {
                direction = currentDir;
                continue;
            }

            if (currentDir != direction)
            {
                // 前回の転換点からの変位変化が閾値を超えている場合のみ有効な転換点とする
                double movement = Math.Abs(points[i - 1].Displacement - lastTurningDisp);
                if (movement >= minDelta)
                {
                    indices.Add(i - 1);
                    lastTurningDisp = points[i - 1].Displacement;
                    direction = currentDir;
                }
            }
        }

        indices.Add(points.Length - 1);
        return indices;
    }

    /// <summary>
    /// LTTB (Largest-Triangle-Three-Buckets) アルゴリズムでセグメントを間引く。
    /// 各バケット内で「前選択点・候補点・次バケット重心」の三角形面積が最大の点を選択する。
    /// </summary>
    private static List<DataPoint> LttbSegment(DataPoint[] data, int start, int end, int threshold)
    {
        int segLen = end - start + 1;
        if (segLen <= threshold) return ExtractRange(data, start, end);

        var sampled = new List<DataPoint>(threshold) { data[start] };

        double bucketSize = (double)(segLen - 2) / (threshold - 2);
        int prevIdx = start;

        for (int bucket = 0; bucket < threshold - 2; bucket++)
        {
            int curStart = start + (int)Math.Floor((bucket + 0) * bucketSize) + 1;
            int curEnd   = start + (int)Math.Floor((bucket + 1) * bucketSize) + 1;
            curEnd = Math.Min(curEnd, end);

            int nextStart = curEnd;
            int nextEnd   = start + (int)Math.Floor((bucket + 2) * bucketSize) + 1;
            nextEnd = Math.Min(nextEnd, end + 1);

            // 次バケットの重心を計算（三角形の頂点C）
            double avgX = 0, avgY = 0;
            int avgCount = nextEnd - nextStart;
            if (avgCount > 0)
            {
                for (int j = nextStart; j < nextEnd; j++)
                {
                    avgX += data[j].Displacement;
                    avgY += data[j].Load;
                }
                avgX /= avgCount;
                avgY /= avgCount;
            }
            else
            {
                // 最終セグメントのフォールバック: 終端点を使用
                avgX = data[end].Displacement;
                avgY = data[end].Load;
            }

            // 現バケット内で三角形面積が最大の点を選択
            double maxArea = -1;
            int selectedIdx = curStart;
            double aX = data[prevIdx].Displacement;
            double aY = data[prevIdx].Load;

            for (int j = curStart; j < curEnd; j++)
            {
                // 三角形面積 = |Ax*(By - Cy) + Bx*(Cy - Ay) + Cx*(Ay - By)|
                double area = Math.Abs(
                    aX * (data[j].Load - avgY) +
                    data[j].Displacement * (avgY - aY) +
                    avgX * (aY - data[j].Load)
                );
                if (area > maxArea)
                {
                    maxArea = area;
                    selectedIdx = j;
                }
            }

            sampled.Add(data[selectedIdx]);
            prevIdx = selectedIdx;
        }

        sampled.Add(data[end]);
        return sampled;
    }

    private static List<DataPoint> ExtractRange(DataPoint[] data, int start, int end)
    {
        var result = new List<DataPoint>(end - start + 1);
        for (int i = start; i <= end; i++)
            result.Add(data[i]);
        return result;
    }
}
