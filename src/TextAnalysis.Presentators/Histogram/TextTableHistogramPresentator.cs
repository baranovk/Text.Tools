using System.Globalization;
using System.Text;
using Functional;
using TextAnalysis.Interfaces;

namespace TextAnalysis.Presentators.Histogram;

public class TextTableHistogramPresentator(int rowWidthInChars) : IHistogramPresentator
{
    public async Task VisualizeTo(Stream output, IReadOnlyCollection<HistogramBucket> buckets, CancellationToken cancellationToken)
    {
        if (0 == buckets.Count) { return; }

        await buckets.Aggregate(-1, (maxWordLength, bucket) =>
        {
            foreach (var keyLength in bucket.Keys.Where(k => k.Length > maxWordLength).Select(k => k.Length))
            {
                if (keyLength > maxWordLength) { maxWordLength = keyLength; }
            }

            return maxWordLength;
        })
        .Pipe(async maxWordLength =>
        {
            var separator = new string(Enumerable.Repeat('-', rowWidthInChars).ToArray());

            await output.WriteAsync(
                Encoding.UTF8.GetBytes(
                    buckets.Aggregate(
                        new StringBuilder(),
                        (sb, bucket) =>
                        {
                            var size = bucket.Size.ToString(CultureInfo.CurrentCulture);
                            var sizeLength = size.Length;
                            if (maxWordLength + 1 + sizeLength > rowWidthInChars) { throw new InvalidOperationException("Row width is too narrow"); }

                            foreach (var key in bucket.Keys)
                            {
                                sb.Append(CultureInfo.CurrentCulture, $"{key}");
                                sb.Append(' ', rowWidthInChars - key.Length - size.Length);
                                sb.Append(size.PadLeft(sizeLength));
                                size = string.Empty;
                            }

                            sb.Append(separator);
                            return sb;
                        }
                    )
                    .ToString()
                ),
                cancellationToken
            ).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
}
