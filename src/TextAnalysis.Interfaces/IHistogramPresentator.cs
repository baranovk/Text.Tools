namespace TextAnalysis.Interfaces;

public interface IHistogramPresentator
{
    Task VisualizeTo(Stream output, IReadOnlyCollection<HistogramBucket> buckets, CancellationToken cancellationToken);
}

public record HistogramBucket(IReadOnlyList<string> Keys, int Size);
