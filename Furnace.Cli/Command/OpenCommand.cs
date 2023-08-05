using System.CommandLine;
using System.Diagnostics;

namespace Furnace.Cli.Command;

public class OpenCommand : CliCommand
{
    public override void Register(RootCommand rootCommand)
    {
        var openFolderCommand = new System.CommandLine.Command("open", "Open the folder containing the given pack files.");
        openFolderCommand.AddArgument(GlobalOptions.PackIdArgument);
        openFolderCommand.AddOption(GlobalOptions.NoInputOption);
        openFolderCommand.SetHandler(OpenFolder, GlobalOptions.PackIdArgument, GlobalOptions.NoInputOption);
        rootCommand.AddCommand(openFolderCommand);
    }
    
    private static void OpenFolder(string? name, bool noInput)
    {
        var d = Program.RootDirectory;
        if (string.IsNullOrEmpty(name) && !noInput)
        {
            var packId = LaunchCommand.AskForPackId("Select pack");
            d = (name == ".") ? d : d.EnumerateDirectories().FirstOrDefault(x => x.Name == packId);
            ArgumentNullException.ThrowIfNull(d, GlobalOptions.NoInputOption.Name);
        }

        Process.Start("explorer.exe", d.FullName);
    }
}