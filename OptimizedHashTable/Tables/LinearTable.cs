using K4os.Hash.xxHash;
using System.Text;
using System.Runtime.CompilerServices;

namespace OptimizedHashTable.Tables;
public class LinearTable<TKey, TValue> : IHashTable<TKey, TValue>
{
    private struct Entry
    {
        public TKey key;
        public TValue value;
        public bool Occupied;
        public int probeAttempts;
    }

    private Entry[] entries;

    public LinearTable(int size)
    {
        if (size <= 0 || (size & (size - 1)) != 0)
            throw new ArgumentException("Size must be a positive power of 2");

        entries = new Entry[size];
    }

    //hash function
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
    public unsafe void Insert(TKey key, TValue value)
    {
        ulong hash = HashKey(key);
        int index = (int)(hash & ((ulong)entries.Length - 1));
        int count = 0;

        //Find empty slot for key/probe
        for (int i = 0; i < entries.Length; i++)
        {
            int probeIndex = (index + i) & (entries.Length - 1);

            if (!entries[probeIndex].Occupied)//Check collision 
            {
                entries[probeIndex].key = key;
                entries[probeIndex].value = value;
                entries[probeIndex].Occupied = true;
                entries[probeIndex].probeAttempts = count;
                return;
            }

            count++;
        }
        throw new InvalidOperationException("Hash Table is Full!");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe TValue GetValue(TKey key)
    {
        ulong hash = HashKey(key);
        int index = (int)(hash & ((ulong)entries.Length - 1));

        for (int i = 0; i < entries.Length; i++)
        {
            int probeIndex = (index + i) & (entries.Length - 1);

            if (!entries[probeIndex].Occupied)
                throw new KeyNotFoundException("key index pointer is empty");

            if (EqualityComparer<TKey>.Default.Equals(entries[probeIndex].key, key))
                return entries[probeIndex].value;
        }

        throw new KeyNotFoundException($"{key} not found in table");
    }

    //resize if full

    //delete value
}
