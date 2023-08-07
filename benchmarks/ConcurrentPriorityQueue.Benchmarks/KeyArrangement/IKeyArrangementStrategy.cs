namespace ConcurrentPriorityQueue.Benchmarks.KeyArrangement;

public interface IKeyArrangementStrategy
{
    public int GetKey();
    public KeyArrangementType ArrangementType { get; }
}