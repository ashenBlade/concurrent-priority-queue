namespace ConcurrentPriorityQueue.Benchmarks.KeyArrangement;

public class UniformStrategy: IKeyArrangementStrategy
{
    public int GetKey() => Random.Shared.Next();

    public KeyArrangementType ArrangementType => KeyArrangementType.Uniform;
}