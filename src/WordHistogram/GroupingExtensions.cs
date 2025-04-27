using Functional;

namespace WordHistogram;

internal static class GroupingExtensions
{
    public static IEnumerable<WordCluster> GroupByLevenshteinDistance(this IEnumerable<string> words, int maxDistance)
            => new ComparerByLevenshteinDistance(maxDistance)
                    .Pipe(comparer => words
                                        .Select(s => new ClusterItem<string>(s))
                                        .OrderBy(s => s, comparer)
                                        .GroupBy(i => i.ClusterKey)
                                        .Select(g => new WordCluster(g.Select(i => i.Value).ToList(), g.Count())));

    public static IEnumerable<WordCluster> GroupBySimilarity(this IEnumerable<WordCluster> clusters, int maxLevenshteinDistance)
        => new ComparerBySimilarity(maxLevenshteinDistance)
                .Pipe(comparer => clusters
                                    .Select(c => new ClusterItem<WordCluster>(c))
                                    .OrderBy(s => s, comparer)
                                    .GroupBy(i => i.ClusterKey)
                                    .Select(g => new WordCluster(g.SelectMany(_ => _.Value.Words).ToList(), g.Sum(_ => _.Value.ClusterSize))));

    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(target)) { return source.Length; }
        if (string.IsNullOrEmpty(source)) { return string.IsNullOrEmpty(target) ? 0 : target.Length; }

        if (source.Length > target.Length)
        {
            (source, target) = (target, source);
        }

        int m = target.Length, n = source.Length;
        var distance = new int[2][] { new int[m + 1], new int[m + 1] };

        // Initialize the distance 'matrix'
        for (var j = 1; j <= m; j++) { distance[0][j] = j; }

        var currentRow = 0;

        for (var i = 1; i <= n; ++i)
        {
            currentRow = i & 1;
            distance[currentRow][0] = i;
            var previousRow = currentRow ^ 1;

            for (var j = 1; j <= m; j++)
            {
                var cost = (target[j - 1] == source[i - 1] ? 0 : 1);

                distance[currentRow][j] = Math.Min(
                    Math.Min(distance[previousRow][j] + 1, distance[currentRow][j - 1] + 1),
                    distance[previousRow][j - 1] + cost
                );
            }
        }

        return distance[currentRow][m];
    }

    private static void MergeClusterItems<T>(ClusterItem<T> x, ClusterItem<T> y)
    {
        if (Guid.Empty == x.ClusterKey && Guid.Empty == y.ClusterKey) { x.ClusterKey = y.ClusterKey = Guid.NewGuid(); }
        else if (Guid.Empty != x.ClusterKey) { y.ClusterKey = x.ClusterKey; }
        else { x.ClusterKey = y.ClusterKey; }
    }

    private sealed class ComparerBySimilarity(int maxDistance) : IComparer<ClusterItem<WordCluster>>
    {
        private readonly int _maxDistance = maxDistance;

        public int Compare(ClusterItem<WordCluster>? x, ClusterItem<WordCluster>? y)
        {
            if (null == x && null == y) { return 0; }
            if (null != x && null == y) { return -1; }
            if (null == x) { return 1; }
            if (0 < x.Value.Words.Count && 0 == y!.Value.Words.Count) { return -1; }
            if (0 == x.Value.Words.Count && 0 < y!.Value.Words.Count) { return 1; }

            if (x.Value.Words.Concat(y!.Value.Words).GroupBy(_ => _).Any(g => g.Count() > 1))
            {
                MergeClusterItems(x, y);
                return 0;
            }

            foreach (var xw in x.Value.Words)
            {
                foreach (var yw in y!.Value.Words)
                {
                    if (_maxDistance >= CalculateLevenshteinDistance(xw, yw))
                    {
                        MergeClusterItems(x, y);
                        return 0;
                    }
                }
            }

            return string.Compare(x.Value.Words[0], y!.Value!.Words[0], StringComparison.CurrentCultureIgnoreCase);
        }
    }

    private sealed class ComparerByLevenshteinDistance(int maxDistance) : IComparer<ClusterItem<string>>
    {
        private readonly int _maxDistance = maxDistance;

        public int Compare(ClusterItem<string>? x, ClusterItem<string>? y)
        {
            if (null == x && null == y) { return 0; }
            if (null != x && null == y) { return -1; }
            if (null == x) { return 1; }

            if (x.Value == y!.Value || _maxDistance >= CalculateLevenshteinDistance(x.Value, y.Value))
            {
                MergeClusterItems(x, y);
                return 0;
            }

            return string.Compare(x.Value, y!.Value, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    private sealed class ClusterItem<T>(T value)
    {
        public T Value { get; private set; } = value;

        public Guid ClusterKey { get; set; }
    }
}

internal record WordCluster(IReadOnlyList<string> Words, int ClusterSize);
