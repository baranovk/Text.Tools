namespace TextAnalysis.Interfaces;

public interface ITextProvider
{
    IEnumerable<Func<CancellationToken, Task<string>>> PrepareTextSources();
}
