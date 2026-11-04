// Lab 8
// Student name: Ella Gladders
// Student number: 78754249

using System;
using System.Collections.Generic;
using System.Linq;

Console.WriteLine("CPEN223 Lab 8");

//Testing: Write some test cases to test well all methods you are to implement
//         This is to demonstrates what test cases you have considered
// print whether each test passed
void Check(string name, bool passed)
{
    Console.WriteLine($"{name}: {(passed ? "PASS" : "FAIL")}");
}

DateTime t0 = new(2026, 10, 1, 12, 0, 0);

List<Sample> samples = new()
{
    new("B", t0.AddSeconds(10), 4),
    new("A", t0, -3),
    new("A", t0.AddSeconds(9), 4),
    new("A", t0.AddSeconds(10), 5),
    new("A", t0, double.NaN),
    new("X", t0, double.PositiveInfinity)
};

// test calibration, invalid values, unknown sensors, and preserved order
var calibration = new Dictionary<string, Calibration>
{
    ["A"] = new(1, 2)
};

var cleaned = EmgLinqAnalyzer.CleanAndCalibrate(samples, calibration);

Check("cleaning", cleaned.SequenceEqual(new[]
{
    new Sample("A", t0, -8),
    new Sample("A", t0.AddSeconds(9), 6),
    new Sample("A", t0.AddSeconds(10), 8)
}));

// test grouping, rms, active fraction, and alphabetical ordering
var summary = EmgLinqAnalyzer.SummarizeSensors(samples, 4);

Check("summary",
    summary.Count == 2 &&
    summary[0].Sensor == "A" &&
    summary[0].Count == 3 &&
    Math.Abs(summary[0].RMS - Math.Sqrt(50.0 / 3)) < 1e-9 &&
    Math.Abs(summary[0].ActiveFraction - 2.0 / 3) < 1e-9 &&
    summary[1] == new SensorStats("B", 1, 4, 1));

// test ranking and alphabetical tie breaking
var tied = new[]
{
    new Sample("B", t0, 5),
    new Sample("A", t0, -5),
    new Sample("C", t0, 8)
};

Check("ranking",
    EmgLinqAnalyzer.MostActiveSensors(tied, 4, 2)
        .SequenceEqual(new[] { "C", "A" }));

// test window boundaries, counts, rms, and output ordering
var windows = EmgLinqAnalyzer.AnalyzeWindows(
    samples, t0, TimeSpan.FromSeconds(10));

Check("windows", windows.SequenceEqual(new[]
{
    new WindowStats("A", 0, 2, Math.Sqrt(12.5)),
    new WindowStats("A", 1, 1, 5),
    new WindowStats("B", 1, 1, 4)
}));

// test matching active windows, including equality with the threshold
Check("simultaneous activity",
    EmgLinqAnalyzer.SimultaneouslyActive(windows, "A", "B", 4)
        .SequenceEqual(new[] { 1 }));
//end Testing code

//Do not change the program skeleton: keep the record structs, the class name,
//method names, parameters, and return types exactly as given.
//You may add private helper methods.

public readonly record struct Sample(string Sensor, DateTime Time, double Value);

public readonly record struct Calibration(double Offset, double Gain);

public readonly record struct SensorStats(string Sensor, int Count,
                                          double RMS, double ActiveFraction);

public readonly record struct WindowStats(string Sensor, int Window,
                                          int Count, double RMS);

public static class EmgLinqAnalyzer
{
    public static List<Sample> CleanAndCalibrate(
        IEnumerable<Sample> samples,
        IReadOnlyDictionary<string, Calibration> calibration)
    {
        // checks for null inputs
        if (samples == null || calibration == null) 
        {
        throw new ArgumentException("Inputs cannot be null.");
        } 

        // check every supplied calibration 
        if (calibration.Values.Any(c => !double.IsFinite(c.Offset) || !double.IsFinite(c.Gain) || c.Gain <= 0))
        {
            throw new ArgumentException("Invalid calibration values.");
        }

        return samples 
            // filter out samples without calibration
            // keep only finite readings from sensors with calibration
            .Where(s => double.IsFinite(s.Value) &&
            calibration.ContainsKey(s.Sensor))
            .Select(s => 
            {
                // transform each retained sample
                Calibration c = calibration[s.Sensor];
                // retrieve the calibration from the dictonary 
                double calibratedValue = (s.Value - c.Offset) * c.Gain;
                return new Sample(s.Sensor, s.Time, calibratedValue);
            })
            .ToList();
    }

