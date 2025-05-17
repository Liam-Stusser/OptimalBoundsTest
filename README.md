# Optimized Hash Table Experiments

## Project Status (As of May 13, 2025)

This project explores building a custom `AdaptiveHashTable` implementation in C# inspired by recent research on pointer optimization and memory layout efficiency (notably from Martin Colton and Andrew Krapivis). The goal is to optimize hash table performance beyond the standard `Dictionary<TKey, TValue>` implementation in .NET.

### Benchmark Results (Current Implementation vs. Standard Dictionary)

Initial benchmark tests (1,000,000 insertions and lookups) consistently show that the .NET standard `Dictionary` outperforms the current `AdaptiveHashTable` implementation in both fill and lookup speed.

| Run | AdaptiveHashtable Fill | AdaptiveHashtable Lookup | Standard Dictionary Fill | Standard Dictionary Lookup |
|-----|------------------------|---------------------------|---------------------------|-----------------------------|
| 1   | 1384 ms                | 645 ms                    | 426 ms                    | 81 ms                       |
| 2   | 1338 ms                | 856 ms                    | 438 ms                    | 81 ms                       |
| 3   | 991 ms                 | 553 ms                    | 422 ms                    | 161 ms                      |
| 4   | 1013 ms                | 521 ms                    | 392 ms                    | 84 ms                       |
| 5   | 1041 ms                | 536 ms                    | 379 ms                    | 81 ms                       |
| 6   | 1058 ms                | 595 ms                    | 369 ms                    | 79 ms                       |

✅ All lookups returned the correct number of found entries (`999999`), confirming functional correctness.

---

##  Planned Improvements

The current implementation is a baseline. Upcoming improvements will focus on addressing performance gaps with the following strategies:

- **Pointer Optimizations**  
  Implement efficient pointer-based chaining or probing techniques to reduce lookup and insertion time.

- **Memory Layout Optimization**  
  Ensure data structures are memory-aligned and cache-friendly to reduce access time and improve spatial locality.

- **Lazy Deletion**  
  Introduce lazy/soft deletion to avoid costly rehashing during remove operations and reduce GC overhead.

- **Profiler Usage**  
  Use profiling tools (e.g., BenchmarkDotNet, Visual Studio Profiler) to identify performance bottlenecks in real-time.

- **Rehashing Strategy**  
  Review and potentially revise the rehashing mechanism to dynamically resize based on load factor and minimize overhead.

---
