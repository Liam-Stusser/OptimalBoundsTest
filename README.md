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

## Update: Linear and Uniform Probing Tables Added (May 19, 2025)

The `AdaptiveHashTable` has been temporarily split into two separate implementations:

- **LinearTable** — a minimal linear probing hash table that highlights the most basic open addressing mechanics.
- **UniformTable** — a more advanced hash table utilizing a uniform probing sequence as groundwork for Elastic and Funnel Hashing.

These designs were built to serve as foundational structures for experimenting with **optimal open addressing** strategies, particularly those described in *"Optimal Bounds for Open Addressing Without Reordering."*

### Benchmark Results (2^20 entries)

| Run | Dictionary Fill | Dictionary Lookup | LinearTable Fill | LinearTable Lookup | UniformTable Fill | UniformTable Lookup |
|-----|-----------------|-------------------|------------------|--------------------|-------------------|----------------------|
| 1   | 932 ms          | 468 ms            | 2525 ms          | 462 ms             | 2584 ms           | 499 ms               |
| 2   | 915 ms          | 408 ms            | 2483 ms          | 522 ms             | 2521 ms           | 455 ms               |
| 3   | 893 ms          | 395 ms            | 2309 ms          | 427 ms             | 2520 ms           | 426 ms               |
| 4   | 853 ms          | 435 ms            | 2390 ms          | 418 ms             | 2619 ms           | 557 ms               |
| 5   | 969 ms          | 402 ms            | 2448 ms          | 467 ms             | 2592 ms           | 485 ms               |
| 6   | 850 ms          | 419 ms            | 2437 ms          | 469 ms             | 2634 ms           | 488 ms               |
| 7   | 923 ms          | 408 ms            | 2453 ms          | 451 ms             | 2551 ms           | 466 ms               |

All runs successfully found all inserted entries (`70% of table size`), confirming correctness.

### Observations

- Both tables maintain the expected **O(log δ⁻¹)** amortized probe complexity for a high load factor.
- The **UniformTable**, while theoretically slightly faster than linear probing, currently lags slightly. This may stem from:
  - Subtle overhead from the probing logic or hashing function.
  - Characteristics of the C# runtime and its memory handling.
  - Need for refinement or tuning in the bitstring construction and probe sequence generator.

While the benchmark compares against .NET’s `Dictionary`, it’s important to note that `Dictionary` benefits from native C++ optimizations and decades of tuning. However, it serves as a solid baseline and performance reference.

### Next Steps

- Implement **Funnel Hashing** in C# and benchmark it against the existing implementations.
- Investigate native compilation via **C++ integration**, eventually wrapping the custom tables in a C#-friendly API with performance closer to or exceeding `Dictionary<TKey, TValue>`.

