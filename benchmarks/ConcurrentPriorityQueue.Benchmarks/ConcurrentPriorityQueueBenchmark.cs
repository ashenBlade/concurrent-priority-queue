using BenchmarkDotNet.Attributes;
using ConcurrentPriorityQueue.PriorityQueue;

namespace ConcurrentPriorityQueue.Benchmarks;

[MemoryDiagnoser]
[MeanColumn]
[MinColumn]
[MaxColumn]
[StdDevColumn]
public class ConcurrentPriorityQueueBenchmark
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

    public IEnumerable<(int, int)> QueueParametersEnumerable => CreateQueueParameters();

    private IEnumerable<(int, int)> CreateQueueParameters()
    {
        yield return ( 32, 16 );
        yield return ( 64, 32 );
        yield return ( 128, 32 );
    }

    [ParamsSource(nameof(QueueParametersEnumerable))]
    public (int DeleteThreshold, int Height) QueueParameters { get; set; }
    
    private (int, int)[] _keyValues;

    public int KeysCount => 100_000;

    [ParamsAllValues]
    public WorkloadType WorkloadType { get; set; }

    [Params(1, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20)]
    public int ThreadsCount { get; set; }
    
    [IterationSetup(Target = nameof(ConcurrentQueueBenchmark))]
    public void ConcurrentIterationSetup()
    {
        var (threshold, height) = QueueParameters;
        var concurrent = new ConcurrentPriorityQueue<int, int>(height: height, deleteThreshold: threshold);
        _threads = new Thread[ThreadsCount];
        var chunkSize = CalculateChunkSize();
        foreach (var (keyValues, i) in _keyValues
                                      .Chunk(chunkSize)
                                      .Select((x, y) => (x, y)))
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

            _threads[i] = new Thread(WorkloadType == WorkloadType.Uniform 
                                         ? UniformConcurrentWorker 
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