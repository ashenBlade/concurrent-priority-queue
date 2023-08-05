using System.Buffers;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ConcurrentPriorityQueue.PriorityQueue.Tests")]

namespace ConcurrentPriorityQueue.PriorityQueue;

public class ConcurrentPriorityQueue<TKey, TValue>
{
    /// <summary>
    /// Максимальное количество логически удаленных узлов по умолчанию
    /// </summary>
    /// <remarks>
    /// Взял из головы
    /// </remarks>
    private const int DefaultDeleteThreshold = 10;

    /// <summary>
    /// Максимальная высота списка по умолчанию
    /// </summary>
    /// <remarks>
    /// Взял из головы
    /// </remarks>
    private const int DefaultHeight = 20;

    /// <summary>
    /// Получить текущее количество элементов в очереди.
    /// </summary>
    /// <remarks>
    /// При каждом вызове делается новое вычисление,
    /// вместо постоянного обновления значения через <see cref="Interlocked"/>
    /// </remarks>
    public int Count => GetAliveNodesCount();

    private int GetAliveNodesCount()
    {
        var node = _head.Successors[0];
        var count = 0;
        while (!IsTail(node))
        {
            if (!node.Deleted)
            {
                count++;
            }
            node = node.Successors[0];
        }

        return count;
    }

    private readonly SkipListNode<TKey, TValue> _head;
    private readonly SkipListNode<TKey, TValue> _tail;
    
    /// <summary>
    /// Максимальная высота списка
    /// </summary>
    private readonly int _height;
    
    /// <summary>
    /// <see cref="Random"/> для вычисления высоты списка
    /// </summary>
    private readonly Random _random = Random.Shared;
    
    /// <summary>
    /// Сравнитель ключей
    /// </summary>
    private readonly IComparer<TKey> _comparer;

