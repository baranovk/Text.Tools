extern alias SUT;
using SUT::WordHistogram;

namespace Text.Tools.Tests.Unit;

internal sealed class WordClusteringTests
{
    [Test]
    public void GrouppingAlgorithm_Should_BuildWordClusters()
    {
        const int maxLevenshteinDistance = 2;
        var words = new string[] { "время", "всех" };

        var clusters = words.GroupByLevenshteinDistance(maxLevenshteinDistance).ToList();
        Assert.That(clusters, Has.Count.EqualTo(2));
    }
}
