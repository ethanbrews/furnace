using Furnace.Log;
using Furnace.Minecraft.Data;
using Furnace.Utility.Extension;
using Spectre.Console;

namespace Furnace.Cli.Command;

public static class LaunchPack
{

    private static List<Tuple<DirectoryInfo, Modrinth.Data.PackIndex.PackIndex>> GetAllInstalledPacksAndDirectories()
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
                .MoreChoicesText("[grey](Move up and down to reveal more packs)[/]")
                .AddChoices(indexDirectoryPairs.Select(x => x.Item2.Name)));

        return indexDirectoryPairs.First(x => x.Item2.Name == selectedName).Item1.Name;
    }

    public static void ListPacks(bool verbose)
    {
        var packs = GetAllInstalledPacksAndDirectories();
        var table = new Table();

        table.AddColumn("Id");
        table.AddColumn("Name");
        table.AddColumn("Version");

        if (verbose)
            table.AddColumn("Minecraft");
        
        foreach (var pack in packs)
        {
            if (verbose)
            {
                
                table.AddRow(pack.Item1.Name, pack.Item2.Name, pack.Item2.VersionId, pack.Item2.Game);
            }
            else
            {
                table.AddRow(pack.Item1.Name, pack.Item2.Name, pack.Item2.VersionId);
            }
        }
        
        AnsiConsole.Write(table);
    }
    
    public static async Task LaunchAsync(string? packId, bool verbose, bool createScript, bool serverSide)
    {
        packId ??= AskForPackId("Which pack should be launched?");

        var launcher = new Modrinth.PackLaunchTask(packId, Program.RootDirectory,
            serverSide ? GameInstallType.Server : GameInstallType.Client);
        
        if (verbose)
            Logger.RegisterHandler(new ConsoleLoggingHandler(LoggingLevel.Debug));

        await launcher.RunAsync(CancellationToken.None);
    }
}