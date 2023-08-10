using System.Collections.Concurrent;
using System.Diagnostics;
using ConcurrentPriorityQueue.Benchmarks.KeyArrangement;
using ConcurrentPriorityQueue.PriorityQueue;

namespace ConcurrentPriorityQueue.Benchmarks.Benchmarks;

public class UniformWorkloadBenchmark: IBenchmark
{
    private const int DefaultValue = 123;
    private readonly IKeyArrangementStrategy _keyStrategy;
    private readonly TimeSpan _benchmarkDuration;

    public UniformWorkloadBenchmark(int threadsCount, IKeyArrangementStrategy keyStrategy, TimeSpan benchmarkDuration)
    {
        _keyStrategy = keyStrategy;
        _benchmarkDuration = benchmarkDuration;
        ThreadsCount = threadsCount;
    }

    public KeyArrangementType KeyArrangement => _keyStrategy.ArrangementType;
    public WorkloadType Workload => WorkloadType.Uniform;
    
    public int ThreadsCount { get; }
    
    public BenchmarkResult Run()
    {
        var lockingResult = BenchmarkLocking();
        var concurrentResult = BenchmarkConcurrent();
        return new BenchmarkResult(lockingResult, concurrentResult);
    }

    private QueueBenchmarkResult BenchmarkConcurrent()
    {
        Console.WriteLine($"------ Равномерная нагрузка;  Ключи: {_keyStrategy.ArrangementType.GetFancyName()}; Конкурентная очередь --------");
        var queue = new ConcurrentPriorityQueue<int, int>(height: 30, deleteThreshold: 1000);
        var threads = new Thread[ThreadsCount];
        var operationsQueue = new ConcurrentQueue<(TimeSpan Duration, long OperationsCount)>();
        var eventSlim = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource();
        for (var i = 0; i < threads.Length; i++)
        {
            var thread = new Thread(() =>
            {
                eventSlim.Wait(cts.Token);
                long operation = 0;
                var timer = Stopwatch.StartNew();
                for (; !cts.Token.IsCancellationRequested; operation++)
                {
                    if (( operation & 1 ) == 0)
                    {
                        var key = _keyStrategy.GetKey();
                        queue.Enqueue(key, DefaultValue);
                    }
                    else
                    {
                        queue.TryDequeue(out _, out _);
                    }
                }

                var elapsed = timer.Elapsed;
                operationsQueue.Enqueue((elapsed, operation));
            });
            thread.Start();
            threads[i] = thread;
        }
        Console.WriteLine($"Запускаю тесты для конкурентной реализации");
        cts.CancelAfter(_benchmarkDuration);
        eventSlim.Set();
        foreach (var thread in threads)
        {
            thread.Join();
        }

        return new QueueBenchmarkResult(_benchmarkDuration, operationsQueue.Sum(x => x.OperationsCount));
    }

    private QueueBenchmarkResult BenchmarkLocking()
    {
        Console.WriteLine($"------ Равномерная нагрузка;  Ключи: {_keyStrategy.ArrangementType.GetFancyName()}; Блокирующая очередь --------");

        var queue = new LockingPriorityQueue<int, int>();
        var threads = new Thread[ThreadsCount];
        var operationsQueue = new ConcurrentQueue<(TimeSpan Duration, long OperationsCount)>();
        var eventSlim = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource();
        for (var i = 0; i < threads.Length; i++)
        {
            var thread = new Thread(() =>
            {
                eventSlim.Wait(cts.Token);
                long operation = 0;
                var timer = Stopwatch.StartNew();
                for (; !cts.Token.IsCancellationRequested; operation++)
                {
                    if (( operation & 1 ) == 0)
                    {
                        var key = _keyStrategy.GetKey();
                        queue.Enqueue(key, DefaultValue);
                    }
                    else
                    {
                        queue.TryDequeue(out _, out _);
                    }
                }

                var elapsed = timer.Elapsed;
                operationsQueue.Enqueue((elapsed, operation));
            });
            thread.Start();
            threads[i] = thread;
        }
        Console.WriteLine($"Запускаю тесты для блокирующей реализации");
        cts.CancelAfter(_benchmarkDuration);
        eventSlim.Set();
        foreach (var thread in threads)
        {
            thread.Join();
        }

        Console.WriteLine($"Тесты завершены");

        return new QueueBenchmarkResult(_benchmarkDuration, operationsQueue.Sum(x => x.OperationsCount));

    }
}