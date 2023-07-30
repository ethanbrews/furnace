using Furnace.Lib.Logging;
using Furnace.Lib.Modrinth;
using Furnace.Modrinth.Data.SearchQueryResult;
using Spectre.Console;

namespace Furnace.Cli.Command;

public static class MiscModrinthCommand
{
    public static async Task SearchPacksAsync(string? query, bool promptToInstall, bool verbose)
    {
        query ??= AnsiConsole.Ask<string>("pack search query:");
        var hits = await new PackSearchTask(query).RunAsync(CancellationToken.None);
        if (promptToInstall)
            await AskAndInstallPack(hits, verbose);
        else
            PrintHits(hits, verbose);
    }

    private static async Task AskAndInstallPack(IEnumerable<Hit> hits, bool verbose)
    {
        Logger.RegisterHandler(new ConsoleLoggingHandler(verbose ? LoggingLevel.Debug : LoggingLevel.Info));
        var selectedHit = AskForHit(hits);
        var installTask = PackInstallTask.InstallLatest(Program.RootDirectory, selectedHit.ProjectId);
        await installTask.RunAsync(CancellationToken.None);
    }
    
    private static void PrintHits(IEnumerable<Hit> hits, bool verbose)
    {
        foreach (var hit in hits)
        {
            if (verbose)
                AnsiConsole.WriteLine($"{hit.ProjectId}:{hit.Slug}:{hit.LatestVersion}:{hit.Author}");
            else
                AnsiConsole.MarkupLine($"[green]{hit.ProjectId}[/] {hit.Title}");
        }
        Console.WriteLine();
    }

    private static Hit AskForHit(IEnumerable<Hit> hits)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<Hit>
                {
                    Converter = hit => $"{hit.ProjectId} : {hit.Title}"
                }
                .Title("Select a pack")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .AddChoices(hits));
    }
}