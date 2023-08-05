using System.CommandLine;
using Spectre.Console;

namespace Furnace.Cli.Command;

public class ListCommand : CliCommand
{
    public override void Register(RootCommand rootCommand)
    {
        var listCommand = new System.CommandLine.Command("list", "List installed modrinth packs");
        listCommand.SetHandler(ListPacks, GlobalOptions.DebugOutputOption);
    }
    
    private static void ListPacks(bool verbose)
    {
        var packs = LaunchCommand.GetAllInstalledPacksAndDirectories();
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
}