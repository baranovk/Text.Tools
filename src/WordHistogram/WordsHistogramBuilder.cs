using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Functional;
using TextAnalysis.Interfaces;
using static Functional.F;
using Unit = System.ValueTuple;

namespace WordHistogram;

public static class WordsHistogramBuilder
{
    private static readonly Regex _wordRegex = new(@"^[A-ZА-Яa-zа-я\-]{3,}$", RegexOptions.Compiled);

    public static async Task<IEnumerable<HistogramBucket>> BuildAsync(
        ITextProvider textProvider,
        Action<string> log,
        int maxLevenshteinDistance = 2,
        CancellationToken cancellationToken = default)
    {
        ConcurrentBag<IEnumerable<WordCluster>> wordClusterBag = [];

        await Parallel.ForEachAsync(
            textProvider.PrepareTextSources(),
            new ParallelOptions { CancellationToken = cancellationToken },
            async (source, ct) =>
            {
                await TryAsync(() => source(ct))
                    .RunAsync()
                    .Bind(textEx
                            => textEx.Match(
                                ex =>
                                {
                                    log(ex.ToString());
                                    return Async<Unit>(new());
                                },
                                text =>
                                {
                                    wordClusterBag.Add(
                                        text.Split(' ')
                                            .Select(s => s.Trim('.', ',', ':', '(', ')').ToLower(CultureInfo.CurrentCulture))
                                            .Where(s => _wordRegex.IsMatch(s))
                                            .GroupByLevenshteinDistance(maxLevenshteinDistance)
                                    );

                                    return Async<Unit>(new());
                                }
                            )
                    ).ConfigureAwait(false);
            }
        ).ConfigureAwait(false);

        return wordClusterBag
            .SelectMany(_ => _)
            .GroupBySimilarity(maxLevenshteinDistance)
            .OrderByDescending(wc => wc.ClusterSize)
            .Select(wc => new HistogramBucket(wc.Words, wc.ClusterSize))
            .ToList();
    }
}
