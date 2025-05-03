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
            WordHistogramScenarioState.GetFilesForAnalysisPath => await TryAsync(
                () => GetFileSystemPath(context, Resources.EnterPathToFilesForAnalysis,
                    (ctx, path) => ctx with { CurrentScenario = SetFilesForAnalysisPath(path).SetState(WordHistogramScenarioState.GetHistogramPath) }
                ))
                .RunAsync()
                .Bind(exceptional => exceptional.Match(ex => Async(HandleError(context, ex)), ctx => Async(ctx)))
                .ConfigureAwait(false),

            WordHistogramScenarioState.GetHistogramPath => await TryAsync(
                () => GetFileSystemPath(context, Resources.EnterPathToHistogram,
                    (ctx, path) => ctx with { CurrentScenario = SetHistogramPath(path).SetState(WordHistogramScenarioState.BuildHistogram) }
                ))
                .RunAsync()
                .Bind(exceptional => exceptional.Match(ex => Async(HandleError(context, ex)), ctx => Async(ctx)))
                .ConfigureAwait(false),

            WordHistogramScenarioState.BuildHistogram => await BuildHistogram(context).ConfigureAwait(false),
            _ => throw new InvalidOperationException()
        };

    private async Task<Context> GetFileSystemPath(Context context, string prompt, Func<Context, string, Context> onValidFileSystemPathEntered)
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
                                            ? Async(OnFinish(context)) : Async(onValidFileSystemPathEntered(context, txtInput.Value)),
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
                    return OnFinish(context);
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

    private Context HandleError(Context context, Exception ex)
        => context.UI.WriteEmpty()
                     .WriteMessage(ex.Message)
                     .Pipe(_ => OnFinish(context));

    private Context OnFinish(Context context)
        => SetState(WordHistogramScenarioState.GetFilesForAnalysisPath)
            .Pipe(_ => context with { CurrentScenario = new MainInteractionScenario() });

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
