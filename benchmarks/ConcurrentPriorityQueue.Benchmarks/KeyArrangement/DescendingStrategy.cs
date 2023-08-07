namespace ConcurrentPriorityQueue.Benchmarks.KeyArrangement;

public class DescendingStrategy: IKeyArrangementStrategy
{
    private int _key;

    public DescendingStrategy(int key = 0) => _key = key;

    public int GetKey() => Interlocked.Decrement(ref _key);

    public KeyArrangementType ArrangementType => KeyArrangementType.Descending;
}