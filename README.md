# Optimized Hash Table Experiments

## Project Status (As of May 13, 2025)

This project explores building a custom `AdaptiveHashTable` implementation in C# inspired by recent research on pointer optimization and memory layout efficiency (notably from Martin Colton and Andrew Krapivins). The goal is to optimize hash table performance beyond the standard `Dictionary<TKey, TValue>` implementation in .NET.

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

### Benchmark Results (s^20 entries at 70% capacity)

| Run | Dictionary Fill | Dictionary Lookup | LinearTable Fill | LinearTable Lookup | UniformTable Fill | UniformTable Lookup |
|-----|-----------------|-------------------|------------------|--------------------|-------------------|----------------------|
| 1   | 367 ms          | 127 ms            | 525 ms           | 213 ms             | 587 ms            | 195 ms               |
| 2   | 264 ms          | 118 ms            | 413 ms           | 184 ms             | 562 ms            | 175 ms               |
| 3   | 252 ms          | 112 ms            | 398 ms           | 183 ms             | 535 ms            | 186 ms               |
| 4   | 274 ms          | 158 ms            | 448 ms           | 209 ms             | 551 ms            | 167 ms               |
| 5   | 292 ms          | 130 ms            | 405 ms           | 195 ms             | 530 ms            | 168 ms               |
| 6   | 268 ms          | 158 ms            | 458 ms           | 205 ms             | 523 ms            | 173 ms               |
| 7   | 307 ms          | 161 ms            | 430 ms           | 198 ms             | 510 ms            | 181 ms               |
| 8   | 267 ms          | 143 ms            | 440 ms           | 191 ms             | 522 ms            | 183 ms               |
| 9   | 398 ms          | 209 ms            | 503 ms           | 182 ms             | 517 ms            | 179 ms               |
| 10  | 339 ms          | 131 ms            | 438 ms           | 203 ms             | 566 ms            | 202 ms               |


### Benchmark Results (2^20 entries at 99% capacity)

| Run | Dictionary Fill | Dictionary Lookup | LinearTable Fill | LinearTable Lookup | UniformTable Fill | UniformTable Lookup |
|-----|-----------------|-------------------|------------------|--------------------|-------------------|----------------------|
| 1   | 593 ms          | 271 ms            | 2401 ms          | 2962 ms            | 942 ms            | 557 ms               |
| 2   | 465 ms          | 179 ms            | 2136 ms          | 2773 ms            | 904 ms            | 462 ms               |
| 3   | 445 ms          | 179 ms            | 2119 ms          | 2685 ms            | 853 ms            | 528 ms               |
| 4   | 425 ms          | 163 ms            | 2106 ms          | 2716 ms            | 863 ms            | 517 ms               |
| 5   | 396 ms          | 173 ms            | 2205 ms          | 2663 ms            | 926 ms            | 517 ms               |
| 6   | 457 ms          | 166 ms            | 2251 ms          | 2787 ms            | 852 ms            | 516 ms               |
| 7   | 385 ms          | 193 ms            | 2128 ms          | 2725 ms            | 867 ms            | 519 ms               |
| 8   | 425 ms          | 188 ms            | 2188 ms          | 2695 ms            | 868 ms            | 587 ms               |
| 9   | 386 ms          | 167 ms            | 2159 ms          | 2693 ms            | 839 ms            | 490 ms               |
| 10  | 409 ms          | 158 ms            | 2486 ms          | 2908 ms            | 960 ms            | 536 ms               |


### Observations (Based on 99% Load Factor Benchmarks)

- Uniform table demonstrates the expected **O(log δ⁻¹)** amortized probe complexity under high load, with no lookup failures across ~1 million entries.
- The **UniformTable** now **outperforms the LinearTable** significantly in **lookup speed**, cutting average probe time nearly in half. This confirms the benefits of its uniform probing strategy in high-density scenarios.
- The **fill times** for UniformTable are still slower than `Dictionary` but **markedly better than LinearTable**, suggesting better memory access patterns and fewer long probe chains.
- Remaining areas for potential optimization:
  - Slight runtime overhead from the bitstring-based probe generation logic.
  - Further tuning of the probe sequence generator could improve fill performance.
  - Exploring deeper memory alignment or cache-awareness in C# may help unlock even better insertion throughput.


While the benchmark compares against .NET’s `Dictionary`, it’s important to note that `Dictionary` benefits from native C++ optimizations and decades of tuning. However, it serves as a solid baseline and performance reference.

### Next Steps

- Implement **Funnel Hashing** in C# and benchmark it against the existing implementations.
- Investigate native compilation via **C++ integration**, eventually wrapping the custom tables in a C#-friendly API with performance closer to or exceeding `Dictionary<TKey, TValue>`.

