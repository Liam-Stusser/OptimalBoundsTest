using OptimizedHashTables;

namespace OptimizedHashTable.Tables;
public class TestTables
{
    public static void Main()
    {
        var results = new List<BenchmarkResult>
        {
            Benchmark.RunDictionary(),
            Benchmark.Run("LinearTable", cap => new LinearTable<ulong, string>(cap)),
            Benchmark.Run("DoubleHashTable", cap => new DoubleHashTable<ulong, string>(cap)),
            Benchmark.Run("QuadraticTable", cap => new QuadraticTable<ulong, string>(cap))
        };

        Benchmark.PrintResults(results.ToArray());
    }
}