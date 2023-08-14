using BenchmarkDotNet.Running;
using ConcurrentPriorityQueue.Benchmarks;

BenchmarkRunner.Run(new[]
{
    typeof(ConcurrentPriorityQueueBenchmark),
    typeof(LockingPriorityQueueBenchmark),
});
