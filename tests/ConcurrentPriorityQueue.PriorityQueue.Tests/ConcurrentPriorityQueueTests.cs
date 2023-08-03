using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Xunit.Sdk;

namespace ConcurrentPriorityQueue.PriorityQueue.Tests;

public class ConcurrentPriorityQueueTests
{
    public const int DefaultHeight = 10;

    private static ConcurrentPriorityQueue<int, int> CreateQueue() =>
        new ConcurrentPriorityQueue<int, int>(DefaultHeight);

    [Fact]
    public void Enqueue__КогдаОчередьПуста__ДолженДобавить1Элемент()
    {
        var queue = CreateQueue();
        
        var key = 1;
        var value = 123;
        
        queue.Enqueue(key, value);

        var stored = queue.GetStoredData();
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

        var data = queue.GetStoredData();
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

        var actual = queue.GetStoredData();
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

        var actual = queue.GetStoredData();
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
    public void Enqueue__КогдаОдновременноДобавляетсяМножествоЭлементов__ДолженДобавитьВсеЭлементы(int elementsCount)
    {
        var queue = CreateQueue();
        var elements = Enumerable.Range(0, elementsCount)
                                 .Select(i => ( Priority: Random.Shared.Next(), Data: Random.Shared.Next() ))
                                 .ToArray();
        var tasks = elements
                   .Select(pair => Task.Run(() =>
                    {
                        queue.Enqueue(pair.Priority, pair.Data);
                    }))
                   .ToArray();
        Task.WaitAll(tasks);

        var actual = queue.GetStoredData().ToHashSet();
        var expected = elements.ToHashSet();
        Assert.Equal(expected, actual);
    }
}