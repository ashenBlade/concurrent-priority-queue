using BenchmarkDotNet.Attributes;
using ConcurrentPriorityQueue.PriorityQueue;

namespace ConcurrentPriorityQueue.Benchmarks;

[MemoryDiagnoser]
[MeanColumn]
[MinColumn]
[MaxColumn]
[StdDevColumn]
public class LockingPriorityQueueBenchmark
{
    private Thread[] _threads;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var random = new Random(42);
        var result = new (int, int)[KeysCount];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = ( random.Next(), random.Next() );
        }

        _keyValues = result;
    }
    
    private (int, int)[] _keyValues;

    public int KeysCount => 100_000;

    [ParamsAllValues]
    public WorkloadType WorkloadType { get; set; }

    [Params(1, 2, 4, 6, 8, 10)]
    public int ThreadsCount { get; set; }

    [IterationSetup]
    public void LockingIterationSetup()
    {
        var locking = new LockingPriorityQueue<int, int>();
        _threads = new Thread[ThreadsCount];
        var chunkSize = CalculateChunkSize();
        foreach (var (keyValues, i) in _keyValues
                                      .Chunk(chunkSize)
                                      .Select((x, y) => ( x, y )))
        {
            void UniformLockingWorker()
            {
                foreach (var (key, value) in keyValues)
                {
                    locking.Enqueue(key, value);
                    locking.TryDequeue(out _, out _);
                }
            }

            void EnqueueDequeueWorker()
            {
                foreach (var (key, value) in keyValues)
                {
                    locking.Enqueue(key, value);       
                }

                for (int j = 0; j < chunkSize; j++)
                {
                    locking.TryDequeue(out _, out _);
                }
            }

            _threads[i] = new Thread(WorkloadType == WorkloadType.Uniform 
                                                ? UniformLockingWorker
                                                : EnqueueDequeueWorker);
        }
    }
    
    private int CalculateChunkSize()
    {
        var (d, r) = Math.DivRem(KeysCount, ThreadsCount);
        return r == 0
                   ? d
                   : d + 1;
    }
    

    [Benchmark(Description = "Locking")]
    public void LockingQueueBenchmark()
    {
        foreach (var t in _threads)
        {
            t.Start();
        }

        foreach (var t in _threads)
        {
            t.Join();
        }
    }
}