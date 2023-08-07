namespace ConcurrentPriorityQueue.Benchmarks;

public record BenchmarkResult(QueueBenchmarkResult Locking, QueueBenchmarkResult Concurrent);
public record QueueBenchmarkResult(TimeSpan Duration, long OperationsCount);