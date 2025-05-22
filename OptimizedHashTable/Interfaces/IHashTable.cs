namespace OptimizedHashTable.Tables

{
    public interface IHashTable<TKey, TValue>
    {
        void Insert(TKey key, TValue value);
        TValue GetValue(TKey key);
    }
}