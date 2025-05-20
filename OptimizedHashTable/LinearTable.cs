using System.Runtime.CompilerServices;

public class LinearTable<TKey, TValue> 
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
        if(size <= 0 || (size & (size - 1)) != 0)
        {
            throw new ArgumentException("Size must be a positive power of 2");
        }
        entries = new Entry[size];
    }

    //hash function
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong HashKey(TKey key)
    {
        ulong h = (ulong)key.GetHashCode();
        h ^= h >> 33;
        h *= 0xff51afd7ed558ccd;
        h ^= h >> 33;
        h *= 0xc4ceb9fe1a85ec53;
        h ^= h >> 33;
        return h;
    }

    public void Insert(TKey key, TValue value)
    {
        ulong hash  = HashKey(key);
        int index = (int)(hash & ((ulong)entries.Length-1));
        int count = 0;
        
        //Find empty slot for key/probe
        for(int i = 0; i < entries.Length; i++)
        {
            int probeIndex = (index + i) & (entries.Length - 1);

            if(!entries[probeIndex].Occupied)//Check collision 
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
    
    public TValue GetValue(TKey key)
    {
        ulong hash  = HashKey(key);
        int index = (int)(hash & ((ulong)entries.Length-1));

        for(int i = 0; i < entries.Length; i++)
        {
            int probeIndex = (index + i) & (entries.Length - 1);

            if(!entries[probeIndex].Occupied)
                throw new KeyNotFoundException("key index pointer is empty");

            if(EqualityComparer<TKey>.Default.Equals(entries[probeIndex].key, key))
                return entries[probeIndex].value;
        }

        throw new KeyNotFoundException($"{key} not found in table");
    }

    //resize if full

    //delete value
}
