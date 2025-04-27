extern alias SUT;
using SUT::WordHistogram;

namespace Text.Tools.Tests.Unit;

internal sealed class StringTests
{
    [Test]
    public void Strings_Should_BeGrouppedByLevenshteinDistance()
    {
        const int maxDistance = 1;
        var firstCluster = new List<string>() { "AAA", "AAB", "AAC" };
        var secondCluster = new List<string>() { "CCC", "CCA" };

        var clusters = firstCluster
                        .Union(secondCluster)
                        .GroupByLevenshteinDistance(maxDistance)
                        .ToList();

        Assert.That(clusters.Count, Is.EqualTo(2));
        Assert.That(clusters.Count(x => firstCluster.Count == x.Words.Count), Is.EqualTo(1));
        Assert.That(clusters.Count(x => secondCluster.Count == x.Words.Count), Is.EqualTo(1));
    }
}
