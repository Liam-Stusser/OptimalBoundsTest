using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public class AdaptiveHashTable
{
    // Represents an entry in the hash table with a key, value, and occupancy flag
    private struct Entry
    {
        public ulong key;
        public string value;
        public bool Occupied;
    }

    private Entry[] table;
    private int capacity;

    //Constructor takes a number and fills a table with random values based on that size
    public AdaptiveHashTable(int capacity)
    {
        this.capacity = capacity;
        table = new Entry[capacity];
    }

    // SplitMix64 PRNG for high-entropy hashing of keys (used for probing)  
    private ulong SplitMix64(ulong x)
    {
        x += 0x9e3779b97f4a7c15;
        x = (x ^ (x >> 30)) * 0xbf58476d1ce4e5b9;
        x = (x ^ (x >> 27)) * 0x94d049bb133111eb;
        return x ^ (x >> 31);
    }

    // Computes the probe index using SplitMix64 to minimize clustering and preserve entropy, this should theoretically allow our O(log²(1/δ)) performance 
    private int Probe(ulong key, int attempt)
    {
        return (int)(SplitMix64(key + (ulong)(attempt % capacity)) % (ulong)capacity); //mod result by table capacity to keep in bounds
    }

    public void Insert(ulong key, string value)
    {
        for (int attempt = 0; attempt < capacity; attempt++)
        {
            int index = Probe(key, attempt);
            if (table[index].Occupied && table[index].key == key)
            {
                table[index].value = value;
                return; //Key already exsists; update value
            }
            if (!table[index].Occupied)
            {
                table[index].key = key;
                table[index].value = value;
                table[index].Occupied = true;
                return;
            }
        }
        Console.Error.WriteLine("HashTable Full");
    }

    public string? Lookup(ulong key)
    {
        for (int attempt = 0; attempt < capacity; attempt++)
        {
            int index = Probe(key, attempt);
            if (!table[index].Occupied)
            {
                return null;
            }
            if (table[index].key == key)
            {
                return table[index].value;
            }
        }
        return null;
    }
}

public class Program
{
    private const int TestSize = 999999;
    private const int FillSize = 1000000;

    // Benchmarks the AdaptiveHashTable by inserting and then re-querying 100,000 pseudo-random IPs
    public static void BenchmarkAdaptiveHashTable()
    {
        var geoCache = new AdaptiveHashTable(FillSize);
        var rng = new Random(42); //Seed randomization to provide constant randomly generated values

        var fillWatch = new Stopwatch();
        fillWatch.Start();
        for (int i = 0; i < TestSize; i++)
        {
            ulong ip = (ulong)rng.NextInt64();
            geoCache.Insert(ip, "Loc" + i);
        }
        fillWatch.Stop();

        rng = new Random(42);
        var lookupWatch = new Stopwatch();
        int found = 0;
        lookupWatch.Start();
        for (int i = 0; i < TestSize; i++)
        {
            ulong ip = (ulong)rng.NextInt64();
            if (geoCache.Lookup(ip) != null)
                found++;
        }
        lookupWatch.Stop();

        Console.WriteLine($"AdaptiveHashtable Fill: {fillWatch.ElapsedMilliseconds} ms | Lookup: {lookupWatch.ElapsedMilliseconds} ms | Found: {found}");

    }

    //Create method to run a standard hash map search to compare against adaptive hash map
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
}