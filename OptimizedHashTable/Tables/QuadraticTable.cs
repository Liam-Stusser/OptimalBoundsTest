using K4os.Hash.xxHash;
using System.Runtime.CompilerServices;
using System.Text;

namespace OptimizedHashTable.Tables;
public class QuadraticTable<TKey, TValue> : IHashTable<TKey, TValue>
{
    private struct Entry
    {
        public TKey key;
        public TValue value;
        public ulong hash;
        public int probeAttempts;
    }

    private Entry[] entries;
    private int mask;
    private int count;
    private readonly EqualityComparer<TKey> comparer = EqualityComparer<TKey>.Default;

    public QuadraticTable(int size)
    {
        if (size <= 0 || (size & (size - 1)) != 0)
            throw new ArgumentException("Table size must be a power of 2");

        entries = new Entry[size];
        mask = size - 1;
        count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ulong Hashkey(TKey key)
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


    private bool IsEmpty(in Entry entry) =>
        typeof(TKey).IsValueType
            ? EqualityComparer<TKey>.Default.Equals(entry.key, default!)
            : entry.key is null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(TKey key, TValue value)
    {
        if (count >= entries.Length / 2) // resize when 50% full
            Resize(entries.Length * 2);

        ulong hash = Hashkey(key);
        int index = (int)(hash & (ulong)mask);

        for (int i = 0; i <= mask; i++)
        {
            int probeIndex = (index + i * i) & mask;
            ref Entry entry = ref entries[probeIndex];

            if (IsEmpty(entry))
            {
                entry.hash = hash;
                entry.key = key;
                entry.value = value;
                entry.probeAttempts = i;
                count++;
                return;
            }
        }

        throw new InvalidOperationException("Hash table is full");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetValue(TKey key)
    {
        ulong hash = Hashkey(key);
        int index = (int)(hash & (ulong)mask);

        for (int i = 0; i <= mask; i++)
        {
            int probeIndex = (index + i * i) & mask;
            ref Entry entry = ref entries[probeIndex];

            if (IsEmpty(entry))
                break;

            if (entry.hash == hash && comparer.Equals(entry.key, key))
                return entry.value;
        }

        throw new KeyNotFoundException("Key not found");
    }

    private void Resize(int newSize)
    {
        var oldEntries = entries;
        entries = new Entry[newSize];
        mask = newSize - 1;
        count = 0;

        foreach (ref var entry in oldEntries.AsSpan())
        {
            if (!IsEmpty(entry))
                Reinsert(entry.key, entry.value, entry.hash);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Reinsert(TKey key, TValue value, ulong hash)
    {
        int index = (int)(hash & (ulong)mask);

        for (int i = 0; i <= mask; i++)
        {
            int probeIndex = (index + i * i) & mask;
            ref Entry entry = ref entries[probeIndex];

            if (IsEmpty(entry))
            {
                entry.key = key;
                entry.value = value;
                entry.hash = hash;
                entry.probeAttempts = i;
                count++;
                return;
            }
        }

        throw new InvalidOperationException("Resize failed: table too full");
    }
}
