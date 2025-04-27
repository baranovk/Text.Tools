namespace TextAnalysis.Interfaces;

public interface IHistogramPresentator
{
    Task Visualize(IEnumerable<HistogramBucket> buckets, CancellationToken cancellationToken);
}

public record HistogramBucket(IReadOnlyList<string> Keys, int Size);
