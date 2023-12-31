using System.Collections.Concurrent;

namespace ConcurrentPriorityQueue.PriorityQueue.Tests;

public class ConcurrentPriorityQueueTests
{
    /// <summary>
    /// Количество повторений для тестов с параллельными операциями.
    /// Тестировать конкурентные операции сложна...
    /// </summary>
    private const int ParallelTestsRepeatCount = 10;
    private static ConcurrentPriorityQueue<int, int> CreateQueue() => new();

    [Fact]
    public void Enqueue__КогдаОчередьПуста__ДолженДобавить1Элемент()
    {
        var queue = CreateQueue();
        
        var key = 1;
        var value = 123;
        
        queue.Enqueue(key, value);

        var stored = queue.DequeueAllInternal();
        Assert.Contains(stored, tuple => tuple == (key, value));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(1, 3)]
    [InlineData(0, 3)]
    [InlineData(0, int.MaxValue)]
    [InlineData(int.MinValue, int.MaxValue)]
    public void Enqueue__КогдаВОчереди1ЭлементИДобавляемыйКлючБольше__ДолженДобавитьКлючПослеЭлемента(int lesserKey, int greaterKey)
    {
        var queue = CreateQueue();
        var value = 123;
        
        queue.Enqueue(lesserKey, value);
        queue.Enqueue(greaterKey, value);

        var data = queue.DequeueAllInternal();
        Assert.Equal(new[]{(lesserKey, value), (greaterKey, value)}, data);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(23, 0)]
    [InlineData(2, 0)]
    [InlineData(2, int.MinValue)]
    [InlineData(int.MaxValue, int.MinValue)]
    public void Enqueue__КогдаОчередьНеПустаИДобавляемыйКлючМеньше__ДолженДобавитьЭлементПередСтарым(
        int greaterKey,
        int lesserKey)
    {
        var queue = CreateQueue();
        var value = 123;
        
        queue.Enqueue(greaterKey, value);
        queue.Enqueue(lesserKey, value);

        var actual = queue.DequeueAllInternal();
        Assert.Equal(new[]{(lesserKey, value), (greaterKey, value)}, actual);
    }
    
