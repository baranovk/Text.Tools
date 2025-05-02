namespace TextAnalysis.Interfaces;

public interface IHistogramPresentator
{
    Task VisualizeTo(Stream output, IReadOnlyCollection<HistogramBucket> buckets, CancellationToken cancellationToken = default);
}

public record HistogramBucket(IReadOnlyList<string> Keys, int Size);
