/// <summary>Rolling physics-step statistics for the interactive demo scenes: a smoothed average
/// over a window plus the peak (the spikes during a collapse). Reset the peak to measure a specific
/// action.</summary>
public class StepStats
{
    private readonly double[] _window;
    private int _count;
    private int _head;
    private double _sum;

    public StepStats(int windowSize = 120)
    {
        _window = new double[windowSize];
    }

    /// <summary>The rolling average step time (ms).</summary>
    public double Average => _count == 0 ? 0.0 : _sum / _count;

    /// <summary>The highest step time seen since the last reset (ms).</summary>
    public double Peak { get; private set; }

    public void Add(double milliseconds)
    {
        if (_count == _window.Length) _sum -= _window[_head];
        else _count++;

        _sum += milliseconds;
        _window[_head] = milliseconds;
        _head = (_head + 1) % _window.Length;

        if (milliseconds > Peak) Peak = milliseconds;
    }

    public void ResetPeak()
    {
        Peak = 0.0;
    }
}
