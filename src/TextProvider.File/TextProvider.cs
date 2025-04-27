using TextAnalysis.Interfaces;

namespace TextProvider.File;

public class TextProvider(string filesDirectoryPath) : ITextProvider
{
    private readonly string _filesDirectoryPath = filesDirectoryPath;

    public IEnumerable<Func<CancellationToken, Task<string>>> PrepareTextSources()
        => Directory.EnumerateFiles(_filesDirectoryPath)
                    .Select(filePath => new Func<CancellationToken, Task<string>>(
                        async (ct) =>
                        {
                            using var fs = new FileStream(filePath, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read });
                            using var sr = new StreamReader(fs);
                            return await sr.ReadToEndAsync(ct).ConfigureAwait(false);
                        }
                    ));
}
