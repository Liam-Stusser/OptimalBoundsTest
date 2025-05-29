using K4os.Hash.xxHash;
using System.Runtime.CompilerServices;
using System.Text;
using System.Numerics; 

namespace OptimizedHashTable.Tables

{
    public class ElasticTable<TKey, TValue>
    {
        private struct Entry
        {
            public TKey Key;
            public TValue Value;
            public bool Occupied;
            public int ProbeAttempts;
            public ulong Hash;
        }

        private readonly Entry[,] entries;
        private readonly int[] masks;
        private readonly int[] sizes;
        private readonly int arraysCount;
        private readonly EqualityComparer<TKey> comparer = EqualityComparer<TKey>.Default;
        private int count;

        public ElasticTable(int totalSize)
        {
            if (totalSize < 4)
                throw new ArgumentException("Size must be at least 4");

            totalSize = RoundPowerTwo(totalSize);

            var sizesList = new List<int>();
            int remaining = totalSize;
            int size = totalSize / 2;

            while (remaining > 0 && size >= 4)
            {
                if (size > remaining) size = remaining;

                sizesList.Add(size);
                remaining -= size;

                size /= 2;
            }

            if (remaining > 0)
                sizesList.Add(remaining);

            sizes = sizesList.ToArray();
            arraysCount = sizes.Length;

            masks = new int[arraysCount];
            for (int i = 0; i < arraysCount; i++)
                masks[i] = sizes[i] - 1;

            int maxSize = 0;
            foreach (var s in sizes)
                if (s > maxSize) maxSize = s;

            entries = new Entry[arraysCount, maxSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RoundPowerTwo(int value)
        {
            if (value < 1) return 1;

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
        public static ulong Injection(int arrayIndex, int probeAttempt)
        {
            ulong result = 0;
            int maxBits = Math.Max(BitOperations.Log2((uint)(arrayIndex == 0 ? 1 : arrayIndex)) + 1,
                                  BitOperations.Log2((uint)(probeAttempt == 0 ? 1 : probeAttempt)) + 1);

            for (int bit = 0; bit < maxBits; bit++)
            {
                ulong bitA = ((ulong)(arrayIndex >> bit) & 1UL) << (bit * 2 + 1);
                ulong bitP = ((ulong)(probeAttempt >> bit) & 1UL) << (bit * 2);
                result |= bitA | bitP;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool KeysEqual(TKey a, TKey b) => comparer.Equals(a, b);

        public bool TryAdd(TKey key, TValue value)
        {
            ulong hash = HashKey(key);

            for (int arrayIndex = 0; arrayIndex < arraysCount; arrayIndex++)
            {
                int mask = masks[arrayIndex];
                int size = sizes[arrayIndex];

                for (int probe = 0; probe < size; probe++)
                {
                    ulong injection = Injection(arrayIndex, probe);
                    int slot = (int)((hash ^ injection) & (uint)mask);

                    ref Entry entry = ref entries[arrayIndex, slot];

                    if (!entry.Occupied)
                    {
                        entry.Key = key;
                        entry.Value = value;
                        entry.Hash = hash;
                        entry.Occupied = true;
                        entry.ProbeAttempts = probe;
                        count++;
                        return true;
                    }
                    else if (KeysEqual(entry.Key, key))
                    {
                        entry.Value = value; 
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            ulong hash = HashKey(key);

            for (int arrayIndex = 0; arrayIndex < arraysCount; arrayIndex++)
            {
                int mask = masks[arrayIndex];
                int size = sizes[arrayIndex];

                for (int probe = 0; probe < size; probe++)
                {
                    ulong injection = Injection(arrayIndex, probe);
                    int slot = (int)((hash ^ injection) & (uint)mask);

                    ref Entry entry = ref entries[arrayIndex, slot];

                    if (!entry.Occupied)
                        break; 

                    if (KeysEqual(entry.Key, key))
                    {
                        value = entry.Value;
                        return true;
                    }
                }
            }

            value = default!;
            return false;
        }

        public bool ContainsKey(TKey key) => TryGetValue(key, out _);

        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var val))
                    return val;
                throw new KeyNotFoundException($"Key not found {key}");
            }
            set => TryAdd(key, value);
        }

        public int Count => count;

        public void BatchFill(IEnumerable<(TKey key, TValue value)> items)
        {
            var enumerator = items.GetEnumerator();

            for (int i = 0; i < arraysCount; i++)
            {
                int Ai = sizes[i];
                int Ai_1 = (i + 1 < arraysCount) ? sizes[i + 1] : 0;
                int delta = Ai - Ai_1;

                int fullFill = Ai;
                int fillMinusHalfDelta = Ai - delta / 2;
                int fill75PercentAi = (int)(Ai * 0.75);
                int fill75PercentAi_1 = (int)(Ai_1 * 0.75);

                bool FillN(int subarrayIndex, int n)
                {
                    int mask = masks[subarrayIndex];

                    for (int countFilled = 0; countFilled < n; countFilled++)
                    {
                        if (!enumerator.MoveNext())
                            return false;

                        var (key, value) = enumerator.Current;
                        ulong hash = HashKey(key);

                        bool inserted = false;
                        int size = sizes[subarrayIndex];

                        for (int probe = 0; probe < size; probe++)
                        {
                            ulong inj = Injection(subarrayIndex, probe);
                            int slot = (int)((hash ^ inj) & (uint)mask);

                            ref Entry entry = ref entries[subarrayIndex, slot];

                            if (!entry.Occupied)
                            {
                                entry.Key = key;
                                entry.Value = value;
                                entry.Hash = hash;
                                entry.Occupied = true;
                                entry.ProbeAttempts = probe;
                                count++;
                                inserted = true;
                                break;
                            }
                            else if (KeysEqual(entry.Key, key))
                            {
                                entry.Value = value;
                                inserted = true;
                                break;
                            }
                        }

                        if (!inserted)
                            throw new InvalidOperationException("Failed to insert during batch fill: hash table too full or pathological collisions.");
                    }

                    return true;
                }

                if (!FillN(i, fullFill)) return;
                if (!FillN(i, fullFill - fillMinusHalfDelta)) return;
                if (!FillN(i, fill75PercentAi)) return;
                if (Ai_1 > 0)
                {
                    if (!FillN(i + 1, fill75PercentAi_1)) return;
                }
            }
        }
    }
}
