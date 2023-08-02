using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;

[assembly: InternalsVisibleTo("ConcurrentPriorityQueue.PriorityQueue.Tests")]

namespace ConcurrentPriorityQueue.PriorityQueue;

// Этапы:
// 1. Очередь с приоритетами на SkipList
//  1.1 Enqueue
//  1.2 Dequeue
//  1.3 Count

// 2. Конкурентная очередь
//  2.1 Enqueue
//  2.2 Dequeue
//  2.3 Count

public class ConcurrentPriorityQueue<TKey, TValue>
{
    public int Count { get; private set; }
    private readonly SkipListNode<TKey, TValue> _head;
    private readonly SkipListNode<TKey, TValue> _tail;
    private readonly int _height;
    private readonly Random _random = Random.Shared;
    private readonly IComparer<TKey> _comparer;
    
    public ConcurrentPriorityQueue(int height, IComparer<TKey>? comparer = null)
    {
        if (height < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Высота списка должна быть положительной");
        }

        ( _head, _tail ) = CreateHeadAndTail(height);
        _height = height;
        _comparer = comparer ?? Comparer<TKey>.Default;
    }

    private static (SkipListNode<TKey, TValue> Head, SkipListNode<TKey, TValue> Tail) CreateHeadAndTail(int height)
    {
        var headSuccessors = new SkipListNode<TKey, TValue>[height];
        var tail = new SkipListNode<TKey, TValue>();
        Array.Fill(headSuccessors, tail);
        var head = new SkipListNode<TKey, TValue>() {Successors = headSuccessors};
        return ( head, tail );
    }

    public bool TryDequeue(out TKey key, out TValue value)
    {
        var first = _head.Successors[0];
        
        if (IsTail(first))
        {
            // Список пуст, возвращаем false
            key = default!;
            value = default!;
            return false;
        }
        
        // Обновляем ссылки на следующие элементы в голове списка
        for (int level = first.Successors.Length - 1; level >= 0; level--)
        {
            _head.Successors[level] = first.Successors[level];
        }

        key = first.Key;
        value = first.Value;
        return true;
    }

    public TValue Dequeue() => Dequeue(out _);

    public TValue Dequeue(out TKey key)
    {
        if (TryDequeue(out key, out var value))
        {
            return value;
        }
        
        throw new InvalidOperationException("Очередь пуста");
    }

    public void Enqueue(TKey key, TValue value)
    {
        // 1. Найти место, куда нужно вставить элемент
        var predecessors = GetPredecessors(key);
        
        // 2. Обновить список
        var height = _random.Next(1, _height);
        var successors = new SkipListNode<TKey, TValue>[height];
        var node = new SkipListNode<TKey, TValue>()
        {
            Key = key, 
            Value = value,
            Successors = successors
        };
        for (int level = height - 1; level >= 0; level--)
        {
            // Новый узел на текущем уровне будет указывать на узел, на который указывал старый узел
            // Было:
            //    | left | -> | right |
            // 
            // Стало:
            //    | left |              ->  | right |
            //                |  new  | ->  | right | 
            successors[level] = predecessors[level].Successors[level];
            
            // Обновляем ссылку у left, чтобы указывал на новый узел
            // Было:
            //    | left |              ->  | right |
            //                |  new  | ->  | right |
            // 
            // Стало:
            //    | left | -> |  new  | ->  | right |
            predecessors[level].Successors[level] = node;
        }
    }

    // Для заданного ключа получить список всех ближайших левых (ключ меньше) узлов
    private SkipListNode<TKey, TValue>[] GetPredecessors(TKey key)
    {
        // P.S. лучше заменить на пулинг 
        
        // Элементы слева
        var predecessors = new SkipListNode<TKey, TValue>[_height];
        // Начинаем перебирать все узлы начиная слева сверху (самый верхний уровень головы)
        var left = _head;
        for (int height = _height - 1; height >= 0; height--)
        {
            // Находим первый узел, ключ которого больше требуемого
            var next = left.Successors[height];
            while (!( IsTail( next ) || IsLessThan(key, next.Key) )) 
            {
                left = next;
                next = next.Successors[height];
            }
            

            predecessors[height] = left;
        }

        return predecessors;
    }

    private bool IsTail(SkipListNode<TKey, TValue> node) => node == _tail;
    private bool IsHead(SkipListNode<TKey, TValue> node) => node == _head;
    
    /// <summary>
    /// left &lt; right
    /// </summary>
    private bool IsLessThan(TKey left, TKey right)
    {
        return _comparer.Compare(left, right) < 0;
    }

    // Для тестов
    internal IReadOnlyList<(TKey Key, TValue Value)> GetStoredData()
    {
        var result = new List<(TKey, TValue)>();
        var element = _head.Successors[0];
        while (!IsTail(element))
        {
            result.Add((element.Key, element.Value));
            element = element.Successors[0];
        }

        return result;
    }
}