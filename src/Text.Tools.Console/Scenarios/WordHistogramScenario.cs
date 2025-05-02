using Functional;
using TextAnalysis.Presentators.Histogram;
using UserInterface.Console.Generic;
using UserInterface.Console.Generic.Scenarios;
using WordHistogram;
using static Functional.F;

namespace Text.Tools.Console.Scenarios;

internal sealed class WordHistogramScenario : StatefullInteractionScenario<WordHistogramScenarioState>
{
    private string? _filesPath;
    private string? _histogramPath;

    public WordHistogramScenario() : base(WordHistogramScenarioState.GetFilesForAnalysisPath)
    {
    }

    public override async Task<Context> Execute(Context context)
        => State switch
        {
            WordHistogramScenarioState.GetFilesForAnalysisPath => await GetFileSystemPath(context, Resources.EnterPathToFilesForAnalysis,
                    (ctx, path) => ctx with { CurrentScenario = SetFilesForAnalysisPath(path).SetState(WordHistogramScenarioState.GetHistogramPath) }
                ).ConfigureAwait(false),
            WordHistogramScenarioState.GetHistogramPath => await GetFileSystemPath(context, Resources.EnterPathToHistogram,
                    (ctx, path) => ctx with { CurrentScenario = SetHistogramPath(path).SetState(WordHistogramScenarioState.BuildHistogram) }
                ).ConfigureAwait(false),
            WordHistogramScenarioState.BuildHistogram => await BuildHistogram(context).ConfigureAwait(false),
            _ => throw new InvalidOperationException()
        };

    private static async Task<Context> GetFileSystemPath(Context context, string prompt, Func<Context, string, Context> onValidFileSystemPathEntered)
        => await (await context.UI
                    .WriteEmpty()
                    .WriteMessage(prompt)
                    .Pipe(_ => AwaitInput(context.UI, input => ValidateFileSystemPath(input), Resources.EnterPath)
                                .Bind(input => Async(Exceptional(input)))
                    )
                    .ConfigureAwait(false)
            ).Match(
                ex => Async(HandleError(context, ex)),
                input => input switch
                {
                    TextInput txtInput => txtInput.IsQuitKey()
                                            ? Async(context with
                                            {
                                                CurrentScenario = new MainInteractionScenario()
                                            })
                                            : Async(onValidFileSystemPathEntered(context, txtInput.Value)),
                    _ => throw new InvalidOperationException()
                }
            ).ConfigureAwait(false);

    private Task<Context> BuildHistogram(Context context)
        => WordsHistogramBuilder
                .BuildAsync(new TextProvider.File.TextProvider(_filesPath!), System.Console.WriteLine)
                .Bind(async histogramBuckets =>
                {
                    using var fs = new FileStream(_histogramPath!, FileMode.CreateNew);
                    await new TextTableHistogramPresentator(500).VisualizeTo(fs, histogramBuckets.ToList()).ConfigureAwait(false);
                    context.UI.WriteEmpty().WriteMessage(Resources.HistogramGenerated);
                    return context with { CurrentScenario = new MainInteractionScenario() };
                });

    private static Task<Validation<UserInput>> ValidateFileSystemPath(UserInput input)
        => input switch
        {
            TextInput iText => iText.IsQuitKey()
                                ? Async(Valid<UserInput>(iText))
                                : Directory.Exists(iText.Value)
                                    ? Async(Valid<UserInput>(iText))
                                    : Async(Invalid<UserInput>(Resources.InvalidDirectoryPath)),
            _ => Async(Validation<UserInput>.Fail(new ValidationError(Resources.InvalidDirectoryPath)))
        };

    private static Context HandleError(Context context, Exception ex)
        => context.UI.WriteEmpty().WriteMessage(ex.Message).Pipe(_ => context with { CurrentScenario = new MainInteractionScenario() });

    private WordHistogramScenario SetFilesForAnalysisPath(string path)
    {
        _filesPath = path;
        return this;
    }

    private WordHistogramScenario SetHistogramPath(string path)
    {
        _histogramPath = path;
        return this;
    }
}

internal enum WordHistogramScenarioState { GetFilesForAnalysisPath, GetHistogramPath, BuildHistogram }