    public static List<SensorStats> SummarizeSensors(
        IEnumerable<Sample> samples,
        double threshold)
    {
       // reject null samples and nonfinite or negative thresholds
    if (samples == null ||
        !double.IsFinite(threshold) ||
        threshold < 0)
    {
        throw new ArgumentException("Invalid samples or threshold.");
    }

    return samples
        // keep only samples with finite values
        .Where(s => double.IsFinite(s.Value))

        // group the remaining samples by sensor name
        .GroupBy(s => s.Sensor)

        // create one summary for each sensor group
        .Select(g => new SensorStats(
            // use the group's key as the sensor name
            g.Key,
            g.Count(),

            // calculate rms by taking the square root of the mean squared value
            Math.Sqrt(g.Average(s => s.Value * s.Value)),

            // divide active readings by total readings using decimal division
            (double)g.Count(s => Math.Abs(s.Value) >= threshold)
                / g.Count()
        ))

        // sort by sensor name using ordinal string comparison
        .OrderBy(stats => stats.Sensor, StringComparer.Ordinal)
        .ToList();
    }

    public static List<string> MostActiveSensors(
        IEnumerable<Sample> samples,
        double threshold,
        int count)
    {
        // reject a negative number of requested sensors
    if (count < 0)
    {
        throw new ArgumentException("Count cannot be negative.");
    }

    // reuse the summaries and their validation of samples and threshold
    return SummarizeSensors(samples, threshold)
        // sort from highest rms to lowest
        .OrderByDescending(stats => stats.RMS)
        // break ties by ascending sensor name
        .ThenBy(stats => stats.Sensor, StringComparer.Ordinal)
        // keep up to count sensors
        .Take(count)
        // return just the sensor names
        .Select(stats => stats.Sensor)
        .ToList();
    }

    public static List<WindowStats> AnalyzeWindows(
        IEnumerable<Sample> samples,
        DateTime start,
        TimeSpan windowSize)
    {
        // reject null samples and nonpositive window sizes
    if (samples == null || windowSize <= TimeSpan.Zero)
    {
        throw new ArgumentException("Invalid samples or window size.");
    }

    return samples
        // keep finite readings at or after the start time
        .Where(s => double.IsFinite(s.Value) && s.Time >= start)

        // group by both sensor name and window index
        .GroupBy(s => (
            Sensor: s.Sensor,
            Window: checked((int)((s.Time - start).Ticks / windowSize.Ticks))
        ))

        // create one summary for each sensor and window combination
        .Select(g => new WindowStats(
            g.Key.Sensor,
            g.Key.Window,
            g.Count(),
            Math.Sqrt(g.Average(s => s.Value * s.Value))
        ))

        // sort by window first, then by sensor name
        .OrderBy(stats => stats.Window)
        .ThenBy(stats => stats.Sensor, StringComparer.Ordinal)

        .ToList();
    }

    public static List<int> SimultaneouslyActive(
        IEnumerable<WindowStats> windows,
        string sensorA,
        string sensorB,
        double threshold)
    {
    // reject null input, invalid sensor names, and invalid thresholds
    if (windows == null ||
        string.IsNullOrEmpty(sensorA) ||
        string.IsNullOrEmpty(sensorB) ||
        string.Equals(sensorA, sensorB, StringComparison.Ordinal) ||
        !double.IsFinite(threshold) ||
        threshold < 0)
    {
        throw new ArgumentException("Invalid windows, sensors, or threshold.");
    }

    // take one snapshot so both sequences use the same input data
    List<WindowStats> snapshot = windows.ToList();

    // keep active windows for sensor a
    var activeA = snapshot.Where(w =>
        w.Sensor == sensorA && w.RMS >= threshold);

    // keep active windows for sensor b
    var activeB = snapshot.Where(w =>
        w.Sensor == sensorB && w.RMS >= threshold);

    return activeA
        // match active entries that have the same window index
        .Join(
            activeB,
            a => a.Window,
            b => b.Window,
            (a, b) => a.Window
        )
        // sort matching window indices in ascending order
        .OrderBy(window => window)
        .ToList();
    }
}
