using System.CommandLine;
using Furnace.Lib.Logging;
using Furnace.Lib.Minecraft.Data;
using Furnace.Lib.Modrinth;
using Furnace.Lib.Modrinth.Data;
using Furnace.Lib.Utility.Extension;
using Furnace.Minecraft.Data;
using Spectre.Console;

namespace Furnace.Cli.Command;

public class LaunchCommand : ICommand
{

    public static List<Tuple<DirectoryInfo, Modrinth.Data.PackIndex.PackIndex>> GetAllInstalledPacksAndDirectories()
    {
        var rootDir = Program.RootDirectory.CreateSubdirectory("Instances");
        
        return rootDir.EnumerateDirectories()
            .Where(dir =>
                dir.GetFiles().FirstOrDefault(f => f.Name == "modrinth.index.json") != null
            ).Select(async directory =>
            {
                await using var stream = directory.GetFileInfo("modrinth.index.json").OpenRead();
                using var reader = new StreamReader(stream);
                var text = await reader.ReadToEndAsync();
                return Tuple.Create(directory, Modrinth.Data.PackIndex.PackIndex.FromJson(text));
            }).Select(t => t.Result).ToList();
    }
    
    public static string AskForPackId(string prompt)
    {
        var indexDirectoryPairs = GetAllInstalledPacksAndDirectories();
        
        var selectedName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .AddChoices(indexDirectoryPairs.Select(x => x.Item2.Name)));

        return indexDirectoryPairs.First(x => x.Item2.Name == selectedName).Item1.Name;
    }

    private static async Task LaunchAsync(string? packId, bool verbose, bool createScript, bool serverSide)
    {
        packId ??= AskForPackId("Which pack should be launched?");

        var launcher = new PackLaunchTask(packId, Program.RootDirectory,
            serverSide ? GameInstallType.Server : GameInstallType.Client,
            createScript ? PackLaunchAction.GenerateScript : PackLaunchAction.Launch);
        
        if (verbose)
            Logger.RegisterHandler(new ConsoleLoggingHandler(LoggingLevel.Debug));

        await launcher.RunAsync(CancellationToken.None);
    }

    public void Register(RootCommand rootCommand)
    {
        var launchCommand = new System.CommandLine.Command("launch", "Launch a modrinth pack.");
        var scriptOnlyOption =
            new Option<bool>("--create-script", () => false, "Save the launch script and do not execute it.");
        launchCommand.AddArgument(GlobalOptions.PackIdArgument);
        launchCommand.AddOption(scriptOnlyOption);
        launchCommand.AddOption(GlobalOptions.ServerOption);
        launchCommand.SetHandler(LaunchAsync, GlobalOptions.PackIdArgument, GlobalOptions.DebugOutputOption, scriptOnlyOption, GlobalOptions.ServerOption);
        rootCommand.AddCommand(launchCommand);
    }
}