    [Fact]
    public void TryDequeue__СПустымСписком__ДолженВернутьFalse()
    {
        var queue = CreateQueue();
        var actual = queue.TryDequeue(out _, out _);
        Assert.False(actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(-1)]
    [InlineData(123)]
    [InlineData(23)]
    [InlineData(323)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void TryDequeue__СоСпискомИз1Элемента__ДолженВернутьХранившийсяЭлемент(int key)
    {
        var queue = CreateQueue();

        var value = 123;
        queue.Enqueue(key, value);
        queue.TryDequeue(out var actualKey, out var actualValue);
        
        Assert.Equal((actualKey, actualValue), (key, value));
    }

    [Theory]
    [InlineData(1, 2)]
    public void TryDequeue__СоСпискомИз2Элементов__ДолженОставить1Элемент(int lesserKey, int greaterKey)
    {
        var queue = CreateQueue();

        var data = 123;
        queue.Enqueue(lesserKey, data);
        queue.Enqueue(greaterKey, data);

        queue.TryDequeue(out _, out _);

        var actual = queue.DequeueAllInternal();
        var expected = new[] {( greaterKey, data )};
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Count__КогдаОчередьПуста__ДолженВернуть0()
    {
        var queue = CreateQueue();
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void Count__КогдаВОчереди1Элемент__ДолженВернуть1()
    {
        var queue = CreateQueue();
        queue.Enqueue(1, 1);
        Assert.Equal(1, queue.Count);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public void Count__КогдаВОчередьНесколькоЭлементов__ДолженВернутьИхКоличество(int count)
    {
        var queue = CreateQueue();
        for (int i = 0; i < count; i++)
        {
            queue.Enqueue(i, i);
        }
        
        Assert.Equal(count, queue.Count);
    }

    [Fact]
    public void Enqueue__КогдаКладется2ЭлементаСОдинаковымКлючом__ДолженСохранитьВПорядкеДобавления()
    {
        var key = 1;
        var dataFirst = 90;
        var dataSecond = 400;
        var queue = CreateQueue();
        
        queue.Enqueue(key, dataFirst);
        queue.Enqueue(key, dataSecond);

        var actualDataFirst = queue.Dequeue(out var actualKeyFirst);
        var actualDataSecond = queue.Dequeue(out var actualKeySecond);
        
        Assert.Equal(new[]{(key, dataFirst), (key, dataSecond)}, new[]{(actualKeyFirst, actualDataFirst), (actualKeySecond, actualDataSecond)});
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(200)]
    [InlineData(500)]
    public void Enqueue__КогдаПараллельноДобавляютсяМножествоЭлементов__ДолженДобавитьВсеЭлементы(int elementsCount)
    {
        var queue = CreateQueue();
        var elements = Enumerable.Range(0, elementsCount)
                                 .Select(_ => ( Priority: Random.Shared.Next(), Data: Random.Shared.Next() ))
                                 .ToArray();
        var tasks = elements
                   .Select(pair => Task.Run(() =>
                    {
                        queue.Enqueue(pair.Priority, pair.Data);
                    }))
                   .ToArray();
        Task.WaitAll(tasks);

        var actual = queue.DequeueAllInternal().ToHashSet();
        var expected = elements.ToHashSet();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(200)]
    [InlineData(500)]
    public async Task TryDequeue__КогдаВыполняютсяПараллельноСтолькоСколькоЭлементовВОчереди__ДолжныВернутьТеЖеЭлементы(
        int elementsCount)
    {
        for (int j = 0; j < ParallelTestsRepeatCount; j++)
        {
            var elements = Enumerable.Range(0, elementsCount)
                                     .Select(_ => ( Key: Random.Shared.Next(), Value: Random.Shared.Next() ))
                                     .ToArray();

            var queue = CreateQueue();
            Array.ForEach(elements, tuple => queue.Enqueue(tuple.Key, tuple.Value));

            var stored = new ConcurrentQueue<(int Key, int Value)>();
            var tasks = new Task[elementsCount];
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var value = queue.Dequeue(out var key);
                    stored.Enqueue(( key, value ));
                });
            }

            await Task.WhenAll(tasks);

            var actual = stored.ToHashSet();
            var expected = elements.ToHashSet();
            Assert.Equal(expected, actual);
        }
    }
    
    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(200)]
    [InlineData(250)]
    [InlineData(500)]
    [InlineData(750)]
    [InlineData(1000)]
    public async Task EnqueueDequeue__КогдаВыполняютсяОдновременно__ДолжныПравильноОбработатьВсеОперации(int elementsCount)
    {
        for (int i = 0; i < ParallelTestsRepeatCount; i++)
        {
            var elements = Enumerable.Range(0, elementsCount)
                                     .Select(_ => ( Key: Random.Shared.Next(), Value: Random.Shared.Next() ))
                                     .ToArray();
            var tcs = new TaskCompletionSource();
            var queue = CreateQueue();
            var dequeued = new ConcurrentQueue<(int Key, int Value)>();
            var enqueueTasks = elements.Select(p => Task.Run(async () =>
            {
                await tcs.Task;
                queue.Enqueue(p.Key, p.Value);
            }));
            var dequeueTasks = Enumerable.Range(0, elementsCount)
                                         .Select(_ => Task.Run(async () =>
                                          {
                                              await tcs.Task;
                                              if (queue.TryDequeue(out var key, out var value))
                                              {
                                                  dequeued.Enqueue((key, value));
                                              }
                                          }));
            var tasks = enqueueTasks
                       .Concat(dequeueTasks)
                       .ToArray();
            var waitTask = Task.WhenAll(tasks);
            tcs.SetResult();
            await waitTask;

            while (queue.TryDequeue(out var key, out var value))
            {
                dequeued.Enqueue((key, value));
            }

            var expected = elements.ToHashSet();
            var actual = dequeued.ToHashSet();
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    // Всегда должен оставаться 1 удаленный узел, формирующий префикс,
    // чтобы избежать гонки
    // [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(50)]
    public void TryDequeue__ПриДостиженииПорогаУдаленныхУзлов__ДолженУдалитьУзлыИзСписка(int deleteThreshold)
    {
        var elements = Enumerable.Range(0, deleteThreshold)
                                 .Select(_ => ( Key: Random.Shared.Next(), Priority: Random.Shared.Next() ))
                                 .ToArray();
        var queue = new ConcurrentPriorityQueue<int, int>(deleteThreshold: deleteThreshold);
        foreach (var (key, priority) in elements)
        {
            queue.Enqueue(key, priority);
        }

        for (int i = 0; i < deleteThreshold; i++)
        {
            queue.Dequeue();
        }

        var storedCount = queue.GetStoredNodesCountRaw();
        Assert.True(storedCount < deleteThreshold);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(500)]
    public async Task Enqueue__КогдаПараллельноДобавляютсяЭлементыСОдинаковымиКлючами__ДолженДобавитьВсеКорректно(
        int elementsCount)
    {
        for (int i = 0; i < ParallelTestsRepeatCount; i++)
        {
            var key = 1;
            var elements = Enumerable.Range(0, elementsCount)
                                     .Select(_ => ( Key: key, Value: Random.Shared.Next() ))
                                     .ToArray();
            var cts = new TaskCompletionSource();
            var queue = CreateQueue();
            var enqueueTasks = elements.Select(tuple => Task.Run(async () =>
                                        {
                                            await cts.Task;
                                            queue.Enqueue(tuple.Key, tuple.Value);
                                        }))
                                       .ToArray();
            var waitTask = Task.WhenAll(enqueueTasks);
            cts.SetResult();
            await waitTask;

            var actual = queue.DequeueAllInternal().ToHashSet();
            var expected = elements.ToHashSet();
        
            Assert.Equal(expected, actual);
        }
    }

    private static void Shuffle<T>(T[] array)
    {
        var i = 0;
        while (i < array.Length)
        {
            var j = Random.Shared.Next(array.Length);
            if (j != i)
            {
                ( array[i], array[j] ) = ( array[j], array[i] );
                i++;
            }
        }
    } 

    [Theory]
    [InlineData(10, 5)]
    [InlineData(10, 10)]
    [InlineData(50, 50)]
    [InlineData(50, 100)]
    [InlineData(50, 12)]
    public async Task Enqueue__КогдаДобавляютсяОдинаковыеИРазныеКлючиБольше__ДолженДобавитьВсеЭлементы(
        int identicalKeysCount,
        int distinctKeysCount)
    {
        for (int i = 0; i < ParallelTestsRepeatCount; i++)
        {
            const int identicalKey = 1;
            var identicalKeys = Enumerable.Range(0, identicalKeysCount)
                                          .Select(_ => ( Key: identicalKey, Value: Random.Shared.Next() ));
            var distinctKeys = Enumerable.Range(1, distinctKeysCount)
                                         .Select(key => ( Key: key, Value: Random.Shared.Next() ));
            var elements = identicalKeys.Concat(distinctKeys).ToArray();
            Shuffle(elements);

            var queue = CreateQueue();
            var tcs = new TaskCompletionSource();
            var waitTask = Task.WhenAll( elements.Select(e => Task.Run(async () =>
            {
                await tcs.Task;
                queue.Enqueue(e.Key, e.Value);
            })).ToArray());
        
            tcs.SetResult();
        
            await waitTask;

            var actual = queue.DequeueAllInternal().ToHashSet();
            var expected = elements.ToHashSet();
            Assert.Equal(expected, actual);
        }
    }
    
    [Theory]
    [InlineData(10, 5)]
    [InlineData(10, 10)]
    [InlineData(50, 50)]
    [InlineData(50, 100)]
    [InlineData(50, 12)]
    public async Task Enqueue__КогдаДобавляютсяОдинаковыеИРазныеКлючиМеньше__ДолженДобавитьВсеЭлементы(
        int identicalKeysCount,
        int distinctKeysCount)
    {
        for (int i = 0; i < ParallelTestsRepeatCount; i++)
        {
            const int identicalKey = 1;
            var identicalKeys = Enumerable.Range(0, identicalKeysCount)
                                          .Select(_ => ( Key: identicalKey, Value: Random.Shared.Next() ));
            var distinctKeys = Enumerable.Range(1 - distinctKeysCount, distinctKeysCount)
                                         .Select(key => ( Key: key, Value: Random.Shared.Next() ));
            var elements = identicalKeys.Concat(distinctKeys).ToArray();
            Shuffle(elements);

            var queue = CreateQueue();
            var tcs = new TaskCompletionSource();
            var waitTask = Task.WhenAll( elements.Select(e => Task.Run(async () =>
            {
                await tcs.Task;
                queue.Enqueue(e.Key, e.Value);
            })).ToArray());
        
            tcs.SetResult();
        
            await waitTask;

            var actual = queue.DequeueAllInternal().ToHashSet();
            var expected = elements.ToHashSet();
            Assert.Equal(expected, actual);
        }
    }

    private record ReferenceTypeKey(int Key): IComparable<ReferenceTypeKey>
    {
        public int CompareTo(ReferenceTypeKey? other) =>
            other is null
                ? 1
                : Key.CompareTo(other.Key);
    }

    [Fact]
    public void TryDequeue__КогдаКлючСылочныйТип__ПослеУдаленияСписокДолженРаботатьНормально()
    {
        var comparer = Comparer<ReferenceTypeKey>.Create((key, typeKey) => key.CompareTo(typeKey));
        var queue = new ConcurrentPriorityQueue<ReferenceTypeKey, int>(comparer: comparer);

        var first = (Key: new ReferenceTypeKey(1), Value: 123 );
        var second = (Key: new ReferenceTypeKey(2), Value: 97 );
        
        queue.Enqueue(first.Key, first.Value);
        queue.Enqueue(second.Key, second.Value);

        queue.Dequeue();

        if (!queue.TryDequeue(out var actualKey, out var actualValue))
        {
            Assert.True(false, "В очереди должен находиться 1 элемент, т.к. добавили 2 и забрали 1");
        }

        var actual = ( actualKey, actualValue );
        var expected = second;
        
        Assert.Equal(expected, actual);
    }
    
    [Fact]
    public void TryDequeue__КогдаКлючСылочныйТип__ПослеУдаленияДолженДобавлятьНовыеЭлементы()
    {
        var comparer = Comparer<ReferenceTypeKey>.Create((key, typeKey) => key?.CompareTo(typeKey) ?? 1);
        var queue = new ConcurrentPriorityQueue<ReferenceTypeKey, int>(comparer: comparer);

        var first = (Key: new ReferenceTypeKey(1), Value: 123 );
        var second = (Key: new ReferenceTypeKey(2), Value: 97 );
        
        queue.Enqueue(first.Key, first.Value);
        queue.Dequeue();
        queue.Enqueue(second.Key, second.Value);

        if (!queue.TryDequeue(out var actualKey, out var actualValue))
        {
            Assert.True(false, "В очереди должен находиться 1 элемент, т.к. добавили 2 и забрали 1");
        }

        var actual = ( actualKey, actualValue );
        var expected = second;
        
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryDequeue__КогдаКлючСылочныйТипИДостигПорогУдаленныхЭлементов__ДолженДобавлятьНовыеЭлементыНормально()
    {
        const int deleteThreshold = 10;
        var comparer = Comparer<ReferenceTypeKey>.Create((key, typeKey) => key?.CompareTo(typeKey) ?? 1);
        var queue = new ConcurrentPriorityQueue<ReferenceTypeKey, int>(deleteThreshold: deleteThreshold, comparer: comparer);

        var deletedElements = Enumerable.Range(0, deleteThreshold)
                                        .Select(key => ( Key: new ReferenceTypeKey(key), Value: Random.Shared.Next() ))
                                        .ToArray();
        
        var enqueuedElement = ( Key: new ReferenceTypeKey( deleteThreshold ), Value: Random.Shared.Next() );
        
        foreach (var (key, value) in deletedElements)
        {
            queue.Enqueue(key, value);
        }
        
        for (var i = 0; i < deletedElements.Length; i++)
        {
            queue.Dequeue();
        }
        
        queue.Enqueue(enqueuedElement.Key, enqueuedElement.Value);
        if (!queue.TryDequeue(out var actualKey, out var actualValue))
        {
            Assert.True(false, "В очереди должен находиться 1 элемент");
        }

        var actual = ( actualKey, actualValue );
        var expected = enqueuedElement;
        
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryPeek__КогдаОчередьПуста__ДолженВернутьFalse()
    {
        var queue = CreateQueue();
        Assert.False(queue.TryPeek(out _, out _));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void TryPeek__КогдаВОчередиНесколькоЭлементов__ДолженВернутьTrue(int elementsCount)
    {
        var queue = CreateQueue();
        
        foreach (var (key, value) in Enumerable.Range(0, elementsCount).Select(_ => (Key: Random.Shared.Next(), Value: Random.Shared.Next())))
        {
            queue.Enqueue(key, value);
        }
        
        Assert.True(queue.TryPeek(out _, out _));
    }

    [Fact]
    public void TryPeek__КогдаВОчередиЕстьЭлементы__НеДолженУдалятьЭлементы()
    {
        var key = 123;
        var value = 98765;
        var queue = CreateQueue();
        queue.Enqueue(key, value);

        queue.TryPeek(out _, out _);

        var actualValue = queue.Dequeue(out var actualKey);

        var actual = ( actualKey, actualValue );
        var expected = ( key, value );
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Clear__КогдаОчередьПолностьюПуста__ДолженОставитьОчередьПустой()
    {
        var queue = CreateQueue();

        queue.Clear();
        
        Assert.Empty(queue.DequeueAllInternal());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void Clear__КогдаВОчередиНесколькоУзлов__ДолженОчиститьОчередь(int elementsCount)
    {
        var elements = Enumerable.Range(0, elementsCount)
                                 .Select(_ => ( Key: Random.Shared.Next(), Value: Random.Shared.Next() ))
                                 .ToArray();

        var queue = CreateQueue();
        
        foreach (var (key, value) in elements)
        {
            queue.Enqueue(key, value);
        }

        queue.Clear();
        
        Assert.Empty(queue.DequeueAllInternal());
    }

    [Fact]
    public void Clear__КогдаВыполняютсяПараллельно__ДолжныОставитьОчередьВПравильноСостоянии()
    {
        for (int i = 0; i < ParallelTestsRepeatCount; i++)
        {
            const int operationsCount = 1000;
            var queue = CreateQueue();
            var tcs = new TaskCompletionSource();
            var waitTask = Task.WhenAll(Enumerable.Range(0, operationsCount)
                                                  .Select(_ => Task.Run(async () =>
                                                   {
                                                       await tcs.Task;
                                                       queue.Clear();
                                                   }))
                                                  .ToArray());
        
            tcs.SetResult();
            waitTask.Wait();
        
            Assert.Empty(queue.DequeueAllInternal());
        }
    }

    [Theory]
    [InlineData(100, 100, 100)]
    [InlineData(250, 100, 100)]
    [InlineData(500, 500, 500)]
    [InlineData(500, 1000, 100)]
    [InlineData(500, 100, 1000)]
    [InlineData(250, 100, 1000)]
    [InlineData(1000, 100, 1000)]
    [InlineData(1000, 1000, 1000)]
    [InlineData(1000, 500, 1000)]
    [InlineData(1000, 500, 0)]
    [InlineData(5000, 2000, 1000)]
    public async Task Clear__КогдаВыполняютсяEnqueueИDequeueПараллельно__ДолжныРаботатьКорректно(
        int clearCount,
        int dequeueCount,
        int enqueueCount)
    {
        for (int i = 0; i < ParallelTestsRepeatCount; i++)
        {

            var tcs = new TaskCompletionSource();
            var queue = CreateQueue();
            var clearTasks = Enumerable.Range(0, clearCount)
                                       .Select(_ => Task.Run(async () =>
                                        {
                                            await tcs.Task;
                                            queue.Clear();
                                        }));
            var enqueueTasks = Enumerable.Range(0, enqueueCount)
                                         .Select(_ => Task.Run(async () =>
                                          {
                                              await tcs.Task;
                                              queue.Enqueue(Random.Shared.Next(), Random.Shared.Next());
                                          }));
            var dequeueTasks = Enumerable.Range(0, dequeueCount)
                                         .Select(_ => Task.Run(async () =>
                                          {
                                              await tcs.Task;
                                              queue.TryDequeue(out _, out _);
                                          }));
            var waitTask = Task.WhenAll(
                clearTasks
                   .Concat(enqueueTasks)
                   .Concat(dequeueTasks)
                   .ToArray()
                );
        
            tcs.SetResult();
            await waitTask;

            var data = ( Key: 1, Value: 123 );
            queue.Enqueue(data.Key, data.Value);

            Assert.Contains(queue.DequeueAllInternal(), tuple => tuple == data);
        }
    }

    [Fact]
    public void GetUnorderedEntries__КогдаОчередьПуста__ДолженВернутьПустойСписок()
    {
        var queue = CreateQueue();
        Assert.Empty(queue.GetUnorderedEntries());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    public void GetUnorderedEntries__КогдаВОчередиТолькоДобавленныеЭлементы__ДолженВернутьХранившиесяЭлементы(int elementsCount)
    {
        var elements = Enumerable.Range(0, elementsCount)
                                 .Select(_ => ( Key: Random.Shared.Next(), Value: Random.Shared.Next() ))
                                 .ToArray();
        var queue = CreateQueue();
        foreach (var (key, value) in elements)
        {
            queue.Enqueue(key, value);
        }

        var actual = queue.GetUnorderedEntries().ToHashSet();
        var expected = elements.ToHashSet();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(10, 10)]
    [InlineData(1, 10)]
    [InlineData(10, 1)]
    [InlineData(20, 1)]
    [InlineData(20, 0)]
    [InlineData(1, 2)]
    public void GetUnorderedEntries__КогдаВОчередиЕстьУдаленныеИДобавленныеЭлементы__ДолженВернутьЖивыеЭлементы(
        int deletedCount,
        int enqueuedCount)
    {
        var elements = Enumerable.Range(0, enqueuedCount)
                                 .Select(_ => ( Key: Random.Shared.Next(), Value: Random.Shared.Next() ))
                                 .ToArray();
        var dequeued = new List<(int, int)>();
        var queue = CreateQueue();
        foreach (var (key, value) in elements)
        {
            queue.Enqueue(key, value);
        }

        for (var i = 0; i < deletedCount; i++)
        {
            if (queue.TryDequeue(out var key, out var value))
            {
                dequeued.Add((key, value));
            }
        }

        var actual = queue.GetUnorderedEntries().Concat(dequeued).ToHashSet();
        var expected = elements.ToHashSet();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public void EnqueueDequeue__КогдаВыполняютсяПоОчереди__ВРезультатеОчередьДолжнаБытьПуста(int iterationsCount)
    {
        var queue = CreateQueue();
        for (int i = 0; i < iterationsCount; i++)
        {
            queue.Enqueue(Random.Shared.Next(), Random.Shared.Next());
            queue.Dequeue();
        }
        
        Assert.Empty(queue.DequeueAllInternal());
    }
}