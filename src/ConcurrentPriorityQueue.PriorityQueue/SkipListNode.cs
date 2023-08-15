namespace ConcurrentPriorityQueue.PriorityQueue;

internal class SkipListNode<TKey, TValue>
{
    /// <summary>
    /// Ключ
    /// </summary>
    public TKey Key = default!;
    
    /// <summary>
    /// Значение
    /// </summary>
    public TValue Value = default!;

    /// <summary>
    /// Массив указателей на другие узлы в списке.
    /// Каждый индекс соответствует своему уровню, начиная снизу.
    /// </summary>
    public SkipListNode<TKey, TValue>[] Successors = null!;

    /// <summary>
    /// Узел логически удален
    /// </summary>
    public volatile bool NextDeleted;
    
    /// <summary>
    /// Узел находится в процессе вставки
    /// </summary>
    public volatile bool Inserting;

    /// <summary>
    /// Блокировка на время обновления узла
    /// </summary>
    public SpinLock UpdateLock = new();
}