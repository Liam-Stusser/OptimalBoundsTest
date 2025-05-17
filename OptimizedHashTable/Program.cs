using System;
using System.Collections.Generic;
using System.Diagnostics;

public class AdaptiveHashTable<TKey, TValue>
{
    private const int MaxProbeDepth = 64;

    private static readonly ulong[] ProbeEntropy = new ulong [MaxProbeDepth] //help prevent collisions on each layer
    {
        0x1a2b3c4d5e6f7081, 0xdeadbeefcafebabe, 0x123456789abcdef0, 0x0f0e0d0c0b0a0908,
        0x1122334455667788, 0xa1b2c3d4e5f60718, 0x99aa88bb77cc66dd, 0x5555555555555555,
        0x8888888888888888, 0x0101010101010101, 0xabcdef0123456789, 0x1020304050607080,
        0x9999999999999999, 0x1111111111111111, 0xfeedfacecafebeef, 0x13579bdf2468ace0,
        0x2468ace013579bdf, 0xcafebabefeedface, 0x0badf00ddeadbeef, 0x4242424242424242,
        0x3141592653589793, 0x2718281828459045, 0xaaaaaaaaaaaaaaaa, 0xbbbbbbbbbbbbbbbb,
        0xcccccccccccccccc, 0xdddddddddddddddd, 0xeeeeeeeeeeeeeeee, 0xfffffffffffffffe,
        0x0000000000000001, 0x1f2e3d4c5b6a7988, 0x6c5d4e3f2a1b0c0d, 0x7e6f5d4c3b2a1908,
        0xfedcba9876543210, 0x1029384756abcdef, 0xa5a5a5a5a5a5a5a5, 0x5a5a5a5a5a5a5a5a,
        0x1234123412341234, 0xabcdabcdabcdabcd, 0xfaceb00c12345678, 0xbaddecafcafed00d,
        0x8badf00ddeadbabe, 0x1e2e3e4e5e6e7e8e, 0x3141deadbeef2718, 0x76543210fedcba98,
        0x89abcdef01234567, 0x0123456789abcdef, 0xbeefcafedeadface, 0x0c0c0c0c0c0c0c0c,
        0x3c3c3c3c3c3c3c3c, 0x7f7f7f7f7f7f7f7f, 0x1b2b3b4b5b6b7b8b, 0x6a5a4a3a2a1a0a09,
        0x9f8e7d6c5b4a3928, 0x1020a0b0c0d0e0f0, 0x9876543210fedcba, 0xc001d00dc0ffee00,
        0xdeadc0de12345678, 0xaaaaaaaaffffffff, 0xf0e0d0c0b0a09080, 0x1234567890abcdef,
        0x0abc1234def56789, 0x789abcde01234567, 0xfedc1234567890ab, 0xbeadc0ffee123456
    }; //table will essentially provide one last layer of randomization for each unique array in our funnel map (might create too much overhead, will find out)

    // Represents an entry in the hash table with a key, value, and occupancy flag
    private struct Entry
    {
        public bool Occupied;
        public TKey Key;
        public TValue Value;
    }
    //Add more variables to hold alpha, beta, delta, and totalcapacity for calculations of new array sizes
    private Entry[] entries;//Turn into a list to allow for multiple arrays to be added
    private int count; //For future implementation of keeping track of probe attempts

    public int Capacity => entries.Length;
    public int Count => count; //Make public for user to see possibly, might remove

    //Constructor initializes a new array of set size to be split later
    public AdaptiveHashTable(int capacity)
    {
        if (capacity <= 0 || (capacity & (capacity - 1)) != 0)
            throw new ArgumentException("Capacity must be a positive power of 2.");

        entries = new Entry[capacity];
    }

    // SplitMix64 PRNG for high-entropy hashing of keys (used for probing)  
    private static ulong SplitMix64(ulong z)
    {
        z += 0x9e3779b97f4a7c15;
        z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9;
        z = (z ^ (z >> 27)) * 0x94d049bb133111eb;
        return z ^ (z >> 31);
    }
    
