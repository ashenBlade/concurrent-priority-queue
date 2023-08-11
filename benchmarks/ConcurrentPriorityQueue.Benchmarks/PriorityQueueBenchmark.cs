using BenchmarkDotNet.Attributes;
using ConcurrentPriorityQueue.PriorityQueue;

namespace ConcurrentPriorityQueue.Benchmarks;

[MemoryDiagnoser]
[MeanColumn]
[MinColumn]
[MaxColumn]
[StdDevColumn]
public class PriorityQueueBenchmark
{
    private Thread[] _concurrentThreads;
    private Thread[] _lockingThreads;

    private (int, int)[] GetKeyValues(KeyGenerationType generationType, int count)
    {
        var x = generationType switch
                {
                    KeyGenerationType.Random     => 0,
                    KeyGenerationType.Ascending  => 1,
                    // KeyGenerationType.Descending => 2,
                };
        var y = count switch
                {
                    100_000    => 0,
                    1_000_000  => 1,
                    // 10_000_000 => 2,
                };
        // var index = x * 3 + y;
        var index = x * 2 + y;
        return _keyValues[index];
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var keyValues = new[]
        {
                CreateRandomKeyValues(14236, 100_000), 
                CreateRandomKeyValues(96756, 1_000_000),
                // CreateRandomKeyValues(841246, 10_000_000),
                CreateAscendingKeyValues(100_000),
                CreateAscendingKeyValues(1_000_000),
                // CreateAscendingKeyValues(10_000_000),
                // CreateDescendingKeyValues(100_000),
                // CreateDescendingKeyValues(1_000_000),
                // CreateDescendingKeyValues(10_000_000)
            };

        (int, int)[] CreateDescendingKeyValues(int count)
        {
            var result = new (int, int)[count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = ( count - i, count - i );
            }
            return result;
        }

        _keyValues = keyValues;
        
        ( int, int )[] CreateRandomKeyValues(int seed, int count)
        {
            var random = new Random(seed);
            var result = new (int, int)[count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = ( random.Next(), random.Next() );
            }

            return result;
        }
        (int, int)[] CreateAscendingKeyValues(int count)
        {
            var result = new (int, int)[count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = ( i, i );
            }

            return result;
        }
    }


    private (int, int)[][] _keyValues;

    // [Params(100_000, 1_000_000, 10_000_000)]
    [Params(100_000, 1_000_000)]
    public int KeysCount { get; set; }

    [ParamsAllValues]
    public WorkloadType WorkloadType { get; set; }

    [ParamsAllValues]
    public KeyGenerationType KeyGenerationType { get; set; }
    
    // [Params(1, 2, 4, 6, 8, 10)]
    [Params(1, 2, 4)]
    public int ThreadsCount { get; set; }
    
    [IterationSetup(Target = nameof(ConcurrentQueueBenchmark))]
    public void ConcurrentIterationSetup()
    {
        var concurrent = new ConcurrentPriorityQueue<int, int>();
        _concurrentThreads = new Thread[ThreadsCount];
        var chunkSize = CalculateChunkSize();
        foreach (var (keyValues, i) in GetKeyValues(KeyGenerationType, KeysCount).Chunk(chunkSize).Select((x, y) => (x, y)))
        {
            void UniformConcurrentWorker()
            {
                foreach (var (key, value) in keyValues)
                {
                    concurrent.Enqueue(key, value);
                    concurrent.TryDequeue(out _, out _);
                }
            }

            void EnqueueDequeueWorker()
            {
                foreach (var (key, value) in keyValues)
                {
                    concurrent.Enqueue(key, value);
                }


                for (int j = 0; j < chunkSize; j++)
                {
                    concurrent.TryDequeue(out _, out _);
                }
            }

            _concurrentThreads[i] = new Thread(WorkloadType == WorkloadType.Uniform 
                                                   ? UniformConcurrentWorker 
                                                   : EnqueueDequeueWorker);
        }
    }
    
    [IterationSetup(Target = nameof(LockingQueueBenchmark))]
    public void LockingIterationSetup()
    {
        var locking = new LockingPriorityQueue<int, int>();
        _lockingThreads = new Thread[ThreadsCount];
        var chunkSize = CalculateChunkSize();
        foreach (var (keyValues, i) in GetKeyValues(KeyGenerationType, KeysCount)
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

        _lockingThreads[i] = new Thread(WorkloadType == WorkloadType.Uniform 
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
    
    [Benchmark(Description = "Concurrent")]
    public void ConcurrentQueueBenchmark()
    {
        var threads = _concurrentThreads;
        foreach (var t in threads)
        {
            t.Start();
        }

        foreach (var t in threads)
        {
            t.Join();
        }
    }

    [Benchmark(Description = "Locking")]
    public void LockingQueueBenchmark()
    {
        var threads = _lockingThreads;
        foreach (var t in threads)
        {
            t.Start();
        }

        foreach (var t in threads)
        {
            t.Join();
        }
    }
}