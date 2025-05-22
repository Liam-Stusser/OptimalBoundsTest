public class ElasticTable<TKey, TValue>
{
    private struct Entry
    {
        public TKey key;
        public TValue value;
        public int probeCount;
        public ulong hash;
    }

    private Entry[,] entries;
}