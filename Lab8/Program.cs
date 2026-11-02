// Lab 8
// Student name:
// Student number:

using System;
using System.Collections.Generic;
using System.Linq;

Console.WriteLine("CPEN223 Lab 8");

//Testing: Write some test cases to test well all methods you are to implement
//         This is to demonstrates what test cases you have considered
//TODO
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
        // TODO: Implement using Where and Select (no loops).
        throw new NotImplementedException();
    }

    public static List<SensorStats> SummarizeSensors(
        IEnumerable<Sample> samples,
        double threshold)
    {
        // TODO: Implement using GroupBy and aggregations (no loops).
        throw new NotImplementedException();
    }

    public static List<string> MostActiveSensors(
        IEnumerable<Sample> samples,
        double threshold,
        int count)
    {
        // TODO: Implement by reusing SummarizeSensors, with
        //       OrderByDescending, ThenBy, Take and Select (no loops).
        throw new NotImplementedException();
    }

    public static List<WindowStats> AnalyzeWindows(
        IEnumerable<Sample> samples,
        DateTime start,
        TimeSpan windowSize)
    {
        // TODO: Implement using GroupBy with a composite (Sensor, Window) key (no loops).
        throw new NotImplementedException();
    }

    public static List<int> SimultaneouslyActive(
        IEnumerable<WindowStats> windows,
        string sensorA,
        string sensorB,
        double threshold)
    {
        // TODO: Implement using Join on matching Window keys (no loops).
        throw new NotImplementedException();
    }
}
