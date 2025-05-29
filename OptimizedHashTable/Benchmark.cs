using System.Diagnostics;
using OptimizedHashTable.Interfaces;
using OptimizedHashTable.Tables;

namespace OptimizedHashTables;

public class BenchmarkResult
{
    public string Name { get; set; } = string.Empty;
    public List<long> FillTimes = new();
    public List<long> LookupTimes = new();
    public List<int> FoundCounts = new();
}

public static class Benchmark
{
    private const int Capacity = 1048576;
    private const int InsertCount = 1038576;

    private static readonly byte[] buffer = new byte[8];

    private static ulong NextULong(Random rng)
    {
        rng.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }

    public static BenchmarkResult Run<T>(string name, Func<int, T> tableFactory)
        where T : IHashTable<ulong, string>
    {
        var result = new BenchmarkResult { Name = name };

        // Warm-up (not measured)
        var warmupTable = tableFactory(Capacity);
        var warmupRng = new Random(42);
        for (int i = 0; i < 1000; i++)
        {
            warmupTable.Insert(NextULong(warmupRng), "Loc" + i);
        }

        for (int trial = 0; trial < 10; trial++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var rng = new Random(42);
            var table = tableFactory(Capacity);

            var fillWatch = Stopwatch.StartNew();
            for (int i = 0; i < InsertCount; i++)
            {
                ulong ip = NextULong(rng);
                table.Insert(ip, "Loc" + i);
            }
            fillWatch.Stop();
            result.FillTimes.Add(fillWatch.ElapsedMilliseconds);

            rng = new Random(42);
            int found = 0;
            var lookupWatch = Stopwatch.StartNew();
            for (int i = 0; i < InsertCount; i++)
            {
                ulong ip = NextULong(rng);
                if (table.GetValue(ip) != null)
                    found++;
            }
            lookupWatch.Stop();
            result.LookupTimes.Add(lookupWatch.ElapsedMilliseconds);
            result.FoundCounts.Add(found);
        }

        return result;
    }

    public static BenchmarkResult RunDictionary()
    {
        var result = new BenchmarkResult { Name = "Dictionary" };

        var warmup = new Dictionary<ulong, string>();
        var warmupRng = new Random(42);
        for (int i = 0; i < 1000; i++)
        {
            warmup[NextULong(warmupRng)] = "Loc" + i;
        }

        for (int trial = 0; trial < 10; trial++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var rng = new Random(42);
            var dict = new Dictionary<ulong, string>(InsertCount);

            var fillWatch = Stopwatch.StartNew();
            for (int i = 0; i < InsertCount; i++)
            {
                dict[NextULong(rng)] = "Loc" + i;
            }
            fillWatch.Stop();
            result.FillTimes.Add(fillWatch.ElapsedMilliseconds);

            rng = new Random(42);
            int found = 0;
            var lookupWatch = Stopwatch.StartNew();
            for (int i = 0; i < InsertCount; i++)
            {
                if (dict.ContainsKey(NextULong(rng)))
                    found++;
            }
            lookupWatch.Stop();
            result.LookupTimes.Add(lookupWatch.ElapsedMilliseconds);
            result.FoundCounts.Add(found);
        }

        return result;
    }

    public static BenchmarkResult RunLinearTable()
    {
        var result = new BenchmarkResult { Name = "LinearTable" };

        var warmup = new LinearTable<ulong, string>(Capacity);
        var warmupRng = new Random(42);
        for (int i = 0; i < 1000; i++)
        {
            warmup[NextULong(warmupRng)] = "Loc" + i;
        }

        for (int trial = 0; trial < 10; trial++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var rng = new Random(42);
            var dict = new LinearTable<ulong, string>(Capacity);

            var fillWatch = Stopwatch.StartNew();
            for (int i = 0; i < InsertCount; i++)
            {
                dict[NextULong(rng)] = "Loc" + i;
            }
            fillWatch.Stop();
            result.FillTimes.Add(fillWatch.ElapsedMilliseconds);

            rng = new Random(42);
            int found = 0;
            var lookupWatch = Stopwatch.StartNew();
            for (int i = 0; i < InsertCount; i++)
            {
                if (dict.ContainsKey(NextULong(rng)))
                    found++;
            }
            lookupWatch.Stop();
            result.LookupTimes.Add(lookupWatch.ElapsedMilliseconds);
            result.FoundCounts.Add(found);
        }

        return result;
    }

    public static BenchmarkResult RunElasticTable()
    {
        var result = new BenchmarkResult { Name = "LinearTable" };

        var warmup = new ElasticTable<ulong, string>(Capacity);
        var warmupRng = new Random(42);
        for (int i = 0; i < 1000; i++)
        {
            warmup[NextULong(warmupRng)] = "Loc" + i;
        }

        for (int trial = 0; trial < 10; trial++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var rng = new Random(42);
            var dict = new ElasticTable<ulong, string>(Capacity);

            var fillWatch = Stopwatch.StartNew();
            for (int i = 0; i < InsertCount; i++)
            {
                dict[NextULong(rng)] = "Loc" + i;
            }
            fillWatch.Stop();
            result.FillTimes.Add(fillWatch.ElapsedMilliseconds);

            rng = new Random(42);
            int found = 0;
            var lookupWatch = Stopwatch.StartNew();
            for (int i = 0; i < InsertCount; i++)
            {
                if (dict.ContainsKey(NextULong(rng)))
                    found++;
            }
            lookupWatch.Stop();
            result.LookupTimes.Add(lookupWatch.ElapsedMilliseconds);
            result.FoundCounts.Add(found);
        }

        return result;
    }

    private static string Stats(IEnumerable<long> values)
    {
        var list = values.ToList();
        var avg = list.Average();
        var min = list.Min();
        var max = list.Max();
        var total = list.Sum();
        return $"avg={avg:F1}ms, min={min}ms, max={max}ms, total={total}ms";
    }

    public static void PrintResults(params BenchmarkResult[] results)
    {
        Console.WriteLine($"\n{"Name",-18} | {"Fill Avg (ms)",14} | {"Lookup Avg (ms)",16} | {"Total Found"}");
        Console.WriteLine(new string('-', 65));
        foreach (var res in results)
        {
            Console.WriteLine($"{res.Name,-18} | {res.FillTimes.Average(),14:F2} | {res.LookupTimes.Average(),16:F2} | {res.FoundCounts.Average(),11:F0}");
        }

        Console.WriteLine("\nDetailed Stats:");
        foreach (var res in results)
        {
            Console.WriteLine($"\n{res.Name}");
            Console.WriteLine($"  Fill:   {Stats(res.FillTimes)}");
            Console.WriteLine($"  Lookup: {Stats(res.LookupTimes)}");
        }
    }
}