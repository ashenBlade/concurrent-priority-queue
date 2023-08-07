namespace ConcurrentPriorityQueue.Benchmarks.KeyArrangement;

public class AscendingStrategy: IKeyArrangementStrategy
{
    private int _key;
    public AscendingStrategy(int key = 0) => _key = key;

    public int GetKey() => Interlocked.Increment(ref _key);

    public KeyArrangementType ArrangementType => KeyArrangementType.Ascending;
}