    private static ulong HashKey(TKey key, int probe)//Create the hash for the key
    {
        unchecked
        {
            ulong raw = (ulong)key.GetHashCode();
            return SplitMix64(raw ^ ProbeEntropy[probe]);
        }
    }

    private int GetIndex(ulong hash, int capacity)
    {
       return (int)(hash & ((ulong)capacity - 1));
    }

    public void Insert(TKey key, TValue value)//Lots to be changed here later when funnel is implemented 
    {
        for (int i = 0; i < MaxProbeDepth; i++)
        {
            ulong hash = HashKey(key, i);//multiply MaxProbeDepth here based on layer when ready
            int index = GetIndex(hash, entries.Length);

            if (!entries[index].Occupied)
            {
                entries[index].Key = key;
                entries[index].Value = value;
                entries[index].Occupied = true;
                count++;
                return;
            }

            if (EqualityComparer<TKey>.Default.Equals(entries[index].Key, key))
            {
                entries[index].Value = value; 
                return;
            }
        }
        //Implement AddLayer method use recursion to try again on new array?
        throw new InvalidOperationException("Hash table insertion failed: probe limit exceeded.");
    }

    public TValue GetValue(TKey key)
    {
        for (int i = 0; i < MaxProbeDepth; i++)
        {
            ulong hash = HashKey(key, i);
            int index = GetIndex(hash, entries.Length);

            if (!entries[index].Occupied)
                break;

            if (EqualityComparer<TKey>.Default.Equals(entries[index].Key, key))
            {
                return entries[index].Value;
            }
        }

        throw new KeyNotFoundException($"Key '{key}' not found in AdaptiveHashTable.");
    }

}

public class Program
{
    private const int TestSize = 63;
    private const int FillSize = 64;

    // Benchmarks the AdaptiveHashTable by inserting and then re-querying pseudo-random IPs
    public static void BenchmarkAdaptiveHashTable()
    {
        var table = new AdaptiveHashTable<ulong,string>(FillSize);
        var rng = new Random(42); //Seed randomization to provide constant randomly generated values

        var fillWatch = new Stopwatch();
        fillWatch.Start();
        for (int i = 0; i < TestSize; i++)
        {
            ulong ip = (ulong)rng.NextInt64();
            table.Insert(ip, "Loc" + i);
        }
        fillWatch.Stop();

        rng = new Random(42);
        var lookupWatch = new Stopwatch();
        int found = 0;
        lookupWatch.Start();
        for (int i = 0; i < TestSize; i++)
        {
            ulong ip = (ulong)rng.NextInt64();
            if (table.GetValue(ip) != null)
                found++;
        }
        lookupWatch.Stop();

        Console.WriteLine($"AdaptiveHashtable Fill: {fillWatch.ElapsedMilliseconds} ms | Lookup: {lookupWatch.ElapsedMilliseconds} ms | Found: {found}");

    }

    //Create method to run a standard hashmap search to compare against adaptive hashmap
    public static void BenchmarkDictionary()
    {
        var stdMap = new Dictionary<ulong, string>();
        var rng = new Random(42);

        var fillWatch = new Stopwatch();
        fillWatch.Start();

        for (int i = 0; i < FillSize; i++)
        {
            ulong ip = (ulong)rng.NextInt64();
            stdMap[ip] = "Loc" + i;
        }

        fillWatch.Stop();

        rng = new Random(42);
        var lookupWatch = new Stopwatch();
        int found = 0;
        lookupWatch.Start();

        for (int i = 0; i < TestSize; i++)
        {
            ulong ip = (ulong)rng.NextInt64();
            if (stdMap.ContainsKey(ip))
                found++;
        }

        lookupWatch.Stop();
        Console.WriteLine($"Standard dictionary Fill: {fillWatch.ElapsedMilliseconds} ms | Lookup: {lookupWatch.ElapsedMilliseconds} ms | Found: {found}");
    }

    public static void Main()
    {
        Console.WriteLine("Running BenchmarkAdaptiveHashTable...");
        BenchmarkAdaptiveHashTable();

        Console.WriteLine("Running BenchmarkDictionary...");
        BenchmarkDictionary();
    }
}//Delete everything in Program including Program after AdaptiveHashTable(name subject to change) is completed and testing finished