    /// <summary>
    /// Максимальное количество хранимых логически удаленных узлов
    /// </summary>
    private readonly int _deleteThreshold;
    
    
    public ConcurrentPriorityQueue(int height = DefaultHeight, int deleteThreshold = DefaultDeleteThreshold, IComparer<TKey>? comparer = null)
    {
        if (height < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Высота списка должна быть положительной");
        }

        if (deleteThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deleteThreshold), deleteThreshold,
                "Предел хранящихся удаленных элементов не может быть отрицательным");
        }

        if (deleteThreshold < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(deleteThreshold), deleteThreshold,
                "Предел хранящихся удаленных элементов должен быть не меньше 2");
        }
        
        ( _head, _tail ) = CreateHeadAndTail(height);
        _height = height;
        _deleteThreshold = deleteThreshold;
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
        // Текущая голова списка
        var currentHead = _head.Successors[0];
        
        // Запоминаем старую голову списка, чтобы избежать гонки при удалении старых узлов
        var observedHead = currentHead;
        
        // Количество пройденных удаленных узлов
        var deletedCount = 0;
        
        // Здесь храним новую голову списка 
        var newHead = ( SkipListNode<TKey, TValue>? ) null;
        
        bool taken;
        while (true)
        {
            if (IsTail(currentHead))
            {
                key = default!;
                value = default!;
                return false;
            }

            if (currentHead.Inserting && 
                newHead is null)
            {
                newHead = currentHead;
            }

            if (currentHead.Deleted)
            {
                deletedCount++;
                currentHead = currentHead.Successors[0];
                continue;
            }

            taken = false;
            currentHead.UpdateLock.Enter(ref taken);
            try
            {
                if (!currentHead.Deleted)
                {
                    currentHead.Deleted = true;
                    deletedCount++;
                    break;
                }
            }
            finally
            {
                if (taken) { currentHead.UpdateLock.Exit(); }
            }
            currentHead = currentHead.Successors[0];
            deletedCount++;
        }
        // На этом моменте, в currentHead хранится узел, который мы удалили

        if (deletedCount < _deleteThreshold)
        {
            key = currentHead.Key;
            value = currentHead.Value;
            return true;
        }

        // На данный момент, если newHead не null, то содержит узел, который был в процесссе вставки в момент обхода.
        newHead ??= currentHead;

        taken = false;
        var updated = false;
        _head.UpdateLock.Enter(ref taken);
        try
        {
            if (_head.Successors[0] == observedHead)
            {
                _head.Successors[0] = newHead;
                updated = true;
            }
        }
        finally
        {
            if (taken) { _head.UpdateLock.Exit(); }
        }

        if (updated)
        {
            Restructure();
        }

        key = currentHead.Key;
        value = currentHead.Value;
        return true;
    }

    private void Restructure()
    {
        var i = _height - 1;
        var pred = _head;
        while (i > 0)
        {
            // Запоминаем старую голову списка, чтобы при обновлении не задеть новую
            var savedHead = _head.Successors[i];
            var next = pred.Successors[i];
            if (!savedHead.Deleted)
            {
                i--;
                continue;
            }

            while (next.Deleted)
            {
                pred = next;
                next = pred.Successors[i];
            }

            var taken = false;
            _head.UpdateLock.Enter(ref taken);
            try
            {
                if (_head.Successors[i] == savedHead)
                {
                    // В голову списка нужно класть только удаленные узлы, так как в противном случае
                    // перед живым узлом могут вставить узел с меньшим ключом, но мы его потеряем
                    _head.Successors[i] = pred.Successors[i];
                    i--;
                }
            }
            finally
            {
                if (taken) { _head.UpdateLock.Exit(); }
            }
        }
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
        // 1. Находим место, куда нужно вставить элемент
        var (predecessors, successors, lastDeleted) = GetInsertLocation(key);
        try
        {

            // 2. Аллоцируем память, под узел
            var height = _random.Next(1, _height);
            var node = new SkipListNode<TKey, TValue>()
            {
                Key = key, 
                Value = value,
                Successors = new SkipListNode<TKey, TValue>[height],
                Inserting = true
            };
        
            // 3. Пытаемся вставить в список на 1 уровне.
            //    Эта операция аналогична добавлению узла в сам список
            var taken = false;
            while (true)
            {
                node.Successors[0] = successors[0];
                var pred = predecessors[0];
            
                taken = false;
                pred.UpdateLock.Enter(ref taken);
                try
                {
                    if (pred.Successors[0] == successors[0] 
                     && !successors[0].Deleted)
                    {
                        pred.Successors[0] = node;
                        break;
                    }
                }
                finally
                {
                    if (taken) { pred.UpdateLock.Exit(); }
                }
            
                ArrayPool<SkipListNode<TKey, TValue>>.Shared.Return(predecessors);
                ArrayPool<SkipListNode<TKey, TValue>>.Shared.Return(successors);

                // Заново рассчитываем предшественников и последователей
                ( predecessors, successors, lastDeleted ) = GetInsertLocation(key);
            }

            // 4. Постепенно наращиваем высоту вставляемого узла
            var i = 1;
            while (i < height)
            {
                if (node.Deleted || 
                    successors[i].Deleted || 
                    successors[i] == lastDeleted)
                {
                    // Узел был удален в процессе вставки
                    break;
                }

                node.Successors[i] = successors[i];
            
                taken = false;
                predecessors[i].UpdateLock.Enter(ref taken);
                try
                {
                    if (predecessors[i].Successors[i] == successors[i])
                    {
                        predecessors[i].Successors[i] = node;
                    }
                    else
                    {
                        // Что делать с дубликатами ключей подумаю потом
                        break;
                    }
                }
                finally
                {
                    if (taken) { predecessors[i].UpdateLock.Exit(); }
                }

                i++;
            }

            node.Inserting = false;
        }
        finally
        {
            ArrayPool<SkipListNode<TKey, TValue>>.Shared.Return(successors);
            ArrayPool<SkipListNode<TKey, TValue>>.Shared.Return(predecessors);
        }
    }

    // Для заданного ключа получить список всех ближайших левых (ключ меньше) узлов
    private (SkipListNode<TKey, TValue>[] Predecessors, SkipListNode<TKey, TValue>[] Successors, SkipListNode<TKey, TValue>? LastDeleted) GetInsertLocation(TKey key)
    {
        // P.S. лучше заменить на пулинг 
        var pred = _head;
        // Последний удаленный узел
        var lastDeleted = ( SkipListNode<TKey, TValue>? ) null;
        // Предшествующие узлы
        var pool = ArrayPool<SkipListNode<TKey, TValue>>.Shared;
        var predecessors = pool.Rent(_height);
        // Последующие узлы
        var successors = pool.Rent(_height);
        try
        {
            var i = _height - 1;
            while (0 <= i)
            {
                var current = pred.Successors[i];
                while (!IsTail(current) && 
                       (
                           IsLessOrEqualThan(current.Key, key)
                        || current.Deleted
                        || ( pred.Deleted && i == 0 ) )
                      )
                {
                    if (pred.Deleted && i == 0)
                    {
                        lastDeleted = current;
                    }

                    pred = current;
                    current = pred.Successors[i];
                }

                predecessors[i] = pred;
                successors[i] = current;
                i--;
            }

            return ( predecessors, successors, lastDeleted );
        }
        catch (Exception)
        {
            pool.Return(predecessors);
            pool.Return(successors);
            throw;
        }
    }

    private bool IsTail(SkipListNode<TKey, TValue> node) => node == _tail;
    private bool IsHead(SkipListNode<TKey, TValue> node) => node == _head;
    
    /// <summary>
    /// left &lt;= right
    /// </summary>
    private bool IsLessOrEqualThan(TKey left, TKey right)
    {
        return _comparer.Compare(left, right) <= 0;
    }

    // Для тестов
    internal IReadOnlyList<(TKey Key, TValue Value)> DequeueAll()
    {
        var result = new List<(TKey, TValue)>();

        while (TryDequeue(out var key, out var value))
        {
            result.Add(( key, value ));
        }

        return result;
    }

    internal int GetStoredNodesCountRaw()
    {
        var node = _head.Successors[0];
        var count = 0;
        while (!IsTail(node))
        {
            count++;
            node = node.Successors[0];
        }
        
        return count;
    }
}