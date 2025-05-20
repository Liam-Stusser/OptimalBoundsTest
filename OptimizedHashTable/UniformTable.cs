using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class UniformTable<TKey, TValue>
{
    private struct Entry
    {
        public TKey key;
        public TValue value;
        public bool occupied;
        public int probeCount;
    }

    private Entry[] entries;
    private int mask;
    private readonly EqualityComparer<TKey> comparer = EqualityComparer<TKey>.Default;

    public UniformTable(int size)
    {
        if (size <= 0 || (size & (size - 1)) != 0)
            throw new ArgumentException("Size must be a positive power of 2");

        entries = new Entry[size];
        mask = size - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Hash(TKey key)
    {
        ulong h = (ulong)key.GetHashCode();
        h ^= h >> 33;
        h *= 0xff51afd7ed558ccdUL;
        h ^= h >> 33;
        h *= 0xc4ceb9fe1a85ec53UL;
        h ^= h >> 33;
        return h;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ProbeStep(ulong baseHash, int size)
    {
        ulong mixed = baseHash * 0x9E3779B97F4A7C15UL;
        mixed ^= mixed >> 33;
        mixed *= 0xc4ceb9fe1a85ec53UL;
        mixed ^= mixed >> 33;
        return ((int)(mixed & (ulong)(size - 1))) | 1; // ensure odd
    }

    public void Insert(TKey key, TValue value)
    {
        ulong hash = Hash(key);
        int baseIndex = (int)(hash & (ulong)(entries.Length - 1));

        // Fast path: check base slot
        if (!entries[baseIndex].occupied)
        {
            entries[baseIndex].key = key;
            entries[baseIndex].value = value;
            entries[baseIndex].occupied = true;
            entries[baseIndex].probeCount = 0;
            return;
        }

        int step = ProbeStep(hash, entries.Length);

        // Uniform probing sequence
        for (int i = 1; i < entries.Length; i++)
        {
            int probeIndex = (baseIndex + i * step) & (entries.Length - 1);

            if (!entries[probeIndex].occupied)
            {
                entries[probeIndex].key = key;
                entries[probeIndex].value = value;
                entries[probeIndex].occupied = true;
                entries[probeIndex].probeCount = i;
                return;
            }
        }

        throw new InvalidOperationException("Hash table is full");
    }

    public TValue GetValue(TKey key)
    {
        ulong hash = Hash(key);
        int index = (int)(hash & (ulong)mask);
        int step = ProbeStep(hash, entries.Length);

        for (int i = 0; i < entries.Length; i++)
        {
            ref Entry entry = ref entries[index];
            if (!entry.occupied) break;

            if (comparer.Equals(entry.key, key))
                return entry.value;

            index = (index + step) & mask;
        }

        throw new KeyNotFoundException($"{key} not found in table");
    }
}