using UserInterface.Console.Generic;

namespace Text.Tools.Console.Scenarios;

internal sealed class MainInteractionScenario : InteractionScenario
{
    public MainInteractionScenario()
        : base(new Interaction("1", "Гистограмма частоты слов", new WordHistogramScenario()))
    {
    }
}
