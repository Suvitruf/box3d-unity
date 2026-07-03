using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>Collects per-step timings for a physics sandbox run: skips warmup, records a fixed
/// number of samples, then logs summary statistics and writes a CSV to PerfResults/ in the
/// project root. Use one recorder per run.</summary>
public class PhysicsPerfRecorder
{
    private const int RollingWindow = 60;

    private readonly string _engineName;
    private readonly string _metadata;
    private readonly int _warmupSteps;
    private readonly double[] _samplesMs;

    private int _warmupSeen;
    private int _sampleCount;
    private double _rollingAccumMs;
    private int _rollingCount;

    public float RollingAverageMs { get; private set; }
    public bool IsFinished { get; private set; }
    public string Summary { get; private set; } = "";

    public PhysicsPerfRecorder(string engineName, int warmupSteps, int measureSteps, string metadata)
    {
        _engineName = engineName;
        _warmupSteps = warmupSteps;
        _samplesMs = new double[measureSteps];
        _metadata = metadata;
    }

    public void AddSample(double milliseconds)
    {
        UpdateRollingAverage(milliseconds);

        if (IsFinished) return;

        if (_warmupSeen < _warmupSteps)
        {
            _warmupSeen++;
            return;
        }

        _samplesMs[_sampleCount] = milliseconds;
        _sampleCount++;
        if (_sampleCount == _samplesMs.Length) Finish();
    }

    private void UpdateRollingAverage(double milliseconds)
    {
        _rollingAccumMs += milliseconds;
        _rollingCount++;
        if (_rollingCount < RollingWindow) return;

        RollingAverageMs = (float)(_rollingAccumMs / _rollingCount);
        _rollingAccumMs = 0.0;
        _rollingCount = 0;
    }

    private void Finish()
    {
        IsFinished = true;

        double[] sorted = (double[])_samplesMs.Clone();
        Array.Sort(sorted);

        double total = 0.0;
        foreach (double sample in sorted) total += sample;
        double average = total / sorted.Length;

        double variance = 0.0;
        foreach (double sample in sorted) variance += (sample - average) * (sample - average);
        double stdDev = Math.Sqrt(variance / sorted.Length);

        double median = Percentile(sorted, 50);
        double p95 = Percentile(sorted, 95);
        double p99 = Percentile(sorted, 99);

        Summary = $"{_engineName}: avg {average:F3} ms | median {median:F3} | p95 {p95:F3} | p99 {p99:F3} | " +
                  $"min {sorted[0]:F3} | max {sorted[^1]:F3} | stddev {stdDev:F3} | steps {sorted.Length} | {_metadata}";
        Debug.Log($"[Perf] {Summary}");

        WriteCsv(average, median, p95, p99, sorted[0], sorted[^1], stdDev);
    }

    private static double Percentile(double[] sorted, int percent)
    {
        double rank = percent / 100.0 * (sorted.Length - 1);
        int lower = (int)rank;
        if (lower + 1 >= sorted.Length) return sorted[^1];
        double fraction = rank - lower;
        return sorted[lower] + (sorted[lower + 1] - sorted[lower]) * fraction;
    }

    private void WriteCsv(double average, double median, double p95, double p99, double min, double max, double stdDev)
    {
        string directory = Path.Combine(Application.dataPath, "..", "PerfResults");
        Directory.CreateDirectory(directory);

        string fileName = $"{_engineName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string path = Path.GetFullPath(Path.Combine(directory, fileName));

        var builder = new StringBuilder();
        builder.AppendLine($"# engine,{_engineName}");
        builder.AppendLine($"# metadata,{_metadata}");
        builder.AppendLine($"# unity,{Application.unityVersion}");
        builder.AppendLine($"# date,{DateTime.Now:O}");
        builder.AppendLine($"# warmupSteps,{_warmupSteps}");
        builder.AppendLine(FormattableString.Invariant(
            $"# summary_ms,avg,{average:F4},median,{median:F4},p95,{p95:F4},p99,{p99:F4},min,{min:F4},max,{max:F4},stddev,{stdDev:F4}"));
        builder.AppendLine("step,ms");
        for (int i = 0; i < _samplesMs.Length; i++)
        {
            builder.AppendLine(FormattableString.Invariant($"{i},{_samplesMs[i]:F4}"));
        }

        File.WriteAllText(path, builder.ToString());
        Debug.Log($"[Perf] CSV written: {path}");
    }
}
