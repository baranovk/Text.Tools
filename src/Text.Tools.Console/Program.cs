using Text.Tools.Console.Scenarios;
using UserInterface.Console.Generic;

namespace Text.Tools.Console;

internal static class Program
{
    static async Task Main() => await UserInterfaceRunner.RunAsync(new MainInteractionScenario()).ConfigureAwait(false);
}
