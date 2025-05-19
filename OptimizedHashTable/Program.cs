using System.Diagnostics;

public class Program
{
    private const int TableCapacity = 1 << 22;        
    private const int InsertCount = (int)(0.20 * TableCapacity); 
    
    private static ulong NextULong(Random rng)
    {
        var buffer = new byte[8];
        rng.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }

    // Benchmark custom uniform probing table
    public static void BenchmarkUniformTable()
    {
        var table = new UniformTable<ulong, string>(TableCapacity);
        var rng = new Random(42);

        var fillWatch = new Stopwatch();
        fillWatch.Start();
        for (int i = 0; i < InsertCount; i++)
        {
            ulong ip = NextULong(rng);
            table.Insert(ip, "Loc" + i);
        }
        fillWatch.Stop();

        rng = new Random(42); // Same seed to regenerate same keys
        var lookupWatch = new Stopwatch();
        int found = 0;
        lookupWatch.Start();
        for (int i = 0; i < InsertCount; i++)
        {
            ulong ip = NextULong(rng);
            if (table.GetValue(ip) != null)
                found++;
        }
        lookupWatch.Stop();

        Console.WriteLine($"UniformTable Fill: {fillWatch.ElapsedMilliseconds} ms | Lookup: {lookupWatch.ElapsedMilliseconds} ms | Found: {found}");
    }

    // Benchmark standard .NET dictionary
    public static void BenchmarkDictionary()
    {
        var dict = new Dictionary<ulong, string>(InsertCount); 
        var rng = new Random(42);

        var fillWatch = new Stopwatch();
        fillWatch.Start();
        for (int i = 0; i < InsertCount; i++)
        {
            ulong ip = NextULong(rng);
            dict[ip] = "Loc" + i; 
        }
        fillWatch.Stop();

        rng = new Random(42); 
        var lookupWatch = new Stopwatch();
        int found = 0;
        lookupWatch.Start();
        for (int i = 0; i < InsertCount; i++)
        {
            ulong ip = NextULong(rng);
            if (dict.ContainsKey(ip))
                found++;
        }
        lookupWatch.Stop();

        Console.WriteLine($"Dictionary Fill: {fillWatch.ElapsedMilliseconds} ms | Lookup: {lookupWatch.ElapsedMilliseconds} ms | Found: {found}");
    }

    public static void Main()
    {
        Console.WriteLine("Benchmarking UniformTable:");
        BenchmarkUniformTable();

        Console.WriteLine("Benchmarking Dictionary:");
        BenchmarkDictionary();
    }
}