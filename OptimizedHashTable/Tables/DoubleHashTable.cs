using K4os.Hash.xxHash;
using System.Text;
using System.Runtime.CompilerServices;
using OptimizedHashTable.Interfaces;

namespace OptimizedHashTable.Tables;

public class DoubleHashTable<TKey, TValue> : IHashTable<TKey, TValue>
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

    public DoubleHashTable(int size)
    {
        if (size <= 0 || (size & (size - 1)) != 0)
            throw new ArgumentException("Size must be a positive power of 2");

        entries = new Entry[size];
        mask = size - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ulong Hash(TKey key)
    {
        if (key is string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            return XXH64.DigestOf(bytes);
        }
        else if (typeof(TKey).IsValueType)
        {
            void* ptr = Unsafe.AsPointer(ref key);
            return XXH64.DigestOf((byte*)ptr, Unsafe.SizeOf<TKey>());
        }

        return (ulong)key!.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe static ulong ProbeStep(TKey key)
    {
        ulong b = (ulong)key.GetHashCode();
        b ^= b >> 33;
        b *= 0x9E3779B97F4A7C15UL;
        b ^= b >> 33;
        b *= 0xc4ceb9fe1a85ec53UL;
        b ^= b >> 33;
        return b | 1; 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Insert(TKey key, TValue value)
    {
        ulong hash = Hash(key);
        ulong step = ProbeStep(key);
        int index = (int)(hash & (ulong)mask);
        ref Entry firstEntry = ref entries[index];

        if (!firstEntry.occupied)
        {
            firstEntry.key = key;
            firstEntry.value = value;
            firstEntry.occupied = true;
            firstEntry.probeCount = 1;
            return;
        }
        else if (comparer.Equals(firstEntry.key, key))
        {
            firstEntry.value = value;
            return;
        }
        else
        {
            for (int i = 1; i < entries.Length; i++)
            {
                int probeIndex = (int)((hash + (ulong)i * step) & (ulong)mask);
                ref Entry entry = ref entries[probeIndex];

                if (!entry.occupied)
                {
                    entry.key = key;
                    entry.value = value;
                    entry.occupied = true;
                    entry.probeCount = i;
                    return;
                }
                else if (comparer.Equals(entry.key, key))
                {
                    // Key already exists, update value
                    entry.value = value;
                    return;
                }
            }
        }

        throw new InvalidOperationException("Hash table is full");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe TValue GetValue(TKey key)
    {
        ulong hash = Hash(key);
        ulong step = ProbeStep(key);
        int index = (int)(hash & (ulong)mask);
        ref Entry firstEntry = ref entries[index];

        if (firstEntry.occupied && comparer.Equals(firstEntry.key, key))
            return firstEntry.value;

        else
        {
            for (int i = 1; i < entries.Length; i++)
            {
                int probeIndex = (int)((hash + (ulong)i * step) & (ulong)mask);
                ref Entry entry = ref entries[probeIndex];

                if (!entry.occupied)
                    break;

                if (comparer.Equals(entry.key, key))
                    return entry.value;
            }
        }

        throw new KeyNotFoundException($"{key} not found in table");
    }
}