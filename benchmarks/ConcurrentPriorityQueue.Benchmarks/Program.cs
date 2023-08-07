

using ConcurrentPriorityQueue.Benchmarks;
using ConcurrentPriorityQueue.Benchmarks.Benchmarks;
using ConcurrentPriorityQueue.Benchmarks.KeyArrangement;

Console.WriteLine("Начинается разогрев");
BenchmarkHelpers.Warmup();
Console.WriteLine("Разогрев закончился. Начинается тестрование");

var benchmark = new UniformWorkloadBenchmark(12, new UniformStrategy(), TimeSpan.FromSeconds(10));
var result = benchmark.Run();
Console.WriteLine($"Результат бенчмарка:\n");
Console.WriteLine($" - Блокирующая очередь:");
Console.WriteLine($"    - Время выполнения: {result.Locking.Duration}");
Console.WriteLine($"    - Количество операций: {result.Locking.OperationsCount}");


Console.WriteLine($" - Конкурентная очередь:");
Console.WriteLine($"    - Время выполнения: {result.Concurrent.Duration}");
Console.WriteLine($"    - Количество операций: {result.Concurrent.OperationsCount}");




