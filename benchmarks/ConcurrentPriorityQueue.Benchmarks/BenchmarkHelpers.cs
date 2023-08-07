using ConcurrentPriorityQueue.Benchmarks.KeyArrangement;
using ConcurrentPriorityQueue.PriorityQueue;

namespace ConcurrentPriorityQueue.Benchmarks;

public static class BenchmarkHelpers
{
    private const int Iterations = 10_000_000;
    public static void Warmup()
    {
        // 1. Разогреваем Locking Priority Queue
        WarmupLockingPriorityQueue();
        // 2. Разогреваем Concurrent Priority Queue
        WarmupConcurrentPriorityQueue();
    }

    private static void WarmupConcurrentPriorityQueue()
    {
        var queue = new ConcurrentPriorityQueue<int, int>();
        for (int i = 0; i < Iterations; i++)
        {
            var (key, value) = GenerateKeyValue();
            queue.Enqueue(key, value);
            queue.TryDequeue(out _, out _);
        }
    }

    private static void WarmupLockingPriorityQueue()
    {
        var queue = new LockingPriorityQueue<int, int>();
        for (int i = 0; i < Iterations; i++)
        {
            var (key, value) = GenerateKeyValue();
            queue.Enqueue(key, value);
            queue.TryDequeue(out _, out _);
        }
    }

    private static (int key, int value) GenerateKeyValue() => (Random.Shared.Next(), Random.Shared.Next());

    public static string GetFancyName(this KeyArrangementType type) => 
        type switch
        {
            KeyArrangementType.Ascending =>
                "Возрастающий",
            KeyArrangementType.Descending => "Убывающий",
            KeyArrangementType.Uniform    => "Равномерный",
            _                             => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}