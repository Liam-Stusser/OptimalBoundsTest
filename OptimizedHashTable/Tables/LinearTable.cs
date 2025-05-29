using K4os.Hash.xxHash;
using System.Text;
using System.Runtime.CompilerServices;

namespace OptimizedHashTable.Tables;

public class LinearTable<TKey, TValue>
{
    private struct Entry
    {
        public TKey key;
        public TValue value;
        public bool Occupied;
        public int probeAttempts;
        public ulong hash;
    }

    private Entry[] entries;
    private readonly EqualityComparer<TKey> comparer = EqualityComparer<TKey>.Default;
    private int mask;
    private int count;
    private int trigger;

    public LinearTable(int size)
    {
        if (size <= 0)
            throw new ArgumentException("Size must be greater than 0");
        size = RoundPowerTwo(size);
        entries = new Entry[size];
        mask = size - 1;
        trigger = size / 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ulong HashKey(TKey key)
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
    public static ulong Injection(ulong h)
    {
        h ^= h >> 33;
        h *= 0xff51afd7ed558ccd;
        h ^= h >> 33;
        h *= 0xc4ceb9fe1a85ec53;
        h ^= h >> 33;
        return h;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RoundPowerTwo(int value)
    {
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value++;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Probe(Entry[] table, TKey key)
    {
        ulong hash = Injection(HashKey(key));
        int index = (int)(hash & (uint)mask);
        int startIndex = index;

        do
        {
            ref var entry = ref table[index];
            if (!entry.Occupied || comparer.Equals(entry.key, key))
                return index;

            index = (index + 1) & mask;
        } while (index != startIndex);

        return -1; 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd(TKey key, TValue value)
    {
        if (count == trigger)
            Resize();

        int slot = Probe(entries, key);
        if (slot == -1) return false;

        ref var entry = ref entries[slot];

        if (entry.Occupied && comparer.Equals(entry.key, key))
        {
            entry.value = value; 
            return true;
        }

        entry.key = key;
        entry.value = value;
        entry.hash = HashKey(key);
        entry.Occupied = true;
        entry.probeAttempts = 0;

        count++;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        int index = Probe(entries, key);

        if (index != -1 && entries[index].Occupied && comparer.Equals(entries[index].key, key))
        {
            value = entries[index].value;
            return true;
        }

        value = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key) => TryGetValue(key, out _);

    public TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (TryGetValue(key, out var value))
                return value;

            throw new KeyNotFoundException($"Key not found {key}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => TryAdd(key, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Resize()
    {
        var oldEntries = entries;
        int newSize = RoundPowerTwo(entries.Length * 2);
        entries = new Entry[newSize];
        mask = newSize - 1;
        trigger = newSize / 2;
        count = 0;

        foreach (ref var old in oldEntries.AsSpan())
        {
            if (!old.Occupied) continue;

            int slot = Probe(entries, old.key);
            ref var dest = ref entries[slot];
            dest.key = old.key;
            dest.value = old.value;
            dest.hash = old.hash;
            dest.Occupied = true;
            dest.probeAttempts = 0;
            count++;
        }
    }
}