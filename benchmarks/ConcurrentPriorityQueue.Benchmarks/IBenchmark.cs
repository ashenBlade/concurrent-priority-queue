using ConcurrentPriorityQueue.Benchmarks.KeyArrangement;

namespace ConcurrentPriorityQueue.Benchmarks;

public interface IBenchmark
{
    public KeyArrangementType KeyArrangement { get; }
    public WorkloadType Workload { get; }
    public int ThreadsCount { get; }
    public BenchmarkResult Run();
}