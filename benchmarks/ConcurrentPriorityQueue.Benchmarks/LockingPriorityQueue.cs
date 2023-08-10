namespace ConcurrentPriorityQueue.Benchmarks;

public class LockingPriorityQueue<TKey, TValue>
{
    private readonly PriorityQueue<TValue, TKey> _queue = new();
    private SpinLock _lock = new();

    public void Enqueue(TKey key, TValue value)
    {
        lock (_queue)
        {
            _queue.Enqueue(value, key);
        }
    }

    public bool TryDequeue(out TKey key, out TValue value)
    {
        lock (_queue)
        {
            return _queue.TryDequeue(out value, out key);
        }
    }

    public void Clear()
    {
        lock (_queue)
        {
            _queue.Clear();
        }
    }

    public bool TryPeek(out TKey key, out TValue value)
    {
        lock (_queue)
        {
            return _queue.TryPeek(out value, out key);
        }
    }
}